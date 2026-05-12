using System;
using MarsLite.Web.Data;
using Nancy;

namespace MarsLite.Web.Modules
{
    /// <summary>
    /// Direct analog of real Mars's <c>WaitingListsModule.cs</c>.
    /// <para>
    /// The shell route <c>/waitinglists/{providerId:int}</c> and its catch-all sibling
    /// <c>/waitinglists/{providerId:int}/{uri*}</c> both return the same Razor view —
    /// the view boots an AngularJS app that handles <c>/config</c>, <c>/entries</c>,
    /// <c>/patientlists</c> client-side via HTML5-mode routing. JSON endpoints below
    /// (<c>/data/config</c>, <c>/entries</c>, etc.) feed that Angular app.
    /// </para>
    /// </summary>
    public class WaitingListsModule : MarsModule
    {
        public WaitingListsModule() : base("/waitinglists")
        {
            Get["/"] = _ => Response.AsRedirect("/");

            Get["/{providerId:int}"] =
            Get["/{providerId:int}/{uri*}"] = parameters =>
            {
                int providerId = parameters.providerId;
                var provider   = MarsLiteDb.FindProvider(providerId);
                if (provider == null) return HttpStatusCode.NotFound;

                return View["WaitingLists/Index", new WaitingListsShellVm
                {
                    ProviderId   = providerId,
                    ProviderName = provider.Name,
                    AngularBase  = "/waitinglists/" + providerId + "/",
                }];
            };

            // ── JSON endpoints consumed by the AngularJS app ───────────────────

            Get["/{providerId:int}/data/lists"] = parameters =>
            {
                int providerId = parameters.providerId;
                return Response.AsJson(MarsLiteDb.GetWaitingListsForProvider(providerId));
            };

            Get["/{providerId:int}/data/config"] = parameters =>
            {
                int providerId = parameters.providerId;
                return Response.AsJson(MarsLiteDb.GetFirstListForProvider(providerId));
            };

            Get["/{providerId:int}/data/entries"] = parameters =>
            {
                int providerId = parameters.providerId;
                var today      = DateTime.Today;
                var entries    = MarsLiteDb.GetEntriesForProvider(providerId);
                return Response.AsJson(new
                {
                    today = today.ToString("yyyy-MM-dd"),
                    entries,
                });
            };
        }
    }

    public class WaitingListsShellVm
    {
        public int    ProviderId   { get; set; }
        public string ProviderName { get; set; }
        public string AngularBase  { get; set; }
    }
}
