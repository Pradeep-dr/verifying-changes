namespace PatientPortalLite.Web.Data.Entities;

public class Patient
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public DateOnly DateOfBirth { get; set; }
    public string Postcode { get; set; } = "";
    public string MobileNumber { get; set; } = "";
    public string NhsNumber { get; set; } = "";

    public List<Appointment> Appointments { get; set; } = new();
    public List<Letter> Letters { get; set; } = new();
}
