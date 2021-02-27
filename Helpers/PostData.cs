using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

using Newtonsoft.Json;
using SS21.Examples.Integration.GraphAPI;
using SS21.Examples.Integration.GraphAPI.Authentication;

namespace SS21.Examples.TeamBot.Helpers
{
    public static class PostData
    {
        private static readonly HttpClient client = new HttpClient();
        public async static Task<Response> Execute(Request request)
        {
            int errPoint = 0;

            IntegrationException.ExceptionType exceptionType = IntegrationException.ExceptionType.Internal;
            string requestString = string.Empty;
            string responseString = string.Empty;
            Dictionary<string, string> requestHeaders = new Dictionary<string, string>();

            try
            {
                Response resp = null;

                // ex: https://graph.microsoft.com/v1.0/users
                Uri graphQueryUrl = null;
                if (request.Credentials != null)
                {
                    graphQueryUrl = new Uri(@request.Credentials.Url + request.Function);
                }
                else
                {
                    graphQueryUrl = new Uri(request.TargetUrl + request.Function);
                }

                string httpMethod = request.HttpMethod.ToString();

                HttpRequestMessage webRequest = new HttpRequestMessage(new System.Net.Http.HttpMethod(httpMethod), graphQueryUrl);
                webRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (request.Credentials != null)
                {
                    webRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.Credentials.AccessToken);
                }

                string requestJSON = request.ToJSON();

                if (!string.IsNullOrEmpty(requestJSON))
                {
                    //webRequest.Headers.Add("content-type", "application/json");
                    webRequest.Content = new StringContent(requestJSON, Encoding.UTF8, "application/json");
                }

                HttpResponseMessage webResponse = null;
                webResponse = await client.SendAsync(webRequest).ConfigureAwait(false);

                if (webResponse.Content != null)
                {
                    string responseContent = await webResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                    resp = request.ResponseObject(responseContent);

                    // capture relevant information from the response headers
                    resp.ProcessResponseHeaders(webResponse.Headers);

                    return resp;
                }
                else
                {
                    resp = request.ResponseObject(null);

                    return resp;
                }
            }
            catch (Exception e)
            {
                string exceptionMessage = e.Message + Environment.NewLine;

                if (e.InnerException != null)
                {
                    exceptionMessage += " Inner Exception - " + e.InnerException + Environment.NewLine;
                }
                throw new IntegrationException(exceptionMessage, "PostData", "Execute", exceptionType, errPoint, requestString, requestHeaders, responseString);
            }
        }

