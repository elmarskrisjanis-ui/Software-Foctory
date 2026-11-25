using Microsoft.EntityFrameworkCore;

namespace BezProcentu.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ProductEntity> Products => Set<ProductEntity>();
    public DbSet<CollectionRunEntity> CollectionRuns => Set<CollectionRunEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var product = modelBuilder.Entity<ProductEntity>();
        product.ToTable("products");
        product.HasKey(p => p.Id);
        product.Property(p => p.StoreSlug).HasMaxLength(40);
        product.Property(p => p.StoreProductId).HasMaxLength(64);
        product.Property(p => p.Name).HasMaxLength(400);
        product.Property(p => p.Url).HasMaxLength(1000);
        product.Property(p => p.ImageUrl).HasMaxLength(1000);
        product.Property(p => p.ImageFallbackUrl).HasMaxLength(1000);
        product.Property(p => p.Brand).HasMaxLength(100);
        product.Property(p => p.CategorySlug).HasMaxLength(60);
        product.Property(p => p.Price).HasPrecision(12, 2);
        product.Property(p => p.OldPrice).HasPrecision(12, 2);
        product.Property(p => p.InterestRatePercent).HasPrecision(6, 3);
        product.Property(p => p.MinAmount).HasPrecision(12, 2);
        product.Property(p => p.MaxAmount).HasPrecision(12, 2);
        product.Property(p => p.MonthlyPayment).HasPrecision(12, 2);
        product.Property(p => p.Provider).HasMaxLength(100);
        product.Property(p => p.Conditions).HasMaxLength(1000);
        product.Property(p => p.OfferSourceUrl).HasMaxLength(1000);
        product.Property(p => p.OfferScope).HasConversion<string>().HasMaxLength(20);
        product.HasIndex(p => new { p.StoreSlug, p.StoreProductId }).IsUnique();
        product.HasIndex(p => new { p.IsOfferActive, p.OfferVerifiedAt, p.CategorySlug });

        var run = modelBuilder.Entity<CollectionRunEntity>();
        run.ToTable("collection_runs");
        run.HasKey(r => r.Id);
        run.Property(r => r.StoreSlug).HasMaxLength(40);
        run.Property(r => r.Status).HasConversion<string>().HasMaxLength(30);
        run.Property(r => r.Error).HasMaxLength(500);
        run.HasIndex(r => new { r.StoreSlug, r.StartedAt });
    }
}
