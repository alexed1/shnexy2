﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using Data.Entities;
using Data.Entities.Enumerations;
using Data.Infrastructure;
using Data.Interfaces;
using KwasantICS.DDay.iCal;
using KwasantICS.DDay.iCal.DataTypes;
using KwasantICS.DDay.iCal.Serialization.iCalendar.Serializers;
using RazorEngine;
using StructureMap;
using UtilitiesLib;
using Encoding = System.Text.Encoding;

namespace KwasantCore.Services
{
    public class Event
    {
        private readonly IUnitOfWork _uow;
        public Event()
        {
            IUnitOfWork uow = ObjectFactory.GetInstance<IUnitOfWork>();
            _uow = uow; //clean this up finish de-static work
        }


        public  EventDO Create (int bookingRequestID, string start, string end)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var startDate = DateTime.Parse(start);
                var endDate = DateTime.Parse(end);

                var isAllDay = startDate.Equals(startDate.Date) && startDate.AddDays(1).Equals(endDate);
                var bookingRequestDO = uow.BookingRequestRepository.GetByKey(bookingRequestID);

                //BookingRequests don't actually have "recipients". We are actually the sole recipient of a BookingRequest.
                //A BookingRequest should not really be thought of as an Email. It should be thought of as a service request from a customer that shares a lot of data properties with our Email object
                //Later in the process, an Event may be created that corresponds to this BookingRequest, and that Event may have Attendees, and may generate
                //outbound Emails that have Recipients.
                ///WRONG: Attendees = String.Join(",", bookingRequestDO.Recipients.Select(eea => eea.EmailAddress.Address).Distinct()),
                //initially, the only attendee is the user who created the booking request

                var curEventDO = new EventDO();