        public async static Task<AuthenticationResponse> Authenticate(AuthenticationRequest authenticationRequest)
        {
            int errPoint = 0;

            IntegrationException.ExceptionType exceptionType = IntegrationException.ExceptionType.Internal;
            string requestString = string.Empty;
            string responseString = string.Empty;
            Dictionary<string, string> requestHeaders = new Dictionary<string, string>();

            try
            {
                // initialise Daemon client by specifying Client Id, Client Secret, Tenant Id, Graph Scope and Authority Format
                IConfidentialClientApplication daemonClient;

                daemonClient = ConfidentialClientApplicationBuilder.Create(authenticationRequest.Credentials.ClientId)
                    .WithAuthority(string.Format(authenticationRequest.AuthorityFormat, authenticationRequest.Credentials.TenantId))
                    .WithRedirectUri(string.Empty)
                    .WithClientSecret(authenticationRequest.Credentials.ClientSecret)
                    .Build();

                // attempt to retrieve a valid access token to invoke the Graph API operations
                AuthenticationResult authenticationResult = await daemonClient.AcquireTokenForClient(new[] { authenticationRequest.GraphScope })
                    .ExecuteAsync().ConfigureAwait(false);

                AuthenticationResponse authenticationResponse = new AuthenticationResponse(authenticationResult);

                return authenticationResponse;
            }
            catch (Exception e)
            {
                string exceptionMessage = e.Message + Environment.NewLine;

                if (e.InnerException != null)
                {
                    exceptionMessage += " Inner Exception - " + e.InnerException + Environment.NewLine;
                }
                throw new IntegrationException(exceptionMessage, "PostData", "Authenticate", exceptionType, errPoint, requestString, requestHeaders, responseString);
            }
        }
        public async static Task<AuthenticationResponse> AuthenticateOnBehalfOf(AuthenticationRequest authenticationRequest, string authToken)
        {
            int errPoint = 0;

            IntegrationException.ExceptionType exceptionType = IntegrationException.ExceptionType.Internal;
            string requestString = string.Empty;
            string responseString = string.Empty;
            Dictionary<string, string> requestHeaders = new Dictionary<string, string>();

            try
            {
                // initialise Daemon client by specifying Client Id, Client Secret, Tenant Id, Graph Scope and Authority Format
                IConfidentialClientApplication daemonClient;

                daemonClient = ConfidentialClientApplicationBuilder.Create(authenticationRequest.Credentials.ClientId)
                    .WithAuthority(string.Format(authenticationRequest.AuthorityFormat, authenticationRequest.Credentials.TenantId))
                    .WithRedirectUri(string.Empty)
                    .WithClientSecret(authenticationRequest.Credentials.ClientSecret)
                    .Build();


                UserAssertion ua = new UserAssertion(authToken, "urn:ietf:params:oauth:grant-type:jwt-bearer");

                List<string> scopes = new List<string>();
                scopes.Add(authenticationRequest.GraphScope);

                // .Result to make sure that the cache is filled-in before the controller tries to get access tokens
                var result = daemonClient.AcquireTokenOnBehalfOf(scopes, ua)
                                                      .ExecuteAsync()
                                                      .GetAwaiter().GetResult();


                // attempt to retrieve a valid access token to invoke the Graph API operations
                AuthenticationResult authenticationResult = await daemonClient.AcquireTokenForClient(new[] { authenticationRequest.GraphScope })
                    .ExecuteAsync().ConfigureAwait(false);

                AuthenticationResponse authenticationResponse = new AuthenticationResponse(authenticationResult);

                return authenticationResponse;
            }
            catch (Exception e)
            {
                string exceptionMessage = e.Message + Environment.NewLine;

                if (e.InnerException != null)
                {
                    exceptionMessage += " Inner Exception - " + e.InnerException + Environment.NewLine;
                }
                throw new IntegrationException(exceptionMessage, "PostData", "Authenticate", exceptionType, errPoint, requestString, requestHeaders, responseString);
            }
        }
        public async static Task<SS21.Examples.Integration.GraphAPI.Authentication.ResponseObjects.BaseObjects.AzureUserAuth> GetAccessToken(AuthenticationRequest authenticationRequest, string grant_type, string usr, string pwd)
        {
            string tokenEndpointUri = "https://login.microsoftonline.com/" + authenticationRequest.Credentials.TenantId + "/oauth2/token";// +  // + " / "; + "oauth2/token";

            var content = new FormUrlEncodedContent(new[]
                {
            new KeyValuePair<string, string>("grant_type", grant_type),
            new KeyValuePair<string, string>("username", usr),
            new KeyValuePair<string, string>("password", pwd),

            new KeyValuePair<string, string>("client_id", authenticationRequest.Credentials.ClientId),
            new KeyValuePair<string, string>("client_secret", authenticationRequest.Credentials.ClientSecret),
            new KeyValuePair<string, string>("resource", "https://graph.microsoft.com"),
            new KeyValuePair<string, string>("scope", "https://graph.microsoft.com/.default")
        }
            );

            using (var client = new HttpClient())
            {
                HttpResponseMessage res = await client.PostAsync(tokenEndpointUri, content).ConfigureAwait(false);

                string json = await res.Content.ReadAsStringAsync().ConfigureAwait(false);

                SS21.Examples.Integration.GraphAPI.Authentication.ResponseObjects.BaseObjects.AzureUserAuth aua = 
                    JsonConvert.DeserializeObject<SS21.Examples.Integration.GraphAPI.Authentication.ResponseObjects.BaseObjects.AzureUserAuth>(json);

                return aua;
            }
        }
    }
}
