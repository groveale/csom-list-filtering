using Microsoft.Identity.Client;
using Microsoft.SharePoint.Client;
using System;
using System.Linq.Expressions;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Groverale
{

    class Program
    {
        static async Task Main(string[] args)
        {
            string siteUrl = "https://groverale.sharepoint.com/sites/toomanylists";

            string clientId = "4d3e3609-0313-4bf8-8b07-17d228f98808"; //e.g. 01e54f9a-81bc-4dee-b15d-e661ae13f382

            string certThumprint = "158E6A5066973CA9F6AE580B783967B1EFCC56C8"; // e.g. CE20E000D53A4C968ED8BA3EFC92C40A2692AE98

            //For SharePoint app only auth, the scope will be the SharePoint tenant name followed by /.default
            var scopes = new string[] { "https://groverale.sharepoint.com/.default" };

            //Tenant id can be the tenant domain or it can also be the GUID found in Azure AD properties.
            string tenantId = "groverale.onmicrosoft.com";

            var accessToken = await GetApplicationAuthenticatedClient(clientId, certThumprint, scopes, tenantId);

            var clientContext = GetClientContextWithAccessToken(siteUrl, accessToken);

            Web web = clientContext.Web;

            clientContext.Load(web);

            clientContext.ExecuteQuery();

            Console.WriteLine(web.Title);

            // Load lists
            ListCollection siteLists = web.Lists;

            // Only take is supported - does not help 
            //SkipAndTakePaging(siteLists, clientContext, 1000, 1000);

            // TitleFiltering
            //LinqWhereFiltering(siteLists, clientContext, "List-1");

            PagingHack(web, clientContext);
            
        }

        public static void PagingHack(Web web, ClientContext clientContext)
        {
            var paging = new ListCollectionPosition();
            paging.PagingInfo = "skip";

            GetListsParameters GetListQuery = new GetListsParameters();
            GetListQuery.RowLimit = 10;
            GetListQuery.ListCollectionPosition = paging;

            CamlQuery camlQuery = new CamlQuery();
            camlQuery.ViewXml = "<View Scope=\"RecursiveAll\"></View>";

            
            var query = web.GetLists(GetListQuery);
            var lists = clientContext.LoadQuery(query);
            clientContext.ExecuteQuery();

            

            foreach (var list in lists)
            {
                Console.WriteLine($"Title: {list.Title} ID: {list.Id}");
            }
        }


        public static void SkipAndTakePaging(ListCollection siteLists, ClientContext clientContext, int skip, int take)
        {
            IEnumerable<List> queryResults = clientContext.LoadQuery(
                            siteLists.Take(take)
                            .Include(
                                list => list.Title,
                                list => list.Id));

            clientContext.ExecuteQuery();

            foreach (var list in queryResults)
            {
                Console.WriteLine($"Title: {list.Title} ID: {list.Id}");
            }
        }

        public static void LinqWhereFiltering(ListCollection siteLists, ClientContext clientContext, string titleFilter)
        {
            IEnumerable<List> queryResults = clientContext.LoadQuery(
             siteLists.Include(
                 list => list.Title,
                 list => list.Id).Where(
                     list => list.Title == titleFilter));
                     //list => list.Id.ToString().StartsWith(titleFilter)));

            clientContext.ExecuteQuery();

            foreach (var list in queryResults)
            {
                Console.WriteLine($"Title: {list.Title} ID: {list.Id}");
            }
        }


        internal static async Task<string> GetApplicationAuthenticatedClient(string clientId, string certThumprint, string[] scopes, string tenantId)
        {
            X509Certificate2 certificate = GetAppOnlyCertificate(certThumprint);
            IConfidentialClientApplication clientApp = ConfidentialClientApplicationBuilder
                                            .Create(clientId)
                                            .WithCertificate(certificate)
                                            .WithTenantId(tenantId)
                                            .Build();

            AuthenticationResult authResult = await clientApp.AcquireTokenForClient(scopes).ExecuteAsync();
            string accessToken = authResult.AccessToken;
            return accessToken;
        }

        public static ClientContext GetClientContextWithAccessToken(string targetUrl, string accessToken)
        {
            ClientContext clientContext = new ClientContext(targetUrl);
            clientContext.ExecutingWebRequest +=
                delegate (object oSender, WebRequestEventArgs webRequestEventArgs)
                {
                    webRequestEventArgs.WebRequestExecutor.RequestHeaders["Authorization"] =
                        "Bearer " + accessToken;
                };
            return clientContext;
        }


        private static X509Certificate2 GetAppOnlyCertificate(string thumbPrint)
        {
            X509Certificate2 appOnlyCertificate = null;
            using (X509Store certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                certStore.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection certCollection = certStore.Certificates.Find(X509FindType.FindByThumbprint, thumbPrint, false);
                if (certCollection.Count > 0)
                {
                    appOnlyCertificate = certCollection[0];
                }
                certStore.Close();
                return appOnlyCertificate;
            }
        }
    }
}