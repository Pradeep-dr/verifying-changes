using Microsoft.AspNetCore.Mvc;

namespace MarsLite.Web.Controllers;

[Route("appointments")]
public class AppointmentsController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();

    [HttpGet("{appointmentId:guid}")]
    public IActionResult Details(Guid appointmentId)
    {
        ViewBag.AppointmentId = appointmentId;
        return View();
    }
}
