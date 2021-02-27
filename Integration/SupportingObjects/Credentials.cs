using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS21.Examples.Integration
{
    public enum API
    {
        GraphAPI = 0,
        DocDriveAPI = 1
    }
    public class Credentials
    {
        private string _clientId;
        private string _clientSecret;
        private string _tenantId;
        private string _url = @"https://graph.microsoft.com/";
        private API _api = API.GraphAPI;

        // upon successful authentication
        public string _accessToken;

        public Credentials(API api, string clientId, string clientSecret, string tenantId)
        {
            _api = api;
            _clientId = clientId;
            _clientSecret = clientSecret;
            _tenantId = tenantId;       
        }
        public Credentials(API api, string url)
        {
            _api = api;
            _url = url;
        }
        public string ClientId
        {
            get
            {
                return _clientId;
            }
        }
        public string ClientSecret
        {
            get
            {
                return _clientSecret;
            }
        }
        public string TenantId
        {
            get
            {
                return _tenantId;
            }
        }
        public string Url
        {
            get
            {
                return _url;
            }
        }
        public string AccessToken
        {
            get
            {
                return _accessToken;
            }
            set
            {
                _accessToken = value;
            }
        }
        public API API
        {
            get
            {
                return _api;
            }
            set
            {
                _api = value;
            }
        }
    }
}
