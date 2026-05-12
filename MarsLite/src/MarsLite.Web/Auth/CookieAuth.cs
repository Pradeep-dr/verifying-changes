using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using MarsLite.Web.Data;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Cookies;
using Nancy.Responses;

namespace MarsLite.Web.Auth
{
    /// <summary>
    /// Minimal cookie authentication for MarsLite. Signs a small payload with HMAC-SHA256
    /// so the cookie can't be tampered with. This mirrors the *behaviour* of the Auth0
    /// session cookie in real Mars without needing an external identity provider.
    /// </summary>
    public static class CookieAuth
    {
        public const string CookieName = "marslite.auth";

        // In a real app this would come from configuration / a secret store.
        private static readonly byte[] SigningKey =
            Encoding.UTF8.GetBytes("marslite-poc-signing-key-change-me-in-prod");

        /// <summary>Plugs the auth check into the Nancy request pipeline.</summary>
        public static void Enable(IPipelines pipelines)
        {
            pipelines.BeforeRequest.AddItemToStartOfPipeline(ctx =>
            {
                if (ctx.Request.Cookies.TryGetValue(CookieName, out var cookie))
                {
                    var user = TryDecode(cookie);
                    if (user != null) ctx.CurrentUser = user;
                }
                return null;
            });
        }

        /// <summary>Issues an auth cookie for the given staff user.</summary>
        public static Response SignIn(StaffUser user, string returnUrl)
        {
            var payload = $"{user.Id}|{user.Username}|{user.DisplayName}|{user.Role}";
            var token   = Encode(payload);

            var resp = new RedirectResponse(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
            resp.WithCookie(new NancyCookie(CookieName, token, httpOnly: true)
            {
                Path    = "/",
                Expires = DateTime.UtcNow.AddHours(8),
            });
            return resp;
        }

        /// <summary>Clears the auth cookie.</summary>
        public static Response SignOut()
        {
            var resp = new RedirectResponse("/auth/login");
            resp.WithCookie(new NancyCookie(CookieName, string.Empty, httpOnly: true)
            {
                Path    = "/",
                Expires = DateTime.UtcNow.AddYears(-1),
            });
            return resp;
        }

        // ── Cookie format: base64url(payload) "." base64url(hmac) ─────────────

        private static string Encode(string payload)
        {
            var payloadBytes = Encoding.UTF8.GetBytes(payload);
            var sig          = ComputeHmac(payloadBytes);
            return B64(payloadBytes) + "." + B64(sig);
        }

        private static UserIdentity TryDecode(string cookieValue)
        {
            try
            {
                var parts = cookieValue.Split('.');
                if (parts.Length != 2) return null;

                var payloadBytes = FromB64(parts[0]);
                var sig          = FromB64(parts[1]);
                var expected     = ComputeHmac(payloadBytes);

                if (!ConstantTimeEquals(sig, expected)) return null;

                var pieces = Encoding.UTF8.GetString(payloadBytes).Split('|');
                if (pieces.Length != 4) return null;

                return new UserIdentity
                {
                    UserId      = int.Parse(pieces[0]),
                    UserName    = pieces[1],
                    DisplayName = pieces[2],
                    Role        = pieces[3],
                    Claims      = new List<string> { pieces[3] },
                };
            }
            catch
            {
                return null;
            }
        }

        private static byte[] ComputeHmac(byte[] payload)
        {
            using (var h = new HMACSHA256(SigningKey)) return h.ComputeHash(payload);
        }

        private static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            var diff = 0;
            for (var i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }

        private static string B64(byte[] b) =>
            Convert.ToBase64String(b).Replace('+', '-').Replace('/', '_').TrimEnd('=');

        private static byte[] FromB64(string s)
        {
            s = s.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4) { case 2: s += "=="; break; case 3: s += "="; break; }
            return Convert.FromBase64String(s);
        }
    }
}
