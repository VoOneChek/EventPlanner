using EventPlannerWebApplication.Data;
using EventPlannerWebApplication.Dto;
using EventPlannerWebApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace EventPlannerWebApplication.Services
{
    public interface ISchedulingService
    {
        Task<List<TimeSlot>> FindBestTimeSlotsAsync(int eventId);
    }

    public class SchedulingService : ISchedulingService
    {
        private readonly EventPlannerDbContext _context;

        public SchedulingService(EventPlannerDbContext context)
        {
            _context = context;
        }

        public async Task<List<TimeSlot>> FindBestTimeSlotsAsync(int eventId)
        {
            var participantIds = await _context.Participants
                .Where(p => p.EventId == eventId)
                .Select(p => p.Id)
                .ToListAsync();

            if (participantIds.Count == 0)
                return new List<TimeSlot>();

            var intervals = await _context.AvailabilityIntervals
                .Include(ai => ai.Participant)
                .Where(ai => ai.Participant.EventId == eventId)
                .OrderBy(ai => ai.StartTime)
                .ToListAsync();

            var groupedByDate = intervals.GroupBy(x => x.StartTime.Date).ToList();

            var results = new List<TimeSlot>();

            foreach (var dayGroup in groupedByDate)
            {
                var userIntervals = dayGroup.GroupBy(x => x.ParticipantId).ToList();

                if (userIntervals.Count != participantIds.Count)
                {
                    continue;
                }

                var currentIntersection = userIntervals.First().ToList();

                for (int i = 1; i < userIntervals.Count; i++)
                {
                    var otherIntervals = userIntervals[i].ToList();

                    currentIntersection = IntersectIntervalLists(currentIntersection, otherIntervals);

                    if (!currentIntersection.Any())
                        break;
                }

                // Общие интервалы дня
                foreach (var slot in currentIntersection)
                {
                    results.Add(new TimeSlot
                    {
                        Date = dayGroup.Key,
                        StartUtc = slot.StartTime,
                        EndUtc = slot.EndTime,
                        DurationMinutes = (slot.EndTime - slot.StartTime).TotalMinutes
                    });
                }
            }

            return results.OrderByDescending(r => r.DurationMinutes).ToList();
        }

        // Пересечение между двумя списками интервалов
        private List<AvailabilityInterval> IntersectIntervalLists(List<AvailabilityInterval> listA, List<AvailabilityInterval> listB)
        {
            var result = new List<AvailabilityInterval>();
            int i = 0;
            int j = 0;

            while (i < listA.Count && j < listB.Count)
            {
                var a = listA[i];
                var b = listB[j];

                var start = a.StartTime > b.StartTime ? a.StartTime : b.StartTime;
                var end = a.EndTime < b.EndTime ? a.EndTime : b.EndTime;

                if (start < end)
                {
                    result.Add(new AvailabilityInterval { StartTime = start, EndTime = end });
                }

                if (a.EndTime < b.EndTime)
                {
                    i++; // следующий интервал списка А
                }
                else
                {
                    j++; // следующий интервал списка В
                }
            }

            return result;
        }
    }
}
