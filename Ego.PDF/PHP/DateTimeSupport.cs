//
// In order to convert some functionality to Visual C#, the PHP Language Conversion Assistant
// creates "support classes" that duplicate the original functionality.  
//
// Support classes replicate the functionality of the original code, but in some cases they are 
// substantially different architecturally. Although every effort is made to preserve the 
// original architecture of the application in the converted project, the user should be aware that 
// the primary goal of these support classes is to replicate functionality, and that at times 
// the architecture of the resulting solution may differ somewhat.
//

/// <summary>
/// Contains conversion support elements such as classes, interfaces and static methods.
/// </summary>

namespace Ego.PDF.PHP
{
    /*******************************/

    /*******************************/

    /*******************************/

    /*******************************/

    /*******************************/

    /*******************************/

    /// <summary>
    /// Provides static methods related to Date/Time functions.
    /// </summary>
    public class DateTimeSupport
    {
        /// <summary>
        /// Returns true if the specified values represent a valid date, false otherwise.
        /// </summary>
        /// <param name="month">The month of the date to be checked.</param>
        /// <param name="day">The day of the date to be checked.</param>
        /// <param name="year">The year of the date to be checked.</param>
        /// <returns>Returns true if the specified values represent a valid date, false otherwise.</returns>
        public static bool CheckDate(int month, int day, int year)
        {
            bool result;
            try
            {
                new System.DateTime(year, month, day);
                result = true;
            }
            catch
            {
                result = false;
            }
            return result;
        }

        /// <summary>
        /// Creates a new System.DateTime instance using the specified values. 
        /// If any of the values is equals to -1, then the equivalent value of the current date will be used.
        /// </summary>
        /// <param name="hour">The hour of the new System.DateTime.</param>
        /// <param name="minute">The minute of the new System.DateTime.</param>
        /// <param name="second">The second of the new System.DateTime.</param>
        /// <param name="month">The month of the new System.DateTime.</param>
        /// <param name="day">The day of the new System.DateTime.</param>
        /// <param name="year">The year of the new System.DateTime.</param>
        /// <returns></returns>
        public static System.DateTime NewDateTime(int hour, int minute, int second, int month, int day, int year)
        {
            System.DateTime now = System.DateTime.Now;
            int theHour = hour == -1 ? now.Hour : hour;
            int theMinute = minute == -1 ? now.Minute : minute;
            int theSecond = second == -1 ? now.Second : second;
            int theMonth = month == -1 ? now.Month : month;
            int theDay = day == -1 ? now.Day : day;
            int tempYear = (0 <= year && year <= 69) ? year + 2000 : 0;
            tempYear = (70 <= year && year <= 99) ? year + 1900 : year;
            int theYear = year == -1 ? now.Year : tempYear;

            return new System.DateTime(theYear, theMonth, theDay, theHour, theMinute, theSecond);
        }

        /// <summary>
        /// Creates a new System.DateTime from the specified timestamp (time measured in the number of seconds).
        /// </summary>
        /// <param name="timestamp">The timestamp that represents the date to be created.</param>
        /// <returns>Returns a new System.DateTime instance created from the specified timestamp.</returns>
        public static System.DateTime NewDateTime(int timestamp)
        {
            long initialTicks = new System.DateTime(1970, 1, 1, 0, 0, 0, 0).Ticks;
            long elapsedUtcTicks = System.Convert.ToInt64(timestamp)*10000000;
            elapsedUtcTicks += initialTicks;
            System.DateTime theDate = new System.DateTime(elapsedUtcTicks).ToLocalTime();
            return theDate;
        }

        /// <summary>
        /// Creates a new OrderedMap that contains the date information associated with the specified timestamp.
        /// </summary>
        /// <param name="timestamp">The timestamp that represents the date to extract the information from.</param>
        /// <returns>Returns a new OrderedMap that contains the specified date information.</returns>
        public static OrderedMap GetDate(int timestamp)
        {
            OrderedMap dateInfo = new OrderedMap();
            System.DateTime theDate = NewDateTime(timestamp);
            dateInfo["seconds"] = theDate.Second;
            dateInfo["minutes"] = theDate.Minute;
            dateInfo["hours"] = theDate.Hour;
            dateInfo["mday"] = theDate.Day;
            switch (theDate.DayOfWeek)
            {
                case System.DayOfWeek.Sunday:
                    dateInfo["wday"] = 0;
                    break;
                case System.DayOfWeek.Monday:
                    dateInfo["wday"] = 1;
                    break;
                case System.DayOfWeek.Tuesday:
                    dateInfo["wday"] = 2;
                    break;
                case System.DayOfWeek.Wednesday:
                    dateInfo["wday"] = 3;
                    break;
                case System.DayOfWeek.Thursday:
                    dateInfo["wday"] = 4;
                    break;
                case System.DayOfWeek.Friday:
                    dateInfo["wday"] = 5;
                    break;
                default:
                    dateInfo["wday"] = 6;
                    break; //System.DayOfWeek.Saturday
            }
            dateInfo["mon"] = theDate.Month;
            dateInfo["year"] = theDate.Year;
            dateInfo["yday"] = theDate.DayOfYear;
            dateInfo["weekday"] = theDate.DayOfWeek.ToString();
            switch (theDate.Month)
            {
                case 1:
                    dateInfo["month"] = "January";
                    break;
                case 2:
                    dateInfo["month"] = "February";
                    break;
                case 3:
                    dateInfo["month"] = "March";
                    break;
                case 4:
                    dateInfo["month"] = "April";
                    break;
                case 5:
                    dateInfo["month"] = "May";
                    break;
                case 6:
                    dateInfo["month"] = "June";
                    break;
                case 7:
                    dateInfo["month"] = "July";
                    break;
                case 8:
                    dateInfo["month"] = "August";
                    break;
                case 9:
                    dateInfo["month"] = "September";
                    break;
                case 10:
                    dateInfo["month"] = "Octuber";
                    break;
                case 11:
                    dateInfo["month"] = "November";
                    break;
                default:
                    dateInfo["month"] = "December";
                    break; //12
            }
            dateInfo[0] = timestamp;

            return dateInfo;
        }

