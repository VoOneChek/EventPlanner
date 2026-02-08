using EventPlannerWebApplication.Data;
using EventPlannerWebApplication.Models;
using EventPlannerWebApplication.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventPlannerWebApplication.Controllers
{
    public class EventController : Controller
    {
        private readonly EventPlannerDbContext _context;
        private readonly IEventService _eventService;
        private readonly ISchedulingService _schedulingService;

        public EventController(EventPlannerDbContext context, IEventService eventService, ISchedulingService schedulingService)
        {
            _context = context;
            _eventService = eventService;
            _schedulingService = schedulingService;
        }

        private void SetViewBagUserName()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId.HasValue)
            {
                var user = _context.Users.FirstOrDefault(u => u.Id == userId);
                ViewBag.UserName = user?.Name;
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            SetViewBagUserName();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            string title,
            string creatorName,
            bool isFixedDate,
            DateTime? fixedStart,
            DateTime? fixedEnd,
            List<DateTime>? flexibleStart,
            List<DateTime>? flexibleEnd,
            int timezoneOffset)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                var ev = await _eventService.CreateEventAsync(
                    title, creatorName, isFixedDate,
                    fixedStart, fixedEnd, flexibleStart, flexibleEnd,
                    timezoneOffset, userId);

                return RedirectToAction("Result", new { code = ev.OwnerCode });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                SetViewBagUserName(); // Восстанавливаем имя при ошибке
                return View();
            }
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
            SetViewBagUserName();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Join(string code, string participantName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(participantName))
                {
                    ModelState.AddModelError("", "Введите код события и имя");
                    return View();
                }

                var ev = await _eventService.GetEventByCodeAsync(code);

                if (ev == null)
                {
                    ModelState.AddModelError("", "Событие не найдено");
                    return View();
                }

                if (ev.OwnerCode == code)
                    return RedirectToAction("Result", new { code = ev.OwnerCode });

                var userId = HttpContext.Session.GetInt32("UserId");
                await _eventService.EnsureUserCanJoinAsync(ev.Id, userId);

                ViewBag.Event = ev;
                ViewBag.ParticipantName = participantName;
                return View("JoinEvent");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                SetViewBagUserName();
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmJoin(
            int eventId,
            string participantName,
            string actionType,
            List<DateTime>? flexibleStart,
            List<DateTime>? flexibleEnd,
            int timezoneOffset)
        {
            try
            {
                var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
                if (ev == null) return RedirectToAction("Join");

                var userId = HttpContext.Session.GetInt32("UserId");

                bool? isAgreed = ev.IsFixedDate ? (actionType == "agree") : (bool?)null;

                await _eventService.ConfirmJoinAsync(
                    eventId, participantName, ev.IsFixedDate, isAgreed,
                    flexibleStart, flexibleEnd, timezoneOffset, userId);

                TempData["JoinSuccess"] = "Ваши данные успешно сохранены!";
                return RedirectToAction("Cancel");
            }
            catch (InvalidOperationException ex)
            {
                // Если ошибка валидации интервалов
                var ev = await _context.Events.FindAsync(eventId);
                ViewBag.Event = ev;
                ViewBag.ParticipantName = participantName;
                ModelState.AddModelError("", ex.Message);
                return View("JoinEvent");
            }
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
            try
            {
                var ev = await _context.Events.FirstOrDefaultAsync(e => e.OwnerCode == code);
                if (ev == null || ev.Status != EventStatus.Created)
                    throw new InvalidOperationException("Событие не найдено или уже рассчитано");

                await _eventService.UpdateStatusAsync(ev.Id, EventStatus.Calculated);
                return RedirectToAction("Result", new { code });
            }
            catch (InvalidOperationException ex)
            {
                TempData["ModalError"] = ex.Message;
                return RedirectToAction("Result", new { code }); // Вернуться назад, если возможно
            }
        }

        [HttpGet]
        public async Task<IActionResult> MyEvents()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var events = await _eventService.GetUserEventsAsync(userId.Value);
            return View(events);
        }

        [HttpGet]
        public async Task<IActionResult> ClosedEvents()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var events = await _eventService.GetUserEventsAsync(userId.Value, EventStatus.Closed);
            return View(events);
        }

        [HttpPost]
        public async Task<IActionResult> FinishEvent(string code)
        {
            try
            {
                var ev = await _context.Events.FirstOrDefaultAsync(e => e.OwnerCode == code);
                if (ev == null) throw new InvalidOperationException("Событие не найдено");

                var userId = HttpContext.Session.GetInt32("UserId");
                if (ev.UserId != userId) throw new InvalidOperationException("Нет прав");

                await _eventService.UpdateStatusAsync(ev.Id, EventStatus.Closed);
                return RedirectToAction("MyEvents");
            }
            catch (InvalidOperationException ex)
            {
                TempData["ModalError"] = ex.Message;
                return RedirectToAction("MyEvents");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteClosed(string code)
        {
            try
            {
                var ev = await _context.Events.FirstOrDefaultAsync(e => e.OwnerCode == code);
                var userId = HttpContext.Session.GetInt32("UserId");
                if (ev == null || ev.UserId != userId) throw new InvalidOperationException("Нет прав");

                await _eventService.DeleteEventAsync(ev.Id, userId!.Value);
                return RedirectToAction("ClosedEvents");
            }
            catch (InvalidOperationException ex)
            {
                TempData["ModalError"] = ex.Message;
                return RedirectToAction("ClosedEvents");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAllClosed()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var events = await _eventService.GetUserEventsAsync(userId.Value, EventStatus.Closed);

            foreach (var ev in events)
            {
                await _eventService.DeleteEventAsync(ev.Id, userId.Value);
            }

            return RedirectToAction("ClosedEvents");
        }
    }
}
