﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Data.States;

namespace KwasantWeb.ViewModels
{   
    public class AnswerViewModel
    {
        public int Id { get; set; }        
        public int QuestionID { get; set; }       
        public int AnswerStatusID { get; set; }
        public int AnswerState { get; set; }
        public string ObjectsType { get; set; }
        public string Text { get; set; }
    }
}