using Microsoft.AspNetCore.Mvc;

namespace EventPlannerWebApplication.Controllers
{
    public class EventController: Controller
    {
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(string title, string timeType,
            DateTime? fixedDate, string fixedStartTime, int? fixedDuration,
            DateTime? flexibleStartDate, DateTime? flexibleEndDate)
        {
            if (timeType == "fixed")
            {
                if (!fixedDate.HasValue || string.IsNullOrEmpty(fixedStartTime) || !fixedDuration.HasValue)
                {
                    ModelState.AddModelError("", "Для фиксированного времени необходимо указать все параметры");
                    return View();
                }

                TempData["Message"] = "Событие с фиксированным временем создано!";
            }
            else
            {
                if (!flexibleStartDate.HasValue || !flexibleEndDate.HasValue)
                {
                    ModelState.AddModelError("", "Для гибкого времени необходимо указать диапазон дат");
                    return View();
                }

                if (flexibleEndDate.Value <= flexibleStartDate.Value)
                {
                    ModelState.AddModelError("", "Дата окончания должна быть позже даты начала");
                    return View();
                }

                TempData["Message"] = "Событие с гибким временем создано!";
            }

            return RedirectToAction("Menu", "Home");
        }

        [HttpGet]
        public IActionResult Join()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Join(string code, string nickname)
        {
            return RedirectToAction("SetAvailability");
        }

        [HttpGet]
        public IActionResult SetAvailability(string eventId, bool? isFixedTime = false)
        {
            ViewBag.IsFixedTime = isFixedTime ?? false;

            if (isFixedTime == true)
            {
                ViewBag.FixedStartTime = DateTime.Now.AddDays(1).AddHours(3);
                ViewBag.FixedEndTime = DateTime.Now.AddDays(1).AddHours(4);
            }

            return View();
        }

        [HttpPost]
        public IActionResult SetAvailability(DateTime startTime, DateTime endTime, bool isFixedTime, string? response)
        {
            if (isFixedTime)
            {
                if (response == "accept")
                {
                    TempData["Message"] = "Вы подтвердили участие в событии";
                }
                else if (response == "decline")
                {
                    TempData["Message"] = "Вы отказались от участия в событии";
                }

                return RedirectToAction("Menu", "Home");
            }
            else
            {
                return View();
            }
        }

        [HttpGet]
        public IActionResult Result()
        {
            return View();
        }
    }
}
