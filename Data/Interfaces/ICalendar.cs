﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace Data.Interfaces
{
    public interface ICalendar
    {
        [Key]
        int Id { get; set; }

        String Name { get; set; }

        UserDO Owner { get; set; }

        List<EventDO> Events { get; set; }
    }
}