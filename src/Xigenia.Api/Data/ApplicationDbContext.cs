using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Xigenia.Domain;

namespace Xigenia.Api.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Obligation> Obligations => Set<Obligation>();

    public DbSet<MoneyMovement> MoneyMovements => Set<MoneyMovement>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Obligation>(entity =>
        {
            entity.ToTable("Obligations", table =>
            {
                table.HasCheckConstraint("CK_Obligations_TotalAmount_Positive", "[Total_Amount] > 0");
                table.HasCheckConstraint("CK_Obligations_Kind_Valid", "[Kind] IN (1, 2)");
                table.HasCheckConstraint("CK_Obligations_DueDate_NotBeforeIssueDate", "[DueDate] IS NULL OR [DueDate] >= [IssueDate]");
            });

            entity.HasKey(o => o.Id);
            entity.Property(o => o.Id).ValueGeneratedNever();

            entity.Property(o => o.OwnerId).IsRequired().HasMaxLength(450);
            entity.HasIndex(o => o.OwnerId);

            entity.Property(o => o.Kind).HasConversion<int>().IsRequired();
            entity.Property(o => o.Title).IsRequired();

            entity.OwnsOne(o => o.Total, owned =>
            {
                owned.Property(m => m.Amount).HasColumnName("Total_Amount").HasColumnType("decimal(18,2)").IsRequired();
                owned.Property(m => m.Currency).HasColumnName("Total_Currency").HasColumnType("varchar(3)").IsRequired();
            });

            entity.Property(o => o.IssueDate).HasColumnType("date").IsRequired();
            entity.Property(o => o.DueDate).HasColumnType("date");
        });

        builder.Entity<MoneyMovement>(entity =>
        {
            entity.ToTable("MoneyMovements", table =>
            {
                table.HasCheckConstraint("CK_MoneyMovements_Amount_Positive", "[Amount] > 0");
                table.HasCheckConstraint("CK_MoneyMovements_Direction_Valid", "[Direction] IN (1, 2)");
            });

            entity.HasKey(m => m.Id);
            entity.Property(m => m.Id).ValueGeneratedNever();

            entity.Property(m => m.OwnerId).IsRequired().HasMaxLength(450);
            entity.HasIndex(m => m.OwnerId);

            entity.HasOne<Obligation>()
                .WithMany()
                .HasForeignKey(m => m.ObligationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(m => m.Direction).HasConversion<int>().IsRequired();

            entity.OwnsOne(m => m.Amount, owned =>
            {
                owned.Property(m => m.Amount).HasColumnName("Amount").HasColumnType("decimal(18,2)").IsRequired();
                owned.Property(m => m.Currency).HasColumnName("Currency").HasColumnType("varchar(3)").IsRequired();
            });

            entity.Property(m => m.MovementDate).HasColumnType("date").IsRequired();
        });
    }
}
