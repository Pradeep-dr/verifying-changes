using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PatientPortalLite.Web.Data;

namespace PatientPortalLite.Web.Controllers;

[Route("letters")]
public class LettersController(PatientPortalDbContext db) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var patientId = Guid.Parse(User.FindFirstValue("patient_id")!);
        var letters = await db.Letters
                              .Where(l => l.PatientId == patientId)
                              .OrderByDescending(l => l.SentOn)
                              .ToListAsync();
        return View(letters);
    }

    [HttpGet("{letterId:guid}")]
    public async Task<IActionResult> Details(Guid letterId)
    {
        var patientId = Guid.Parse(User.FindFirstValue("patient_id")!);
        var letter = await db.Letters
                             .FirstOrDefaultAsync(l => l.Id == letterId && l.PatientId == patientId);
        if (letter is null) return NotFound();

        if (!letter.IsRead)
        {
            letter.IsRead = true;
            await db.SaveChangesAsync();
        }

        return View(letter);
    }
}
