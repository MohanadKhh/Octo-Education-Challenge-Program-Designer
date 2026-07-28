using Microsoft.EntityFrameworkCore;
using ProgramDesigner.Domain.Entities;

namespace ProgramDesigner.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<LearningProgram> Programs => Set<LearningProgram>();
    public DbSet<Node> Nodes => Set<Node>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── LearningProgram ────────────────────────────────────────────
        modelBuilder.Entity<LearningProgram>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);

            entity.HasOne(p => p.RootNode)
                  .WithMany()
                  .HasForeignKey(p => p.RootNodeId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Node (self-referencing tree) ───────────────────────────────
        modelBuilder.Entity<Node>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Name).IsRequired().HasMaxLength(200);
            entity.Property(n => n.Type).HasConversion<string>();
            entity.Property(n => n.Rule).HasConversion<string>();
            entity.Property(n => n.StepType).HasMaxLength(100);

            // Self-referencing FK for tree structure
            entity.HasMany(n => n.Children)
                  .WithOne()
                  .HasForeignKey(n => n.ParentNodeId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Index for ordering children within a parent
            entity.HasIndex(n => new { n.ParentNodeId, n.Order });
        });
    }
}
