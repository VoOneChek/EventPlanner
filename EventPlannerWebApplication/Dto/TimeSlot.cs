namespace EventPlannerWebApplication.Dto
{
    public class TimeSlot
    {
        public DateTime Date { get; set; }
        public DateTime StartUtc { get; set; }
        public DateTime EndUtc { get; set; }
        public double DurationMinutes { get; set; }
    }
}
