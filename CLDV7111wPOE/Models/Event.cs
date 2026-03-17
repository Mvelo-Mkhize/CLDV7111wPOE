using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CLDV7111wPOE.Models
{
    public class Event
    {
        public int EventId { get; set; }

        [Required]
        public string EventName { get; set; }

        [Required]
        public DateTime EventDate { get; set; }

        public string Description { get; set; }

        public int VenueId { get; set; }

        [ForeignKey("VenueId")]
        public Venue Venue { get; set; }

        public int ExpectedAttendees { get; set; }

        public string OrganizerName { get; set; }

        public string OrganizerContact { get; set; }

        public Booking Booking { get; set; }
    }
}