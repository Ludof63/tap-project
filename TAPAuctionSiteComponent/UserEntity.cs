using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using TAP21_22_AuctionSite.Interface;
using Siri;

namespace Siri { 
    [Index(nameof(Username), nameof(SiteId), IsUnique = true, Name = "UsernameUnique")] 
    public class UserEntity {
        [Key]
        public int UserId { get; set; }
        [MinLength(DomainConstraints.MinUserName)]
        [MaxLength(DomainConstraints.MaxUserName)]
        [Required]
        public string Username { get; set; }
        public string Password { get; set; } //HASH DELLA PASSWORD

        //NAVIGATION

        public SiteEntity? SiteEntity { get; set; }
        public int SiteId { get; set; }
        public List<AuctionEntity> AuctionSeller { get; set; } 
        public List<AuctionEntity> AuctionBidder { get; set; }
        
        public List<SessionEntity> Sessions { get; set; }

        public UserEntity(string username, string password, int siteId) {
            Username = username;
            Password = password;
            SiteId = siteId;
            AuctionBidder = new List<AuctionEntity>();
            AuctionSeller = new List<AuctionEntity>();
            Sessions = new List<SessionEntity>();
        }
    }
}
