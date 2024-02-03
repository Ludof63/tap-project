using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TAP21_22.AlarmClock.Interface;
using TAP21_22_AuctionSite.Interface;

namespace Siri {
    public class HostFactory : IHostFactory {
        public void CreateHost(string connectionString) { 
            if (String.IsNullOrEmpty(connectionString)) throw new AuctionSiteArgumentNullException("Connection strings cannot be null or empty");
            
            using (var c = new AuctionSiteContext(connectionString)) {
                try {
                    c.Database.EnsureDeleted();
                    c.Database.EnsureCreated();
                } catch (SqlException e) {
                    throw new AuctionSiteUnavailableDbException("Unavailable DB", e);
                }
            }
            
        }

        public IHost LoadHost(string connectionString, IAlarmClockFactory alarmClockFactory) {
            if (String.IsNullOrEmpty(connectionString)) throw new AuctionSiteArgumentNullException("Connection strings cannot be null or empty");
            if (alarmClockFactory == null) throw new AuctionSiteArgumentNullException("Connection strings cannot be null or empty");
            
            using (var c = new AuctionSiteContext(connectionString)) {
                try {
                    if (!c.Database.CanConnect()) throw new AuctionSiteUnavailableDbException(); 
                    return new Host(connectionString, alarmClockFactory);
                } catch (SqlException e) {
                    throw new AuctionSiteUnavailableDbException("Unavailable DB", e);
                }
            }
            
        }
    }

    public class Host : IHost {
        public string ConnectionString { get; set; }
        public IAlarmClockFactory AlarmClockFactory { get; set; }

        public Host(string connectionString, IAlarmClockFactory alarmClockFactory) {
            ConnectionString = connectionString;
            AlarmClockFactory = alarmClockFactory;
        }

        private void validateSiteName(string name) {
            if (name == null)
                throw new AuctionSiteArgumentNullException("Site name cannot be null");
            if (name.Length < DomainConstraints.MinSiteName || name.Length > DomainConstraints.MaxSiteName)
                throw new AuctionSiteArgumentException("Invalid length of name");
        }

        public void CreateSite(string name, int timezone, int sessionExpirationTimeInSeconds, double minimumBidIncrement) {
            validateSiteName(name);
            if (timezone < DomainConstraints.MinTimeZone || timezone > DomainConstraints.MaxTimeZone)
                throw new AuctionSiteArgumentOutOfRangeException("Invalid value of timezone");
            if (minimumBidIncrement <= 0)
                throw new AuctionSiteArgumentOutOfRangeException("Invalid value of minimum bid increment");
            if (sessionExpirationTimeInSeconds <= 0)
                throw new AuctionSiteArgumentOutOfRangeException("Invalid value of session expiration time in seconds"); 
    
            var site = new SiteEntity(name, timezone, sessionExpirationTimeInSeconds, minimumBidIncrement); 
            
            using (var c = new AuctionSiteContext(ConnectionString)) {
                c.Sites.Add(site);
                try {
                    c.SaveChanges();
                } catch (AuctionSiteNameAlreadyInUseException e) {
                    throw new AuctionSiteNameAlreadyInUseException(name, "This name is already used for another site", e);
                }
            }
        }

        public IEnumerable<(string Name, int TimeZone)> GetSiteInfos() {
            List<SiteEntity> sites;
            using (var c = new AuctionSiteContext(ConnectionString)) {
                try {
                    sites = sites = c.Sites.ToList();
                } catch (SqlException e) {
                    throw new AuctionSiteUnavailableDbException("Unexpected error", e);
                }
            }
            foreach (var site in sites) {
                yield return (site.Name, site.Timezone);
            }
        }

        public ISite LoadSite(string name) {
            validateSiteName(name);
            using (var c = new AuctionSiteContext(ConnectionString)) {
                try {
                    var site = c.Sites.FirstOrDefault(s => s.Name == name);
                    if (site != null) return new Site(site, ConnectionString, AlarmClockFactory.InstantiateAlarmClock(site.Timezone));
                } catch (SqlException e) {
                    throw new AuctionSiteUnavailableDbException("Not available DB", e);
                }
            } 
            throw new AuctionSiteInexistentNameException($"Site called {name} is not present");
        }
    }
}
