using System;

using Square9.CustomNode;

namespace DeleteDocument
{
    public class DocumentDeletion : CustomNode
    {
        public override void Run()
        {
            if (Process.ProcessType != ProcessType.GlobalAction)
            {
                LogHistory("\"Delete Document\" may only be used for GlobalAction processes.");
                Process.SetStatus(ProcessStatus.Errored);
                return;
            }

            RestRequests s9ApiRequests = null;
            try
            {
                var s9ApiUrl = Engine.GetEngineConfigSetting("Square9Api");

                var adminCredentials = Authentication.GetAdminCredentials();
                var username = adminCredentials.Key;
                var password = adminCredentials.Value;

                s9ApiRequests = new RestRequests(s9ApiUrl, username, password);
            }
            catch (Exception ex)
            {
                LogHistory("Unable to initialize Square 9 API connection: " + ex.Message);
                Process.SetStatus(ProcessStatus.Errored);
                return;
            }

            try
            {
                var licenseToken = s9ApiRequests.GetLicenseToken();

                var databaseId = Process.Document.GetDocumentDatabaseId();
                var archiveId = Process.Document.GetDocumentArchiveId();
                var docId = Process.Document.GetDocumentId();

                var secureId = s9ApiRequests.GetDocumentSecureID(databaseId, archiveId, docId, licenseToken);

                s9ApiRequests.DeleteDocument(databaseId, archiveId, docId, licenseToken, secureId);

                s9ApiRequests.ReleaseLicense(licenseToken);
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