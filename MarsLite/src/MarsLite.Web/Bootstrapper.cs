using System.Collections.Generic;
using MarsLite.Web.Auth;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Conventions;
using Nancy.TinyIoc;
using Nancy.ViewEngines.Razor;

namespace MarsLite.Web
{
    /// <summary>
    /// Nancy bootstrapper — equivalent to the Castle Windsor bootstrapper in real Mars.
    /// Uses Nancy's built-in TinyIoC container for simplicity in the lite POC.
    /// </summary>
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);

            // Authentication pipeline — populates Context.CurrentUser from a signed cookie
            // before any module runs. Mirrors how real Mars resolves user identity from
            // the Auth0 session cookie.
            CookieAuth.Enable(pipelines);

            // CSRF protection — issues a cookie + view-token pair; POST handlers that
            // accept it must call `this.ValidateCsrfToken()`.
            Nancy.Security.Csrf.Enable(pipelines);
        }

        protected override void ConfigureConventions(NancyConventions conventions)
        {
            base.ConfigureConventions(conventions);

            // Serve /Content and /Scripts directly from disk (in addition to the
            // OWIN static-file middleware in Startup.cs — belt and braces).
            conventions.StaticContentsConventions.Add(
                StaticContentConventionBuilder.AddDirectory("Content", "Content"));
            conventions.StaticContentsConventions.Add(
                StaticContentConventionBuilder.AddDirectory("Scripts", "Scripts"));
        }
    }

    /// <summary>
    /// Hooks Razor up to MarsLite. Adds the namespaces every view needs so we don't have
    /// to repeat them in each .cshtml header.
    /// </summary>
    public class RazorConfig : IRazorConfiguration
    {
        public bool AutoIncludeModelNamespace => true;
        public bool GetDisableAutoIncludeModelNamespace() => false;

        public IEnumerable<string> GetAssemblyNames()
        {
            yield return "MarsLite.Web";
        }

        public IEnumerable<string> GetDefaultNamespaces()
        {
            yield return "MarsLite.Web";
            yield return "MarsLite.Web.Auth";
            yield return "MarsLite.Web.Data";
            yield return "MarsLite.Web.Modules";
        }
    }
}
