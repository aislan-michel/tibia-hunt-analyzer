using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TibiaHuntAnalyzer.Infrastructure.Persistence;

public sealed class HuntAnalyzerDbContextFactory : IDesignTimeDbContextFactory<HuntAnalyzerDbContext>
{
    public HuntAnalyzerDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<HuntAnalyzerDbContext>()
            .UseSqlite("Data Source=tibia-hunt-analyzer-design-time.db")
            .Options;

        return new HuntAnalyzerDbContext(options);
    }
}
