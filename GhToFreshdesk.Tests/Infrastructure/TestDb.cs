using Microsoft.EntityFrameworkCore;
using GhToFreshdesk.Infrastructure.Persistence;

namespace GhToFreshdesk.Tests.Infrastructure;

public static class TestDb
{
    public static AppDbContext NewContext()
    {
        var path = Path.Combine(Path.GetTempPath(), $"gtf_{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={path}")
            .Options;

        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}