﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Data.Interfaces;

namespace Data.Entities
{
    public class EmailAddressDO : IEmailAddressDO
    {
        [Key]
        public int Id { get; set; }

        public String Name { get; set; }
        [MaxLength(256)]
        public String Address { get; set; }

        public virtual List<RecipientDO> Recipients { get; set; }

        public EmailAddressDO()
        {
            Recipients = new List<RecipientDO>();
        }


        public EmailAddressDO(string emailAddress)
        {
            Recipients = new List<RecipientDO>();
            Address = emailAddress;
        }
    }
}
