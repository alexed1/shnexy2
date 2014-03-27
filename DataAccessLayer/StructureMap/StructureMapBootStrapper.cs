﻿using Shnexy.DataAccessLayer.Repositories;
using Shnexy.Models;
using StructureMap;
using StructureMap.Configuration.DSL;

using Shnexy.DataAccessLayer;
using Shnexy.DataAccessLayer.Interfaces;

namespace Shnexy.DataAccessLayer.StructureMap
{
    public class StructureMapBootStrapper
    {
        #region Method

        public static void ConfigureDependencies(string type)
        {
            switch (type)
            {
                case "test":
                    ObjectFactory.Initialize(x => x.AddRegistry<TestMode>());
                    break;
                default:
                    ObjectFactory.Initialize(x => x.AddRegistry<DevMode>());
                    break;
            }
        }

        public class ShnexyCoreRegistry : Registry
        {
            public ShnexyCoreRegistry()
            {
                For<IEmail>().Use<Email>();
                For<IEvent>().Use<Event>();
                For<IEmailRepository>().Use<EmailRepository>();
                For<ICustomer>().Use<Customer>();
            }
        }

        public class DevMode : ShnexyCoreRegistry 
        {
            public DevMode()
            {
                For<IUnitOfWork>().Use(new UnitOfWork(new ShnexyDbContext()));

            }
        }

        public class TestMode : ShnexyCoreRegistry
        {
            public TestMode()
            {
                For<IUnitOfWork>().Use(new UnitOfWork(new ShnexyDbContext()));

            }
        }

        #endregion
    }
}

