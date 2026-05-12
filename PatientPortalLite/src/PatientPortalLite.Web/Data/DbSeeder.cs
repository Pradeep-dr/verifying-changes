using Microsoft.EntityFrameworkCore;
using PatientPortalLite.Web.Data.Entities;

namespace PatientPortalLite.Web.Data;

public static class DbSeeder
{
    // Stable test patient (same GUID used in PatientPortal SpecFlow tests for familiarity)
    public static readonly Guid TestPatientId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public static async Task SeedAsync(PatientPortalDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        if (!await db.Patients.AnyAsync())
        {
            db.Patients.Add(new Patient
            {
                Id           = TestPatientId,
                FirstName    = "Sarah",
                LastName     = "Johnson",
                DateOfBirth  = new DateOnly(1985, 4, 12),
                Postcode     = "SW1A 1AA",
                MobileNumber = "07700 900123",
                NhsNumber    = "999 111 0001",
            });
        }

        if (!await db.Appointments.AnyAsync())
        {
            var now = DateTime.Today.AddHours(9);
            db.Appointments.AddRange(
                new Appointment { PatientId = TestPatientId, ScheduledAt = now.AddDays(7),  Clinician = "Dr. Michael Chen",   Specialty = "Cardiology",        Location = "St Mary's — Clinic 3A",     Status = "Confirmed", Notes = "Follow-up consultation" },
                new Appointment { PatientId = TestPatientId, ScheduledAt = now.AddDays(21), Clinician = "Ms. Emma Williams",  Specialty = "Orthopaedics",      Location = "Royal London — Outpatients", Status = "Confirmed", Notes = "Knee assessment" },
                new Appointment { PatientId = TestPatientId, ScheduledAt = now.AddDays(-14),Clinician = "Dr. James Patel",    Specialty = "General Surgery",   Location = "St Mary's — Clinic 1",      Status = "Completed", Notes = "Post-op review" },
                new Appointment { PatientId = TestPatientId, ScheduledAt = now.AddDays(45), Clinician = "Dr. Lisa Thompson",  Specialty = "Endocrinology",     Location = "Manchester General — A-Wing",Status = "Pending",   Notes = "Annual check" });
        }

        if (!await db.Letters.AnyAsync())
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            db.Letters.AddRange(
                new Letter { PatientId = TestPatientId, SentOn = today.AddDays(-2),  Subject = "Appointment confirmation",   Sender = "St Mary's Hospital",    IsRead = false, Body = "Your appointment has been confirmed for…" },
                new Letter { PatientId = TestPatientId, SentOn = today.AddDays(-10), Subject = "Pre-assessment instructions",Sender = "Cardiology Department", IsRead = true,  Body = "Please follow the instructions below before your appointment…" },
                new Letter { PatientId = TestPatientId, SentOn = today.AddDays(-30), Subject = "Discharge summary",          Sender = "General Surgery",       IsRead = true,  Body = "Following your recent procedure…" });
        }

        await db.SaveChangesAsync();
    }
}
