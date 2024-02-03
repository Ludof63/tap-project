using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using TAP21_22.AlarmClock.Interface;
using TAP21_22_AuctionSite.Interface;
using Siri;

#nullable enable
namespace Siri {
    public class Site : ISite {
        public string Name { get; }
        public int Timezone { get; }
        public int SessionExpirationInSeconds { get; }
        public double MinimumBidIncrement { get; }
        public int Id { get; }

        private readonly string connectionString;
        private readonly IAlarmClock alarmClock;
        private IAlarm alarm;
       

        public Site(int id, string name, int timezone, int sessionExpirationInSeconds, double minimumBidIncrement, string connectionString, IAlarmClock alarmClock) {
            Name = name;
            Timezone = timezone;
            SessionExpirationInSeconds = sessionExpirationInSeconds;
            MinimumBidIncrement = minimumBidIncrement;
            this.connectionString = connectionString;
            this.alarmClock = alarmClock;
            this.Id = id;
            alarm = alarmClock.InstantiateAlarm(5 * 60 * 1000);
            alarm.RingingEvent += deleteExpiredSessions;
            deleteExpiredSessions();
        }

        public Site(SiteEntity site, string connectionString, IAlarmClock alarmClock) : 
            this(site.SiteId, site.Name, site.Timezone, site.SessionExpirationInSeconds, site.MinimumBidIncrement, connectionString, alarmClock) { }

        public IEnumerable<IUser> ToyGetUsers() { 
            
            IEnumerable<IUser> ToyGetUsersAux(List<UserEntity> users) {
                foreach (var user in users) {
                    yield return new User(user, this, connectionString);
                }
            }


            List<UserEntity>? users;
            using (var context = new AuctionSiteContext(connectionString)) {
                var site = context.Sites.FirstOrDefault(s => s.SiteId == Id);
                if (site == null) throw new AuctionSiteInvalidOperationException("Site deleted");
                users = context.Users.Where(u => u.SiteId == Id).ToList();
            }

            return ToyGetUsersAux(users);
        }

        public IEnumerable<ISession> ToyGetSessions() {

            IEnumerable<ISession> ToyGetSessionsAux(List<SessionEntity> sessions) {
                foreach (var session in sessions) {
                    yield return new Session(session, new User(session.UserEntity!, this, connectionString), this, connectionString);
                }
            }


            using (var context = new AuctionSiteContext(connectionString)) {
                var site = context.Sites.FirstOrDefault(s => s.SiteId == Id);
                if (site == null) throw new AuctionSiteInvalidOperationException("Site deleted");
                var sessions = context.Sessions.Include(s => s.UserEntity).Where(s => s.SiteId == Id).ToList();
                return ToyGetSessionsAux(sessions);
            }


        }

        public IEnumerable<IAuction> ToyGetAuctions(bool onlyNotEnded) {
            IEnumerable<IAuction> ToyGetAuctionsAux(List<AuctionEntity> auctions) {
                foreach (var auction in auctions) {
                    yield return new Auction(auction, this, connectionString);
                }
            }

            List<AuctionEntity> auctions;
            using (var context = new AuctionSiteContext(connectionString)) {
                var site = context.Sites.FirstOrDefault(s => s.SiteId == Id);
                if (site == null) throw new AuctionSiteInvalidOperationException("Site deleted");
                auctions = context.Auctions.Include(a => a.Owner)
                    .Where(a => a.SiteId == Id && (!onlyNotEnded || a.EndsOn > Now())).ToList();
            }

            return ToyGetAuctionsAux(auctions);
        }

        public ISession? Login(string username, string password) {
            validateAttribute(username, password);

            using (var context = new AuctionSiteContext(connectionString)) {

                var site = context.Sites.FirstOrDefault(s => s.SiteId == Id);
                if (site == null) throw new AuctionSiteInvalidOperationException("Site deleted");

                var user = context.Users
                    .FirstOrDefault(u => u.Username == username && u.SiteId == Id);
                if (user == null) return null;

                if (!User.verifyPassword(user.Password, password)) return null;

                var session = context.Sessions.FirstOrDefault(s => s.UserId == user.UserId && s.ValidUntil > Now());
                if (session != null) return new Session(session, new User(user, this, connectionString), this, connectionString);

                


                var sessionEntity = new SessionEntity(username + Name + Now(), Now().AddSeconds(SessionExpirationInSeconds),
                    user.UserId, Id);

                context.Sessions.Add(sessionEntity);
                context.SaveChanges();

                return new Session(sessionEntity, new User(user, this, connectionString), this, connectionString);
            }

            
        }

        public void CreateUser(string username, string password) {
            var user = validateAttribute(username, password); 
            using (var context = new AuctionSiteContext(connectionString)) {
                var site = context.Sites.FirstOrDefault(s => s.SiteId == Id);
                if (site == null) throw new AuctionSiteInvalidOperationException("Site deleted");

                context.Users.Add(user);
                try {
                    context.SaveChanges();
                } catch (AuctionSiteNameAlreadyInUseException e) {
                    throw new AuctionSiteNameAlreadyInUseException(username, e.Message, e);
                }

            }
        }

        public void Delete() {
            using (var context = new AuctionSiteContext(connectionString)) {
                var site = context.Sites.FirstOrDefault(s => s.SiteId == Id);
                if (site == null) throw new AuctionSiteInvalidOperationException("Site already deleted");

                var users = ToyGetUsers();
                foreach (var user in users) {
                    user.Delete();
                }

                context.Remove(site);
                context.SaveChanges();
            }
        }

        public DateTime Now() {
            return alarmClock.Now;
        }

        private void deleteExpiredSessions() {
            using (var context = new AuctionSiteContext(connectionString)) {
                var sessionExpired = context.Sessions.ToList();
                var ss = sessionExpired.Where(s => s.SiteId == Id && s.ValidUntil < Now()).ToList(); 
                context.RemoveRange(ss);
                context.SaveChanges();
            }
            alarm = alarmClock.InstantiateAlarm(5 * 60 * 1000);
        }

        private UserEntity validateAttribute(string username, string password) {
            if (password == null) throw new AuctionSiteArgumentNullException($"{nameof(password)} cannot be null");
            if (password.Length < DomainConstraints.MinUserPassword)
                throw new AuctionSiteArgumentException("Value too short", nameof(password));

            var user = new UserEntity(username, User.hashPassword(password), Id);
            var vc = new ValidationContext(user);
            try {
                Validator.ValidateObject(user, vc, true);
            } catch (ValidationException e) {
                if (e.ValidationAttribute!.GetType() == typeof(MinLengthAttribute)) 
                    throw new AuctionSiteArgumentException("Value too short", e.ValidationResult.MemberNames.First(), e);
                
                if (e.ValidationAttribute!.GetType() == typeof(MaxLengthAttribute)) 
                    throw new AuctionSiteArgumentException("Value too long", e.ValidationResult.MemberNames.First(), e);

                if (e.ValidationAttribute!.GetType() == typeof(RequiredAttribute)) 
                    throw new AuctionSiteArgumentNullException($"{e.ValidationResult.MemberNames.First()} cannot be null", e);
            }


            return user;
        }
    }
}
