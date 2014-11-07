﻿using Data.Infrastructure;

namespace KwasantWeb.AlertQueues
{
    public class BookingRequestUpdatedQueue : PersonalAlertQueue<BookingRequestUpdatedData>
    {
        public BookingRequestUpdatedQueue() 
        {
            AlertManager.AlertConversationMemberAdded += id =>
            {
                if (ObjectID == id)
                    AppendUpdate(new BookingRequestUpdatedData());
            };
        }
    }

    public class BookingRequestUpdatedData
    {
    }
}