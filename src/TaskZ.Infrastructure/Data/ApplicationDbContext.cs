using Microsoft.EntityFrameworkCore;
using TaskZ.Core.Entities;
using Task = TaskZ.Core.Entities.Task;

namespace TaskZ.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Project> Projects { get; set; }
        public DbSet<Task> Tasks { get; set; }
        public DbSet<TaskHistory> TaskHistories { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<User> Users { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Project>(entity =>
            {
                entity.ToTable("Projects");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).IsRequired();

                entity.HasMany(e => e.Tasks)
                      .WithOne()
                      .HasForeignKey(e => e.ProjectId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Task>(entity =>
            {
                entity.ToTable("Tasks");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Status)
                      .IsRequired()
                      .HasConversion<string>();
                entity.Property(e => e.Priority)
                      .IsRequired()
                      .HasConversion<string>();

                entity.HasMany(e => e.History)
                      .WithOne()
                      .HasForeignKey(e => e.TaskId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Comments)
                      .WithOne()
                      .HasForeignKey(e => e.TaskId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TaskHistory>(entity =>
            {
                entity.ToTable("TaskHistories");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Timestamp).IsRequired();
            });

            modelBuilder.Entity<Comment>(entity =>
            {
                entity.ToTable("Comments");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Content).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.CreatedAt).IsRequired();
            });
        }
    }


}