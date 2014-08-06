﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Data.Entities;

namespace Data.Interfaces
{
    public interface IEmail
    {
        [Key]
        int Id { get; set; }

        EmailAddressDO From { get; set; }
        String Subject { get; set; }
        String HTMLText { get; set; }
        DateTimeOffset DateReceived { get; set; }

        //IEnumerable<EmailAddress> To { get; set; }
        //IEnumerable<EmailAddress> BCC { get; set; }
        //IEnumerable<EmailAddress> CC { get; set; }
        //IEnumerable<Attachment> Attachments { get; set; }
        List<EventDO> Events { get; set; }
        DateTimeOffset DateCreated { get; set; }
        int EmailStatus { get; set; }
    }
}