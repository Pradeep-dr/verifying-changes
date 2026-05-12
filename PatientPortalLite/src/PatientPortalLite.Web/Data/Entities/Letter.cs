namespace PatientPortalLite.Web.Data.Entities;

public class Letter
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public DateOnly SentOn { get; set; }
    public string Subject { get; set; } = "";
    public string Sender { get; set; } = "";
    public string Body { get; set; } = "";
    public bool IsRead { get; set; }

    public Patient? Patient { get; set; }
}
