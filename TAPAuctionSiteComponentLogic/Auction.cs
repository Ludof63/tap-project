using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TAP21_22_AuctionSite.Interface;
#nullable enable
namespace Siri {
    public class Auction : IAuction {
        public int Id { get; }
        public IUser Seller { get; }
        public string Description { get; }
        public DateTime EndsOn { get; }

        private readonly string connectionString;
        private readonly Site site;

        public Auction(int id, IUser seller, Site site, string description, DateTime endOn, string connectionString) {
            Id = id;
            Seller = seller;
            Description = description;
            EndsOn = endOn;
            this.connectionString = connectionString;
            this.site = site;
        }

        public Auction(AuctionEntity auction, Site site, string connectionString) : 
            this(auction.AuctionId, new User(auction.Owner!, site, connectionString), site, auction.Description, auction.EndsOn, connectionString) { }


        public IUser? CurrentWinner() {
            if(checkIfDeleted()) throw new AuctionSiteInvalidOperationException("This auction appears to have been deleted");
            using (var context = new AuctionSiteContext(connectionString)) {
                var user = context.Auctions.Include(a => a.Winner).FirstOrDefault(a => a.AuctionId == Id)?.Winner;
                return user != null ? new User(user, site, connectionString) : null;
            }
        }

        public double CurrentPrice() {
            if (checkIfDeleted()) throw new AuctionSiteInvalidOperationException("Auction deleted");
            using (var context = new AuctionSiteContext(connectionString)) {
                var bidValue = context.Auctions.FirstOrDefault(a => a.AuctionId == Id)?.CurrentPrice;
                return bidValue ?? 0;
            }
        }

        public void Delete() {
            if (checkIfDeleted()) throw new AuctionSiteInvalidOperationException("Auction deleted");
            using (var context = new AuctionSiteContext(connectionString)) {
                var thisAuction = context.Auctions.SingleOrDefault(a => a.AuctionId == Id);
                if (thisAuction != null) {
                    context.Remove(thisAuction);
                    context.SaveChanges();
                }
            }
        }

       /* public void SetWinnerToNull() {
            using (var context = new AuctionSiteContext(connectionString)) {
                var auction = context.Auctions.FirstOrDefault(a => a.AuctionId == Id);
                if(auction == null) throw new AuctionSiteInvalidOperationException("Auction deleted");
                auction.WinnerId = null;
                context.SaveChanges();
            }
        }*/

        public bool Bid(ISession session, double offer) {
            if (offer < 0)
                throw new AuctionSiteArgumentOutOfRangeException(nameof(offer), offer, "Must be greater than zero");
            if (checkIfDeleted()) throw new AuctionSiteInvalidOperationException("Auction deleted");
            if (session == null) throw new AuctionSiteArgumentNullException("Session cannot be null");

            var userSession = session as Session;
            if (userSession == null) throw new AuctionSiteInvalidOperationException($"Invalid type of {nameof(session)}");
            if (!userSession.IsValid(((Site)site).Id)) throw new AuctionSiteArgumentException("Invalid session");

            if (EndsOn < site.Now()) return false; 

            using (var context = new AuctionSiteContext(connectionString)) {
                var user = context.Users.First(u => u.UserId == ((User) session.User).Id);
                var auction = context.Auctions.First(a => a.AuctionId == Id);

                if (user == null) throw new AuctionSiteInvalidOperationException("User deleted");

                ((Session)session).IncreaseExpirationTime();


                if (auction.WinnerId != null && auction.WinnerId != user.UserId &&
                    offer < CurrentPrice() + site.MinimumBidIncrement)
                    return false;


                if (auction.WinnerId == null) {
                    if (offer < CurrentPrice()) return false;
                    auction.MaximumBidValue = offer;
                    auction.WinnerId = user.UserId;
                } else if(auction.WinnerId == user.UserId) {
                    if (offer < auction.MaximumBidValue + site.MinimumBidIncrement) return false;
                    auction.MaximumBidValue = offer;
                } else {
                    if (offer < CurrentPrice() + site.MinimumBidIncrement) return false;
                    if (auction.MaximumBidValue < offer) {
                        auction.CurrentPrice = Math.Min(auction.MaximumBidValue + site.MinimumBidIncrement, offer);
                        auction.MaximumBidValue = offer;
                        auction.WinnerId = user.UserId;
                    } else {
                        auction.CurrentPrice = Math.Min(auction.MaximumBidValue, offer + site.MinimumBidIncrement);
                    }
                }

                context.SaveChanges();
            }

            return true;

        }


        private bool checkIfDeleted() {
            using (var context = new AuctionSiteContext(connectionString)) {
                var auction = context.Auctions.FirstOrDefault(a => a.AuctionId == Id);
                return auction == null;
            }
        }

        public override bool Equals(object? obj) {
            var other = obj as Auction;
            if (other == null) return false;
            return other.Id == Id;
        }

        public override int GetHashCode() { return Id.GetHashCode(); }
    }
}