                curEventDO.IsAllDay = isAllDay;
                curEventDO.StartDate = startDate;
                curEventDO.EndDate = endDate;
                curEventDO.BookingRequestID = bookingRequestDO.Id;
                curEventDO.CreatedBy = bookingRequestDO.User;
                curEventDO = AddAttendee(bookingRequestDO.User, curEventDO);
                return curEventDO;

            }
        }



        public EventDO AddAttendee(UserDO curUserDO, EventDO curEvent)
        {
            var curAttendee = new Attendee();
            var curAttendeeDO = curAttendee.Create(curUserDO);
            curEvent.Attendees.Add(curAttendeeDO);
            return curEvent;
        }

        //Processes the incoming attendee information, which is currently just a comma delimited string
        public void ManageAttendeeList(IUnitOfWork uow, EventDO eventDO, string curAttendees)
        {

            IUnitOfWork uow2 = ObjectFactory.GetInstance<IUnitOfWork>();

            var attendeesSet = curAttendees.Split(',').ToList();

            List<AttendeeDO> eventAttendees = eventDO.Attendees ?? new List<AttendeeDO>();
            var attendeesToDelete = eventAttendees.Where(attendeeDO => !attendeesSet.Contains(attendeeDO.EmailAddress.Address)).ToList();
            foreach (var attendeeToDelete in attendeesToDelete)
                uow2.AttendeeRepository.Remove(attendeeToDelete);

            foreach ( string attendeeString in attendeesSet.Where(attString => !eventAttendees.Select(a => a.EmailAddress.Address).Contains(attString)))
            {
                var attendee = new Attendee();
                AttendeeDO curAttendee = attendee.Create(attendeeString, eventDO);     
                eventDO.Attendees.Add(curAttendee);
            }

            uow2.SaveChanges();
            
        }


        public void Dispatch(EventDO eventDO)
        {
            var emailAddressRepository = _uow.EmailAddressRepository;

            if (eventDO.Attendees == null)
                eventDO.Attendees = new List<AttendeeDO>();

            string fromEmail = ConfigRepository.Get("fromEmail");
            string fromName = ConfigRepository.Get("fromName");

            EmailDO outboundEmail = new EmailDO();
            var fromEmailAddr = emailAddressRepository.GetOrCreateEmailAddress(fromEmail);
            fromEmailAddr.Name = fromName;

            outboundEmail.From = fromEmailAddr;
            foreach (var attendeeDO in eventDO.Attendees)
            {
                var toEmailAddress = emailAddressRepository.GetOrCreateEmailAddress(attendeeDO.EmailAddress.Address);
                toEmailAddress.Name = attendeeDO.Name;
                outboundEmail.AddEmailRecipient(EmailParticipantType.TO, toEmailAddress);
            }
            outboundEmail.Subject = String.Format(ConfigRepository.Get("emailSubject"), eventDO.Summary, eventDO.StartDate);

            var parsedHTMLEmail = Razor.Parse(Properties.Resources.HTMLEventInvitation, new RazorViewModel(eventDO));
            var parsedPlainEmail = Razor.Parse(Properties.Resources.PlainEventInvitation, new RazorViewModel(eventDO));

            outboundEmail.HTMLText = parsedHTMLEmail;
            outboundEmail.PlainText = parsedPlainEmail;

            outboundEmail.Status = EmailStatus.QUEUED;

            iCalendar ddayCalendar = new iCalendar();
            DDayEvent dDayEvent = new DDayEvent();
            if (eventDO.IsAllDay)
            {
                dDayEvent.IsAllDay = true;
            }
            else
            {
                dDayEvent.DTStart = new iCalDateTime(eventDO.StartDate);
                dDayEvent.DTEnd = new iCalDateTime(eventDO.EndDate);
            }
            dDayEvent.DTStamp = new iCalDateTime(DateTime.Now);
            dDayEvent.LastModified = new iCalDateTime(DateTime.Now);

            dDayEvent.Location = eventDO.Location;
            dDayEvent.Description = eventDO.Description;
            dDayEvent.Summary = eventDO.Summary;
            foreach (AttendeeDO attendee in eventDO.Attendees)
            {
                dDayEvent.Attendees.Add(new KwasantICS.DDay.iCal.DataTypes.Attendee()
                {
                    CommonName = attendee.Name,
                    Type = "INDIVIDUAL",
                    Role = "REQ-PARTICIPANT",
                    ParticipationStatus = ParticipationStatus.NeedsAction,
                    RSVP = true,
                    Value = new Uri("mailto:" + attendee.EmailAddress),
                });
                attendee.Event = eventDO;
            }
            dDayEvent.Organizer = new Organizer(fromEmail) { CommonName = fromName };

            ddayCalendar.Events.Add(dDayEvent);
            ddayCalendar.Method = CalendarMethods.Request;

            AttachCalendarToEmail(ddayCalendar, outboundEmail);

            if (eventDO.Emails == null)
                eventDO.Emails = new List<EmailDO>();
            eventDO.Emails.Add(outboundEmail);

            new Email(_uow, outboundEmail).Send();
        }

        private static void AttachCalendarToEmail(iCalendar iCal, EmailDO emailDO)
        {
            iCalendarSerializer serializer = new iCalendarSerializer(iCal);
            string fileToAttach = serializer.Serialize(iCal);

            AttachmentDO attachmentDO = GetAttachment(fileToAttach);

            if (emailDO.Attachments == null)
                emailDO.Attachments = new List<AttachmentDO>();

            attachmentDO.Email = emailDO;
            emailDO.Attachments.Add(attachmentDO);
        }

        private static AttachmentDO GetAttachment(string fileToAttach)
        {
            return Email.CreateNewAttachment(
                new System.Net.Mail.Attachment(
                    new MemoryStream(Encoding.UTF8.GetBytes(fileToAttach)),
                    new ContentType { MediaType = "application/ics", Name = "invite.ics" }
                    ) { TransferEncoding = TransferEncoding.Base64 });
        }
        
    }

    public class RazorViewModel
    {
        public bool IsAllDay { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public String Summary { get; set; }
        public String Description { get; set; }
        public String Location { get; set; }
        public List<RazorAttendeeViewModel> Attendees { get; set; }

        public RazorViewModel(EventDO ev)
        {
            IsAllDay = ev.IsAllDay;
            StartDate = ev.StartDate;
            EndDate = ev.EndDate;
            Summary = ev.Summary;
            Description = ev.Description;
            Location = ev.Location;
            Attendees = ev.Attendees.Select(a => new RazorAttendeeViewModel { Name = a.Name, EmailAddress = a.EmailAddress.Address}).ToList();
        }

        public class RazorAttendeeViewModel
        {
            public String EmailAddress { get; set; }
            public String Name { get; set; }
        }
    }
}
