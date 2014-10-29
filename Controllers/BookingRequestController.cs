﻿using System.Net;
using System.Web.Mvc;
using Data.Entities;
using Data.Interfaces;
using Data.States;
using KwasantCore.Interfaces;
using KwasantCore.Managers;
using KwasantCore.Managers.APIManagers.Packagers.Kwasant;
using KwasantCore.Services;
using KwasantWeb.ViewModels;
using KwasantWeb.ViewModels.JsonConverters;
using StructureMap;
using System.Net.Mail;
using System;
using Data.Repositories;
using Data.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using Utilities;

namespace KwasantWeb.Controllers
{
    [KwasantAuthorize(Roles = "Booker")]
    public class BookingRequestController : Controller
    {
       // private DataTablesPackager _datatables;
        private BookingRequest _br;
        private int recordcount;
        Booker _booker;
        private JsonPackager _jsonPackager;
        public BookingRequestController()
        {
           // _datatables = new DataTablesPackager();
            _br = new BookingRequest();
            _booker = new Booker();
            _jsonPackager = new JsonPackager();
        }

        // GET: /BookingRequest/
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult ShowUnprocessed()
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
               // var jsonResult = Json(_datatables.Pack(_br.GetUnprocessed(uow)), JsonRequestBehavior.AllowGet);
                var unprocessedBRs = _br.GetUnprocessed(uow);
                var jsonResult = Json(_jsonPackager.Pack(unprocessedBRs), JsonRequestBehavior.AllowGet);
                jsonResult.MaxJsonLength = int.MaxValue;
                return jsonResult;
            }
        }

        // GET: /BookingRequest/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var currBooker = this.GetUserId();

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var bookingRequestDO = uow.BookingRequestRepository.GetByKey(id);
                if (bookingRequestDO == null)
                    return HttpNotFound();
                bookingRequestDO.State = BookingRequestState.Booking;
                bookingRequestDO.BookerID = currBooker;
                bookingRequestDO.LastUpdated = DateTimeOffset.Now;
                uow.SaveChanges();
                AlertManager.BookingRequestCheckedOut(bookingRequestDO.Id, currBooker);

                return RedirectToAction("Index", "Dashboard", new { id });
            }
        }

        [HttpGet]
        public ActionResult ProcessOwnerChange(int bookingRequestId)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var currBooker = this.GetUserId();
                string result = _booker.ChangeOwner(uow, bookingRequestId, currBooker);
                return Content(result);
            }
        }

        [HttpGet]
        public ActionResult MarkAsProcessed(int id)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                //call to VerifyOwnership 
                var currBooker = this.GetUserId();
                string verifyOwnership = _booker.IsBookerValid(uow, id, currBooker);
                if (verifyOwnership != "valid")
                    return Json(new KwasantPackagedMessage { Name = "DifferentOwner", Message = verifyOwnership }, JsonRequestBehavior.AllowGet);

                BookingRequestDO bookingRequestDO = uow.BookingRequestRepository.GetByKey(id);
                bookingRequestDO.State = BookingRequestState.Resolved;
                uow.SaveChanges();
                AlertManager.BookingRequestStateChange(bookingRequestDO.Id);

                return Json(new KwasantPackagedMessage { Name = "Success", Message = "Status changed successfully" }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult Invalidate(int id)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                //call to VerifyOwnership
                var currBooker = this.GetUserId();
                string verifyOwnership = _booker.IsBookerValid(uow, id, currBooker);
                if (verifyOwnership != "valid")
                    return Json(new KwasantPackagedMessage { Name = "DifferentOwner", Message = verifyOwnership }, JsonRequestBehavior.AllowGet);

                BookingRequestDO bookingRequestDO = uow.BookingRequestRepository.GetByKey(id);
                bookingRequestDO.State = BookingRequestState.Invalid;
                uow.SaveChanges();
                AlertManager.BookingRequestStateChange(bookingRequestDO.Id);
                return Json(new KwasantPackagedMessage { Name = "Success", Message = "Status changed successfully" }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult GetBookingRequests(int? bookingRequestId, int draw, int start, int length)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                string userId = _br.GetUserId(uow.BookingRequestRepository, bookingRequestId.Value);
                int recordcount = _br.GetBookingRequestsCount(uow.BookingRequestRepository, userId);
                var jsonResult = Json(new
                {
                    draw = draw,
                    recordsTotal = recordcount,
                    recordsFiltered = recordcount,
                    data = _jsonPackager.Pack(_br.GetAllByUserId(uow.BookingRequestRepository, start, length, userId))
                }, JsonRequestBehavior.AllowGet);

                jsonResult.MaxJsonLength = int.MaxValue;
                return jsonResult;
            }
        }


        [AllowAnonymous]
        public ActionResult Generate(string emailAddress, string meetingInfo)
        {
            string result = "";
            try
            {
                using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
                {
                    MailMessage message = new MailMessage();
                    message.From = new MailAddress(emailAddress);
                    BookingRequestRepository bookingRequestRepo = uow.BookingRequestRepository;
                    BookingRequestDO bookingRequest = Email.ConvertMailMessageToEmail(bookingRequestRepo, message);
                    bookingRequest.DateReceived = DateTime.Now;
                    bookingRequest.PlainText = meetingInfo;
                    _br.Process(uow, bookingRequest);

                    uow.SaveChanges();

                    ObjectFactory.GetInstance<ITracker>().Track(bookingRequest.User, "SiteActivity", "SubmitsViaTryItOut", new Dictionary<string, object> { { "BookingRequestID", bookingRequest.Id } });

                    return new JsonResult() { Data = new { Message = "Thanks! We'll be emailing you a meeting request that demonstrates how convenient Kwasant can be", UserID = bookingRequest.UserID }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
                }
            }
            catch (Exception e)
            {
                return new JsonResult() { Data = new { Message = "Sorry! Something went wrong. Alpha software..." }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
            }

        }

        // GET: /RelatedItems 
        [HttpGet]
        public ActionResult ShowRelatedItems(int bookingRequestId, int draw, int start, int length)
        {
            List<RelatedItemShowVM> obj = new List<RelatedItemShowVM>();
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var jsonResult = Json(new
                {
                    draw = draw,
                    data = _jsonPackager.Pack(BuildRelatedItemsJSON(uow, bookingRequestId, start, length)),
                    recordsTotal = recordcount,
                    recordsFiltered = recordcount,

                }, JsonRequestBehavior.AllowGet);
                jsonResult.MaxJsonLength = int.MaxValue;
                return jsonResult;
            }
        }

        public List<RelatedItemShowVM> BuildRelatedItemsJSON(IUnitOfWork uow, int bookingRequestId, int start, int length)
        {
            List<RelatedItemShowVM> bR_RelatedItems = _br
                .GetRelatedItems(uow, bookingRequestId)
                .Select(AutoMapper.Mapper.Map<RelatedItemShowVM>)
                .ToList();

            recordcount = bR_RelatedItems.Count;
            return bR_RelatedItems.OrderByDescending(x => x.Date).Skip(start).Take(length).ToList();
        }

        [HttpPost]
        public void ReleaseBooker(int bookingRequestId)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                BookingRequestDO bookingRequestDO = uow.BookingRequestRepository.GetByKey(bookingRequestId);
                bookingRequestDO.State = BookingRequestState.Unstarted;
                bookingRequestDO.BookerID = null;
                bookingRequestDO.User = bookingRequestDO.User;
                uow.SaveChanges();
            }
        }

        public ActionResult ShowBRSOwnedByBooker()
        {
            return View("ShowMyBRs");
        }


       //Get all checkout BR's owned by the logged
        public ActionResult GetBRSOwnedByBooker()
        {
            var curBooker = this.GetUserId();
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                //var jsonResult = Json(_datatables.Pack(_br.GetCheckOutBookingRequest(uow, curBooker)), JsonRequestBehavior.AllowGet);
                var bookerOwnedRequests = _br.GetCheckOutBookingRequest(uow, curBooker);
                var jsonResult = Json(_jsonPackager.Pack(bookerOwnedRequests), JsonRequestBehavior.AllowGet);
                jsonResult.MaxJsonLength = int.MaxValue;
                return jsonResult;
            }
        }

        public ActionResult ShowInProcessBRS()
        {
            return View("ShowInProcessBRs");
        }


       //Get  BR's that are currently checked out
        public ActionResult GetInProcessBRS()
        {    
            string curBooker="";
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                //var jsonResult = Json(_datatables.Pack(_br.GetCheckOutBookingRequest(uow, curBooker)), JsonRequestBehavior.AllowGet);
                var inProcessBRs = _br.GetCheckOutBookingRequest(uow, curBooker);
                var jsonResult = Json(_jsonPackager.Pack(inProcessBRs), JsonRequestBehavior.AllowGet);
                jsonResult.MaxJsonLength = int.MaxValue;
                return jsonResult;
            }
        }

        // GET: /Conversation Members
        [HttpGet]
        public ActionResult ShowConversation(int bookingRequestId, int? curEmailId)
        {

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                BookingRequestConversationVM bookingRequestConversation = new BookingRequestConversationVM
                {
                    FromAddress = uow.EmailRepository.GetQuery().Where(e => e.ConversationId == bookingRequestId).Select(e => e.From.Address).ToList(),
                    DateReceived = uow.EmailRepository.GetQuery().Where(e => e.ConversationId == bookingRequestId).ToList().Select(e => e.DateReceived.ToString("MMM dd") + _br.getCountDaysAgo(e.DateReceived)).ToList(),
                    ConversationMembers = uow.EmailRepository.GetQuery().Where(e => e.ConversationId == bookingRequestId).Select(e => e.Id).ToList(),
                    HTMLText = uow.EmailRepository.GetQuery().Where(e => e.ConversationId == bookingRequestId).Select(e => e.HTMLText).ToList(),
                    CurEmailId = curEmailId
                };

                return View(bookingRequestConversation);
            }
        }

        // GET: /BookingRequest/
        public ActionResult ShowAllBookingRequests()
        {
            return View();
        }

        [HttpGet]
        public ActionResult GetAllBookingRequests()
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var jsonResult = Json(_jsonPackager.Pack(_br.GetAllBookingRequests(uow)), JsonRequestBehavior.AllowGet);
                jsonResult.MaxJsonLength = int.MaxValue;
                return jsonResult;
            }
        }

    }
}