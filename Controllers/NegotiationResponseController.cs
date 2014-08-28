﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Data.Entities;
using Data.Interfaces;
using KwasantCore.Managers;
using KwasantWeb.ViewModels;
using Microsoft.AspNet.Identity;
using StructureMap;

namespace KwasantWeb.Controllers
{
    public class NegotiationResponseController : Controller
    {
        private const bool EnforceUserInAttendees = true;

        [KwasantAuthorize(Roles = "Customer")]
        public ActionResult View(int negotiationID)
        {
            ConfirmUserInAttendees(negotiationID);

            var userID = User.Identity.GetUserId();

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var negotiationDO = uow.NegotiationsRepository.GetQuery().FirstOrDefault(n => n.Id == negotiationID);
                if (negotiationDO == null)
                    throw new ApplicationException("Negotiation with ID " + negotiationID + " does not exist.");

                //Temporary for now, perhaps it can be a parameter (if they see the 'already answered page', maybe they want to force-change their answers).
                var checkAlreadyAnswered = false;

                var questionIDs = negotiationDO.Questions.Select(q => q.Id).Distinct().ToList();
                var alreadyAnsweredQuestions = uow.QuestionResponseRepository.GetQuery().Where(qr => qr.UserID == userID && questionIDs.Contains(qr.QuestionID)).ToList();
                if (checkAlreadyAnswered)
                {
                    if (alreadyAnsweredQuestions.Select(aq => aq.QuestionID).Distinct().Count() == questionIDs.Count)
                    {
                        //Answered everything already
                        return View("~/Views/ClarificationResponse/AllAnswered.cshtml");
                    }
                }


                var model = new NegotiationResponseVM
                {
                    Id = negotiationDO.Id,
                    Name = negotiationDO.Name,
                    BookingRequestID = negotiationDO.BookingRequestID,
                    State = negotiationDO.NegotiationState,

                    Attendees = negotiationDO.Attendees.Select(a => a.Name).ToList(),
                    Questions = negotiationDO.Questions.Select(q =>
                    {
                        var answeredQuestions = alreadyAnsweredQuestions.Where(aq => aq.QuestionID == q.Id).ToList();
                        var linkedCalendar = answeredQuestions.Select(aq => aq.Calendar).FirstOrDefault();
                        return (NegotiationQuestionVM) new NegotiationResponseQuestionVM
                        {
                            AnswerType = q.AnswerType,
                            Id = q.Id,
                            Status = q.QuestionStatus,
                            Text = q.Text,
                            
                            //The following three properties let us setup the page with their existing answers, if they've already provided some answers
                            //It also doubles as selecting the first answer if no previous answer was picked (having a radio button unchecked by default is ugly)
                            SelectedAnswerID =
                                //If we have a selected answer, use that
                                answeredQuestions.Select(aq => aq.AnswerID).FirstOrDefault() ??

                                //Otherwise, if we have no entered text, and no calendar
                                (String.IsNullOrEmpty(answeredQuestions.Select(aq => aq.Text).FirstOrDefault()) &&
                                 (linkedCalendar != null)
                                
                                    //Then use the first answer
                                    ? q.Answers.Select(a => a.Id).FirstOrDefault() 
                                
                                    //Otherwise, since we have selected text or a calendar, we use null (no answer ID selected)
                                    : (int?) null),

                            SelectedCalendarID = linkedCalendar == null ? (int?)null : linkedCalendar.Id,
                            SelectedText = answeredQuestions.Select(aq => aq.Text).FirstOrDefault(),

                            NegotiationId = negotiationDO.Id,

                            CalendarEvents = linkedCalendar == null
                                ? new List<QuestionCalendarEventVM>()
                                : linkedCalendar.Events.Select(e => new QuestionCalendarEventVM
                                {
                                    StartDate = e.StartDate,
                                    EndDate = e.EndDate
                                }).ToList(),

                            CalendarID = q.CalendarID,
                            Answers = q.Answers.Select(a =>
                                (NegotiationAnswerVM) new NegotiationResponseAnswerVM
                                {
                                    Status = a.AnswerStatus,
                                    Id = a.Id,
                                    QuestionId = q.Id,
                                    Text = a.Text,
                                    CalendarEvents = q.Calendar == null
                                        ? new List<QuestionCalendarEventVM>()
                                        : q.Calendar.Events.Select(e => new QuestionCalendarEventVM
                                        {
                                            StartDate = e.StartDate,
                                            EndDate = e.EndDate
                                        }).ToList(),
                                }).ToList()
                        };
                    }).ToList()
                };

                return View(model);
            }
        }

        [KwasantAuthorize(Roles = "Customer")]
        [HttpPost]
        public ActionResult ProcessResponse(NegotiationResponsePostData value)
        {
            ConfirmUserInAttendees(value.NegotiationID);

            var userID = User.Identity.GetUserId();

            if (value.Responses != null)
            {
                using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
                {
                    foreach (var response in value.Responses)
                    {
                        var questionResponse = uow.QuestionResponseRepository.GetQuery().FirstOrDefault(qr => qr.QuestionID == response.QuestionID);
                        if (questionResponse == null)
                        {
                            questionResponse = new QuestionResponseDO();
                            uow.QuestionResponseRepository.Add(questionResponse);
                        }

                        questionResponse.QuestionID = response.QuestionID;
                        questionResponse.AnswerID = response.AnswerID;
                        questionResponse.CalendarID = response.CalendarID;
                        questionResponse.Text = response.Response;
                        questionResponse.UserID = userID;
                    }
                    uow.SaveChanges();
                }
            }

            return View();
        }

        public void ConfirmUserInAttendees(int negotiationID)
        {
            if (!EnforceUserInAttendees)
                return;

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var negotiationDO = uow.NegotiationsRepository.GetByKey(negotiationID);
                if(negotiationDO == null)
                    throw new HttpException(404, "Negotiation not found.");

                var attendees = negotiationDO.Attendees;
                var currentUserID = User.Identity.GetUserId();

                var existingUserDO = uow.UserRepository.GetQuery().Where(u => u.Id == currentUserID).FirstOrDefault();
                if (existingUserDO == null)
                    throw new HttpException(404, "User not found.");

                var currentUserEmail = existingUserDO.EmailAddress.Address.ToLower();

                foreach(var attendee in attendees)
                    if (attendee.EmailAddress.Address.ToLower() == currentUserEmail)
                        return;

                throw new HttpException(404, "Negotiation not found.");
            }
        }

        public class NegotiationResponsePostData
        {
            public int NegotiationID { get; set; }
            public List<NegotiationQuestionAnswerPair> Responses { get; set; }
        }

        public class NegotiationQuestionAnswerPair
        {
            public int QuestionID { get; set; }
            public int? AnswerID { get; set; }
            public int? CalendarID { get; set; }
            public String Response { get; set; }
        }
	}
}