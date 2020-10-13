using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Wism.Client.Data.Entities;

namespace Wism.Client.Data.DbContexts
{
    public class WismClientDbContext : DbContext
    {
        public DbSet<Entities.Army> Armies { get; set; }

        public DbSet<Entities.Command> Commands { get; set; }

        public WismClientDbContext(DbContextOptions<WismClientDbContext> options)
            : base(options)
        {            
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Army>().HasData(
                new Army()
                {
                    HitPoints = 2,
                    Id = 1,
                    Name = "Hero",
                    Strength = 5,
                    X = 0,
                    Y = 0
                },
                new Army()
                {
                    HitPoints = 2,
                    Id = 2,
                    Name = "Light Infantry",
                    Strength = 3,
                    X = 0,
                    Y = 5
                });

            modelBuilder.Entity<ArmyAttackCommand>()
                .HasBaseType<Command>();
            modelBuilder.Entity<ArmyMoveCommand>()
                .HasBaseType<Command>();

            //modelBuilder.Entity<Command>().HasData(
            //    new ArmyMoveCommand()
            //    {
            //        X = 0,
            //        Y = 1
            //    },
            //    new ArmyMoveCommand()
            //    {
            //        X = 0,
            //        Y = 2
            //    },
            //    new ArmyMoveCommand()
            //    {
            //        X = 0,
            //        Y = 3
            //    },
            //    new ArmyMoveCommand()
            //    {
            //        X = 0,
            //        Y = 4
            //    },
            //    new ArmyAttackCommand()
            //    {
            //        X = 0,
            //        Y = 5
            //    });

            base.OnModelCreating(modelBuilder);
        }
    }
}
