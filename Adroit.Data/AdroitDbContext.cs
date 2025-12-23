using Adroit.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Adroit.Data;

public class AdroitDbContext : DbContext
{
    public AdroitDbContext(DbContextOptions<AdroitDbContext> options) : base(options)
    {
    }

    public DbSet<ShortUrl> ShortUrls { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ShortUrl>(entity =>
        {
            entity.ToTable("ShortUrls");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.ShortCode)
                .IsRequired()
                .HasMaxLength(12);

            entity.Property(e => e.LongUrl)
                .IsRequired()
                .HasMaxLength(2048);

            entity.Property(e => e.ClickCount)
                .HasDefaultValue(0);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // Index for fast lookups by short code (case-insensitive)
            entity.HasIndex(e => e.ShortCode)
                .IsUnique()
                .HasDatabaseName("IX_ShortUrls_ShortCode");

            // Index for lookups by long URL
            entity.HasIndex(e => e.LongUrl)
                .HasDatabaseName("IX_ShortUrls_LongUrl");
        });
    }
}
