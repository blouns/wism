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
            modelBuilder.Entity<Army>();
                //.HasData(
                //    new Army()
                //    {
                //        HitPoints = 2,
                //        Id = Guid.Parse("{5771F514-EDCE-463B-9F54-D0BB30ADEB57}"),
                //        Name = "Hero",
                //        Strength = 5,
                //        X = 0,
                //        Y = 0
                //    },
                //    new Army()
                //    {
                //        HitPoints = 2,
                //        Id = Guid.Parse("{DDC9272A-F93D-4BD2-9351-7877B40FEADA}"),
                //        Name = "Light Infantry",
                //        Strength = 3,
                //        X = 0,
                //        Y = 5
                //    });

            modelBuilder.Entity<Command>();
            modelBuilder.Entity<AttackCommand>()
                .HasBaseType<Command>();                
            modelBuilder.Entity<MoveCommand>()
                .HasBaseType<Command>();

            modelBuilder.Entity<ArmyCommand>()
                .HasKey(a => new { a.ArmyId, a.CommandId });

            base.OnModelCreating(modelBuilder);
        }
    }
}
