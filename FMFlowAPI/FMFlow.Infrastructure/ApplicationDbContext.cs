using System.Reflection;
using FMFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FMFlow.Infrastructure
{
	public class ApplicationDbContext : DbContext
	{
		public DbSet<FlowUser> FMFlowUsers { get; set; }

		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
			: base(options)
		{
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);
			modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
		}
	}
}
