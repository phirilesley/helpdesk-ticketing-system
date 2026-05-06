using HelpDeskSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HelpDeskSystem.Persistence;

public class HelpDeskDbContextFactory : IDesignTimeDbContextFactory<HelpDeskDbContext>
{
    public HelpDeskDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<HelpDeskDbContext>();

        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=localhost,14333;Database=HelpDeskSystem;User Id=sa;Password=HelpDesk123!@#;TrustServerCertificate=true;MultipleActiveResultSets=true";

        optionsBuilder.UseSqlServer(connectionString);

        return new HelpDeskDbContext(optionsBuilder.Options);
    }
}
