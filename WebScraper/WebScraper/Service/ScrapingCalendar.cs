using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using Quartz;
using Quartz.Impl.Calendar;
using Quartz.Util;

namespace WebScraper
{
    class ScrapingCalendar : BaseCalendar
    {
        new public string Description => "Calendar for scraping";
        new public ICalendar CalendarBase => new HolidayCalendar();

        override public DateTimeOffset GetNextIncludedTimeUtc(DateTimeOffset timeUtc)
        {
            // Call base calendar implementation first
            DateTimeOffset baseTime = base.GetNextIncludedTimeUtc(timeUtc);
            if (timeUtc != DateTimeOffset.MinValue && baseTime > timeUtc)
            {
                timeUtc = baseTime;
            }

            //apply the timezone
            timeUtc = TimeZoneUtil.ConvertTime(timeUtc, TimeZone);

            // Get timestamp for 00:00:00, with the correct timezone offset
            DateTimeOffset day = new DateTimeOffset(timeUtc.Date, timeUtc.Offset);

            while (!IsTimeIncluded(day))
            {
                day = day.AddDays(1);
            }

            return day;
        }

        override public bool IsTimeIncluded(DateTimeOffset timeUtc)
        {
            return CalendarBase.IsTimeIncluded(timeUtc) && timeUtc.DayOfWeek != DayOfWeek.Saturday && timeUtc.DayOfWeek != DayOfWeek.Sunday;
        }
    }
}
