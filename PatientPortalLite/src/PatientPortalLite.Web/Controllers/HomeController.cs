using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PatientPortalLite.Web.Data;
using PatientPortalLite.Web.Models;

namespace PatientPortalLite.Web.Controllers;

public class HomeController(PatientPortalDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var patientId = Guid.Parse(User.FindFirstValue("patient_id")!);
        var patient   = await db.Patients
                                .Include(p => p.Appointments)
                                .Include(p => p.Letters)
                                .FirstAsync(p => p.Id == patientId);

        var now = DateTime.Now;
        var upcoming = patient.Appointments
                              .Where(a => a.ScheduledAt >= now)
                              .OrderBy(a => a.ScheduledAt)
                              .Take(2)
                              .ToList();

        return View(new HomeVm
        {
            FirstName            = patient.FirstName,
            UpcomingAppointments = upcoming,
            UnreadLetters        = patient.Letters.Count(l => !l.IsRead),
            TotalAppointments    = patient.Appointments.Count,
            TotalLetters         = patient.Letters.Count,
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
