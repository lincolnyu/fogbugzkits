using System;
using System.IO;
using System.Text;

namespace FogbugzKits.GapFinder
{
    class Program
    {
        #region Methods

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: gapfinder <csv file>");
                return;
            }

            var fn = args[0];
            var morningEnd = new TimeSpan(12, 30, 0);
            var afternoonStart = new TimeSpan(13, 30, 0);
            var afternoonEnd = new TimeSpan(18, 0, 0);

            using (var sr = new StreamReader(fn))
            {
                var expectedDt = default(DateTime);
                var once = false;

                var lineCount = 1;
                var errorCount = 0;
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    if (line == null)
                    {
                        continue;
                    }
                    var segs = line.Split(',');
                    if (segs.Length < 2 || segs[0].Equals("Start", StringComparison.OrdinalIgnoreCase)) // header
                    {
                        continue;
                    }

                    var dt1 = ParseFogbugzDateTime(segs[0]);
                    var dt2 = ParseFogbugzDateTime(segs[1]);

                    if (!once)
                    {
                        if (dt1.TimeOfDay != TimeSpan.FromHours(9))
                        {
                            Console.WriteLine("not starting from 9:00 at line {0}: {1}", lineCount, line);
                            errorCount++;
                        }
                        once = true;
                    }
                    else if (dt1 != expectedDt)
                    {
                        Console.WriteLine("gap/unexpected start found at line {0}: {1}", lineCount, line);
                        errorCount++;
                    }

                    if (dt1 > dt2)
                    {
                        Console.WriteLine("start later than end found at line {0}: {1}", lineCount, line);
                        errorCount++;
                    }
                    else if (dt1.TimeOfDay < morningEnd && dt2.TimeOfDay >= afternoonStart)
                    {
                        Console.WriteLine("working lunchbreak found at line {0}: {1}", lineCount, line);
                        errorCount++;
                    }
                    else if (dt1.Date < dt2.Date)
                    {
                        Console.WriteLine("overnight work found at line {0}: {1}", lineCount, line);
                        errorCount++;
                    }

                    if (dt2.TimeOfDay < morningEnd)
                    {
                        expectedDt = dt2;
                    }
                    else if (dt2.TimeOfDay == morningEnd)
                    {
                        expectedDt = new DateTime(dt2.Year, dt2.Month, dt2.Day, 13, 30, 0);
                    }
                    else if (dt2.TimeOfDay < afternoonEnd)
                    {
                        expectedDt = dt2;
                    }
                    else
                    {
                        if (dt2.TimeOfDay != afternoonEnd)
                        {
                            Console.WriteLine("ending at unexpected time found at line {0}:{1}", lineCount, line);
                            errorCount++;
                        }
                        DateTime nextDay;
                        if (dt2.DayOfWeek == DayOfWeek.Friday)
                        {
                            nextDay = dt2.AddDays(3);
                        }
                        else
                        {
                            nextDay = dt2.AddDays(1);
                        }
                        expectedDt = new DateTime(nextDay.Year, nextDay.Month, nextDay.Day, 9, 0, 0);
                    }

                    lineCount++;
                }
                Console.WriteLine("{0} lines successfully processed, {1} anomalies found.", lineCount - 1, errorCount);
            }
        }

        static DateTime ParseFogbugzDateTime(string timeStr)
        {
            var i1 = timeStr.IndexOf('/');
            var i2 = timeStr.IndexOf('/', i1 + 1);
            var sb = new StringBuilder();
            sb.Append(timeStr.Substring(i1 + 1, i2 - i1 - 1));
            sb.Append('/');
            sb.Append(timeStr.Substring(0, i1));
            sb.Append(timeStr.Substring(i2));
            var dt = DateTime.Parse(sb.ToString());
            return dt;
        }

        #endregion
    }
}

