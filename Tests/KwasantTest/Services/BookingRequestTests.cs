using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mail;
using Daemons;
using Data.Entities;
using Data.Infrastructure;
using Data.Interfaces;
using Data.Repositories;
using Data.States;
using KwasantCore.Services;
using KwasantCore.StructureMap;
using KwasantTest.Daemons;
using KwasantTest.Fixtures;
using Moq;
using NUnit.Framework;
using StructureMap;
using Utilities;

namespace KwasantTest.Services
{
    [TestFixture]
    public class BookingRequestManagerTests
    {
        public IUnitOfWork _uow;
        private FixtureData _fixture;
        private IConfigRepository _configRepository;

        [SetUp]
        public void Setup()
        {
            StructureMapBootStrapper.ConfigureDependencies(StructureMapBootStrapper.DependencyType.TEST);
            _uow = ObjectFactory.GetInstance<IUnitOfWork>();
            var configRepositoryMock = new Mock<IConfigRepository>();
            configRepositoryMock
                .Setup(c => c.Get<string>(It.IsAny<string>()))
                .Returns<string>(key =>
                {
                    switch (key)
                    {
                        case "MaxBRIdle":
                            return "1";
                        default:
                            return new ConfigRepository().Get<string>(key);
                    }
                });
            _configRepository = configRepositoryMock.Object;
            ObjectFactory.Configure(cfg => cfg.For<IConfigRepository>().Use(_configRepository));

            _fixture = new FixtureData();
        }

        private void AddTestRequestData()
        {
            MailMessage message = new MailMessage(new MailAddress("customer@gmail.com", "Mister Customer"), new MailAddress("kwa@sant.com", "Bookit Services")) { };

            BookingRequestRepository bookingRequestRepo = _uow.BookingRequestRepository;
            BookingRequestDO bookingRequest = Email.ConvertMailMessageToEmail(bookingRequestRepo, message);
            (new BookingRequest()).Process(_uow, bookingRequest);
        }



       

        


        [Test]
        [Category("BRM")]
        public void NewCustomerCreated()
        {
            AlertReporter curAnalyticsManager = new AlertReporter();
            curAnalyticsManager.SubscribeToAlerts();

            List<UserDO> customersNow = _uow.UserRepository.GetAll().ToList();
            Assert.AreEqual(0, customersNow.Count);

            MailMessage message = new MailMessage(new MailAddress("customer@gmail.com", "Mister Customer"), new MailAddress("kwa@sant.com", "Booqit Services"))
            {
            };

            BookingRequestRepository bookingRequestRepo = _uow.BookingRequestRepository;
            BookingRequestDO bookingRequest = Email.ConvertMailMessageToEmail(bookingRequestRepo, message);
            (new BookingRequest()).Process(_uow, bookingRequest);

            _uow.SaveChanges();

            customersNow = _uow.UserRepository.GetAll().ToList();

            Assert.AreEqual(1, customersNow.Count);
            Assert.AreEqual("customer@gmail.com", customersNow.First().EmailAddress.Address);
            Assert.AreEqual("Mister Customer", customersNow.First().FirstName);
            //test analytics system

            FactDO curAction = _uow.FactRepository.FindOne(k => k.ObjectId == bookingRequest.Id);
            Assert.NotNull(curAction);
        }

        [Test]
        [Category("BRM")]
        public void ExistingCustomerNotCreatedButUsed()
        {
            List<UserDO> customersNow = _uow.UserRepository.GetAll().ToList();
            Assert.AreEqual(0, customersNow.Count);

            UserDO user = _fixture.TestUser1();
            _uow.UserRepository.Add(user);
            _uow.SaveChanges();

            customersNow = _uow.UserRepository.GetAll().ToList();
            Assert.AreEqual(1, customersNow.Count);

            MailMessage message = new MailMessage(new MailAddress(user.EmailAddress.Address, user.FirstName), new MailAddress("kwa@sant.com", "Booqit Services"))
            {
            };

            BookingRequestRepository bookingRequestRepo = _uow.BookingRequestRepository;
            BookingRequestDO bookingRequest = Email.ConvertMailMessageToEmail(bookingRequestRepo, message);
            (new BookingRequest()).Process(_uow, bookingRequest);

            customersNow = _uow.UserRepository.GetAll().ToList();
            Assert.AreEqual(1, customersNow.Count);
            Assert.AreEqual(user.EmailAddress, customersNow.First().EmailAddress);
            Assert.AreEqual(user.FirstName, customersNow.First().FirstName);
        }

