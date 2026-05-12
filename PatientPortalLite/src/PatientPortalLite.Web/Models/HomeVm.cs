using PatientPortalLite.Web.Data.Entities;

namespace PatientPortalLite.Web.Models;

public class HomeVm
{
    public string FirstName { get; set; } = "";
    public List<Appointment> UpcomingAppointments { get; set; } = new();
    public int UnreadLetters { get; set; }
    public int TotalAppointments { get; set; }
    public int TotalLetters { get; set; }
}