        /// <summary>
        /// Returns an OrderedMap that contains the same structure returned by the C function call.
        /// </summary>
        /// <param name="timestamp">The timestamp that represents the date to extract the information from.</param>
        /// <param name="associative">The value that indicates whether to return an associative OrderedMap or a numerical one.</param>
        /// <returns>Returns an OrderedMap that contains information about the specified date.</returns>
        public static OrderedMap LocalTime(int timestamp, bool associative)
        {
            OrderedMap dateInfo = new OrderedMap();
            System.DateTime theDate = NewDateTime(timestamp);
            dateInfo[associative ? "tm_sec" : "0"] = theDate.Second;
            dateInfo[associative ? "tm_min" : "1"] = theDate.Minute;
            dateInfo[associative ? "tm_hour" : "2"] = theDate.Hour;
            dateInfo[associative ? "tm_mday" : "3"] = theDate.Day;
            dateInfo[associative ? "tm_mon" : "4"] = theDate.Month - 1; //Month of the year starting with 0 for January.
            dateInfo[associative ? "tm_year" : "5"] = theDate.Year - 1900; //Years since 1900.
            switch (theDate.DayOfWeek)
            {
                case System.DayOfWeek.Sunday:
                    dateInfo[associative ? "tm_wday" : "6"] = 0;
                    break;
                case System.DayOfWeek.Monday:
                    dateInfo[associative ? "tm_wday" : "6"] = 1;
                    break;
                case System.DayOfWeek.Tuesday:
                    dateInfo[associative ? "tm_wday" : "6"] = 2;
                    break;
                case System.DayOfWeek.Wednesday:
                    dateInfo[associative ? "tm_wday" : "6"] = 3;
                    break;
                case System.DayOfWeek.Thursday:
                    dateInfo[associative ? "tm_wday" : "6"] = 4;
                    break;
                case System.DayOfWeek.Friday:
                    dateInfo[associative ? "tm_wday" : "6"] = 5;
                    break;
                default:
                    dateInfo["wday"] = 6;
                    break; //System.DayOfWeek.Saturday
            }
            dateInfo[associative ? "tm_yday" : "7"] = theDate.DayOfYear - 1;
            dateInfo[associative ? "tm_isdst" : "8"] = System.TimeZone.CurrentTimeZone.IsDaylightSavingTime(theDate)
                                                           ? 1
                                                           : 0;

            return dateInfo;
        }

        /// <summary>
        /// Returns a string that contains the current time represented in milliseconds and seconds.
        /// </summary>
        /// <returns>Returns a string that contains the current time represented in milliseconds and seconds.</returns>
        public static string Microtime()
        {
            System.DateTime now = System.DateTime.Now;
            double millisecond = now.Millisecond/1000.0;
            string result = millisecond.ToString() + " " + Timestamp(now);
            return result;
        }

        /// <summary>
        /// Returns an OrderedMap that contains information about the current date.
        /// </summary>
        /// <returns>Returns an OrderedMap that contains information about the current date.</returns>
        public static OrderedMap GetTimeOfDay()
        {
            OrderedMap dateInfo = new OrderedMap();
            System.DateTime now = System.DateTime.Now;
            dateInfo["sec"] = Timestamp(now);
            dateInfo["usec"] = now.Millisecond;
            dateInfo["minuteswest"] = System.TimeZone.CurrentTimeZone.GetUtcOffset(now).TotalMinutes*-1;
            dateInfo["dsttime"] = System.TimeZone.CurrentTimeZone.DaylightName;
            return dateInfo;
        }

        /// <summary>
        /// Returns the current time measured in the number of seconds.
        /// </summary>
        /// <returns>Returns the current time measured in the number of seconds since January 1 1970 00:00:00 GMT.</returns>
        public static int Time()
        {
            long initialTicks = new System.DateTime(1970, 1, 1, 0, 0, 0, 0).Ticks;
            long todayTicks = System.DateTime.UtcNow.Ticks;
            int elapsedSeconds = System.Convert.ToInt32((todayTicks - initialTicks)/10000000);
            return elapsedSeconds;
        }

        /// <summary>
        /// Returns the specified time measured in the number of seconds.
        /// </summary>
        /// <param name="dateTime">The System.DateTime to obtain the number of seconds from.</param>
        /// <returns>Returns the specified time measured in the number of seconds since January 1 1970 00:00:00 GMT.</returns>
        public static int Timestamp(System.DateTime dateTime)
        {
            long initialTicks = new System.DateTime(1970, 1, 1, 0, 0, 0, 0).Ticks;
            long dateTicks = dateTime.ToUniversalTime().Ticks;
            int elapsedSeconds = System.Convert.ToInt32((dateTicks - initialTicks)/10000000);
            return elapsedSeconds;
        }
    }
}