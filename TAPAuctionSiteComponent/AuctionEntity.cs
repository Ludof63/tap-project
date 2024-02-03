using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TAP21_22_AuctionSite.Interface;

namespace Siri {
    public class AuctionEntity {
        [Key]
        public int AuctionId { get; set; }

        public string Description { get; set; }
        public DateTime EndsOn { get; set; }
        public double MaximumBidValue { get; set; }
        public double CurrentPrice { get; set; }

        //NAVIGATION
        public UserEntity? Owner { get; set; }
        public int OwnerId { get; set; }
        public UserEntity? Winner { get; set; }
        public int? WinnerId { get; set; }
        public SiteEntity? SiteEntity { get; set; }
        public int SiteId { get; set; }

        public AuctionEntity(string description, DateTime endsOn, double currentPrice, int ownerId, int siteId) {
            Description = description;
            EndsOn = endsOn;
            OwnerId = ownerId;
            SiteId = siteId;
            MaximumBidValue = currentPrice;
            CurrentPrice = currentPrice;
        }
    }
}
