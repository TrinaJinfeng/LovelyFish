using LovelyFish.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;

namespace LovelyFish.API.Server
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LovelyFishContext>
    {
        public LovelyFishContext CreateDbContext(string[] args)
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            string connectionString = configuration.GetConnectionString("DefaultConnection")!;

            var optionsBuilder = new DbContextOptionsBuilder<LovelyFishContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new LovelyFishContext(optionsBuilder.Options);
        }
    }
}
