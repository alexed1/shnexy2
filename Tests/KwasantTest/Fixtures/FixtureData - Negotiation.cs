﻿using Data.Constants;
using Data.Entities;

namespace KwasantTest.Fixtures
{
    public partial class FixtureData
    {
        public NegotiationDO TestNegotiation1()
        {
            var curNegotiationDO = new NegotiationDO
            {
                Id = 1,
                BookingRequestID = TestBookingRequest1().Id,
                NegotiationStateID = NegotiationState.InProcess,
                Name = "Negotiation 1"
            };
            curNegotiationDO.Questions.Add(TestQuestion1());
            return curNegotiationDO;
        }
    }
}
