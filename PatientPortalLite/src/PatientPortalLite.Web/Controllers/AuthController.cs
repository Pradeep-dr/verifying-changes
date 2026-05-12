using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PatientPortalLite.Web.Data;
using PatientPortalLite.Web.Models;
using System.Security.Claims;

namespace PatientPortalLite.Web.Controllers;

[Route("auth")]
public class AuthController(PatientPortalDbContext db) : Controller
{
    // ── Step 1: Demographics ──────────────────────────────────────────────
    [AllowAnonymous]
    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = null)
    {
        var safe = SafeReturnUrl(returnUrl);

        // Already signed in? Skip the form and go to the safe landing URL.
        if (User.Identity?.IsAuthenticated == true)
            return LocalRedirect(safe);

        // ASP.NET Core auto-populates ModelState from the query string. Without this
        // clear, `asp-for="ReturnUrl"` would render the raw (unsanitised) query value
        // — re-introducing the open-redirect we just defended against.
        ModelState.Clear();
        return View(new LoginVm { ReturnUrl = safe });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm vm)
    {
        var safe = SafeReturnUrl(vm.ReturnUrl);

        // If a user is already signed in, don't let a stale tab re-trigger demographics
        // entry — just bounce them to the landing URL.
        if (User.Identity?.IsAuthenticated == true)
            return LocalRedirect(safe);

        if (!ModelState.IsValid)
        {
            vm.ReturnUrl = safe;
            return View(vm);
        }

        var match = await db.Patients.FirstOrDefaultAsync(p =>
            p.LastName == vm.LastName &&
            p.DateOfBirth == vm.DateOfBirth &&
            p.Postcode.Replace(" ", "") == vm.Postcode!.Replace(" ", ""));

        if (match is null)
        {
            // Generic error — never reveal which field didn't match.
            vm.Error     = "We couldn't find a matching record. Please check your details.";
            vm.ReturnUrl = safe;
            return View(vm);
        }

        // Demographics confirmed → store *sanitised* return URL and move to OTC step.
        HttpContext.Session.SetString("pending_patient_id", match.Id.ToString());
        HttpContext.Session.SetString("return_url", safe);
        return RedirectToAction(nameof(VerifyCode));
    }

    // ── Step 2: One-time code ─────────────────────────────────────────────
    [AllowAnonymous]
    [HttpGet("verify")]
    public IActionResult VerifyCode()
    {
        // Already signed in? Skip OTC entirely.
        if (User.Identity?.IsAuthenticated == true)
            return LocalRedirect(SafeReturnUrl(HttpContext.Session.GetString("return_url")));

        // Can't land here without first completing the demographics step.
        if (HttpContext.Session.GetString("pending_patient_id") is null)
            return RedirectToAction(nameof(Login));

        return View(new VerifyCodeVm());
    }

    [AllowAnonymous]
    [HttpPost("verify")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyCode(VerifyCodeVm vm)
    {
        if (User.Identity?.IsAuthenticated == true)
            return LocalRedirect(SafeReturnUrl(HttpContext.Session.GetString("return_url")));

        var pendingId = HttpContext.Session.GetString("pending_patient_id");
        if (pendingId is null) return RedirectToAction(nameof(Login));

        if (!ModelState.IsValid) return View(vm);

        // Demo OTC: 123456 always works (in real flow this would be SMS/email-delivered)
        if (vm.Code != "123456")
        {
            vm.Error = "Invalid code. Use 123456 for this demo.";
            return View(vm);
        }

        var patient = await db.Patients.FindAsync(Guid.Parse(pendingId));
        if (patient is null) return RedirectToAction(nameof(Login));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, patient.Id.ToString()),
            new Claim(ClaimTypes.Name,           $"{patient.FirstName} {patient.LastName}"),
            new Claim("patient_id",              patient.Id.ToString()),
            new Claim("amr",                     "demographics"),
            new Claim("amr",                     "one_time_code"),
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                                      new ClaimsPrincipal(identity));

        var landing = SafeReturnUrl(HttpContext.Session.GetString("return_url"));
        HttpContext.Session.Clear();
        return LocalRedirect(landing);
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    /// <summary>
    /// Accept only same-origin paths to prevent open-redirect attacks via crafted
    /// <c>?returnUrl=</c> values (e.g. <c>https://evil.com</c>, <c>//evil.com</c>,
    /// or <c>/\evil.com</c>). Falls back to <c>/</c>.
    /// </summary>
    private string SafeReturnUrl(string? url) =>
        !string.IsNullOrEmpty(url) && Url.IsLocalUrl(url) ? url : "/";
}
