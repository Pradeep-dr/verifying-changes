using System;
using System.IO;
using MarsLite.Web.Data;
using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Nancy.Owin;
using Owin;

[assembly: OwinStartup(typeof(MarsLite.Web.Startup))]

namespace MarsLite.Web
{
    /// <summary>
    /// OWIN pipeline configuration. Equivalent to the OWIN <c>Startup</c> in real Mars —
    /// wires up static files and then hands every other request to the Nancy bootstrapper.
    /// </summary>
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Initialise the SQLite database on startup (creates schema + seed data)
            MarsLiteDb.Initialise();

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // Static file roots: /Content and /Scripts behave like real Mars
            app.UseStaticFiles(new StaticFileOptions
            {
                RequestPath = new PathString("/Content"),
                FileSystem  = new PhysicalFileSystem(Path.Combine(baseDir, "Content")),
                ServeUnknownFileTypes = true,
            });
            app.UseStaticFiles(new StaticFileOptions
            {
                RequestPath = new PathString("/Scripts"),
                FileSystem  = new PhysicalFileSystem(Path.Combine(baseDir, "Scripts")),
                ServeUnknownFileTypes = true,
            });

            // Hand everything else to Nancy
            app.UseNancy();
        }
    }
}
