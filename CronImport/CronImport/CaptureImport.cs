﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Cronos;
using Square9.CustomNode;

namespace CronImport
{
    public class CaptureImport : CaptureImporter
    {
        string LastRunFile = "";

        public override List<string> Import()
        {
            LastRunFile = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + $@"\Square_9_Softworks\CronImport\{Workflow.ID}\LastExecution.txt";

            var savedExpression = Settings.GetStringSetting("CronExpression");
            var cronExpression = CronExpression.Parse(savedExpression);

            var lastRunTime = GetLastRunTime();
            DateTime? nextTime = cronExpression.GetNextOccurrence(lastRunTime, TimeZoneInfo.Local); // Run the comparison using local time.

            if (!nextTime.HasValue)
            {
                throw new FormatException("Provided CRON Expression is not valid.");
            }

            var currentTime = DateTime.UtcNow;
            if (currentTime.Ticks >= nextTime.Value.Ticks)
            {
                var importDirectory = Settings.GetStringSetting("SourcePath");
                var filesToImport = Directory.GetFiles(importDirectory).ToList();

                RecordLastRunTime(currentTime);

                return filesToImport;
            }

            return new List<string>();
        }

        /// <summary>
        /// Retrieves the last recorded time (int UTC) the workflow import was run.
        /// </summary>
        /// <returns></returns>
        private DateTime GetLastRunTime()
        {
            if (File.Exists(LastRunFile))
            {
                var lastRunDateTime = File.ReadAllText(LastRunFile);
                var lastRunDateTimeLocal = DateTime.Parse(lastRunDateTime); // DateTime parse returns local time, so it needs to be converted to UTC.
                return new DateTime(lastRunDateTimeLocal.Ticks, DateTimeKind.Utc);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LastRunFile));

                var newDateTime = new DateTime(0, DateTimeKind.Utc);
                File.WriteAllText(LastRunFile, newDateTime.ToString());
                return newDateTime;
            }
        }

        /// <summary>
        /// Updates the workflow import's last run time (in UTC).
        /// </summary>
        /// <param name="dateTime"></param>
        private void RecordLastRunTime(DateTime dateTime)
        {
            File.WriteAllText(LastRunFile, dateTime.ToString());
        }
    }
}
