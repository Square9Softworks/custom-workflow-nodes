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
        string LastRunFile = "";

        public override List<string> Import()
        {
            LastRunFile = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + $@"\Square_9_Softworks\CronImport\{Workflow.ID}\LastExecution.txt";

            var savedExpression = Settings.GetStringSetting("CronExpression");
            var cronExpression = CronExpression.Parse(savedExpression);

            var lastRunTime = GetLastRunTime();
            DateTime? nextTime = cronExpression.GetNextOccurrence(lastRunTime);

            if (!nextTime.HasValue)
            {
                throw new FormatException("Provided CRON Expression is not valid.");
            }

            if (DateTime.UtcNow.Ticks >= nextTime.Value.Ticks)
            {
                var importDirectory = Settings.GetStringSetting("SourcePath");
                var filesToImport = Directory.GetFiles(importDirectory).ToList();

                RecordLastRunTime(DateTime.UtcNow);

                return filesToImport;
            }

            return new List<string>();
        }

        /// <summary>
        /// Retrieves the last recorded time the workflow import was run.
        /// </summary>
        /// <returns></returns>
        private DateTime GetLastRunTime()
        {
            if (File.Exists(LastRunFile))
            {
                var lastRunDateTime = File.ReadAllText(LastRunFile);
                return DateTime.Parse(lastRunDateTime).ToUniversalTime();
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
        /// Updates the workflow import's last run time.
        /// </summary>
        /// <param name="dateTime"></param>
        private void RecordLastRunTime(DateTime dateTime)
        {
            File.WriteAllText(LastRunFile, dateTime.ToString());
        }
    }
}
