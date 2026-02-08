using EventPlannerWebApplication.Data;
using EventPlannerWebApplication.Models;
using EventPlannerWebApplication.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventPlannerWebApplication.Controllers
{
    public class EventController: Controller
    {
        private readonly EventPlannerDbContext _context;
        private readonly ISchedulingService _schedulingService;

        public EventController(EventPlannerDbContext context, ISchedulingService schedulingService)
        {
            _context = context;
            _schedulingService = schedulingService;
        }

        [HttpGet]
        public IActionResult Create()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId.HasValue)
            {
                var user = _context.Users.FirstOrDefault(u => u.Id == userId);
                ViewBag.UserName = user?.Name;
            }
            return View();
        }

        [HttpPost]
        public IActionResult Create(
            string title,
            string creatorName,
            bool isFixedDate,
            DateTime? fixedStart,
            DateTime? fixedEnd,
            List<DateTime>? flexibleStart,
            List<DateTime>? flexibleEnd,
            int timezoneOffset)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(creatorName))
            {
                ModelState.AddModelError("", "Введите название события и имя");
                return View();
            }

            var ev = new Event
            {
                Title = title,
                IsFixedDate = isFixedDate,
                CreatedAt = DateTime.UtcNow,
                Status = EventStatus.Created,
                PublicCode = Guid.NewGuid().ToString("N")[..6],
                OwnerCode = Guid.NewGuid().ToString("N")[..8]
            };

            var participant = new Participant
            {
                Name = creatorName,
                Event = ev
            };

            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId.HasValue)
            {
                ev.UserId = userId.Value;
                participant.UserId = userId.Value;
            }

            if (isFixedDate)
            {
                if (!ValidateDateTimeInterval(fixedStart, fixedEnd, "", out var startUtc, out var endUtc, timezoneOffset))
                    return View();

                participant.AvailabilityIntervals.Add(new AvailabilityInterval
                {
                    StartTime = startUtc,
                    EndTime = endUtc,
                    Participant = participant
                });
                ev.Participants.Add(participant);
            }
            else
            {
                if (ValidateIntervalList(flexibleStart, flexibleEnd, out var intervals, timezoneOffset))
                {
                    foreach (var interval in intervals)
                    {
                        interval.Participant = participant;
                        participant.AvailabilityIntervals.Add(interval);
                    }
                    ev.Participants.Add(participant);
                }
                else
                {
                    return View();
                }
            }

            _context.Events.Add(ev);
            _context.SaveChanges();

            return RedirectToAction("Result", new { code = ev.OwnerCode });
        }


        public IActionResult Cancel()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId.HasValue)
            {
                return RedirectToAction("Menu", "Home");
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Join()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId != null)
            {
                var user = _context.Users.FirstOrDefault(u => u.Id == userId);
                ViewBag.UserName = user?.Name;
            }

            return View();
        }

        [HttpPost]
        public IActionResult Join(string code, string participantName)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(participantName))
            {
                ModelState.AddModelError("", "Введите код события и имя");
                return View();
            }

            var ev = _context.Events
                .Include(e => e.Participants)
                    .ThenInclude(p => p.AvailabilityIntervals)
                .FirstOrDefault(e => e.PublicCode == code || e.OwnerCode == code);

            if (ev == null)
            {
                ModelState.AddModelError("", "Событие не найдено");
                return View();
            }

            if (ev.PublicCode == code)
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId != null)
                {
                    bool exists = _context.Participants
                        .Any(p => p.EventId == ev.Id && p.UserId == userId);

                    if (exists)
                    {
                        ModelState.AddModelError("", "Вы уже участвуете в этом событии");
                        return View();
                    }
                }

                ViewBag.Event = ev;
                ViewBag.ParticipantName = participantName;

                return View("JoinEvent");
            }
            else
                return RedirectToAction("Result", new { code = ev.OwnerCode });
        }

        [HttpPost]
        public IActionResult ConfirmJoin(
            int eventId,
            string participantName,
            string actionType,
            List<DateTime>? flexibleStart,
            List<DateTime>? flexibleEnd, 
            int timezoneOffset)
        {

            var ev = _context.Events.FirstOrDefault(e => e.Id == eventId);
            if (ev == null)
                return RedirectToAction("Join");

            var participant = new Participant
            {
                Name = participantName,
                EventId = eventId
            };

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId != null)
                participant.UserId = userId.Value;

            if (ev.IsFixedDate)
            {
                participant.IsAgreed = actionType == "agree";
            }
            else
            {
                if (ValidateIntervalList(flexibleStart, flexibleEnd, out var intervals, timezoneOffset))
                {
                    foreach (var interval in intervals)
                    {
                        interval.Participant = participant;
                        participant.AvailabilityIntervals.Add(interval);
                    }
                }
                else
                {
                    ViewBag.Event = ev;
                    ViewBag.ParticipantName = participantName;
                    return View("JoinEvent");
                }
            }

            _context.Participants.Add(participant);
            _context.SaveChanges();

            TempData["JoinSuccess"] = "Ваши данные успешно сохранены!";

            return RedirectToAction("Cancel");
        }

        [HttpGet]
        public async Task<IActionResult> Result(string code)
        {
            var ev = await _context.Events
                .Include(e => e.Participants)
                .FirstOrDefaultAsync(e => e.OwnerCode == code);

            if (ev == null)
                return NotFound();

            var creator = ev.Participants.FirstOrDefault();
            ViewBag.CreatorName = creator?.Name ?? "Неизвестно";

            if (ev.Status == EventStatus.Calculated && !ev.IsFixedDate)
            {
                var bestSlots = await _schedulingService.FindBestTimeSlotsAsync(ev.Id);
                ViewBag.BestSlots = bestSlots;
            }

            return View(ev);
        }

        [HttpPost]
        public async Task<IActionResult> Calculate(string code)
        {
            var ev = await _context.Events.FirstOrDefaultAsync(e => e.OwnerCode == code);
            if (ev == null || ev.Status != EventStatus.Created)
                return NotFound();

            ev.Status = EventStatus.Calculated;
            await _context.SaveChangesAsync();

            return RedirectToAction("Result", new { code });
        }

        [HttpGet]
        public async Task<IActionResult> MyEvents()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var events = await _context.Events
                .Where(e => e.UserId == userId && e.Status != EventStatus.Closed)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            return View(events);
        }

        [HttpGet]
        public async Task<IActionResult> ClosedEvents()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var events = await _context.Events
                .Where(e => e.UserId == userId && e.Status == EventStatus.Closed)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            return View(events);
        }

        [HttpPost]
        public async Task<IActionResult> FinishEvent(string code)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            var ev = await _context.Events
                .FirstOrDefaultAsync(e => e.OwnerCode == code && e.UserId == userId);

            if (ev == null)
                return NotFound();

            ev.Status = EventStatus.Closed;
            await _context.SaveChangesAsync();

            return RedirectToAction("MyEvents");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteClosed(string code)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            var ev = await _context.Events
                .FirstOrDefaultAsync(e => e.OwnerCode == code && e.UserId == userId && e.Status == EventStatus.Closed);

            if (ev == null)
                return NotFound();

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();

            return RedirectToAction("ClosedEvents");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAllClosed()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            var events = await _context.Events
                .Where(e => e.UserId == userId && e.Status == EventStatus.Closed)
                .ToListAsync();

            _context.Events.RemoveRange(events);
            await _context.SaveChangesAsync();

            return RedirectToAction("ClosedEvents");
        }

        #region Private Validation Methods

        // Проверяет, участвует ли пользователь в событии
        private bool CheckIfUserParticipates(int eventId, int userId)
        {
            return _context.Participants.Any(p => p.EventId == eventId && p.UserId == userId);
        }

        // Проверяет один интервал дат. Возвращает false и пишет в ModelState, если ошибка.
        // Если все ок, возвращает true и конвертированные даты.
        private bool ValidateDateTimeInterval(DateTime? start, DateTime? end, string prefix, out DateTime startUtc, out DateTime endUtc, int timezoneOffset)
        {
            startUtc = default;
            endUtc = default;

            if (!start.HasValue || !end.HasValue)
            {
                ModelState.AddModelError("", $"{prefix}Укажите дату начала и конца");
                return false;
            }

            startUtc = start.Value.AddMinutes(timezoneOffset);
            startUtc = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc);

            endUtc = end.Value.AddMinutes(timezoneOffset);
            endUtc = DateTime.SpecifyKind(endUtc, DateTimeKind.Utc);

            if (startUtc <= DateTime.UtcNow)
            {
                ModelState.AddModelError("", $"{prefix}Событие не может быть в прошлом");
                return false;
            }

            if (startUtc >= endUtc)
            {
                ModelState.AddModelError("", $"{prefix}Дата начала должна быть раньше даты окончания");
                return false;
            }

            return true;
        }

        // Проверяет список интервалов. Возвращает false при ошибке.
        // При успехе возвращает список объектов AvailabilityInterval.
        private bool ValidateIntervalList(List<DateTime>? startList, List<DateTime>? endList, out List<AvailabilityInterval> validIntervals, int timezoneOffset)
        {
            validIntervals = new List<AvailabilityInterval>();

            if (startList == null || endList == null || startList.Count == 0)
            {
                ModelState.AddModelError("", "Добавьте хотя бы один интервал");
                return false;
            }

            for (int i = 0; i < startList.Count; i++)
            {
                string prefix = $"Интервал #{i + 1}: ";

                if (ValidateDateTimeInterval(startList[i], endList[i], prefix, out var startUtc, out var endUtc, timezoneOffset))
                {
                    validIntervals.Add(new AvailabilityInterval
                    {
                        StartTime = startUtc,
                        EndTime = endUtc
                    });
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}
