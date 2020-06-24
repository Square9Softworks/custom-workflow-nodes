using System;

using Square9.CustomNode;

namespace DeleteDocument
{
    public class DocumentDeletion : ActionNode
    {
        public override void Run()
        {
            RestRequests s9ApiRequests = null;
            try
            {
                var s9ApiClient = Engine.GetSquare9ApiClient();
                s9ApiRequests = new RestRequests(s9ApiClient);
            }
            catch (Exception ex)
            {
                LogHistory("Unable to initialize Square 9 API connection: " + ex.Message);
                Process.SetStatus(ProcessStatus.Errored);
                return;
            }

            try
            {
                var databaseId = Process.Document.DatabaseId;
                var archiveId = Process.Document.ArchiveId;
                var docId = Process.Document.DocumentId;

                var secureId = s9ApiRequests.GetDocumentSecureID(databaseId, archiveId, docId);

                s9ApiRequests.DeleteDocument(databaseId, archiveId, docId, secureId);
            }
            catch (Exception ex)
            {
                LogHistory(ex.Message);
                Process.SetStatus(ProcessStatus.Errored);
                return;
            }
        }
    }
}