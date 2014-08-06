﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Data.Interfaces;

namespace Data.Entities
{
    public class AttendeeDO : IAttendee
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("Event")]
        public int EventID { get; set; }

        public String Name { get; set; }

        [ForeignKey("EmailAddress")]
        public int EmailAddressID { get; set; }
        public virtual EmailAddressDO EmailAddress { get; set; }

        public virtual EventDO Event { get; set; }

        //[ForeignKey("Negotiation")]
        //public int? NegotiationID { get; set; }
        //public virtual NegotiationDO Negotiation { get; set; }
    }
}
