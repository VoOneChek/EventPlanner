namespace EventPlannerWebApplication.Models
{
    public class AvailabilityInterval
    {
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public int ParticipantId { get; set; }
        public Participant Participant { get; set; } = null!;
    }
}
