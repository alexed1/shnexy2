using System;
using System.Collections.Generic;
using System.Diagnostics;
using Data.Entities;
using Data.Infrastructure;
using Data.Interfaces;
using Data.States;
using KwasantCore.Services;
using StructureMap;
using Utilities;
using Utilities.Logging;

//NOTES: Do NOT put Incidents here. Put them in IncidentReporter


namespace KwasantCore.Managers
{
    public class AlertReporter
    {
        //Register for interesting events
        public void SubscribeToAlerts()
        {
            AlertManager.AlertEmailReceived += NewEmailReceived;
            AlertManager.AlertEventBooked += NewEventBooked;
            AlertManager.AlertEmailSent += EmailDispatched;
            AlertManager.AlertBookingRequestCreated += ProcessBookingRequestCreated;
            AlertManager.AlertBookingRequestStateChange += ProcessBookingRequestStateChange;
            AlertManager.AlertExplicitCustomerCreated += NewExplicitCustomerCreated;
        
            AlertManager.AlertUserRegistration += UserRegistration;
            AlertManager.AlertBookingRequestCheckedOut += ProcessBookingRequestCheckedOut;
            AlertManager.AlertBookingRequestOwnershipChange += BookingRequestOwnershipChange;
  
            
            AlertManager.AlertPostResolutionNegotiationResponseReceived += OnPostResolutionNegotiationResponseReceived;
        }

        private static void OnPostResolutionNegotiationResponseReceived(int negotiationId)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var negotiationDO = uow.NegotiationsRepository.GetByKey(negotiationId);
                
                IConfigRepository configRepository = ObjectFactory.GetInstance<IConfigRepository>();
                string fromAddress = configRepository.Get("EmailAddress_GeneralInfo");

                const string subject = "New response to resolved negotiation request";
                const string messageTemplate = "A customer has submitted a new response to an already-resolved negotiation request ({0}). Click {1} to view the booking request.";

                var bookingRequestURL = String.Format("{0}/BookingRequest/Details/{1}", Server.ServerUrl, negotiationDO.BookingRequestID);
                var message = String.Format(messageTemplate, negotiationDO.Name, "<a href='" + bookingRequestURL + "'>here</a>");

                var toRecipient = negotiationDO.BookingRequest.Booker.EmailAddress;

                EmailDO curEmail = new EmailDO
                    {
                        Subject = subject,
                        PlainText = message,
                        HTMLText = message,
                        From = uow.EmailAddressRepository.GetOrCreateEmailAddress(fromAddress),
                        Recipients = new List<RecipientDO>()
                            {
                                new RecipientDO
                                    {
                                        EmailAddress = toRecipient,
                                        EmailParticipantType = EmailParticipantType.To
                                    }
                            }
                    };

