using System;
using Microsoft.Data.SqlClient;
using Siri;
using TAP21_22_AuctionSite.Interface;

namespace Main {
    class UI {
        static void Main(string[] args) {
            try {
                using (var c =
                    new AuctionSiteContext(
                        @"Data Source=.;Initial Catalog=PROGETTO_TAP_SIRI;Integrated Security=True;")) {
                    c.Database.EnsureDeleted();
                    c.Database.EnsureCreated();
                }
            } catch (SqlException e) {
                throw new AuctionSiteUnavailableDbException("Unavailable DB", e);
            }
            

            Console.WriteLine("Hello World!");
        }
    }
}
