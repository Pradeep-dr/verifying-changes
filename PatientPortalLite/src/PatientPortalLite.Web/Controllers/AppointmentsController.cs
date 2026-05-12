using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PatientPortalLite.Web.Data;

namespace PatientPortalLite.Web.Controllers;

[Route("appointments")]
public class AppointmentsController(PatientPortalDbContext db) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var patientId = Guid.Parse(User.FindFirstValue("patient_id")!);
        var appointments = await db.Appointments
                                   .Where(a => a.PatientId == patientId)
                                   .OrderByDescending(a => a.ScheduledAt)
                                   .ToListAsync();
        return View(appointments);
    }

    [HttpGet("{appointmentId:guid}")]
    public async Task<IActionResult> Details(Guid appointmentId)
    {
        var patientId = Guid.Parse(User.FindFirstValue("patient_id")!);
        var appointment = await db.Appointments
                                  .FirstOrDefaultAsync(a => a.Id == appointmentId && a.PatientId == patientId);
        if (appointment is null) return NotFound();
        return View(appointment);
    }
}
