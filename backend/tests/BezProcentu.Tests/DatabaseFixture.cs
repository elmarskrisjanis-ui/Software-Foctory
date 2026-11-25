using BezProcentu.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace BezProcentu.Tests;

/// <summary>Real PostgreSQL 18 in a throwaway container (requires Docker). Test-only credentials.</summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18")
        .WithDatabase("bezprocentu_test")
        .WithUsername("test")
        .WithPassword("test-only-password")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnectionString).Options);

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public async Task ResetAsync()
    {
        await using var db = CreateContext();
        await db.Database.ExecuteSqlRawAsync("TRUNCATE products, collection_runs RESTART IDENTITY");
    }
}

[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "database";
}
