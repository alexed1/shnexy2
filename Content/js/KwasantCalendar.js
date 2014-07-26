(function ($) {
    var settings;
    var storedCalendars = [];
    var calendar;


    $.fn.KCalendar = function (options) {
        
        //Setup defaults
        settings = $.extend({
            topElement: this,

            showDay: true,
            showWeek: true,
            showMonth: true,
            
            showNavigator: true,

            requireConfirmation: true,

            getCalendarBackendURL: function () { alert('getCalendarBackendURL must be set in the options.'); },
            getMonthBackendURL: function () { alert('getMonthBackendURL must be set in the options.'); },
            
            getEditURL: function (id) { alert('getEditURL function must be set in the options, unless providing an onEdit function override.'); },
            getNewURL: function (start, end) { alert('getNewURL function must be set in the options, unless providing an onEdit function override.'); },
            getDeleteURL: function (id) { alert('getDeleteURL function must be set in the options, unless providing an onEdit function override.'); },
            getMoveURL: function (id, newstart, newend) { alert('getMoveURL function must be set in the options, unless providing an onEdit function override.'); },

            onEventClick: onEventClick,
            onEventNew: onEventNew,
            onEventDelete: onEventDelete,
            onEventMove: onEventMove,

        }, options);

        createCalendars();


        this.refreshCalendars = function () {
            $.each(storedCalendars, function (i, ele) {
                ele.commandCallBack('refresh');
            });
        };
        calendar = this;

        return this;
    };

    var createCalendars = function() {
        //First, setup the HTML

        //This displays the toolbar to swap between day, week and month
        var toolbar = $("<div id='toolbar' class='toolbar'></div>");
        var inner = $("<div class='divCalender-inner'></div>");

        var switcher = new DayPilot.Switcher();
        storedCalendars = [];

        var queueCalendarForInit = function(createFunc, name) {
            var calendarPair = createFunc();

            if (calendarPair === null ||
                calendarPair === undefined ||
                calendarPair.dp === null ||
                calendarPair.dp === undefined) {
                return;
            }

            var swapButton = $("<a id=" + getRandomID() + " href='#'>" + name + "</a>");
            storedCalendars.push(calendarPair.dp);
            switcher.addView(calendarPair.dp);
            switcher.addButton(swapButton.get(0), calendarPair.dp);

            inner.append(calendarPair.div);
            toolbar.append(swapButton);
        };

        if (settings.showDay) {
            queueCalendarForInit(createDayCalendar, 'Day');
        }
        if (settings.showWeek) {
            queueCalendarForInit(createWeekCalendar, 'Week');
        }
        if (settings.showMonth) {
            queueCalendarForInit(createMonthCalendar, 'Month');
        }

        var toolbarRow = $("<div class='row'></div>");
        toolbarRow.append(toolbar);

        var calendarRow = $("<div class='row'></div>");
        calendarRow.append(inner);

        var calendarBox = $("<div class='col-md-6 container_box divCalender'></div>");
        calendarBox.append(toolbarRow);
        calendarBox.append(calendarRow);

        settings.topElement.append(calendarBox);

        var firstToDisplay = null;
        for (var i = 0; i < storedCalendars.length; i++) {
            storedCalendars[i].init();
            if (i == 0)
                firstToDisplay = storedCalendars[0];
        }
        
        if (settings.showNavigator) {
            var nav = createNavigator();
            var wrapper = $('<div class="col-lg-2 container_box calendar-area">');
            wrapper.append(nav.div);
            settings.topElement.append(wrapper);
            nav.dp.init();
            switcher.addNavigator(nav.dp);
        }

        if (firstToDisplay !== null)
            switcher.show(firstToDisplay);

    };

    var getRandomID = function() {
        var idLength = 10;
        return new Array(idLength + 1).join((Math.random().toString(36) + '00000000000000000').slice(2, 18)).slice(0, idLength);
    };

    var createDayCalendar = function () {
        var calendar = createDefaultCalendar();
        calendar.dp.viewType = 'Day';
        
        return calendar;
    };
    var createWeekCalendar = function () {
        var calendar = createDefaultCalendar();
        calendar.dp.viewType = 'Week';
        calendar.dp.headerHeight = 30;
        
        return calendar;
    };
    var createMonthCalendar = function () {
        var id = getRandomID();
        var divHolder = $("<div id='" + id + "'></div>");

        var dp = new DayPilot.Month(id);
        dp.backendUrl = '/Calendar/Month?bookingRequestID=17';
        dp.onAjaxError = function (args) { var request = args.request; if (DayPilot.Modal) { new DayPilot.Modal().showHtml(args.request.responseText); } else { alert('AJAX callback error (500)'); }; };
        dp.allowMultiSelect = true;
        dp.afterEventRender = function (e, div) {; };
        dp.afterRender = function (data, isCallBack) {; };
        dp.api = 1;
        dp.autoRefreshCommand = 'refresh';
        dp.autoRefreshEnabled = false;
        dp.autoRefreshInterval = 60;
        dp.autoRefreshMaxCount = 20;
        dp.backColor = '#FFFFD5';
        dp.borderColor = 'Black';
        dp.cellHeaderBackColor = '';
        dp.cellHeaderFontColor = null;
        dp.cellHeaderFontFamily = 'Tahoma, Arial, Helvetica, sans-serif';
        dp.cellHeaderFontSize = '10pt';
        dp.cellHeight = 90;
        dp.cellHeaderHeight = 16;
        dp.cellMode = false;
        dp.theme = 'calendar_white';
        dp.cssOnly = false;
        dp.eventBackColor = 'White';
        dp.eventBorderColor = 'Black';
        dp.eventCorners = 'Rounded';
        dp.eventFontColor = '#000000';
        dp.eventFontFamily = 'Tahoma, Arial, Helvetica, sans-serif';
        dp.eventFontSize = '10px';
        dp.eventHeight = 50;
        dp.eventMoveToPosition = false;
        dp.eventStartTime = true;
        dp.eventEndTime = true;
        dp.eventTextLayer = 'Bottom';
        dp.eventTextAlignment = 'Center';
        dp.eventTextLeftIndent = 50;
        dp.innerBorderColor = '#CCCCCC';
        dp.headerBackColor = '#ECE9D8';
        dp.headerFontColor = '#000000';
        dp.headerFontSize = '12px';
        dp.headerHeight = 50;
        dp.heightSpec = 'Auto';
        dp.height = '550';
        dp.hideUntilInit = false;
        dp.locale = 'en-us';
        dp.messageHideAfter = 5000;
        dp.nonBusinessBackColor = '#FFF4BC';
        dp.shadowType = 'Fill';
        dp.showWeekend = true;
        dp.showToolTip = true;
        dp.timeFormat = 'Auto';
        dp.viewType = 'Month';
        dp.weekStarts = 0;
        dp.width = '100%';
        dp.weeks = 1;
        dp.eventClickHandling = 'JavaScript';
        dp.eventDoubleClickHandling = 'JavaScript';
        dp.eventSelectHandling = 'JavaScript';
        dp.eventMoveHandling = 'JavaScript';
        dp.eventResizeHandling = 'JavaScript';
        dp.eventRightClickHandling = 'ContextMenu';
        dp.headerClickHandling = 'JavaScript';
        dp.timeRangeDoubleClickHandling = 'Disabled';
        dp.timeRangeSelectedHandling = 'JavaScript';
        dp.onEventDoubleClick = function (e) {; };
        dp.onEventSelect = function (e, change) {; };
        dp.onEventResize = function (e, newStart, newEnd) { settings.onEventMove(e.id(), newStart, newEnd); };
        dp.onEventRightClick = function (e) {; };
        dp.onHeaderClick = function (e) { var day = e.day;; };
        dp.onTimeRangeDoubleClick = function (start, end) {; };
        DayPilot.Locale.register(new DayPilot.Locale('en-us', { 'dayNames': ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'], 'dayNamesShort': ['Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa'], 'monthNames': ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December', ''], 'monthNamesShort': ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec', ''], 'timePattern': 'h:mm tt', 'datePattern': 'M/d/yyyy', 'dateTimePattern': 'M/d/yyyy h:mm tt', 'timeFormat': 'Clock12Hours', 'weekStarts': 0 }));


        dp.onEventClick = function (e) { settings.onEventClick(e.id()); };
        dp.onTimeRangeSelected = function (start, end) { settings.onEventNew(start, end); };
        dp.onEventDelete = function (e) { settings.onEventDelete(e.id()); };;
        dp.onEventMove = function (e, newStart, newEnd) { settings.onEventMove(e.id(), newStart, newEnd); };;
        dp.backendUrl = settings.getMonthBackendURL();
        
        dp.contextMenu = new DayPilot.Menu([
            { text: "Delete", onclick: function () { settings.onEventDelete(this.source.value()); } }
        ]);

        return { dp: dp, div: divHolder };
    };
    

   
    //EventBackColor = "#638EDE",
    //DurationBarVisible = false,
    //EventHeaderVisible = false,
    //Height = 550,
    //HeaderHeight = 50,
    //CellHeight = 25,
    //CssOnly = false,
    //ContextMenu = "menu1",
    //Theme = "calendar_white",
    //ViewType = ViewType.Day,
    //EventClickJavaScript = "eventClick(e)",
    //EventMoveJavaScript = "eventMove(e, newStart, newEnd)",
    //EventResizeJavaScript = "eventMove(e, newStart, newEnd)",
    //TimeRangeSelectedJavaScript = "eventNew(start, end, resource)",
    //EventDeleteJavaScript = "eventDelete(e)",

    var createDefaultCalendar = function() {
        var id = getRandomID();
        var divHolder = $("<div id='" + id + "'></div>");

        var dp = new DayPilot.Calendar(id);

        dp.allDayEnd = 'DateTime';
        dp.allDayEventBackColor = 'white';
        dp.allDayEventBorderColor = '#000000';
        dp.allDayEventFontFamily = 'Tahoma, Arial, Helvetica, sans-serif';
        dp.allDayEventFontSize = '8pt';
        dp.allDayEventFontColor = '#000000';
        dp.allDayEventHeight = 25;
        dp.allowMultiSelect = true;
        dp.api = 1;
        dp.autoRefreshCommand = 'refresh';
        dp.autoRefreshEnabled = false;
        dp.autoRefreshInterval = 60;
        dp.autoRefreshMaxCount = 20;
        dp.borderColor = '#CED2CE';
        dp.businessBeginsHour = 9;
        dp.businessEndsHour = 18;
        dp.cellBackColor = '#FFFFFF';
        dp.cellBackColorNonBusiness = '#FFF4BC';
        dp.cellBorderColor = '#DEDFDE';
        dp.cellHeight = 25;
        dp.cellDuration = 30;
        dp.columnMarginRight = 5;
        dp.columnWidthSpec = 'Auto';
        dp.columnWidth = 200;
        dp.cornerBackColor = '#F3F3F9';
        dp.crosshairColor = 'gray';
        dp.crosshairOpacity = 20;
        dp.crosshairType = 'Header';
        dp.theme = 'calendar_white';
        dp.cssOnly = false;
        dp.deleteImageUrl = null;
        dp.dayBeginsHour = 0;
        dp.dayEndsHour = 24;
        dp.days = 1;
        dp.durationBarColor = '#0000ff';
        dp.durationBarVisible = false;
        dp.durationBarWidth = 5;
        dp.durationBarImageUrl = null;
        dp.eventArrangement = 'SideBySide';
        dp.eventBackColor = '#638EDE';
        dp.eventBorderColor = '#2951A5';
        dp.eventFontFamily = 'Tahoma, Arial, Helvetica, sans-serif';
        dp.eventFontSize = '8pt';
        dp.eventFontColor = '#ffffff';
        dp.eventHeaderFontSize = '8pt';
        dp.eventHeaderFontColor = '#ffffff';
        dp.eventHeaderHeight = 14;
        dp.eventHeaderVisible = false;
        dp.eventSelectColor = '#0000ff';
        dp.headerFontSize = '10pt';
        dp.headerFontFamily = 'Tahoma, Arial, Helvetica, sans-serif';
        dp.headerFontColor = '#42658C';
        dp.headerHeight = 50;
        dp.headerHeightAutoFit = false;
        dp.headerLevels = 1;
        dp.height = 550;
        dp.heightSpec = 'BusinessHours';
        dp.hideFreeCells = false;
        dp.hideUntilInit = false;
        dp.hourHalfBorderColor = '#EBEDEB';
        dp.hourBorderColor = '#DEDFDE';
        dp.hourFontColor = '#42658C';
        dp.hourFontFamily = 'Tahoma, Arial, Helvetica, sans-serif';
        dp.hourNameBackColor = '#F3F3F9';
        dp.hourNameBorderColor = '#DEDFDE';
        dp.hourWidth = 45;
        dp.initScrollPos = '450';
        dp.loadingLabelText = 'Loading...';
        dp.loadingLabelVisible = true;
        dp.loadingLabelFontSize = '10pt';
        dp.loadingLabelFontFamily = 'Tahoma, Arial, Helvetica, sans-serif';
        dp.loadingLabelFontColor = '#ffffff';
        dp.loadingLabelBackColor = '#ff0000';
        dp.locale = 'en-us';
        dp.messageHideAfter = 5000;
        dp.moveBy = 'Full';
        dp.rtl = false;
        dp.roundedCorners = true;
        dp.scrollLabelsVisible = true;
        dp.scrollDownUrl = null;
        dp.scrollUpUrl = null;
        dp.selectedColor = '#316AC5';
        dp.shadow = 'Fill';
        dp.showToolTip = false;
        dp.showAllDayEvents = false;
        dp.showAllDayEventStartEnd = false;
        dp.showHeader = true;
        dp.showHours = true;
        dp.timeFormat = 'Clock12Hours';
        dp.timeHeaderCellDuration = 60;
        dp.useEventBoxes = 'Always';
        dp.useEventSelectionBars = false;
        dp.viewType = 'Day';
        dp.onAjaxError = function (args) { var request = args.request; if (DayPilot.Modal) { new DayPilot.Modal().showHtml(args.request.responseText); } else { alert('AJAX callback error (500)'); }; };
        dp.afterRender = function (data, isCallBack) {; };
        dp.eventClickHandling = 'JavaScript';
        dp.eventSelectHandling = 'Disabled';
        dp.eventDeleteHandling = 'JavaScript';
        dp.eventDoubleClickHandling = 'Disabled';
        dp.eventEditHandling = 'CallBack';
        dp.eventHoverHandling = 'Bubble';
        dp.eventMoveHandling = 'JavaScript';
        dp.eventResizeHandling = 'JavaScript';
        dp.eventRightClickHandling = 'ContextMenu';
        dp.headerClickHandling = 'JavaScript';
        dp.timeRangeDoubleClickHandling = 'Disabled';
        dp.timeRangeSelectedHandling = 'JavaScript';
        dp.onEventSelect = function (e, change) {; };
        dp.onEventDoubleClick = function (e) {; };
        dp.onEventEdit = function (e, newText) {; };
        dp.onEventResize = function (e, newStart, newEnd) { settings.onEventMove(e.id(), newStart, newEnd); };
        dp.onEventRightClick = function (e) {; };
        dp.onHeaderClick = function (c) {; };
        dp.onTimeRangeDoubleClick = function (start, end, resource) {; };
        
        DayPilot.Locale.register(new DayPilot.Locale('en-us', { 'dayNames': ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'], 'dayNamesShort': ['Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa'], 'monthNames': ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December', ''], 'monthNamesShort': ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec', ''], 'timePattern': 'h:mm tt', 'datePattern': 'M/d/yyyy', 'dateTimePattern': 'M/d/yyyy h:mm tt', 'timeFormat': 'Clock12Hours', 'weekStarts': 0 }));

        dp.backendUrl = settings.getCalendarBackendURL();

        dp.onEventClick = function(e) { settings.onEventClick(e.id()); };
        dp.onTimeRangeSelected = function(start, end) { settings.onEventNew(start, end); };
        dp.onEventDelete = function (e) { settings.onEventDelete(e.id()); };
        dp.onEventMove = function (e, newStart, newEnd) { settings.onEventMove(e.id(), newStart, newEnd); };;

        dp.contextMenu = new DayPilot.Menu([
            { text: "Delete", onclick: function() { settings.onEventDelete(this.source.value()); } }
        ]);

        return { dp: dp, div: divHolder };
    };

    var createNavigator = function() {
        var id = getRandomID();
        var divHolder = $("<div id='" + id + "'></div>");

        var dp_navigator = new DayPilot.Navigator(id);
        dp_navigator.api = 1;
        dp_navigator.cellHeight = 20;
        dp_navigator.cellWidth = 20;
        dp_navigator.command = 'navigate';
        dp_navigator.theme = 'navigator_white';
        dp_navigator.cssOnly = true;
        dp_navigator.dayHeaderHeight = 20;
        dp_navigator.locale = 'en-us';
        dp_navigator.month = 7;
        dp_navigator.orientation = 'Vertical';
        dp_navigator.rowsPerMonth = 'Six';
        dp_navigator.selectMode = 'day';
        dp_navigator.showMonths = 1;
        dp_navigator.showWeekNumbers = false;
        dp_navigator.skipMonths = 1;
        dp_navigator.titleHeight = 20;
        dp_navigator.weekStarts = 0;
        dp_navigator.weekNumberAlgorithm = 'Auto';
        dp_navigator.year = 2014;
        dp_navigator.onAjaxError = function(args) {
            var request = args.request;
            if (DayPilot.Modal) {
                new DayPilot.Modal().showHtml(args.request.responseText);
            } else {
                alert('AJAX callback error (500)');
            }
            ;
        };
        dp_navigator.timeRangeSelectedHandling = 'Bind';
        dp_navigator.onTimeRangeSelected = function(start, end) { ; };
        dp_navigator.visibleRangeChangedHandling = 'JavaScript';
        dp_navigator.onVisibleRangeChanged = function(start, end) { ; };
        DayPilot.Locale.register(new DayPilot.Locale('en-us', { 'dayNames': ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'], 'dayNamesShort': ['Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa'], 'monthNames': ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December', ''], 'monthNamesShort': ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec', ''], 'timePattern': 'h:mm tt', 'datePattern': 'M/d/yyyy', 'dateTimePattern': 'M/d/yyyy h:mm tt', 'timeFormat': 'Clock12Hours', 'weekStarts': 0 }));


        return { dp: dp_navigator, div: divHolder };
    };

    var onEventClick = function(id) {
        if (Kwasant.IFrame.PopupsActive()) {
            return;
        }
        
        Kwasant.IFrame.Display(settings.getEditURL(id),
            {
                horizontalAlign: 'right',
                callback: calendar.refreshCalendars
            });
    };
    var onEventNew = function(start, end) {
        if (Kwasant.IFrame.PopupsActive()) {
            return;
        }
        
        Kwasant.IFrame.Display(settings.getNewURL(-1, start, end),
            {
                horizontalAlign: 'right',
                callback: calendar.refreshCalendars
            });
    };
    var onEventMove = function(id, newStart, newEnd) {
        if (Kwasant.IFrame.PopupsActive()) {
            return;
        }
        
        Kwasant.IFrame.Display(settings.getMoveURL(id, newStart, newEnd),
            {
                modal: true,
                callback: calendar.refreshCalendars
            });
    };
    var onEventDelete = function(id) {
        Kwasant.IFrame.Display(settings.getDeleteURL(id),
            {
                modal: true,
                callback: calendar.refreshCalendars
            });
    };
}(jQuery));