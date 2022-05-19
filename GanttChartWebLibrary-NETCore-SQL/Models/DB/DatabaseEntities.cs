using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GanttChartWebLibrary_NETCore_SQL.Models.DB
{
    public partial class DatabaseEntities : DbContext
    {
        public DatabaseEntities()
        {
        }

        public DatabaseEntities(DbContextOptions<DatabaseEntities> options)
            : base(options)
        {
        }

        public virtual DbSet<Predecessor> Predecessors { get; set; } = null!;
        public virtual DbSet<Task> Tasks { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(@$"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename={ Path.Combine(Directory.GetCurrentDirectory(), "App_Data") }\Database.mdf;Integrated Security=True");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.UseCollation("Romanian_CI_AS");

            modelBuilder.Entity<Predecessor>(entity =>
            {
                entity.HasKey(e => new { e.DependentTaskId, e.PredecessorTaskId });

                entity.Property(e => e.DependentTaskId).HasColumnName("DependentTaskID");

                entity.Property(e => e.PredecessorTaskId).HasColumnName("PredecessorTaskID");

                entity.HasOne(d => d.DependentTask)
                    .WithMany(p => p.Predecessors)
                    .HasForeignKey(d => d.DependentTaskId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("Tasks_PredecessorHosts");

                entity.HasOne(d => d.Task)
                    .WithMany(p => p.Successors)
                    .HasForeignKey(d => d.PredecessorTaskId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("Tasks_Predecessors");
            });

            modelBuilder.Entity<Task>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Completion).HasColumnType("datetime");

                entity.Property(e => e.Finish).HasColumnType("datetime");

                entity.Property(e => e.Start).HasColumnType("datetime");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
