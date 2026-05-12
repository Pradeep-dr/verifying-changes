namespace PatientPortalLite.Web.Data.Entities;

public class Appointment
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string Clinician { get; set; } = "";
    public string Specialty { get; set; } = "";
    public string Location { get; set; } = "";
    public string Status { get; set; } = "Confirmed"; // Confirmed, Pending, Cancelled, Completed
    public string Notes { get; set; } = "";

    public Patient? Patient { get; set; }
}
