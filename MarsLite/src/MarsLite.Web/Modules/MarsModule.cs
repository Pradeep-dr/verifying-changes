using MarsLite.Web.Auth;
using Nancy;
using Nancy.Responses;

namespace MarsLite.Web.Modules
{
    /// <summary>
    /// Base class for every secured Nancy module. Direct analog of <c>MarsModule</c>
    /// (and the secure-base modules) in real Mars. Modules that inherit from this
    /// will redirect anonymous users to <c>/auth/login</c>.
    /// </summary>
    public abstract class MarsModule : NancyModule
    {
        protected MarsModule() : base() { ApplyAuth(); }
        protected MarsModule(string modulePath) : base(modulePath) { ApplyAuth(); }

        private void ApplyAuth()
        {
            Before.AddItemToStartOfPipeline(ctx =>
            {
                if (ctx.CurrentUser == null)
                {
                    return new RedirectResponse(
                        "/auth/login?returnUrl=" + System.Uri.EscapeDataString(ctx.Request.Path));
                }
                return null;
            });
        }

        /// <summary>The signed-in staff user (or <c>null</c> if anonymous).</summary>
        protected UserIdentity CurrentStaff => Context.CurrentUser as UserIdentity;
    }
}
