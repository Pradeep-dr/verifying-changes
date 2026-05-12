using System;
using Microsoft.Owin.Hosting;

namespace MarsLite.Web
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var url = Environment.GetEnvironmentVariable("MARSLITE_URL") ?? "http://localhost:5185";

            using (WebApp.Start<Startup>(url))
            {
                Console.WriteLine();
                Console.WriteLine("┌─────────────────────────────────────────────┐");
                Console.WriteLine("│  MarsLite (Nancy / .NET Framework 4.8)      │");
                Console.WriteLine("│  Listening on " + url.PadRight(31) + "│");
                Console.WriteLine("│  Demo: staff@drdoctor.dev / password123     │");
                Console.WriteLine("│  Press Ctrl+C to stop                        │");
                Console.WriteLine("└─────────────────────────────────────────────┘");

                // Keep the host alive
                var quit = new System.Threading.ManualResetEvent(false);
                Console.CancelKeyPress += (s, e) => { e.Cancel = true; quit.Set(); };
                quit.WaitOne();
            }
        }
    }
}
