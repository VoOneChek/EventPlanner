namespace EventPlannerWebApplication.Models
{
    public class Participant
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool? IsAgreed { get; set; }

        public int EventId { get; set; }
        public Event Event { get; set; } = null!;

        public int? UserId { get; set; }
        public User? User { get; set; }

        public ICollection<AvailabilityInterval> AvailabilityIntervals { get; set; } = new List<AvailabilityInterval>();
    }
}
