using EventPlannerWebApplication.Data;
using EventPlannerWebApplication.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventPlannerWebApplication.Services
{
    public interface IEventService
    {
        Task<Event> CreateEventAsync(string title, string creatorName, bool isFixedDate,
            DateTime? fixedStart, DateTime? fixedEnd,
            List<DateTime>? flexibleStart, List<DateTime>? flexibleEnd,
            int timezoneOffset, int? userId);

        Task<Event?> GetEventByCodeAsync(string code);
        Task EnsureUserCanJoinAsync(int eventId, int? userId);
        Task ConfirmJoinAsync(int eventId, string participantName, bool isFixed, bool? isAgreed,
            List<DateTime>? flexibleStart, List<DateTime>? flexibleEnd, int timezoneOffset, int? userId);

        Task UpdateStatusAsync(int eventId, EventStatus status);
        Task<List<Event>> GetUserEventsAsync(int userId, EventStatus? statusFilter = null);
        Task DeleteEventAsync(int eventId, int userId);
    }

    public class EventService : IEventService
    {
        private readonly EventPlannerDbContext _context;
        private readonly ITimeService _timeService;

        public EventService(EventPlannerDbContext context, ITimeService timeService)
        {
            _context = context;
            _timeService = timeService;
        }

        public async Task<Event> CreateEventAsync(string title, string creatorName, bool isFixedDate,
            DateTime? fixedStart, DateTime? fixedEnd,
            List<DateTime>? flexibleStart, List<DateTime>? flexibleEnd,
            int timezoneOffset, int? userId)
        {
            var ev = new Event
            {
                Title = title,
                IsFixedDate = isFixedDate,
                CreatedAt = DateTime.UtcNow,
                Status = EventStatus.Created,
                PublicCode = Guid.NewGuid().ToString("N")[..6],
                OwnerCode = Guid.NewGuid().ToString("N")[..8]
            };

            if (userId.HasValue)
            {
                ev.UserId = userId.Value;
            }

            var participant = new Participant
            {
                Name = creatorName,
                Event = ev,
                UserId = userId
            };

            if (isFixedDate)
            {
                ValidateAndConvertInterval(fixedStart, fixedEnd, "", out var startUtc, out var endUtc, timezoneOffset);

                participant.AvailabilityIntervals.Add(new AvailabilityInterval
                {
                    StartTime = startUtc,
                    EndTime = endUtc,
                    Participant = participant
                });
            }
            else
            {
                var intervals = ValidateAndConvertIntervalList(flexibleStart, flexibleEnd, timezoneOffset);
                foreach (var interval in intervals)
                {
                    interval.Participant = participant;
                    participant.AvailabilityIntervals.Add(interval);
                }
            }

            ev.Participants.Add(participant);
            _context.Events.Add(ev);
            await _context.SaveChangesAsync();

            return ev;
        }

        public async Task<Event?> GetEventByCodeAsync(string code)
        {
            return await _context.Events
                .Include(e => e.Participants)
                    .ThenInclude(p => p.AvailabilityIntervals)
                .FirstOrDefaultAsync(e => e.PublicCode == code || e.OwnerCode == code);
        }

        public async Task EnsureUserCanJoinAsync(int eventId, int? userId)
        {
            if (userId.HasValue)
            {
                bool exists = await _context.Participants
                    .AnyAsync(p => p.EventId == eventId && p.UserId == userId);
                if (exists)
                    throw new InvalidOperationException("Вы уже участвуете в этом событии");
            }
        }

        public async Task ConfirmJoinAsync(int eventId, string participantName, bool isFixed, bool? isAgreed,
            List<DateTime>? flexibleStart, List<DateTime>? flexibleEnd, int timezoneOffset, int? userId)
        {
            var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            if (ev == null) throw new InvalidOperationException("Событие не найдено");

            var participant = new Participant
            {
                Name = participantName,
                EventId = eventId,
                UserId = userId
            };

            if (isFixed)
            {
                participant.IsAgreed = isAgreed;
            }
            else
            {
                var intervals = ValidateAndConvertIntervalList(flexibleStart, flexibleEnd, timezoneOffset);
                foreach (var interval in intervals)
                {
                    interval.Participant = participant;
                    participant.AvailabilityIntervals.Add(interval);
                }
            }

            _context.Participants.Add(participant);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateStatusAsync(int eventId, EventStatus status)
        {
            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null) throw new InvalidOperationException("Событие не найдено");

            ev.Status = status;
            await _context.SaveChangesAsync();
        }

        public async Task<List<Event>> GetUserEventsAsync(int userId, EventStatus? statusFilter = null)
        {
            var query = _context.Events.Where(e => e.UserId == userId);

            if (statusFilter.HasValue)
            {
                query = query.Where(e => e.Status == statusFilter.Value);
            }
            else
            {
                query = query.Where(e => e.Status != EventStatus.Closed);
            }

            return await query.OrderByDescending(e => e.CreatedAt).ToListAsync();
        }

        public async Task DeleteEventAsync(int eventId, int userId)
        {
            var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId && e.UserId == userId);
            if (ev == null) throw new InvalidOperationException("Событие не найдено или доступ запрещен");

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();
        }

        #region Private Validation Methods

        private void ValidateAndConvertInterval(DateTime? start, DateTime? end, string prefix,
            out DateTime startUtc, out DateTime endUtc, int timezoneOffset)
        {
            startUtc = default;
            endUtc = default;

            if (!start.HasValue || !end.HasValue)
                throw new InvalidOperationException($"{prefix}Укажите дату начала и конца");

            startUtc = _timeService.ConvertToUtc(start.Value, timezoneOffset);
            endUtc = _timeService.ConvertToUtc(end.Value, timezoneOffset);

            if (startUtc <= DateTime.UtcNow)
                throw new InvalidOperationException($"{prefix}Событие не может быть в прошлом");

            if (startUtc >= endUtc)
                throw new InvalidOperationException($"{prefix}Дата начала должна быть раньше даты окончания");
        }

        private List<AvailabilityInterval> ValidateAndConvertIntervalList(List<DateTime>? startList, List<DateTime>? endList, int timezoneOffset)
        {
            if (startList == null || endList == null || startList.Count == 0)
                throw new InvalidOperationException("Добавьте хотя бы один интервал");

            var result = new List<AvailabilityInterval>();

            for (int i = 0; i < startList.Count; i++)
            {
                string prefix = $"Интервал #{i + 1}: ";
                ValidateAndConvertInterval(startList[i], endList[i], prefix, out var startUtc, out var endUtc, timezoneOffset);

                result.Add(new AvailabilityInterval
                {
                    StartTime = startUtc,
                    EndTime = endUtc
                });
            }
            return result;
        }

        #endregion
    }
}
