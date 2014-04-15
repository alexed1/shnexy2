﻿using System.Reflection;
using Data.DataAccessLayer.Interfaces;
using Data.DataAccessLayer.Repositories;
using Data.Models;

namespace Data.Constants
{
    public class EmailStatusConstants
    {
        public const int QUEUED = 1;
        public const int SENT = 2;
        public const int UNPROCESSED = 3;
        public const int PROCESSED = 4;
        public const int EVENT_UNSET = 5;
        public const int EVENT_SET = 6;
        
        public static void ApplySeedData(IUnitOfWork uow)
        {
            FieldInfo[] constants = typeof (EmailStatusConstants).GetFields();
            EmailStatusRepository emailStatusRepo = new EmailStatusRepository(uow);
            foreach (FieldInfo constant in constants)
            {
                string name = constant.Name;
                object value = constant.GetValue(null);
                emailStatusRepo.Add(new EmailStatusDO
                {
                    EmailStatusID = (int)value,
                    Value = name
                });
            }
        }
    }
}
