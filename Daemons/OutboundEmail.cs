﻿using System;
using System.Linq;
using Daemons.EventExposers;
using Data.Entities;
using Data.Entities.Enumerations;
using Data.Interfaces;
using Data.Repositories;
using KwasantCore.Managers.APIManager.Packagers;
using KwasantCore.Managers.APIManager.Packagers.Mandrill;
using KwasantCore.Managers.APIManager.Packagers;
using KwasantCore.Services;
using StructureMap;
using Utilities.Logging;

namespace Daemons
{
    public class OutboundEmail : Daemon
    {

        private string logString;
        public OutboundEmail()
        {
            RegisterEvent<string, int>(MandrillPackagerEventHandler.EmailSent, (id, emailID) =>
            {
                IUnitOfWork unitOfWork = ObjectFactory.GetInstance<IUnitOfWork>();
                EmailRepository emailRepository = unitOfWork.EmailRepository;
                var emailToUpdate = emailRepository.GetQuery().FirstOrDefault(e => e.Id == emailID);
                if (emailToUpdate == null)
                {
                    Logger.GetLogger().Error("Email id " + emailID + " recieved a callback saying it was sent from Mandrill, but the email was not found in our database");
                    return;
                }

                emailToUpdate.EmailStatus = EmailStatus.SENT;
                unitOfWork.SaveChanges();
            });

            RegisterEvent<string, string, int>(MandrillPackagerEventHandler.EmailRejected, (id, reason, emailID) =>
            {
                IUnitOfWork unitOfWork = ObjectFactory.GetInstance<IUnitOfWork>();
                EmailRepository emailRepository = unitOfWork.EmailRepository;
                var emailToUpdate = emailRepository.GetQuery().FirstOrDefault(e => e.Id == emailID);
                if (emailToUpdate == null)
                {
                    Logger.GetLogger().Error("Email id " + emailID + " recieved a callback saying it was rejected from Mandrill, but the email was not found in our database");
                    return;
                }

                Logger.GetLogger().Error(String.Format("Email was rejected with id '{0}'. Reason: {1}", emailID, reason));

                emailToUpdate.EmailStatus = EmailStatus.SEND_REJECTED;
                unitOfWork.SaveChanges();
            });

            RegisterEvent<int, string, string, int>(MandrillPackagerEventHandler.EmailCriticalError, (errorCode, name, message, emailID) =>
            {
                IUnitOfWork unitOfWork = ObjectFactory.GetInstance<IUnitOfWork>();
                EmailRepository emailRepository = unitOfWork.EmailRepository;
                var emailToUpdate = emailRepository.GetQuery().FirstOrDefault(e => e.Id == emailID);
                if (emailToUpdate == null)
                {
                    Logger.GetLogger().Error("Email id " + emailID + " recieved a callback saying it recieved a critical error from Mandrill, but the email was not found in our database");
                    return;
                }

                Logger.GetLogger().Error(String.Format("Email failed. Error code: {0}. Name: {1}. Message: {2}. EmailID: {3}", errorCode, name, message, emailID));

                emailToUpdate.EmailStatus = EmailStatus.SEND_CRITICAL_ERROR;
                unitOfWork.SaveChanges();
            });
        }

        public override int WaitTimeBetweenExecution
        {
            get { return (int)TimeSpan.FromSeconds(10).TotalMilliseconds; }
        }

        protected override void Run()
        {
            while (ProcessNextEventNoWait()) { }
            using (IUnitOfWork unitOfWork = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                EnvelopeRepository envelopeRepository = unitOfWork.EnvelopeRepository;
                var numSent = 0;
                foreach (EnvelopeDO envelope in envelopeRepository.FindList(e => e.Email.EmailStatus == EmailStatus.QUEUED))
                {
                    try
                    {
                        IEmailPackager packager = ObjectFactory.GetNamedInstance<IEmailPackager>(envelope.Handler);
                        packager.Send(envelope);
                    numSent++;
                }
                    catch (StructureMapConfigurationException ex)
                    {
                        Logger.GetLogger().ErrorFormat("Unknown email packager: {0}", envelope.Handler);
                        throw new UnknownEmailPackagerException(string.Format("Unknown email packager: {0}", envelope.Handler), ex);
                    }
                }
                unitOfWork.SaveChanges();


                if (numSent == 0)
                {
                    logString = "nothing sent";
            }
                else
                {
                    logString = "Emails sent:" + numSent;
        }
                    
                Logger.GetLogger().Info(logString);
    }
}
    }
}
