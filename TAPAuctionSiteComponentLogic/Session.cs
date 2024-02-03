using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TAP21_22_AuctionSite.Interface;
using Siri;
#nullable enable
namespace Siri {
    public class Session : ISession {
        public string Id { get; }
        public DateTime ValidUntil {
            get {
                using (var context = new AuctionSiteContext(connectionString)) {
                    var session = context.Sessions.FirstOrDefault(s => s.SessionId == Id);
                    if (session == null) throw new AuctionSiteInvalidOperationException("Session deleted");
                    return session.ValidUntil;
                }
            }
        }

        public IUser User { get; }

        private readonly string connectionString;
        private readonly Site site;

        public Session(string id, IUser user, Site site, string connectionString) {
            Id = id;
            User = user;
            this.site = site;
            this.connectionString = connectionString;
        }

        public Session(SessionEntity session, IUser user, Site site, string connectionString) : this(session.SessionId, user, site, connectionString) {}

        public void Logout() {
            using (var context = new AuctionSiteContext(connectionString)) {
                var session = context.Sessions.FirstOrDefault(s => s.SessionId == Id);
                if (session == null) throw new AuctionSiteInvalidOperationException("Session already destroyed");
                context.Remove(session);
                context.SaveChanges();
            }
        }

        public void IncreaseExpirationTime() {
            using (var context = new AuctionSiteContext(connectionString)) {
                var session = context.Sessions.FirstOrDefault(s => s.SessionId == Id);
                if (session == null) throw new AuctionSiteInvalidOperationException("Session deleted");
                session.ValidUntil = site.Now().AddSeconds(site.SessionExpirationInSeconds);
                context.SaveChanges();
            }
        }

        public bool IsValid(int? siteId) {
            var sessions = site.ToyGetSessions();
            if (sessions.FirstOrDefault(s => s.Id == Id) == null) return false;
            if(siteId != null) return siteId == site.Id && ValidUntil > site.Now();
            return ValidUntil > site.Now();
        }

        public IAuction CreateAuction(string description, DateTime endsOn, double startingPrice) {
            if (ValidUntil < site.Now()) throw new AuctionSiteInvalidOperationException("Session expired");
            if (site.ToyGetSessions().FirstOrDefault(s => s.Id == Id) == null)
                throw new AuctionSiteInvalidOperationException("Session deleted");

            var auction = validateAuction(description, endsOn, startingPrice);

            using (var context = new AuctionSiteContext(connectionString)) {
                context.Auctions.Add(auction);
                context.SaveChanges();
                auction = context.Auctions.Include(a => a.Owner).First(a => a == auction);
                IncreaseExpirationTime();
                return new Auction(auction, site, connectionString);
            }
        }

        public override bool Equals(object? obj) {
            var other = obj as Session;
            if (other == null) return false;
            return other.Id == Id;
        }

        public override int GetHashCode() { return Id.GetHashCode(); }

        private AuctionEntity validateAuction(string description, DateTime endsOn, double startingPrice) {
            var auction = new AuctionEntity(description, endsOn, startingPrice, ((User)User).Id, site.Id);

            if (description == null)
                throw new AuctionSiteArgumentNullException($"{nameof(description)} cannot be null");
            if (description == "") throw new AuctionSiteArgumentException("Cannot be empty", nameof(description));
            if (endsOn < site.Now()) throw new AuctionSiteUnavailableTimeMachineException("Invalid endsOn");
            if (startingPrice < 0)
                throw new AuctionSiteArgumentOutOfRangeException(nameof(startingPrice), startingPrice,
                    "Must be greater than 0");

            var vc = new ValidationContext(auction);
            try {
                Validator.ValidateObject(auction, vc, true);
            } catch (ValidationException e) {
                if (e.ValidationAttribute!.GetType() == typeof(RequiredAttribute)) {
                    throw new AuctionSiteArgumentNullException($"{e.ValidationResult.MemberNames.First()} cannot be null", e);
                }

            }
           
            return auction;
        }
    }
}
