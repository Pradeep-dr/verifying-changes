using System;
using Nancy;

namespace MarsLite.Web.Modules
{
    /// <summary>
    /// Stub Appointments module — mirrors the routing shape only, no real backing data.
    /// </summary>
    public class AppointmentsModule : MarsModule
    {
        public AppointmentsModule() : base("/appointments")
        {
            Get["/"] = _ => View["Appointments/Index"];

            Get["/{appointmentId:guid}"] = parameters =>
            {
                ViewBag.AppointmentId = (Guid)parameters.appointmentId;
                return View["Appointments/Details"];
            };
        }
    }
}
