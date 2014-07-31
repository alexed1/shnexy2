﻿using System;
using System.ComponentModel.DataAnnotations;

namespace Data.Entities.Constants
{
    public class EmailStatusRow
    {
        [Key]
        public int Id { get; set; }
        public String Name { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
