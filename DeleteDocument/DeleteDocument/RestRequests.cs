using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;

namespace DeleteDocument
{
    public class RestRequests
    {
        public HttpClient Client;
        public JavaScriptSerializer serializer = new JavaScriptSerializer();

        public RestRequests(string url, string username, string password)
        {
            var authHeader = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));

            Client = new HttpClient()
            {
                BaseAddress = new Uri(url.EndsWith("/") ? url : url + "/"),
                DefaultRequestHeaders = { Authorization = authHeader}
            };
            Client.DefaultRequestHeaders.Add("User-Agent", $"DeleteDocument/{Assembly.GetExecutingAssembly().GetName().Version}");
        }

        /// <summary>
        /// Gets a license token if one is available and user authentication is valid.
        /// </summary>
        /// <returns></returns>
        public string GetLicenseToken()
        {
            var builder = new UriBuilder(Client.BaseAddress + "api/licenses");
            var requestUrl = builder.ToString();

            var response = Client.GetAsync(requestUrl).Result;
            var result = response.Content.ReadAsStringAsync().Result;
            if (response.IsSuccessStatusCode)
            {
                var license = serializer.Deserialize<License>(result);
                return license.Token;
            }
            else
            {
                throw new Exception("Unable to get a License: " + result);
            }
        }

        /// <summary>
        /// Releases a license token from the API.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public void ReleaseLicense(string token)
        {
            var builder = new UriBuilder(Client.BaseAddress + $"api/licenses/{token}");
            var requestUrl = builder.ToString();

            var response = Client.GetAsync(requestUrl).Result;
            if (!response.IsSuccessStatusCode)
            {
                var error = response.Content.ReadAsStringAsync().Result;
                throw new Exception("Unable to release License token: " + error);
            }
        }

        /// <summary>
        /// Gets a document's secure ID. Requires API Full Access permissions in order to work.
        /// </summary>
        /// <param name="DatabaseID"></param>
        /// <param name="ArchiveID"></param>
        /// <param name="DocumentID"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public string GetDocumentSecureID(int DatabaseID, int ArchiveID, int DocumentID, string Token)
        {
            var builder = new UriBuilder(Client.BaseAddress + $"api/dbs/{DatabaseID}/archives/{ArchiveID}");
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["DocumentID"] = DocumentID.ToString();
            query["token"] = Token;
            builder.Query = query.ToString();
            var requestUrl = builder.ToString();

            var response = Client.GetAsync(requestUrl).Result;
            var result = response.Content.ReadAsStringAsync().Result;
            if (response.IsSuccessStatusCode)
            {
                return result;
            }
            else
            {
                throw new Exception("Unable to get Document Secure ID: " + result);
            }
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
            var builder = new UriBuilder(Client.BaseAddress + $"api/dbs/{DatabaseID}/archives/{ArchiveID}/documents/{DocumentID}/Delete");
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["token"] = Token;
            query["Secureid"] = SecureID;
            builder.Query = query.ToString();
            var requestUrl = builder.ToString();

            var response = Client.GetAsync(requestUrl).Result;
            if (!response.IsSuccessStatusCode)
            {
                var error = response.Content.ReadAsStringAsync().Result;
                throw new Exception("Unable to delete document: " + error);
            }
        }
    }
}
