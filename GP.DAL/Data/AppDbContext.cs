using GP.DAL.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GP.DAL.Data
{
	public class AppDbContext : IdentityDbContext<ApplicationUser>
	{
		public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
		{

		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
            // Patient -> Appointments (Cascade OK)
            modelBuilder.Entity<Patient>()
                .HasMany(p => p.Appointments)
                .WithOne(a => a.Patient)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Appointment -> Notes (Restrict to avoid multiple cascade paths)
            modelBuilder.Entity<Appointment>()
                .HasMany(a => a.Notes)
                .WithOne(n => n.Appointment)
                .HasForeignKey(n => n.AppointmentId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict); // <-- changed from Cascade

            // Appointment -> Analysis (NoAction is fine here)
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Analysis)
                .WithOne(n => n.Appointment)
                .HasForeignKey<Analysis>(n => n.AppointmentId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            // Analysis -> Notes (Cascade)
            modelBuilder.Entity<Analysis>()
                .HasMany(a => a.Notes)
                .WithOne(n => n.Analysis)
                .HasForeignKey(n => n.AnalysisId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(modelBuilder);
			modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
          
        }


		public DbSet<Patient> Patients { get; set; }
		//public DbSet<Doctor> Doctors { get; set; }
		public DbSet<Appointment> Appointments { get; set; }
		public DbSet<Note> Notes { get; set; }
		public DbSet<Analysis> Analyses { get; set; }
		public DbSet<Keypoint> KeyPoints { get; set; }
        //public DbSet<PatientPhoto> PatientPhotos { get; set; }
        //public DbSet<ProcessedImage> ProcessedImages { get; set; }
		

	
	}
}
