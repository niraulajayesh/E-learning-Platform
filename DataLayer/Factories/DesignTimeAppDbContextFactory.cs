using DataLayer.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DataLayer.Factories;

/// <summary>
/// Allows EF Core CLI tools (dotnet ef migrations add / update-database) to
/// construct AppDbContext at design time without needing a running web host.
/// Uses a local SQL Server connection string for migration authoring only.
/// </summary>
public class DesignTimeAppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        // This connection string is used only by EF Core tooling at design time.
        // The production connection string is read from appsettings.json at runtime.
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\MSSQLLocalDB;Database=ElearningPlatformDb;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True",
            sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));

        return new AppDbContext(optionsBuilder.Options);
    }
}





