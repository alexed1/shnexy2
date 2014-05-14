﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mail;
using Data.Entities;
using Data.Interfaces;
using Data.Repositories;
using KwasantCore.Managers.APIManager.Packagers.Twilio;
using KwasantCore.Services;
using Microsoft.WindowsAzure;
using S22.Imap;

using StructureMap;

namespace Daemons
{
    public class InboundEmail : Daemon
    {
        private bool isValid = true;
        private readonly IImapClient _client;
        private TwilioPackager _twilio;
        private static string GetIMAPServer()
        {
            return CloudConfigurationManager.GetSetting("InboundEmailHost");
        }

        private static int GetIMAPPort()
        {
            int port;
            if (int.TryParse(CloudConfigurationManager.GetSetting("InboundEmailPort"), out port))
                return port;
            throw new Exception("Invalid value for 'InboundEmailPort'");
        }

        private static string GetUserName()
        {
            return CloudConfigurationManager.GetSetting("INBOUND_EMAIL_USERNAME");
        }
        private static string GetPassword()
        {
            return CloudConfigurationManager.GetSetting("INBOUND_EMAIL_PASSWORD");
        }

        private static bool UseSSL()
        {
            bool useSSL;
            if (bool.TryParse(CloudConfigurationManager.GetSetting("InboundEmailUseSSL"), out useSSL))
                return useSSL;
            throw new Exception("Invalid value for 'InboundEmailUseSSL'");
        }

        public InboundEmail()
        {

            _twilio = new TwilioPackager();

            try
            {
                _client = new ImapClient(GetIMAPServer(), GetIMAPPort(), GetUserName(), GetPassword(), AuthMethod.Login, UseSSL());
            }
            catch (Exception ex)
            {
                //We log in the future
                isValid = false;
                throw new ApplicationException(ex.Message); //we were generating exceptions here and missing them
            }
        }

        public InboundEmail(IImapClient client)
        {
            _client = client;
        }

        public override int WaitTimeBetweenExecution
        {
            get { return (int)TimeSpan.FromSeconds(10).TotalMilliseconds; }
        }

        protected override void Run()
        {
            if (!isValid)
                return;

            IEnumerable<uint> uids = _client.Search(SearchCondition.Unseen());
            List<MailMessage> messages = _client.GetMessages(uids).ToList();
           
            //if at least 1 message received, send sms to the mainalert
            if (messages.Count >0)
                _twilio.SendSMS("+14158067915", "Inbound Email has been received");

            IUnitOfWork unitOfWork = ObjectFactory.GetInstance<IUnitOfWork>();
            EmailRepository emailRepository = new EmailRepository(unitOfWork);
            foreach (MailMessage message in messages)
            {
                BookingRequestRepository bookingRequestRepo = new BookingRequestRepository(unitOfWork);
                BookingRequestDO bookingRequest = Email.ConvertMailMessageToEmail(bookingRequestRepo, message);

                BookingRequest.ProcessBookingRequest(unitOfWork, bookingRequest);
            }
            emailRepository.UnitOfWork.SaveChanges();
        }

        protected override void CleanUp()
        {
            _client.Dispose();
        }
    }
}
