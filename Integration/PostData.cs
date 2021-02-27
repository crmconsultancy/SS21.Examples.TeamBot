using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using SS21.Examples.Integration.GraphAPI;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using SS21.Examples.Integration.GraphAPI.Authentication;

namespace SS21.Examples.Integration
{
    public static class PostData
    {
        private static readonly HttpClient client = new HttpClient();
        public async static Task<Response> Execute(Request request)
        {
            int errPoint = 0;

            string responseContent = "No Response";
            string payload = string.Empty;
            Uri serviceUrl = null;

            IntegrationException.ExceptionType exceptionType = IntegrationException.ExceptionType.Internal;
            string requestString = string.Empty;
            string responseString = string.Empty;
            Dictionary<string, string> requestHeaders = new Dictionary<string, string>();

            try
            {
                Response resp = null;

                errPoint = 1;
                if (request.Credentials != null)
                {
                    serviceUrl = new Uri(@request.Credentials.Url + request.Function);
                }
                else
                {
                    serviceUrl = new Uri(request.TargetUrl + request.Function);
                }

                errPoint = 2;
                string httpMethod = request.HttpMethod.ToString();

                errPoint = 3;
                HttpRequestMessage webRequest = new HttpRequestMessage(new System.Net.Http.HttpMethod(httpMethod), serviceUrl);

                errPoint = 4;
                if (request.Credentials != null)
                {
                    webRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    webRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.Credentials.AccessToken);
                }
                else
                {
                    webRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                }

                errPoint = 5;
                payload = request.ReturnPayload();

                if (!string.IsNullOrEmpty(payload))
                {
                    webRequest.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                }

                errPoint = 6;
                HttpResponseMessage webResponse = null;
                webResponse = await client.SendAsync(webRequest).ConfigureAwait(false);

                if (webResponse.Content != null)
                {
                    errPoint = 61;
                    responseContent = await webResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                    errPoint = 62;
                    resp = request.ResponseObject(responseContent);

                    return resp;
                }
                else
                {
                    errPoint = 71;
                    resp = request.ResponseObject(null);
                    return resp;
                }
            }
            catch (Exception e)
            {
                string exceptionMessage = "PostData[" + errPoint + "] ";
                if (serviceUrl != null)
                {
                    exceptionMessage += "(" +serviceUrl.ToString() + ") ";
                }
                if (!String.IsNullOrEmpty(payload))
                {
                    exceptionMessage += "[" + payload + "] ";
                }
                exceptionMessage += "- " + responseContent + ". ";
                exceptionMessage += " : " + e.Message + Environment.NewLine;

                if (e.InnerException != null)
                {
                    exceptionMessage += " Inner Exception - " + e.InnerException + Environment.NewLine;
                }
                
                throw new Exception(exceptionMessage);

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

                errPoint = 1;
                daemonClient = ConfidentialClientApplicationBuilder.Create(authenticationRequest.Credentials.ClientId)
                    .WithAuthority(string.Format(authenticationRequest.AuthorityFormat, authenticationRequest.Credentials.TenantId))
                    .WithRedirectUri(string.Empty)
                    .WithClientSecret(authenticationRequest.Credentials.ClientSecret)
                    .Build();

                errPoint = 2;
                // attempt to retrieve a valid access token to invoke the Graph API operations
                AuthenticationResult authenticationResult = await daemonClient.AcquireTokenForClient(new[] { authenticationRequest.GraphScope })
                    .ExecuteAsync().ConfigureAwait(false);

                errPoint = 3;
                GraphAPI.AuthenticationResponse authenticationResponse = new GraphAPI.AuthenticationResponse(authenticationResult);

                return authenticationResponse;
            }
            catch (Exception e)
            {
                string exceptionMessage = e.Message + Environment.NewLine;

                if (e.InnerException != null)
                {
                    exceptionMessage += " Inner Exception - " + e.InnerException + Environment.NewLine;
                }

                throw new Exception("Authenticate[" + errPoint + "] - " + exceptionMessage);
                // throw new IntegrationException(exceptionMessage, "PostData", "Authenticate", exceptionType, errPoint, requestString, requestHeaders, responseString);
            }
        }
        public async static Task<GraphAPI.AuthenticationResponse> AuthenticateOnBehalfOf(GraphAPI.AuthenticationRequest authenticationRequest, string authToken)
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

                GraphAPI.AuthenticationResponse authenticationResponse = new GraphAPI.AuthenticationResponse(authenticationResult);

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
        public async static Task<GraphAPI.Authentication.ResponseObjects.BaseObjects.AzureUserAuth> GetAccessToken(GraphAPI.AuthenticationRequest authenticationRequest, string grant_type, string usr, string pwd)
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

                GraphAPI.Authentication.ResponseObjects.BaseObjects.AzureUserAuth aua = JsonConvert.DeserializeObject<GraphAPI.Authentication.ResponseObjects.BaseObjects.AzureUserAuth>(json);

                return aua;
            }
        }
    }
}
