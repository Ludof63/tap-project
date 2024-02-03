using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TAP21_22_AuctionSite.Interface;
using Siri;

namespace Siri {
    public class AuctionSiteContext : TapDbContext {
        
        public AuctionSiteContext(string connectionString) : base(new DbContextOptionsBuilder<AuctionSiteContext>().UseSqlServer(connectionString).Options){}


        protected override void OnConfiguring(DbContextOptionsBuilder options) { 
            //options.LogTo(Console.WriteLine).EnableSensitiveDataLogging(); 
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            var user = modelBuilder.Entity<UserEntity>();
            user.HasMany(u => u.AuctionSeller)
                .WithOne(a => a.Owner!)
                .HasForeignKey(a => a.OwnerId)
                .OnDelete(DeleteBehavior.NoAction); 
            user.HasMany(u => u.AuctionBidder)
                .WithOne(a => a.Winner!)
                .HasForeignKey(a => a.WinnerId)
                .OnDelete(DeleteBehavior.SetNull); 
            user.HasMany(u => u.Sessions)
                .WithOne(s => s.UserEntity!)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            var session = modelBuilder.Entity<SessionEntity>();
            session.HasOne(s => s.SiteEntity).WithMany(u => u!.Sessions).HasForeignKey(s => s.SiteId)
                .OnDelete(DeleteBehavior.NoAction);

            var auction = modelBuilder.Entity<AuctionEntity>();
            auction.HasOne(auction => auction.SiteEntity)
                .WithMany(site => site!.Auctions)
                .HasForeignKey(auction => auction.SiteId)
                .OnDelete(DeleteBehavior.NoAction);
            

            auction.Navigation(a => a.Owner).AutoInclude();
        }

        public override int SaveChanges() {
            try {
                return base.SaveChanges();
            } catch (SqlException e) {
                throw new AuctionSiteUnavailableDbException("Unavailable Db", e);
            } catch (DbUpdateException e) {
                var sqlException = e.InnerException as SqlException;
                if (sqlException == null)
                    throw new AuctionSiteInvalidOperationException("Missing information from DB", e);
                switch (sqlException.Number) {
                    case < 54: throw new AuctionSiteUnavailableDbException("Not available DB", e);
                    case 2601: throw new AuctionSiteNameAlreadyInUseException(null, $"{sqlException.Message}", e);
                    case 2627: throw new AuctionSiteNameAlreadyInUseException(null, "Primary key already in use", e);
                    case 547: throw new AuctionSiteInvalidOperationException("Foreign key not found", e);
                    default: throw new AuctionSiteInvalidOperationException("Query error", e);
                }
               
            }
        }

        public DbSet<SiteEntity> Sites { get; set; }
        public DbSet<AuctionEntity> Auctions { get; set; }
        public DbSet<UserEntity> Users { get; set; }
        public DbSet<SessionEntity> Sessions { get; set; }
    }
}
