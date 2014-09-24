﻿using System.Web.Mvc;
using Data.Validations;
using FluentValidation;
using Data.Entities;
using Data.Interfaces;
using KwasantWeb.ViewModels;
using Microsoft.AspNet.Identity;
using StructureMap;
using KwasantCore.Services;
using System.Net.Mail;
using Utilities;
using Utilities.Logging;
using System;

namespace KwasantWeb.Controllers
{

    public class HomeController : Controller
    {
        public ActionResult Index(string emailAddress)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                UserDO userDO;
                if (!String.IsNullOrEmpty(emailAddress))
                {
                    var user = new User();
                    var emailAddressDO = uow.EmailAddressRepository.GetOrCreateEmailAddress(emailAddress);
                    userDO = user.GetOrCreateFromBR(uow, emailAddressDO);
                    
                    //Save incase we created..
                    uow.SaveChanges();
                }
                else
                {
                    var userID = System.Web.HttpContext.Current.User.Identity.GetUserId();
                    userDO = uow.UserRepository.GetByKey(userID);
                }

                var returnVM = new HomeVM {SegmentWriteKey = new ConfigRepository().Get("SegmentWriteKey")};

                if (userDO != null)
                {
                    if (String.IsNullOrEmpty(userDO.FirstName))
                        returnVM.UserName = userDO.LastName;
                    else if (!String.IsNullOrEmpty(userDO.LastName))
                        returnVM.UserName = userDO.FirstName + " " + userDO.LastName;
                    else
                        returnVM.UserName = userDO.FirstName;

                    returnVM.UserID = userDO.Id;
                    returnVM.UserEmail = userDO.EmailAddress.Address;
                }

                return View(returnVM);
            }
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }


        //Validate emailAddress and meetingInfo then call Generate() parameterized method in BookingRequest controller
        public ActionResult ProcessHomePageBookingRequest(string emailAddress, string meetingInfo)
        {
            string result = "";
            try
            {
                var emailAddressDO = new EmailAddressDO(emailAddress);

                EmailAddressValidator emailAddressValidator = new EmailAddressValidator();
                emailAddressValidator.ValidateAndThrow(emailAddressDO);

                if (meetingInfo.Trim().Length < 30)
                    return new JsonResult() { Data = new { Message = "Meeting information must have at least 30 characters" } };
               
                return RedirectToAction("Generate", "BookingRequest", new { emailAddress = emailAddress, meetingInfo = meetingInfo });
            }
            catch (ValidationException ex)
            {
                return new JsonResult() { Data = new { Message = "You need to provide a valid Email Address.", JsonRequestBehavior = JsonRequestBehavior.AllowGet } };
            }
            catch (Exception ex)
            {
                Logger.GetLogger().Error("Error processing a home page try it out form schedule me", ex);
                return new JsonResult() { Data = new { Message = "Something went wrong. Sorry about that", JsonRequestBehavior = JsonRequestBehavior.AllowGet } };
                
            }
        }


        
        //  EmailAddress  is valid then send mail .    
        // return "success" or  error 
        public ActionResult ProcessSubmittedEmail(string name, string emailId, string message)
        {
            string result = "";
            try
            {
                EmailAddressDO emailAddressDO = new EmailAddressDO(emailId);

                EmailAddressValidator emailAddressValidator = new EmailAddressValidator();
                emailAddressValidator.ValidateAndThrow(emailAddressDO);

                EmailAddress emailAddress = new EmailAddress();

                using (IUnitOfWork uow = ObjectFactory.GetInstance<IUnitOfWork>())
                {
                    Email email = new Email(uow);
                    emailAddress.ConvertFromMailAddress(uow, new MailAddress(emailId, name));
                    string toRecipient ="info@kwasant.com";
                    string fromAddress =emailId;
                  
                   // EmailDO emailDO = email.GenerateBasicMessage(emailAddressDO, message);
                    string subject = "Customer query";
                    EmailDO emailDO = email.GenerateBasicMessage(emailAddressDO, subject, message, fromAddress, toRecipient);
                    email.Send(emailDO);
                    uow.SaveChanges();
                }
                result = "success";
            }
            catch (ValidationException ex)
            {
                result = "You need to provide a valid Email Address.";
            }
            catch (System.Exception ex)
            {
                result = "Something went wrong with our effort to send this message. Sorry! Please try emailing your message directly to info@kwasant.com";
                Logger.GetLogger().Error("Error processing a home page email form submission.", ex);
            }
            return Content(result);
        }
    }
}