                uow.EnvelopeRepository.ConfigurePlainEmail(curEmail);
                uow.SaveChanges();
            }
        }

        private void NewExplicitCustomerCreated(string curUserId)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                FactDO curAction = new FactDO
                    {
                        Name = "CustomerCreated",
                        PrimaryCategory = "User",
                        SecondaryCategory = "Customer",
                        Activity = "Created",
                        CustomerId = curUserId,
                        CreateDate = DateTimeOffset.Now,
                        ObjectId = 0,
                        Data = string.Format("User with email {0} created from: {1}", uow.UserRepository.GetByKey(curUserId).EmailAddress.Address, new StackTrace())
                    };
                AddFact(uow, curAction);
                uow.SaveChanges();
            }
        }

        public void NewEmailReceived(int emailId, string customerId)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                string emailSubject = uow.EmailRepository.GetByKey(emailId).Subject;
                emailSubject = emailSubject.Length <= 10 ? emailSubject : (emailSubject.Substring(0, 10) + "...");

                FactDO curAction = new FactDO()
                    {
                        Name = "EmailReceived",
                        PrimaryCategory = "Email",
                        SecondaryCategory = "Intake",
                        Activity = "Received",
                        CustomerId = customerId,
                        CreateDate = DateTimeOffset.Now,
                        ObjectId = emailId
                    };
                curAction.Data = string.Format("{0} {1} {2}: ObjectId: {3} EmailAddress: {4} Subject: {5}", curAction.PrimaryCategory, curAction.SecondaryCategory, curAction.Activity, emailId, (uow.UserRepository.GetByKey(curAction.CustomerId).EmailAddress.Address), emailSubject);

                SaveFact(curAction);
            }
        }

        public void NewEventBooked(int eventId, string customerId)
        {
            FactDO curAction = new FactDO()
                {
                    Name = "EventBooked",
                    PrimaryCategory = "Event",
                    SecondaryCategory = "",
                    Activity = "Created",
                    CustomerId = customerId,
                    CreateDate = DateTimeOffset.Now,
                    ObjectId = eventId
                };
            SaveFact(curAction);
        }
        public void EmailDispatched(int emailId, string customerId)
        {
            FactDO curAction = new FactDO()
                {
                    Name = "",
                    PrimaryCategory = "Email",
                    SecondaryCategory = "",
                    Activity = "Sent",
                    CustomerId = customerId,
                    CreateDate = DateTimeOffset.Now,
                    ObjectId = emailId
                };
            SaveFact(curAction);
        }

        public void ProcessBookingRequestCreated(int bookingRequestId) 
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var bookingRequestDO = uow.BookingRequestRepository.GetByKey(bookingRequestId);
                FactDO curAction = new FactDO()
                    {
                        Name = "",
                        PrimaryCategory = "BookingRequest",
                        SecondaryCategory = "",
                        Activity = "Created",
                        CustomerId = bookingRequestDO.UserID,
                        CreateDate = DateTimeOffset.Now,
                        ObjectId = bookingRequestId
                    };
                curAction.Data = curAction.Name + ": ID= " + curAction.ObjectId;
                AddFact(uow, curAction);
                uow.SaveChanges();
            }
        }
        public void ProcessBookingRequestStateChange(int bookingRequestId)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var bookingRequestDO = uow.BookingRequestRepository.GetByKey(bookingRequestId);
                if (bookingRequestDO == null)
                    throw new ArgumentException(string.Format("Cannot find a Booking Request by given id:{0}", bookingRequestId), "bookingRequestId");
                string status = bookingRequestDO.BookingRequestStateTemplate.Name;
                FactDO curAction = new FactDO()
                    {
                        PrimaryCategory = "BookingRequest",
                        SecondaryCategory = "None",
                        Activity = "StateChange",
                        CustomerId = bookingRequestDO.User.Id,
                        ObjectId = bookingRequestDO.Id,
                        Status = status,
                        CreateDate = DateTimeOffset.Now,
                    };
                curAction.Data = "BookingRequest ID= " + bookingRequestDO.Id;
                AddFact(uow, curAction);
                uow.SaveChanges();
                
            }
        }
        private void SaveFact(FactDO curAction)
        {
            using (IUnitOfWork uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                AddFact(uow, curAction);
                uow.SaveChanges();
            }
        }
        private void AddFact(IUnitOfWork uow, FactDO curAction)
        {
            Debug.Assert(uow != null);
            Debug.Assert(curAction != null);
            var configRepo = ObjectFactory.GetInstance<IConfigRepository>();
            if (string.IsNullOrEmpty(curAction.Data))
            {
                curAction.Data = string.Format("{0} {1} {2}:" + " ObjectId: {3} EmailAddress: {4}",
                                               curAction.PrimaryCategory,
                                               curAction.SecondaryCategory,
                                               curAction.Activity,
                                               curAction.ObjectId,
                                               uow.UserRepository.GetByKey(curAction.CustomerId).EmailAddress.Address);
            }
            if (configRepo.Get("LogLevel", String.Empty) == "Verbose")
                Logger.GetLogger().Info(curAction.Data);
            uow.FactRepository.Add(curAction);
        }



        public void UserRegistration(UserDO curUser)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                FactDO curFactDO = new FactDO
                    {
                        Name = "",
                        PrimaryCategory = "User",
                        SecondaryCategory = "",
                        Activity = "Registered",
                        CustomerId = curUser.Id,
                        CreateDate = DateTimeOffset.Now,
                        ObjectId = 0,
                        Data = "User registrated with " + curUser.EmailAddress.Address
                    };
                Logger.GetLogger().Info(curFactDO.Data);
                uow.FactRepository.Add(curFactDO);
                uow.SaveChanges();
            }

        }

       

      

        public void ProcessBookingRequestCheckedOut(int bookingRequestId, string bookerId)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var bookingRequestDO = uow.BookingRequestRepository.GetByKey(bookingRequestId);
                if (bookingRequestDO == null)
                    throw new ArgumentException(string.Format("Cannot find a Booking Request by given id:{0}", bookingRequestId), "bookingRequestId");
                string status = bookingRequestDO.BookingRequestStateTemplate.Name;
                FactDO curAction = new FactDO()
                    {
                        PrimaryCategory = "BookingRequest",
                        SecondaryCategory = "Ownership",
                        Activity = "Checkout",
                        CustomerId = bookingRequestDO.User.Id,
                        ObjectId = bookingRequestDO.Id,
                        BookerId = bookerId,
                        Status = status,
                        CreateDate = DateTimeOffset.Now,
                    };
                
                curAction.Data = string.Format("BookingRequest ID {0} Booker EmailAddress: {1}", bookingRequestDO.Id, uow.UserRepository.GetByKey(bookerId).EmailAddress.Address);
                AddFact(uow, curAction);
                uow.SaveChanges();
            }
        }

        //Do we need/use both this and the immediately preceding event? 
        public void BookingRequestOwnershipChange(int bookingRequestId, string bookerId)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var bookingRequestDO = uow.BookingRequestRepository.GetByKey(bookingRequestId);
                if (bookingRequestDO == null)
                    throw new ArgumentException(string.Format("Cannot find a Booking Request by given id:{0}", bookingRequestId), "bookingRequestId");
                string status = bookingRequestDO.BookingRequestStateTemplate.Name;
                FactDO curAction = new FactDO()
                    {
                        PrimaryCategory = "BookingRequest",
                        SecondaryCategory = "Ownership",
                        Activity = "Change",
                        CustomerId = bookingRequestDO.User.Id,
                        ObjectId = bookingRequestDO.Id,
                        BookerId = bookerId,
                        Status = status,
                        CreateDate = DateTimeOffset.Now,
                    };
                
                curAction.Data = string.Format("BookingRequest ID {0} Booker EmailAddress: {1}", bookingRequestDO.Id, uow.UserRepository.GetByKey(bookerId).EmailAddress.Address);
                AddFact(uow, curAction);
                uow.SaveChanges();

            }
        }
    }
}