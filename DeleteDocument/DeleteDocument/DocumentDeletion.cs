using System;

using RestSharp;
using RestSharp.Authenticators;

using Square9.ApiConnector;
using Square9.AuthenticationSettings;
using Square9.CustomNode;
using Square9.Objects;

namespace DeleteDocument
{
    public class DocumentDeletion : CustomNode
    {
        public override void Run()
        {
            if (Process.ProcessType != ProcessType.GlobalAction)
            {
                LogHistory("\"Delete Document\" may only be used for GlobalAction processes.");
                return;
            }

            Square9Api s9Api = null;
            CustomRestRequests s9ApiCustom = null;
            try
            {
                var s9ApiUrl = Engine.GetEngineConfigSetting("Square9Api");

                string username, password;
                using (var authSettings = new AuthenticationSettingsManager())
                {
                    username = authSettings.GetStoredAdminUser();
                    password = authSettings.GetStoredAdminPassword();
                }

                s9Api = new Square9Api(s9ApiUrl, username, password);
                s9ApiCustom = new CustomRestRequests(s9ApiUrl, username, password);
            }
            catch (Exception ex)
            {
                LogHistory("Unable to initialize Square 9 API connection: " + ex.Message);
                return;
            }

            try
            {
                var license = s9Api.Requests.Licenses.GetLicense();

                var databaseId = Process.Document.GetDocumentDatabaseId();
                var archiveId = Process.Document.GetDocumentArchiveId();
                var docId = Process.Document.GetDocumentId();

                var secureId = s9Api.Requests.Documents.GetDocumentSecureID(databaseId, archiveId, docId, license.Token);

                s9ApiCustom.DeleteDocument(databaseId, archiveId, docId, license.Token, secureId);

                s9Api.Requests.Licenses.ReleaseLicense(license.Token);
            }
            catch (Exception ex)
            {
                LogHistory(ex.Message);
                return;
            }
        }
    }

    public class CustomRestRequests
    {
        private RestClient ApiClient;

        public CustomRestRequests(string url, string username, string password)
        {
            ApiClient = new RestClient(url)
            {
                Authenticator = new HttpBasicAuthenticator(username, password)
            };
        }

        /// <summary>
        /// Deletes a document from an Archive.
        /// </summary>
        /// <param name="DatabaseID"></param>
        /// <param name="ArchiveID"></param>
        /// <param name="DocumentID"></param>
        /// <param name="Token"></param>
        /// <param name="SecureID"></param>
        public void DeleteDocument(int DatabaseID, int ArchiveID, int DocumentID, string Token, string SecureID)
        {
            var request = new RestRequest("api/dbs/{db}/archives/{arch}/documents/{doc}/Delete", Method.GET);

            request.AddParameter("db", DatabaseID, ParameterType.UrlSegment);
            request.AddParameter("arch", ArchiveID, ParameterType.UrlSegment);
            request.AddParameter("doc", DocumentID, ParameterType.UrlSegment);

            request.AddParameter("token", Token, ParameterType.QueryString);
            request.AddParameter("Secureid", SecureID, ParameterType.QueryString);

            var deletionResponse = ApiClient.Execute(request);

            if (deletionResponse.StatusCode != System.Net.HttpStatusCode.OK)
            {
                if (deletionResponse.Content.Contains("LicenseExpired"))
                {
                    throw new LicenseExpiredException(deletionResponse.Content);
                }
                else
                {
                    var error = "Unable to delete document: ";
                    error += !String.IsNullOrEmpty(deletionResponse.ErrorMessage) ? deletionResponse.ErrorMessage : deletionResponse.Content;
                    throw new ApiException(error);
                }
            }
        }
    }
}