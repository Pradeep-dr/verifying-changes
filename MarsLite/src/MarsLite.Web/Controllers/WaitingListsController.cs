using Microsoft.AspNetCore.Mvc;

namespace MarsLite.Web.Controllers;

[Route("waitinglists")]
public class WaitingListsController : Controller
{
    [HttpGet("{providerId:int}")]
    public IActionResult Index(int providerId)
    {
        ViewBag.ProviderId = providerId;
        return View();
    }

    [HttpGet("{providerId:int}/config")]
    public IActionResult Config(int providerId)
    {
        ViewBag.ProviderId = providerId;
        return View();
    }

    [HttpGet("{providerId:int}/entries")]
    public IActionResult Entries(int providerId)
    {
        ViewBag.ProviderId = providerId;
        return View();
    }

    [HttpGet("{providerId:int}/patientlists")]
    public IActionResult PatientLists(int providerId)
    {
        ViewBag.ProviderId = providerId;
        return View();
    }
}
