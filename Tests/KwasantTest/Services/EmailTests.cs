﻿using System;
using Data.Entities;
using Data.Interfaces;
using Data.Repositories;
using FluentValidation;
using KwasantCore.Managers.APIManager.Packagers;
using KwasantCore.Services;
using KwasantCore.StructureMap;
using KwasantTest.Fixtures;
using Moq;
using NUnit.Framework;
using StructureMap;
using Utilities;

namespace KwasantTest.Services
{
    [TestFixture]
    public class EmailTests
    {
        private IUnitOfWork _uow;
        private BookingRequestRepository _bookingRequestRepo;
        private FixtureData _fixture;
        private EventDO _curEventDO;

        [SetUp]
        public void Setup()
        {
            StructureMapBootStrapper.ConfigureDependencies(StructureMapBootStrapper.DependencyType.TEST);
            _uow = ObjectFactory.GetInstance<IUnitOfWork>();

            //_bookingRequestRepo = new BookingRequestRepository(_uow);
            _fixture = new FixtureData();
            _curEventDO = new EventDO();
        }

       

        [Test]
        [Category("Email")]
        public void CanCreateStandardInviteEmail()
        {
            //SETUP  
            _curEventDO = _fixture.TestEvent2();
            string expectedSubject = "Invitation via Kwasant: " + _curEventDO.Summary + "@ " + _curEventDO.StartDate;
           
            
            //EXECUTE
            var curEmail = new Email(_uow, _curEventDO);
            curEmail.CreateStandardInviteEmail(_curEventDO);
            //VERIFY
            //Assert.AreEqual(_curEmailDO.Subject,  expectedSubject);

        }

        [Test]
        [Category("Email")]
        public void CreateSIE_FailsIfInvalidEventInput()
        {
            //SETUP
            string expectedSubject = "Invitation via Kwasant: " + _curEventDO.Summary + "@ " + _curEventDO.StartDate;
            
            Assert.Throws<ValidationException>(() =>
            {

                new Email(_uow, _curEventDO);

            });
            //EXECUTE

            //VERIFY


        }

        [Test]
        [Category("Email")]
        public void CanSendSIE()
        {
            //SETUP  
            _curEventDO = _fixture.TestEvent2();
            string expectedSubject = "Invitation via Kwasant: " + _curEventDO.Summary + "@ " + _curEventDO.StartDate;

            //EXECUTE
            _uow.EventRepository.Add(_curEventDO);
            _uow.SaveChanges();

            var email = new Email(_uow, _curEventDO);
            email.Send();

            //VERIFY
            var envelope = _uow.EnvelopeRepository.FindOne(e => e.Email.Subject == expectedSubject);
            Assert.NotNull(envelope, "Envelope was not created.");
            Assert.Contains(_curEventDO, envelope.Email.Events, "Envelope doesn't contain the event.");
        }

        [Test]
        [Category("Email")]
        public void CanConstructEmailWithEmailDO()
        {
            //SETUP  
            EmailDO _curEmailDO = _fixture.TestEmail1();
            
            //EXECUTE
            _uow.EmailRepository.Add(_curEmailDO);
            _uow.SaveChanges();

            Email curEmail = new Email(_uow, _curEmailDO);
            curEmail.Send();

            //VERIFY
            var envelope = _uow.EnvelopeRepository.FindOne(e => e.Email.Id == _curEmailDO.Id);
            Assert.NotNull(envelope, "Envelope was not created.");
        }
   
       
    }
}
