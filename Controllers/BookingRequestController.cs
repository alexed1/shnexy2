﻿using System.Net;
using System.Web.Mvc;
using System.Web.Routing;
using Data.Entities;
using Data.Interfaces;
using KwasantCore.Managers.APIManager.Packagers.DataTable;
using KwasantCore.Managers.APIManager.Packagers.Kwasant;
using KwasantCore.Services;
using StructureMap;
using Utilities;
using Utilities.Logging;
using System.Net.Mail;
using Data.Infrastructure.StructureMap;
using System;
using Data.Repositories;


namespace KwasantWeb.Controllers
{
    public class BookingRequestController : Controller
    {
        private DataTablesPackager _datatables;

        public BookingRequestController()
        {
            _datatables = new DataTablesPackager();
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
                var jsonResult = Json(_datatables.Pack(new BookingRequest().GetUnprocessed(uow.BookingRequestRepository)), JsonRequestBehavior.AllowGet);
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
            BookingRequestDO bookingRequestDO = null;
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                bookingRequestDO = uow.BookingRequestRepository.GetByKey(id);   
            }

            if (bookingRequestDO == null)
            {
                return HttpNotFound();
            }
            else
            {
                //Redirect to Calendar control to open Booking Agent UI. It takes email id as parameter to which email message will be dispalyed in the left column of Booking Agent UI
                return RedirectToAction("Index", new RouteValueDictionary(new { controller = "Calendar", action = "Index", id = id }));
            }
        }


        [HttpGet]
        public ActionResult SetStatus(int id, int status)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                BookingRequestDO bookingRequestDO = uow.BookingRequestRepository.GetByKey(id);
                bookingRequestDO.BRStateID = status;
                bookingRequestDO.User = bookingRequestDO.User;
                bookingRequestDO.BRState = bookingRequestDO.BRState; //this line makes no sense.
                uow.SaveChanges();
                return Json(new KwasantPackagedMessage { Name = "Success", Message = "Status changed successfully" }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult GetBookingRequests(int? id)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var jsonResult = Json(_datatables.Pack((new BookingRequest()).GetBookingRequests(uow.BookingRequestRepository, id.Value)), JsonRequestBehavior.AllowGet);
                jsonResult.MaxJsonLength = int.MaxValue;
                return jsonResult;
            }
        }


        //create a BookingRequest
        public ActionResult Generate(string emailAddress,string meetingInfo)
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
                    (new BookingRequest()).ProcessBookingRequest(uow, bookingRequest);
                    uow.SaveChanges();
                    result = "Thanks! We'll be emailing you a meeting request that demonstrates how convenient Kwasant can be";
	}
            }
            catch (Exception)
            {
                result = "Sorry! Something went wrong. Alpha software...";
            }
            return Content(result);
        }
	}
}