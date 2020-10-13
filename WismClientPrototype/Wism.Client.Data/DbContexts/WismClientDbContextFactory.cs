using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Text;

namespace Wism.Client.Data.DbContexts
{
    public class WismClientDbContextFactory : IDesignTimeDbContextFactory<WismClientDbContext>
    {
        // TODO: Refactor this to leverage appsettings at design time? Eliminate duplication.
        private readonly string connectionString = "Data Source=WismClient.db";

        public WismClientDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<WismClientDbContext>();
            optionsBuilder.UseSqlite(connectionString);

            return new WismClientDbContext(optionsBuilder.Options);
        }
    }
}
