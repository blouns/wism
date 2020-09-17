using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using BranallyGames.Wism.Repository.Entities;

namespace BranallyGames.Wism.Repository.DbContexts
{
    public class WismDbContext : DbContext
    {
        public WismDbContext(DbContextOptions<WismDbContext> options)
            : base(options)
        {
        }

        public DbSet<World> Worlds { get; set; }

        public DbSet<Player> Players { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<World>().HasData(
                new World()
                {
                    Id = Guid.Parse("{517FED59-D8DC-4B59-B6CA-2052F75AABF7}"),
                    ShortName = "Etheria",
                    DisplayName = "Etheria"
                },
                new World()
                {
                    Id = Guid.Parse("{B0113C1C-4EE8-421D-82CE-1B65207C9017}"),
                    ShortName = "Britannia",
                    DisplayName = "Britannia",

                },
                new World()
                {
                    Id = Guid.Parse("{EE082E96-71DA-453B-9252-17C8B4F3BE65}"),
                    ShortName = "USA",
                    DisplayName = "United States of America",
                });

            modelBuilder.Entity<Player>().HasData(
                new Player()
                {
                    Id = Guid.Parse("{986C478B-6554-4A2C-805F-DC059632B707}"),
                    ShortName = "Brian",
                    DisplayName = "Branally",
                    WorldId = Guid.Parse("{517FED59-D8DC-4B59-B6CA-2052F75AABF7}")
                },
                new Player()
                {
                    Id = Guid.Parse("{3DEF6DBB-FA0C-4010-A73F-AAD13B50697B}"),
                    ShortName = "Dan",
                    DisplayName = "Danally",
                    WorldId = Guid.Parse("{517FED59-D8DC-4B59-B6CA-2052F75AABF7}")
                },
                new Player()
                {
                    Id = Guid.Parse("{A0466732-E79E-4789-B883-7B43506795E3}"),
                    ShortName = "Brian",
                    DisplayName = "Branally",
                    WorldId = Guid.Parse("{B0113C1C-4EE8-421D-82CE-1B65207C9017}")
                },
                new Player()
                {
                    Id = Guid.Parse("{1B9991BF-E454-486A-8F5C-297DD69FE60C}"),
                    ShortName = "Jacob",
                    DisplayName = "Jake the Snake",
                    WorldId = Guid.Parse("{B0113C1C-4EE8-421D-82CE-1B65207C9017}")
                },
                new Player()
                {
                    Id = Guid.Parse("{B07173B3-290B-4490-BF9A-D3588A251854}"),
                    ShortName = "Brian",
                    DisplayName = "Branally",
                    WorldId = Guid.Parse("{EE082E96-71DA-453B-9252-17C8B4F3BE65}")
                },
                new Player()
                {
                    Id = Guid.Parse("{3EF6BA4B-3F46-46A5-8705-2A1EA045A381}"),
                    ShortName = "Owen",
                    DisplayName = "Owen Little",
                    WorldId = Guid.Parse("{EE082E96-71DA-453B-9252-17C8B4F3BE65}")
                });

            base.OnModelCreating(modelBuilder);
        }
    }
}
