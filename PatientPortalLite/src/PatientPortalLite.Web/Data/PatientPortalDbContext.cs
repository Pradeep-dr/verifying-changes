using Microsoft.EntityFrameworkCore;
using PatientPortalLite.Web.Data.Entities;

namespace PatientPortalLite.Web.Data;

public class PatientPortalDbContext(DbContextOptions<PatientPortalDbContext> options) : DbContext(options)
{
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Letter> Letters => Set<Letter>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Patient>().HasIndex(p => p.NhsNumber).IsUnique();
        mb.Entity<Appointment>().HasOne(a => a.Patient).WithMany(p => p.Appointments).HasForeignKey(a => a.PatientId);
        mb.Entity<Letter>().HasOne(l => l.Patient).WithMany(p => p.Letters).HasForeignKey(l => l.PatientId);
    }
}
