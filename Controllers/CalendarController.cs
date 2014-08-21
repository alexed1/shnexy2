﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Data.Entities;
using Data.Interfaces;
using Data.Repositories;
using KwasantCore.Managers;
using KwasantCore.Services;
using KwasantWeb.Controllers.External.DayPilot;
using KwasantWeb.Controllers.External.DayPilot.Providers;
using KwasantWeb.ViewModels;
using StructureMap;

namespace KwasantWeb.Controllers
{
    [HandleError]
    [KwasantAuthorize(Roles = "Admin")]
    public class CalendarController : Controller
    {

        #region "Action"

        public ActionResult Index(int id = 0)
        {
            if (id <= 0)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                IBookingRequestRepository bookingRequestRepository = uow.BookingRequestRepository;
                var bookingRequestDO = bookingRequestRepository.GetByKey(id);

                if (bookingRequestDO == null)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

                var linkedNegotiationID = bookingRequestDO.Negotiations.Select(n => (int?)n.Id).FirstOrDefault();

                return View(new CalendarViewModel
                {
                    LinkedNegotiationID = linkedNegotiationID,

                    BookingRequestID = bookingRequestDO.Id,
                    LinkedCalendarIDs = bookingRequestDO.Calendars.Select(calendarDO => calendarDO.Id).ToList(),

                    //In the future, we won't need this - the 'main' calendar will be picked by the booker
                    ActiveCalendarID = bookingRequestDO.Calendars.Select(calendarDO => calendarDO.Id).FirstOrDefault()
                });
            }
        }

        public ActionResult GetSpecificCalendar(int calendarID)
        {
            if (calendarID <= 0)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var calendarRepository = uow.CalendarRepository;
                var calendarDO = calendarRepository.GetByKey(calendarID);
                if (calendarDO == null)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

                return View("~/Views/Negotiation/EventWindows.cshtml", new EventWindowViewModel
                {
                    ActiveCalendarID = calendarID
                });
            }
        }

        public ActionResult GetNegotiationCalendars(int calendarID)
        {
            if (calendarID <= 0)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var calendarRepository = uow.CalendarRepository;
                var calendarDO = calendarRepository.GetByKey(calendarID);
                if (calendarDO == null)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

                IEnumerable<CalendarDO> calendarsViaNegotiationRequest;
                if (calendarDO.Negotiation != null)
                {
                    calendarsViaNegotiationRequest = calendarDO.Negotiation.Calendars;
                    var recipientAddresses =
                        calendarDO.Negotiation.BookingRequest.Recipients.Select(r => r.EmailAddress)
                            //Get email addresses for each recipient
                            .SelectMany(a => uow.UserRepository.GetOrCreateUser(a).Calendars).ToList();
                    
                    //Grab the user from the email and find their calendars
                    calendarsViaNegotiationRequest = calendarsViaNegotiationRequest.Union(recipientAddresses);
                }
                else
                {
                    calendarsViaNegotiationRequest = new List<CalendarDO>();
                }

                return View("~/Views/Negotiation/EventWindows.cshtml", new EventWindowViewModel
                {
                    LinkedCalendarIDs = calendarsViaNegotiationRequest.Select(c => c.Id).Union(new[] { calendarID }).Distinct().ToList(),
                    ActiveCalendarID = calendarID
                });
            }
        }

        #endregion "Action"

        #region "DayPilot-Related Methods"
        public ActionResult Day(string calendarIDs)
        {
            var ids = calendarIDs.Split(',').Where(c => !String.IsNullOrEmpty(c)).Select(int.Parse).ToArray();
            return new KwasantCalendarController(new EventDataProvider(true, ids)).CallBack(this);
        }

        public ActionResult Month(string calendarIDs)
        {
            var ids = calendarIDs.Split(',').Where(c => !String.IsNullOrEmpty(c)).Select(int.Parse).ToArray();
            return new KwasantMonthController(new EventDataProvider(true, ids)).CallBack(this);
        }

        public ActionResult Navigator(string calendarIDs)
        {
            var ids = calendarIDs.Split(',').Where(c => !String.IsNullOrEmpty(c)).Select(int.Parse).ToArray();
            return new KwasantNavigatorControl(new EventDataProvider(true, ids)).CallBack(this);
        }

        //public ActionResult GetEvents(string calendarIDs)
        //{
        //    var ids = calendarIDs.Split(',').Select(int.Parse).ToArray();
        //    return new KwasantCalendarController(new EventDataProvider(true, ids)).GetEvents();
        //}

        public ActionResult Rtl()
        {
            return View();
        }
        public ActionResult Columns50()
        {
            return View();
        }
        public ActionResult Height100Pct()
        {
            return View();
        }

        public ActionResult Notify()
        {
            return View();
        }

        public ActionResult Crosshair()
        {
            return View();
        }

        public ActionResult ThemeBlue()
        {
            return View();
        }

        public ActionResult ThemeGreen()
        {
            return View();
        }

        public ActionResult ThemeWhite()
        {
            return View();
        }

        public ActionResult ThemeTraditional()
        {
            return View();
        }

        public ActionResult ThemeTransparent()
        {
            return View();
        }

        public ActionResult TimeHeaderCellDuration()
        {
            return View();
        }

        public ActionResult ActiveAreas()
        {
            return View();
        }

        public ActionResult JQuery()
        {
            return View();
        }

        public ActionResult HeaderAutoFit()
        {
            return View();
        }

        public ActionResult ExternalDragDrop()
        {
            return View();
        }

        public ActionResult EventArrangement()
        {
            return View();
        }

        public ActionResult AutoRefresh()
        {
            return View();
        }

        public ActionResult Today()
        {
            return View();
        }

        public ActionResult DaysResources()
        {
            return View();
        }

        public ActionResult Resources()
        {
            return View();
        }

        public ActionResult ContextMenu()
        {
            return View();
        }

        public ActionResult Message()
        {
            return View();
        }

        public ActionResult DayRange()
        {
            return View();
        }

        public ActionResult EventSelecting()
        {
            return View();
        }

        public ActionResult AutoHide()
        {
            return View();
        }

        public ActionResult GoogleLike()
        {
            return View();
        }

        public ActionResult RecurringEvents()
        {
            return View();
        }

        public ActionResult ThemeSilver()
        {
            return RedirectToAction("ThemeTraditional");
        }

        public ActionResult ThemeGreenWithBar()
        {
            return RedirectToAction("ThemeGreen");
        }

        public ActionResult Outlook2000()
        {
            return RedirectToAction("ThemeTraditional");
        }

        #endregion "DayPilot-Related Methods"

        #region "Quick Copy Methods"
        [HttpGet]
        public ActionResult ProcessQuickCopy(string copyType,string selectedText)
        {
            string value = (new Calendar()).ProcessQuickCopy(copyType, selectedText);
            string status = "valid";
            if (value == "Invalid Selection") { status = "invalid"; }
            var jsonResult = Json(new { status = status, value = value, copytype = copyType }, JsonRequestBehavior.AllowGet);
            return jsonResult;
        }
        #endregion "Quick Copy Methods"
    }
}
