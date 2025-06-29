using GP.DAL.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.DAL.Data.Configrations
{
	public class PatientConfigration : IEntityTypeConfiguration<Patient>
	{
		public void Configure(EntityTypeBuilder<Patient> builder)
		{
			builder.Property(p=>p.Height).HasColumnType("decimal(18,2)");
			builder.Property(p=>p.Weight).HasColumnType("decimal(18,2)");
		}
	}
}
