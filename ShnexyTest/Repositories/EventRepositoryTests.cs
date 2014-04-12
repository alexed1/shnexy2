﻿using System.Data.Entity;
using Data.DataAccessLayer.Infrastructure;
using Data.DataAccessLayer.Interfaces;
using Data.DataAccessLayer.Repositories;
using Data.DataAccessLayer.StructureMap;
using NUnit.Framework;
using ShnexyTest.Fixtures;
using StructureMap;

namespace ShnexyTest.Repositories
{
    [TestFixture]
    public class EventRepositoryTests
    {

        public ICustomerRepository customerRepo;
        public IUnitOfWork _uow;
        private FixtureData _fixture;

        [SetUp]
        public void Setup()
        {

            Database.SetInitializer(new DropCreateDatabaseAlways<ShnexyDbContext>());
            StructureMapBootStrapper.ConfigureDependencies("test");

            _uow = ObjectFactory.GetInstance<IUnitOfWork>();
            _uow.Db.Database.Initialize(true);

            customerRepo = new CustomerRepository(_uow);
            _fixture = new FixtureData(_uow);
        }
    }
}
