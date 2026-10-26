namespace ClinicIntelligente.Infrastructure.Persistence.Contexts;

using ClinicIntelligente.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Doctor> Doctors { get; set; }
    public DbSet<LabTechnicien> LabTechniciens { get; set; }
    public DbSet<Specialty> Specialties { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<Consultation> Consultations { get; set; }
    public DbSet<Prescription> Prescriptions { get; set; }
    public DbSet<LabOrder> LabOrders { get; set; }
    public DbSet<LabResult> LabResults { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<ChatMessage>  ChatMessages  { get; set; }
    public DbSet<DoctorReview> DoctorReviews { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ─────────────── USER INHERITANCE (TPT) ───────────────
        builder.Entity<User>().ToTable("Users");
        builder.Entity<Patient>().ToTable("Patients");
        builder.Entity<Doctor>().ToTable("Doctors");
        builder.Entity<LabTechnicien>().ToTable("LabTechniciens");

        // ─────────────── USER ───────────────
        builder.Entity<User>(e =>
        {
            e.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            e.Property(u => u.LastName).IsRequired().HasMaxLength(100);
            e.Property(u => u.Email).IsRequired().HasMaxLength(200);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.PasswordHash).IsRequired();
        });

        // ─────────────── PATIENT ───────────────
        builder.Entity<Patient>(e =>
        {
            e.Property(p => p.Cin).IsRequired().HasMaxLength(20);
            e.HasIndex(p => p.Cin).IsUnique();
        });

        // ─────────────── DOCTOR ───────────────
        builder.Entity<Doctor>(e =>
        {
            e.Property(d => d.LicenseNumber).HasMaxLength(50);
        });

        // ─────────────── APPOINTMENT ───────────────
        builder.Entity<Appointment>(e =>
        {
            e.HasOne(a => a.Patient)
             .WithMany(p => p.Appointments)
             .HasForeignKey(a => a.PatientId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(a => a.Doctor)
             .WithMany(d => d.Appointments)
             .HasForeignKey(a => a.DoctorId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
             
        });

        // ─────────────── CONSULTATION (1:1) ───────────────
        builder.Entity<Consultation>(e =>
        {
            e.HasOne(c => c.Appointment)
             .WithOne(a => a.Consultation)
             .HasForeignKey<Consultation>(c => c.AppointmentId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ─────────────── PRESCRIPTION ───────────────
        builder.Entity<Prescription>(e =>
        {
            e.HasOne(p => p.Consultation)
             .WithMany(c => c.Prescriptions)
             .HasForeignKey(p => p.ConsultationId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ─────────────── LAB ORDER ───────────────
        builder.Entity<LabOrder>(e =>
        {
            e.HasOne(lo => lo.Consultation)
             .WithMany(c => c.LabOrders)
             .HasForeignKey(lo => lo.ConsultationId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ─────────────── LAB RESULT ───────────────
        builder.Entity<LabResult>(e =>
        {
            e.HasOne(lr => lr.LabOrder)
             .WithOne(lo => lo.LabResult)
             .HasForeignKey<LabResult>(lr => lr.LabOrderId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(lr => lr.Technicien)
             .WithMany(t => t.LabResults)
             .HasForeignKey(lr => lr.TechnicienId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.NoAction)
             ; 
        });
        // ─────────────── NOTIFICATION ───────────────
        builder.Entity<Notification>(e =>
        {
            e.ToTable("Notifications");

            e.HasOne(n => n.Recipient)
             .WithMany()
             .HasForeignKey(n => n.RecipientId)
             .OnDelete(DeleteBehavior.Cascade)
             .IsRequired(false);


            e.HasOne(n => n.Appointment)
             .WithMany()
             .HasForeignKey(n => n.AppointmentId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ─────────────── DOCTOR → SPECIALTY ───────────────
        builder.Entity<Doctor>(e =>
        {
            e.HasOne(d => d.Specialty)
             .WithMany(s => s.Doctors)
             .HasForeignKey(d => d.SpecialtyId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ─────────────── LAB TECH → SPECIALTY ───────────────
        builder.Entity<LabTechnicien>(e =>
        {
            e.HasOne(lt => lt.Specialty)
             .WithMany(s => s.LabTechniciens)
             .HasForeignKey(lt => lt.SpecialtyId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ─────────────── ENUMS → STRING ───────────────
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var properties = entityType.ClrType
                .GetProperties()
                .Where(p => p.PropertyType.IsEnum);

            foreach (var property in properties)
            {
                builder.Entity(entityType.ClrType)
                    .Property(property.Name)
                    .HasConversion<string>();
            }
        }

        //  ONLY ONE FILTER (ROOT ENTITY)
        builder.Entity<User>().HasQueryFilter(u => u.IsActive);

        // ─────────────── DOCTOR REVIEWS ───────────────
        builder.Entity<DoctorReview>(e =>
        {
            e.HasOne(r => r.Doctor)
             .WithMany()
             .HasForeignKey(r => r.DoctorId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.Patient)
             .WithMany()
             .HasForeignKey(r => r.PatientId)
             .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(r => r.Appointment)
             .WithMany()
             .HasForeignKey(r => r.AppointmentId)
             .OnDelete(DeleteBehavior.NoAction);

            // One review per completed appointment
            e.HasIndex(r => r.AppointmentId).IsUnique();

            e.Property(r => r.Rating).IsRequired();
            e.Property(r => r.Comment).HasMaxLength(1000);
        });

        // ─────────────── CHAT MESSAGES ───────────────
        builder.Entity<ChatMessage>(e =>
        {
            e.Property(m => m.Content).IsRequired().HasMaxLength(2000);

            e.HasOne(m => m.Sender)
             .WithMany()
             .HasForeignKey(m => m.SenderId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(m => m.Receiver)
             .WithMany()
             .HasForeignKey(m => m.ReceiverId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}