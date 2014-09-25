﻿using System.Collections.Generic;
using Data.Entities;
using Data.Interfaces;
using Data.Repositories;
using Data.States;
using FluentValidation;
using KwasantCore.Services;
using KwasantCore.StructureMap;
using KwasantTest.Fixtures;
using NUnit.Framework;
using StructureMap;

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
        public void CanConstructEmailWithEmailDO()
        {
            //SETUP  
            EmailDO _curEmailDO = _fixture.TestEmail1();
           
            //EXECUTE
            _uow.EnvelopeRepository.ConfigurePlainEmail(_curEmailDO);
            _uow.SaveChanges();

            //VERIFY
            var envelope = _uow.EnvelopeRepository.FindOne(e => e.Email.Id == _curEmailDO.Id);
            Assert.NotNull(envelope, "Envelope was not created.");
            Assert.AreEqual(envelope.Handler, EnvelopeDO.GmailHander, "Envelope handler should be Gmail");
            Assert.AreEqual(EmailState.Queued, _curEmailDO.EmailStatus);
        }

        [Test]
        [Category("Email")]
        public void CanSendTemplateEmail()
        {
            // SETUP
            EmailDO _curEmailDO = _fixture.TestEmail1();
            const string templateName = "test_template";
            
            // EXECUTE
            _uow.EnvelopeRepository.ConfigureTemplatedEmail(_curEmailDO,
                                                            templateName,
                                                            new Dictionary<string, string>()
                                                                {{"test_key", "test_value"}});

            // VERIFY
            var envelope = _uow.EnvelopeRepository.FindOne(e => e.Email.Id == _curEmailDO.Id);
            Assert.NotNull(envelope, "Envelope was not created.");
            Assert.AreEqual(envelope.TemplateName, templateName);
            Assert.AreEqual(envelope.Handler, EnvelopeDO.MandrillHander, "Envelope handler should be Mandrill");
        }

        [Test]
        [Category("Email")]
        public void FailsToSendInvalidEmail()
        {
            // SETUP
            EmailDO curEmailDO = _fixture.TestEmail1();
            curEmailDO.Subject = "";

            // EXECUTE

            // VERIFY
            Assert.Throws<ValidationException>(() => { _uow.EnvelopeRepository.ConfigurePlainEmail(curEmailDO); _uow.SaveChanges(); }, "Email should fail to be sent as it is invalid.");
        }

        [Test]
        [Category("Email")]
        public void FailsToSendInvalidTemplatedEmail()
        {
            // SETUP
            EmailDO curEmailDO = _fixture.TestEmail1();
            curEmailDO.Subject = "";

            // EXECUTE

            // VERIFY
            Assert.Throws<ValidationException>(() => _uow.EnvelopeRepository.ConfigureTemplatedEmail(curEmailDO, "test_template", null), "Email should fail to be sent as it is invalid.");
        }
    }
}
