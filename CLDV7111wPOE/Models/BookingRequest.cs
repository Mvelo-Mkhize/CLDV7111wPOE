using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CLDV7111wPOE.Models
{
    public class BookingRequest
    {
        [Key]
        public int RequestId { get; set; }

        [Required(ErrorMessage = "Event name is required")]
        [Display(Name = "Event Name")]
        public string EventName { get; set; }

        [Required(ErrorMessage = "Event date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Event Date")]
        public DateTime EventDate { get; set; }

        [Display(Name = "Expected Attendees")]
        [Range(1, int.MaxValue, ErrorMessage = "Expected attendees must be at least 1")]
        public int? ExpectedAttendees { get; set; } // Nullable int to make it optional

        [Display(Name = "Request Date")]
        public DateTime RequestDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Please select a venue")]
        public int? VenueId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Pending";

        [ForeignKey("CustomerId")]
        [ValidateNever]
        public User Customer { get; set; }

        [ForeignKey("VenueId")]
        [ValidateNever]
        public Venue Venue { get; set; }
    }
}