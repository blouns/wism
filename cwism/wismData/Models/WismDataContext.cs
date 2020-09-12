using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace WismData.Models
{
    public class WismDataContext : DbContext
    {
        public WismDataContext(DbContextOptions<WismDataContext> options)
            : base(options)
        {
        }

        public DbSet<World> World { get; set; }
    }
}
