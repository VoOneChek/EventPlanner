namespace EventPlannerWebApplication.Models
{
    public class Participant
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public int EventId { get; set; }
        public Event Event { get; set; } = null!;

        public ICollection<AvailabilityInterval> AvailabilityIntervals { get; set; } = new List<AvailabilityInterval>();
    }
}
