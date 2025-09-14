using System;
using System.ComponentModel.DataAnnotations;

namespace LovelyFish.API.Server.Models
{
    public class FishOwner
    {
        [Key]
        public int OwnerID { get; set; }         // key id
        public string UserName { get; set; }     // name
        public string? Phone { get; set; }       // phone, can be null
        public string? Email { get; set; }       // email
        public string? Location { get; set; }    // location
        public string? FishName { get; set; }    // fish name
        public bool IsContactPublic { get; set; } // contact public or not

        public string UserId { get; set; } = "";
        public ApplicationUser? User { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  // creat date
    }
}
