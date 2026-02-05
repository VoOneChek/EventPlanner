namespace EventPlannerWebApplication.Models
{
    public class Event
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;

        public string PublicCode { get; set; } = string.Empty;
        public string OwnerCode {  get; set; } = string.Empty;

        public bool IsFixedDate { get; set; }
        public DateTime? FixedDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public EventStatus Status { get; set; }

        public ICollection<Participant> Participants { get; set; } = new List<Participant>();
        public ICollection<AvailabilityInterval> AvailabilityIntervals { get; set; } = new List<AvailabilityInterval>();
    }
}
