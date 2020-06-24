using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Square9.CustomNode;

namespace InboxImport
{
    public class InboxPickup : ActionImporter
    {
        public override List<GlobalSearchDocument> Import()
        {
            var documents = new List<GlobalSearchDocument>();

            RestRequests s9ApiRequests = null;
            try
            {
                var s9ApiClient = Engine.GetSquare9ApiClient();
                s9ApiRequests = new RestRequests(s9ApiClient);
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to initialize Square 9 API connection: " + ex.Message);
            }

            InboxList inboxes = s9ApiRequests.GetInboxes();
            var inboxName = Settings.GetStringSetting("InboxName");
            Inbox selectedInbox = inboxes.Inboxes.FirstOrDefault(x => x.Name == inboxName);
            if (selectedInbox == null)
            {
                throw new Exception("The requested source inbox does not exist.");
            }

            var inboxFiles = Directory.GetFiles(selectedInbox.Path).ToList();
            if (inboxFiles.Count > 0)
            {
                ArchiveList archives = s9ApiRequests.GetArchives(DatabaseId, 0);
                var archiveName = Settings.GetStringSetting("ArchiveName");
                Archive selectedArchive = archives.Archives.FirstOrDefault(x => x.Name == archiveName);
                if (selectedArchive == null)
                {
                    throw new Exception("The requested destination archive does not exist.");
                }

                for (var i = 0; i < inboxFiles.Count; i++)
                {
                    var uploadedFileName = s9ApiRequests.PostFile(inboxFiles[i]);
                    var newDocUpdateSession = new KeyValuePair<Guid, string>(Guid.NewGuid(), "");
                    var docIdHashPair = s9ApiRequests.IndexDocument(DatabaseId, selectedArchive.Id, uploadedFileName, newDocUpdateSession);
                    var newDocId = docIdHashPair.Key;

                    documents.Add(new GlobalSearchDocument(selectedArchive.Id, newDocId));

                    System.IO.File.Delete(inboxFiles[i]);
                }
            }

            return documents;
        }
    }
}
