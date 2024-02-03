using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TAP21_22_AuctionSite.Interface;
#nullable enable
namespace Siri {
    public class User : IUser {
        public string Username { get; }
        public string Password { get; } 
        public int Id { get; }

        private readonly string connectionString;
        private readonly Site site;

        public User(string username, string password, int id, Site site, string connectionString) {
            Username = username;
            Password = password;
            Id = id;
            this.connectionString = connectionString;
            this.site = site;
        }

        public User(UserEntity user, Site site, string connectionString) : this(user.Username, user.Password, user.UserId, site, connectionString) { }

        public IEnumerable<IAuction> WonAuctions() { 
            IEnumerable<IAuction> WonAuctionsAux(List<AuctionEntity> auctions) {
                foreach (var auction in auctions) {
                    yield return new Auction(auction, site, connectionString);
                }
            }

            using (var context = new AuctionSiteContext(connectionString)) {
                var user = context.Users.Include(u => u.AuctionBidder).FirstOrDefault(u => u.UserId == Id);
                if (user == null) throw new AuctionSiteInvalidOperationException("User deleted");
                context.Auctions.Include(a => a.Owner);
                var auctions = user.AuctionBidder.Where(a => a.EndsOn < site.Now()).ToList();
                return WonAuctionsAux(auctions);
            }
        }

        public void Delete() {
            var activeAuction = site.ToyGetAuctions(true).FirstOrDefault(a => Equals(a.Seller) || Equals(a.CurrentWinner()));
            if (activeAuction != null) throw new AuctionSiteInvalidOperationException("There are some active auctions");

            using (var context = new AuctionSiteContext(connectionString)) {
                var user = context.Users.FirstOrDefault(u => u.UserId == Id);
                if (user == null) throw new AuctionSiteInvalidOperationException("User already deleted");

                var allAuctionSell = site.ToyGetAuctions(false).Where(s => s.Seller.Equals(this));

                foreach (var auction in allAuctionSell) {
                    auction.Delete();
                }

                context.Remove(user);
                context.SaveChanges();
            }
        }

        public override bool Equals(object? obj) {
            var item = obj as User;
            if (item == null) return false;
            return Id == item.Id;
        }

        public override int GetHashCode() {
            return Id.GetHashCode();
        }

        public static string hashPassword(string password) {
            byte[] salt;
            new RNGCryptoServiceProvider().GetBytes(salt = new byte[16]);
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000);
            byte[] hash = pbkdf2.GetBytes(20);
            byte[] hashBytes = new byte[36];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 20);
            return Convert.ToBase64String(hashBytes);
        }

        public static bool verifyPassword(string hashed, string password) {
            byte[] hashBytes = Convert.FromBase64String(hashed);
            byte[] salt = new byte[16];
            Array.Copy(hashBytes, 0, salt, 0, 16);
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000);
            byte[] hash = pbkdf2.GetBytes(20);
            for(int i=0; i<20; i++)
                if (hashBytes[i + 16] != hash[i])
                    return false;
            return true;
        }
    }
}
