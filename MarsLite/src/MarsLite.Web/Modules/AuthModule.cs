using MarsLite.Web.Auth;
using MarsLite.Web.Data;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;

namespace MarsLite.Web.Modules
{
    /// <summary>
    /// /auth/login and /auth/logout. In real Mars these would redirect to / from Auth0.
    /// For the lite POC we serve a local Razor login form and validate against SQLite.
    /// </summary>
    public class AuthModule : NancyModule
    {
        public AuthModule() : base("/auth")
        {
            Get["/login"] = _ =>
            {
                // If the user already has a valid session, don't show the login form —
                // send them on to the safe return URL (or the home page).
                // Cast the dynamic Query value to string so SafeReturnUrl's return type
                // isn't inferred as dynamic (which would defeat the extension method below).
                string raw = Request.Query.returnUrl;
                var landing = SafeReturnUrl(raw);

                if (Context.CurrentUser != null)
                {
                    return Response.AsRedirect(landing);
                }

                return View["Auth/Login", new LoginViewModel { ReturnUrl = landing }];
            };

            Post["/login"] = _ =>
            {
                this.ValidateCsrfToken();

                var input  = this.Bind<LoginRequest>();
                var safe   = SafeReturnUrl(input.ReturnUrl);

                var user = MarsLiteDb.FindUserByUsername(input.Username ?? string.Empty);
                if (user == null || !PasswordHasher.Verify(input.Password ?? string.Empty, user.PasswordHash))
                {
                    // Don't leak whether the username exists — same generic error either way.
                    return View["Auth/Login", new LoginViewModel
                    {
                        ReturnUrl = safe,
                        Error     = "Invalid username or password.",
                    }];
                }

                return CookieAuth.SignIn(user, safe);
            };

            Post["/logout"] = _ =>
            {
                this.ValidateCsrfToken();
                return CookieAuth.SignOut();
            };
        }

        /// <summary>
        /// Accepts only local (same-origin) paths. Prevents an attacker from crafting
        /// <c>/auth/login?returnUrl=https://evil.com</c> to phish users after sign-in.
        /// </summary>
        internal static string SafeReturnUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return "/";
            // Must start with "/" — but reject "//", "/\", and "/\\" which can target other hosts
            if (url[0] != '/') return "/";
            if (url.Length > 1 && (url[1] == '/' || url[1] == '\\')) return "/";
            return url;
        }
    }

    public class LoginRequest
    {
        public string Username  { get; set; }
        public string Password  { get; set; }
        public string ReturnUrl { get; set; }
    }

    public class LoginViewModel
    {
        public string ReturnUrl { get; set; }
        public string Error     { get; set; }
    }
}
