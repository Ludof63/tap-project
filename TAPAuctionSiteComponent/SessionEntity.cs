using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Siri;

namespace Siri {
    public class SessionEntity {
        [Key]
        public string SessionId { get; set; }
        public DateTime ValidUntil { get; set; }
        
        public int UserId { get; set; }
        public UserEntity? UserEntity { get; set; }

        public int SiteId { get; set; }
        public SiteEntity? SiteEntity { get; set; }

        public SessionEntity(string sessionId, DateTime validUntil, int userId, int siteId) {
            SessionId = sessionId;
            ValidUntil = validUntil;
            UserId = userId;
            SiteId = siteId;
        }
    }
}
