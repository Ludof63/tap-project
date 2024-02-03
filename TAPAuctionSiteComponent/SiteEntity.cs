using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using TAP21_22_AuctionSite.Interface;
using Siri;

namespace Siri {
    [Index(nameof(Name), IsUnique = true, Name = "NameUnique")]
    public class SiteEntity {
        [Key]
        public int SiteId { get; set; }
        [MaxLength(DomainConstraints.MaxSiteName)]
        [MinLength(DomainConstraints.MinSiteName)]
        public string Name { get; set; }
        [Range(DomainConstraints.MinTimeZone, DomainConstraints.MaxTimeZone)] 
        public int Timezone { get; set; }
        [Range(0, int.MaxValue)]
        public int SessionExpirationInSeconds { get; set; }
        [Range(0, double.MaxValue)]
        public double MinimumBidIncrement { get; set; }
        
        //NAVIGATION
        public List<UserEntity> Users { get; set; }
        public List<AuctionEntity> Auctions { get; set; }
        public List<SessionEntity> Sessions { get; set; }


        public SiteEntity(string name, int timezone, int sessionExpirationInSeconds, double minimumBidIncrement) {
            Name = name;
            Timezone = timezone;
            SessionExpirationInSeconds = sessionExpirationInSeconds;
            MinimumBidIncrement = minimumBidIncrement;

            Users = new List<UserEntity>();
            Auctions = new List<AuctionEntity>();
            Sessions = new List<SessionEntity>();
        }
    }
}
