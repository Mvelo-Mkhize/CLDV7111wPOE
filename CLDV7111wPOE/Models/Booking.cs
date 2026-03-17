using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CLDV7111wPOE.Models
{
    public class Booking
    {
        public int BookingId { get; set; }

        [Required]
        public int EventId { get; set; }

        [Required]
        public int VenueId { get; set; }

        public DateTime BookingDate { get; set; } = DateTime.Now;

        [Required]
        public int CreatedBy { get; set; }

        [ForeignKey("EventId")]
        [ValidateNever]
        public Event Event { get; set; }

        [ForeignKey("VenueId")]
        [ValidateNever]
        public Venue Venue { get; set; }

        [ForeignKey("CreatedBy")]
        [ValidateNever]
        public User User { get; set; }
    }
}