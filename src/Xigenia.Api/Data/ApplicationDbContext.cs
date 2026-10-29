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

    public DbSet<FinancialAccount> FinancialAccounts => Set<FinancialAccount>();

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
                table.HasCheckConstraint("CK_MoneyMovements_Classification_Valid", "[Classification] IN (0, 1, 2, 3)");
                table.HasCheckConstraint("CK_MoneyMovements_TransferClassification_Consistent", "([TransferGroupId] IS NULL AND [Classification] <> 3) OR ([TransferGroupId] IS NOT NULL AND [Classification] = 3)");
            });

            entity.HasKey(m => m.Id);
            entity.Property(m => m.Id).ValueGeneratedNever();

            entity.Property(m => m.OwnerId).IsRequired().HasMaxLength(450);
            entity.HasIndex(m => m.OwnerId);

            entity.HasOne<Obligation>()
                .WithMany()
                .HasForeignKey(m => m.ObligationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<FinancialAccount>()
                .WithMany()
                .HasForeignKey(m => m.FinancialAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(m => m.FinancialAccountId);

            entity.Property(m => m.TransferGroupId);
            entity.HasIndex(m => m.TransferGroupId);

            entity.Property(m => m.Direction).HasConversion<int>().IsRequired();

            entity.Property(m => m.Classification).HasConversion<int>().IsRequired();

            entity.OwnsOne(m => m.Amount, owned =>
            {
                owned.Property(m => m.Amount).HasColumnName("Amount").HasColumnType("decimal(18,2)").IsRequired();
                owned.Property(m => m.Currency).HasColumnName("Currency").HasColumnType("varchar(3)").IsRequired();
            });

            entity.Property(m => m.MovementDate).HasColumnType("date").IsRequired();
        });

        builder.Entity<FinancialAccount>(entity =>
        {
            entity.ToTable("FinancialAccounts", table =>
            {
                table.HasCheckConstraint("CK_FinancialAccounts_Type_Valid", "[Type] IN (1, 2, 3, 4)");
                table.HasCheckConstraint("CK_FinancialAccounts_OpeningBalance_Nonnegative", "[Type] <> 4 OR [OpeningBalance] >= 0");
                table.HasCheckConstraint("CK_FinancialAccounts_CreditLimit_Positive", "[CreditLimit] IS NULL OR [CreditLimit] > 0");
                table.HasCheckConstraint("CK_FinancialAccounts_CardFields_CreditCardOnly", "[Type] = 4 OR ([CreditLimit] IS NULL AND [StatementClosingDay] IS NULL AND [PaymentDueDay] IS NULL)");
                table.HasCheckConstraint("CK_FinancialAccounts_StatementClosingDay_Range", "[StatementClosingDay] IS NULL OR ([StatementClosingDay] >= 1 AND [StatementClosingDay] <= 31)");
                table.HasCheckConstraint("CK_FinancialAccounts_PaymentDueDay_Range", "[PaymentDueDay] IS NULL OR ([PaymentDueDay] >= 1 AND [PaymentDueDay] <= 31)");
            });

            entity.HasKey(a => a.Id);
            entity.Property(a => a.Id).ValueGeneratedNever();

            entity.Property(a => a.OwnerId).IsRequired().HasMaxLength(450);
            entity.HasIndex(a => a.OwnerId);

            entity.Property(a => a.Name).IsRequired().HasMaxLength(200);
            entity.Property(a => a.Type).HasConversion<int>().IsRequired();
            entity.Property(a => a.Currency).IsRequired().HasColumnType("varchar(3)").HasMaxLength(3);
            entity.Property(a => a.OpeningBalance).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(a => a.IsActive).IsRequired();
            entity.Property(a => a.CreatedAt).HasColumnType("datetimeoffset").IsRequired();

            entity.Property(a => a.CreditLimit).HasColumnType("decimal(18,2)");
            entity.Property(a => a.StatementClosingDay);
            entity.Property(a => a.PaymentDueDay);
        });
    }
}
