using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Cronos;
using Square9.CustomNode;

namespace CronImport
{
    public class CaptureImport : CaptureImporter
    {
        public override List<string> Import()
        {
            var savedExpression = Settings.GetStringSetting("CronExpression");
            var cronExpression = CronExpression.Parse(savedExpression);

            var currentTime = DateTime.UtcNow;
            var roundedTime = currentTime.Date + new TimeSpan(currentTime.Hour, currentTime.Minute, 0); // Disregard seconds in the current time comparison.

            DateTime? nextTime = cronExpression.GetNextOccurrence(roundedTime, true);

            if (!nextTime.HasValue)
            {
                throw new FormatException("Provided CRON Expression is not valid.");
            }

            if (roundedTime == nextTime)
            {
                var importDirectory = Settings.GetStringSetting("SourcePath");
                return Directory.GetFiles(importDirectory).ToList();
            }

            return new List<string>();
        }
    }
}
