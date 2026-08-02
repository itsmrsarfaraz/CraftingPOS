using CraftingPOS.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CraftingPOS.Tests.TestSupport;

/// <summary>
/// Provides a real, isolated SQLite database per test (":memory:" with an
/// open connection kept alive for the fixture's lifetime — closing the
/// connection would destroy the in-memory DB). Using real SQLite instead
/// of mocked repositories means query filters, cascade deletes, and
/// column-type behavior are all exercised exactly as in production.
/// </summary>
public class SqliteInMemoryFixture : IDisposable
{
    private readonly SqliteConnection _connection;
    public AppDbContext Context { get; }

    public SqliteInMemoryFixture()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new AppDbContext(options);
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}