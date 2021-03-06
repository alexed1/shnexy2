using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using KwasantICS.DDay.iCal.DataTypes;
using KwasantICS.DDay.iCal.Evaluation;
using KwasantICS.DDay.iCal.Interfaces.Components;
using KwasantICS.DDay.iCal.Interfaces.DataTypes;
using KwasantICS.DDay.iCal.Interfaces.Evaluation;
using KwasantICS.DDay.iCal.Structs;
using KwasantICS.DDay.iCal.Utility;

namespace KwasantICS.DDay.iCal
{    
    /// <summary>
    /// A class that contains time zone information, and is usually accessed
    /// from an iCalendar object using the <see cref="Kwaant.iCalendareZone"/> method.        
    /// </summary>
#if !SILVERLIGHT
    [Serializable]
#endif
    public class iCalTimeZoneInfo : 
        CalendarComponent,
        ITimeZoneInfo
    {
        #region Private Fields

        TimeZoneInfoEvaluator m_Evaluator;
        DateTime m_End;

        #endregion

        #region Constructors

        public iCalTimeZoneInfo() : base()
        {
            // FIXME: how do we ensure SEQUENCE doesn't get serialized?
            //base.Sequence = null;
            // iCalTimeZoneInfo does not allow sequence numbers
            // Perhaps we should have a custom serializer that fixes this?

            Initialize();
        }
        public iCalTimeZoneInfo(string name) : this()
        {
            this.Name = name;
        }

        void Initialize()
        {
            m_Evaluator = new TimeZoneInfoEvaluator(this);
            SetService(m_Evaluator);
        }

        #endregion

        #region Overrides
        
        protected override void OnDeserializing(StreamingContext context)
        {
            base.OnDeserializing(context);

            Initialize();
        }

        public override bool Equals(object obj)
        {
            iCalTimeZoneInfo tzi = obj as iCalTimeZoneInfo;
            if (tzi != null)
            {
                return object.Equals(TimeZoneName, tzi.TimeZoneName) &&
                    object.Equals(OffsetFrom, tzi.OffsetFrom) &&
                    object.Equals(OffsetTo, tzi.OffsetTo);
            }
            return base.Equals(obj);
        }
                              
        #endregion

        #region ITimeZoneInfo Members

        virtual public string TZID
        {
            get
            {
                ITimeZone tz = Parent as ITimeZone;
                if (tz != null)
                    return tz.TZID;
                return null;
            }
        }

        /// <summary>
        /// Returns the name of the current Time Zone.
        /// <example>
        ///     The following are examples:
        ///     <list type="bullet">
        ///         <item>EST</item>
        ///         <item>EDT</item>
        ///         <item>MST</item>
        ///         <item>MDT</item>
        ///     </list>
        /// </example>
        /// </summary>
        virtual public string TimeZoneName
        {
            get
            {
                if (TimeZoneNames.Count > 0)
                    return TimeZoneNames[0];
                return null;
            }
            set
            {
                TimeZoneNames.Clear();
                TimeZoneNames.Add(value);
            }
        }

        virtual public IUTCOffset TZOffsetFrom
        {
            get { return OffsetFrom; }
            set { OffsetFrom = value; }
        }

        virtual public IUTCOffset OffsetFrom
        {
            get { return Properties.Get<IUTCOffset>("TZOFFSETFROM"); }
            set { Properties.Set("TZOFFSETFROM", value); }
        }

        virtual public IUTCOffset OffsetTo
        {
            get { return Properties.Get<IUTCOffset>("TZOFFSETTO"); }
            set { Properties.Set("TZOFFSETTO", value); }
        }

        virtual public IUTCOffset TZOffsetTo
        {
            get { return OffsetTo; }
            set { OffsetTo = value; }
        }

        virtual public IList<string> TimeZoneNames
        {
            get { return Properties.GetMany<string>("TZNAME"); }
            set { Properties.Set("TZNAME", value); }
        }

        virtual public TimeZoneObservance? GetObservance(IDateTime dt)
        {
            if (Parent == null)
                throw new Exception("Cannot call GetObservance() on a TimeZoneInfo whose Parent property is null.");
                        
            if (string.Equals(dt.TZID, TZID))
            {
                // Normalize date/time values within this time zone to a local value.
                DateTime normalizedDt = dt.Value;

                // Let's evaluate our time zone observances to find the 
                // observance that applies to this date/time value.
                IEvaluator parentEval = Parent.GetService(typeof(IEvaluator)) as IEvaluator;
                if (parentEval != null)
                {
                    // Evaluate the date/time in question.
                    parentEval.Evaluate(Start, DateUtil.GetSimpleDateTimeData(Start), normalizedDt, true);

                    // NOTE: We avoid using period.Contains here, because we want to avoid
                    // doing an inadvertent time zone lookup with it.
                    IPeriod period = m_Evaluator
                        .Periods
                        .FirstOrDefault(p =>
                            p.StartTime.Value <= normalizedDt &&
                            p.EndTime.Value > normalizedDt
                        );

                    if (period != null)
                    {
                        return new TimeZoneObservance(period, this);
                    }
                }
            }

            return null;            
        }

        virtual public bool Contains(IDateTime dt)
        {
            TimeZoneObservance? retval = GetObservance(dt);
            return (retval != null && retval.HasValue);
        }

        #endregion

        #region IRecurrable Members

        virtual public IDateTime DTStart
        {
            get { return Start; }
            set { Start = value; }
        }

        virtual public IDateTime Start
        {
            get { return Properties.Get<IDateTime>("DTSTART"); }
            set { Properties.Set("DTSTART", value); }
        }

        virtual public IList<IPeriodList> ExceptionDates
        {
            get { return Properties.GetMany<IPeriodList>("EXDATE"); }
            set { Properties.Set("EXDATE", value); }
        }

        virtual public IList<IRecurrencePattern> ExceptionRules
        {
            get { return Properties.GetMany<IRecurrencePattern>("EXRULE"); }
            set { Properties.Set("EXRULE", value); }
        }

        virtual public IList<IPeriodList> RecurrenceDates
        {
            get { return Properties.GetMany<IPeriodList>("RDATE"); }
            set { Properties.Set("RDATE", value); }
        }

        virtual public IList<IRecurrencePattern> RecurrenceRules
        {
            get { return Properties.GetMany<IRecurrencePattern>("RRULE"); }
            set { Properties.Set("RRULE", value); }
        }

        virtual public IDateTime RecurrenceID
        {
            get { return Properties.Get<IDateTime>("RECURRENCE-ID"); }
            set { Properties.Set("RECURRENCE-ID", value); }
        }

        #endregion

        #region IRecurrable Members

        virtual public void ClearEvaluation()
        {
            RecurrenceUtil.ClearEvaluation(this);
        }

        virtual public IList<Occurrence> GetOccurrences(IDateTime dt)
        {
            return RecurrenceUtil.GetOccurrences(this, dt, true);
        }

        virtual public IList<Occurrence> GetOccurrences(DateTime dt)
        {
            return RecurrenceUtil.GetOccurrences(this, new iCalDateTime(dt), true);
        }

        virtual public IList<Occurrence> GetOccurrences(IDateTime startTime, IDateTime endTime)
        {
            return RecurrenceUtil.GetOccurrences(this, startTime, endTime, true);
        }

        virtual public IList<Occurrence> GetOccurrences(DateTime startTime, DateTime endTime)
        {
            return RecurrenceUtil.GetOccurrences(this, new iCalDateTime(startTime), new iCalDateTime(endTime), true);
        }

        #endregion
    }    
}
