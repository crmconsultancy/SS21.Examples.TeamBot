using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SS21.Examples.Integration
{
    public abstract class Request
    {
        private string _name;
        private Credentials _credentials;
        private string _contentType = "application/json";
        private HttpMethod _httpMethod;
        private string _targetUrl;
        private string _function;
        private object _baseObject;
        private string _accessToken;
        private string _payload;
        public Request()
        {
            _credentials = null;
        }

        public Request(string url)
        {
            _credentials = null;
            _targetUrl = url;
        }

        public Request(Credentials credentials)
        {
            _credentials = credentials;
            _targetUrl = _credentials.Url + _function;
        }

        public virtual Response ResponseObject(string data)
        {
            return null;
        }

        public virtual string ReturnPayload()
        {
            return null;
        }
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }
        public Credentials Credentials
        {
            get
            {
                return _credentials;
            }
        }
        public string ContentType
        {
            get
            {
                return _contentType;
            }
            set
            {
                _contentType = value;
            }
        }

        public HttpMethod HttpMethod
        {
            get
            {
                return _httpMethod;
            }
            set
            {
                _httpMethod = value;
            }
        }
        public string TargetUrl
        {
            get
            {
                return _targetUrl;
            }
            set
            {
               _targetUrl = value;
            }
        }
        public string Function
        {
            get
            {
                return _function;
            }
            set
            {
                _function = value;
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
        public object BaseObject
        {
            get
            {
                return _baseObject;
            }
            set
            {
                _baseObject = value;
            }
        }
    }
}