        [Test]
        [Category("BRM")]
        public void ParseAllDay()
        {

            MailMessage message = new MailMessage(new MailAddress("customer@gmail.com", "Mister Customer"), new MailAddress("kwa@sant.com", "Booqit Services"))
            {
                Body = "CCADE",
            };

            BookingRequestRepository bookingRequestRepo = _uow.BookingRequestRepository;
            BookingRequestDO bookingRequest = Email.ConvertMailMessageToEmail(bookingRequestRepo, message);
            (new BookingRequest()).Process(_uow, bookingRequest);

            bookingRequest = bookingRequestRepo.GetAll().ToList().First();
            Assert.AreEqual(1, bookingRequest.Instructions.Count);
            Assert.AreEqual(InstructionConstants.EventDuration.MarkAsAllDayEvent, bookingRequest.Instructions.First().Id);
        }

        [Test]
        [Category("BRM")]
        public void Parse30MinsInAdvance()
        {

            MailMessage message = new MailMessage(new MailAddress("customer@gmail.com", "Mister Customer"), new MailAddress("kwa@sant.com", "Booqit Services"))
            {
                Body = "cc30",
            };

            BookingRequestRepository bookingRequestRepo = _uow.BookingRequestRepository;
            BookingRequestDO bookingRequest = Email.ConvertMailMessageToEmail(bookingRequestRepo, message);
            (new BookingRequest()).Process(_uow, bookingRequest);

            bookingRequest = bookingRequestRepo.GetAll().ToList().First();
            Assert.AreEqual(1, bookingRequest.Instructions.Count);
            Assert.AreEqual(InstructionConstants.TravelTime.Add30MinutesTravelTime, bookingRequest.Instructions.First().Id);
        }

        [Test]
        [Category("BRM")]
        public void Parse60MinsInAdvance()
        {

            MailMessage message = new MailMessage(new MailAddress("customer@gmail.com", "Mister Customer"), new MailAddress("kwa@sant.com", "Booqit Services"))
            {
                Body = "cc60",
            };

            BookingRequestRepository bookingRequestRepo = _uow.BookingRequestRepository;
            BookingRequestDO bookingRequest = Email.ConvertMailMessageToEmail(bookingRequestRepo, message);
            (new BookingRequest()).Process(_uow, bookingRequest);

            bookingRequest = bookingRequestRepo.GetAll().ToList().First();
            Assert.AreEqual(1, bookingRequest.Instructions.Count);
            Assert.AreEqual(InstructionConstants.TravelTime.Add60MinutesTravelTime, bookingRequest.Instructions.First().Id);
        }

        [Test]
        [Category("BRM")]
        public void Parse90MinsInAdvance()
        {

            MailMessage message = new MailMessage(new MailAddress("customer@gmail.com", "Mister Customer"), new MailAddress("kwa@sant.com", "Booqit Services"))
            {
                Body = "cc90",
            };

            BookingRequestRepository bookingRequestRepo = _uow.BookingRequestRepository;
            BookingRequestDO bookingRequest = Email.ConvertMailMessageToEmail(bookingRequestRepo, message);
            (new BookingRequest()).Process(_uow, bookingRequest);

            bookingRequest = bookingRequestRepo.GetAll().ToList().First();
            Assert.AreEqual(1, bookingRequest.Instructions.Count);
            Assert.AreEqual(InstructionConstants.TravelTime.Add90MinutesTravelTime, bookingRequest.Instructions.First().Id);
        }

