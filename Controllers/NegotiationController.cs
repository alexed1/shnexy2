﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AutoMapper;
using Data.Entities;
using Data.Interfaces;
using Data.Repositories;
using Data.States;
using Data.States.Templates;
using KwasantCore.Interfaces;
using KwasantCore.Managers;
using KwasantCore.Managers.APIManagers.Packagers.Kwasant;
using KwasantCore.Services;
using KwasantWeb.ViewModels;
using StructureMap;

namespace KwasantWeb.Controllers
{
    public class NegotiationController : Controller
    {
        Booker _booker;
        string _currBooker;
        private readonly IAttendee _attendee;
        private readonly IEmailAddress _emailAddress;
        private readonly INegotiation _negotiation;
        private readonly IAnswer _answer;
        private readonly IQuestion _question;

        public NegotiationController()
        {
            _booker = new Booker();
            _attendee = ObjectFactory.GetInstance<IAttendee>();
            _emailAddress = ObjectFactory.GetInstance<IEmailAddress>();
            _negotiation = ObjectFactory.GetInstance<INegotiation>();
            _question = ObjectFactory.GetInstance<IQuestion>();
            _answer = ObjectFactory.GetInstance<IAnswer>();
        }

        public ActionResult Edit(int negotiationID, int bookingRequestID)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                _currBooker = this.GetUserId();
                string verifyOwnership = _booker.IsBookerValid(uow, bookingRequestID, _currBooker);
                if (verifyOwnership != "valid")
                    return Json(new KwasantPackagedMessage { Name = "DifferentOwner", Message = verifyOwnership }, JsonRequestBehavior.AllowGet);

                //First - we order by start date
                Func<NegotiationAnswerVM, DateTimeOffset?> firstSort = a => a.EventStartDate;
                //Second - order by end date
                Func<NegotiationAnswerVM, DateTimeOffset?> secondSort = a => a.EventEndDate;

                var negotiationVM = GetNegotiationVM(negotiationID, firstSort, secondSort);
                return View(negotiationVM);
            }
        }

        public ActionResult Review(int negotiationID)
        {
            //The following are order delegates.
            //We always order in ascending order, so it's important to keep that in mind.

            //First - If a question is selected, it should be at the top
            Func<NegotiationAnswerVM, long> firstSort = a => a.AnswerState == AnswerState.Selected ? 0 : 1;

            //Second - We then order a question by the number of votes. Note that because we're ordering by ascending, the more votes it has, the lower the rank will be
            //So, we subtract the votes by 1, which orders in the correct direction
            Func<NegotiationAnswerVM, long> secondSort = a => 1 - a.VotedByList.Count;

            //Third - we order events by their starting date
            Func<NegotiationAnswerVM, long> thirdSort = a => (!a.EventStartDate.HasValue ? 0 : a.EventStartDate.Value.Ticks);

            var negotiationVM = GetNegotiationVM(negotiationID, firstSort, secondSort, thirdSort);
            return View(negotiationVM);
        }

        private static NegotiationVM GetNegotiationVM<T>(int negotiationID, params Func<NegotiationAnswerVM, T>[] orders)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var negotiationDO = uow.NegotiationsRepository.GetQuery().FirstOrDefault(n => n.Id == negotiationID);
                if (negotiationDO == null)
                    throw new ApplicationException("Negotiation with ID " + negotiationID + " does not exist.");

                var curVM = new NegotiationVM
                {
                    Id = negotiationDO.Id,
                    Name = negotiationDO.Name,
                    BookingRequestID = negotiationDO.BookingRequestID,
                    Attendees = negotiationDO.Attendees.Select(a => a.Name).ToList(),
                    Questions = negotiationDO.Questions.Select(q =>
                    {
                        var answers = q.Answers.Select(a =>
                            new NegotiationAnswerVM
                            {
                                Id = a.Id,
                                Text = a.Text,
                                AnswerState = a.AnswerStatus,
                                VotedByList = uow.QuestionResponseRepository.GetQuery()
                                    .Where(qr => qr.AnswerID == a.Id)
                                    .Select(qr => qr.User.FirstName + " " + qr.User.LastName)
                                    .ToList(),
                                EventID = a.EventID,
                                EventStartDate = a.Event == null ? (DateTimeOffset?)null : a.Event.StartDate,
                                EventEndDate = a.Event == null ? (DateTimeOffset?)null : a.Event.EndDate,
                            });

                        answers = orders.Aggregate(answers, (current, t) => current.OrderBy(t));

                        return new NegotiationQuestionVM
                        {
                            Type = q.AnswerType,
                            Id = q.Id,
                            CalendarID = q.CalendarID,
                            Text = q.Text,
                            Answers = answers.ToList()
                        };
                    }).ToList()
                };
                return curVM;
            }
        }


        public ActionResult Create(int bookingRequestID)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var bookingRequestDO = uow.BookingRequestRepository.GetByKey(bookingRequestID);

                var emailAddresses = _emailAddress.GetEmailAddresses(uow, bookingRequestDO.HTMLText, bookingRequestDO.PlainText, bookingRequestDO.Subject);
                emailAddresses.Add(bookingRequestDO.User.EmailAddress);

                //need to add the addresses of people cc'ed or on the To line of the BookingRequest
                emailAddresses.AddRange(bookingRequestDO.Recipients.Select(r => r.EmailAddress));

                return View("~/Views/Negotiation/Edit.cshtml", new NegotiationVM
                {
                    Name = uow.EmailRepository.GetByKey(bookingRequestID).Subject,
                    BookingRequestID = bookingRequestID,
                    Attendees = emailAddresses.Select(ea => ea.Address).ToList(),
                    Questions = new List<NegotiationQuestionVM>
                    { new NegotiationQuestionVM
                        {
                            Type = "Text"
                        }
                    }
                });
            }
        }

        [HttpPost]
        public JsonResult ProcessSubmittedForm(NegotiationVM curNegotiationVM)
        {


            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                NegotiationDO negotiationDO = _negotiation.GetOrCreate(curNegotiationVM.Id, uow);
                _negotiation.Update(uow, curNegotiationVM, negotiationDO);

                uow.SaveChanges();

                using (var subUoW = ObjectFactory.GetInstance<IUnitOfWork>())
                {
                    var communicationManager = ObjectFactory.GetInstance<CommunicationManager>();
                    communicationManager.DispatchNegotiationRequests(subUoW, negotiationDO.Id);
                    subUoW.SaveChanges();
                }

                return Json(negotiationDO.Id, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult MarkResolved(int negotiationID)
        {
            //throws exception if it fails
            _negotiation.Resolve(negotiationID);
            return Json(true, JsonRequestBehavior.AllowGet);
        }
    }
}