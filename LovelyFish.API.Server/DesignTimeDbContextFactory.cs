using LovelyFish.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace LovelyFish.API.Server
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LovelyFishContext>
    {
        public LovelyFishContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            string connectionString = configuration.GetConnectionString("DefaultConnection");

            var optionsBuilder = new DbContextOptionsBuilder<LovelyFishContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new LovelyFishContext(optionsBuilder.Options);
        }
    }
}
