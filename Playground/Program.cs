﻿using System.Data.Entity;
using System.Net.Mail;
using Daemons;
using Data.Infrastructure;
using KwasantCore.Services;
using KwasantCore.StructureMap;
using Shnexy.Controllers;
using StructureMap;

namespace Playground
{
    class Program
    {
        /// <summary>
        /// This is a sandbox for devs to use. Useful for directly calling some library without needing to launch the main application
        /// </summary>
        /// <param name="args"></param>
        private static void Main(string[] args)
        {
            StructureMapBootStrapper.ConfigureDependencies("test"); //set to either "test" or "dev"
            
            Database.SetInitializer(new ShnexyInitializer());
            ShnexyDbContext db = new ShnexyDbContext();
            db.Database.Initialize(true);

        }
    }
}
