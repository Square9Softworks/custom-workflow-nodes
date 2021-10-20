using System;
using System.Collections.Generic;
using System.Linq;

using Square9.CustomNode;

namespace DeletePageRange
{
    public class PageDeletion : CaptureNode
    {
        public override void Run()
        {
            var pagesToDelete = ReadPagesSetting();
            if (pagesToDelete.Count < 1)
            {
                LogHistory("No pages were set for deletion.");
                Process.SetStatus(ProcessStatus.Errored);
                return;
            }

            var documentPageCount = Process.Document.GetPages().Count;

            pagesToDelete.Reverse();
            foreach (var page in pagesToDelete)
            {
                if (page > documentPageCount)
                {
                    LogHistory($"Page to delete ({page}) is outside of the document page range ({documentPageCount}). Page deletion stopped.");
                    Process.SetStatus(ProcessStatus.Errored);
                    return;
                }
                Process.Document.RemovePage(page - 1);
                documentPageCount--;
            }
        }

        /// <summary>
        /// Retrieves the provided list of pages to be deleted.
        /// </summary>
        /// <returns></returns>
        private List<int> ReadPagesSetting()
        {
            try
            {
                var pages = new List<int>();

                var pagesSetting = Settings.GetStringSetting("Pages");
                LogHistory("Pages set for deletion: " + pagesSetting);

                if (pagesSetting.Trim().ToUpper() == "ALL")
                {
                    var pageCount = Process.Document.GetPages().Count();
                    pagesSetting = "1-" + pageCount;
                }

                var pageGroups = pagesSetting.Split(',');

                foreach (var pageGroup in pageGroups)
                {
                    var pageRange = pageGroup.Split('-');

                    if (pageRange.Length == 2)  // page range
                    {
                        if (int.TryParse(pageRange[0], out var startPage) && int.TryParse(pageRange[1], out var endPage))
                        {
                            var rangeCount = endPage - startPage + 1; // The page range is inclusive, so add 1.
                            pages.AddRange(Enumerable.Range(startPage, rangeCount));
                        }
                        else
                        {
                            throw new ArgumentException("Page range must contain valid integers.");
                        }
                    }
                    else if (pageRange.Length == 1)  // single page
                    {
                        if (int.TryParse(pageRange[0], out var page))
                        {
                            pages.Add(page);
                        }
                        else
                        {
                            throw new ArgumentException("Page selection must be a valid integer.");
                        }
                    }
                    else
                    {
                        throw new FormatException("Page range was entered in an invalid format.");
                    }
                }

                pages.Sort();
                return pages;
            }
            catch (Exception ex)
            {
                LogHistory("Unable to read Pages setting: " + ex.Message);
                Process.SetStatus(ProcessStatus.Errored);
                return new List<int>();
            }
        }
    }
}