        [Test]
        [Category("BRM")]
        public void Parse120MinsInAdvance()
        {

            MailMessage message = new MailMessage(new MailAddress("customer@gmail.com", "Mister Customer"), new MailAddress("kwa@sant.com", "Booqit Services"))
            {
                Body = "cc120",
            };

            BookingRequestRepository bookingRequestRepo = _uow.BookingRequestRepository;
            BookingRequestDO bookingRequest = Email.ConvertMailMessageToEmail(bookingRequestRepo, message);
            (new BookingRequest()).Process(_uow, bookingRequest);

            bookingRequest = bookingRequestRepo.GetAll().ToList().First();
            Assert.AreEqual(1, bookingRequest.Instructions.Count);
            Assert.AreEqual(InstructionConstants.TravelTime.Add120MinutesTravelTime, bookingRequest.Instructions.First().Id);
        }

        [Test]
        [Category("BRM")]
        public void ShowUnprocessedRequestTest()
        {
            object requests = (new BookingRequest()).GetUnprocessed(_uow);
            object requestNow = _uow.BookingRequestRepository.GetAll().Where(e => e.BookingRequestState == BookingRequestState.Unprocessed).OrderByDescending(e => e.Id).Select(e => new { request = e, body = e.HTMLText.Trim().Length > 400 ? e.HTMLText.Trim().Substring(0, 400) : e.HTMLText.Trim() }).ToList();

            Assert.AreEqual(requestNow, requests);
    }

        [Test]
        [Category("BRM")]
        public void SetStatusTest()
        {
            MailMessage message = new MailMessage(new MailAddress("customer@gmail.com", "Mister Customer"), new MailAddress("kwa@sant.com", "Bookit Services")){};

            BookingRequestRepository bookingRequestRepo = _uow.BookingRequestRepository;
            BookingRequestDO bookingRequest = Email.ConvertMailMessageToEmail(bookingRequestRepo, message);
            (new BookingRequest()).Process(_uow, bookingRequest);
            bookingRequest.BookingRequestState = BookingRequestState.Invalid;
            _uow.SaveChanges();

            IEnumerable<BookingRequestDO> requestNow = _uow.BookingRequestRepository.GetAll().ToList().Where(e => e.BookingRequestState == BookingRequestState.Invalid);
            Assert.AreEqual(1, requestNow.Count());
}

        [Test]
        [Category("BRM")]
        public void GetBookingRequestsTest()
        {
            AddTestRequestData();
            List<Object> requests = (new BookingRequest()).GetAllByUserId(_uow.BookingRequestRepository, 0, 10, _uow.BookingRequestRepository.GetAll().FirstOrDefault().User.Id);
            Assert.AreEqual(1, requests.Count);
        }

        //This test takes too long see. KW-340. Temporarily ignoring it.
        [Test]
        [Category("BRM"), Ignore]
        public void TimeOutStaleBRTest()
        {
            var timeOut = TimeSpan.FromSeconds(65);
            Stopwatch staleBRDuration = new Stopwatch();

            MailMessage message = new MailMessage(new MailAddress("customer@gmail.com", "Mister Customer"), new MailAddress("kwa@sant.com", "Bookit Services")) { };
            BookingRequestRepository bookingRequestRepo = _uow.BookingRequestRepository;
            BookingRequestDO bookingRequest = Email.ConvertMailMessageToEmail(bookingRequestRepo, message);
            (new BookingRequest()).Process(_uow, bookingRequest);

            bookingRequest.BookingRequestState = BookingRequestState.CheckedOut;
            bookingRequest.BookerId = bookingRequest.User.Id;
            bookingRequest.LastUpdated = DateTimeOffset.Now;
            _uow.SaveChanges();

            staleBRDuration.Start();
            do
            {
                var om = new OperationsMonitor();
                DaemonTests.RunDaemonOnce(om);

            } while (staleBRDuration.Elapsed < timeOut);
            staleBRDuration.Stop();


            IEnumerable<BookingRequestDO> requestNow = _uow.BookingRequestRepository.GetAll().ToList().Where(e => e.BookingRequestState == BookingRequestState.Unprocessed);
            Assert.AreEqual(1, requestNow.Count());

        }

    }
}
