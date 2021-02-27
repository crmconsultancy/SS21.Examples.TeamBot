using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS21.Examples.Integration
{
    public class IntegrationException : Exception
    {
        private ExceptionType _exceptionType;
        private string _className;
        private string _function;
        private int _errorPoint;
        private string _exceptionMessage;
        private string _requestString;
        public Dictionary<string, string> _requestHeaders;
        private string _responseString;

        public IntegrationException()
        {
            _requestHeaders = new Dictionary<string, string>();
        }

        public IntegrationException(string message)
        : base(message)
        {
            _requestHeaders = new Dictionary<string, string>();
        }
        public IntegrationException(string message, string aClass, string function, IntegrationException.ExceptionType type, int errorPoint = 0, string requestString = null, Dictionary<string, string> requestHeaders = null, string responseString = null) : base(message)
        {
            _exceptionMessage = message;
            _className = aClass;
            _function = function;
            _exceptionType = type;
            _errorPoint = errorPoint;
            if (!string.IsNullOrEmpty(requestString))
            {
                _requestString = requestString;
            }

            if(requestHeaders != null)
            {
                _requestHeaders = requestHeaders;
            }

            if (!string.IsNullOrEmpty(responseString))
            {
                _responseString = responseString;
            }

            BuildErrorMessage();
        } 
        public IntegrationException(string message, Exception innerException)
        : base(message, innerException)
        {
            _requestHeaders = new Dictionary<string, string>();
        }

        public void BuildErrorMessage()
        {
            _exceptionMessage = _exceptionType.ToString() + " Exception thrown from '" + _className + "' Class at '" + _function + "' Function." + Environment.NewLine;
            _exceptionMessage += " " + base.Message;
        }
        public string RequestHeadersAsString
        {
            get
            {
                string headers = string.Empty;

                if (_requestHeaders != null)
                {
                    foreach (KeyValuePair<string, string> kv in _requestHeaders)
                    {
                        headers += kv.Key + ": " + kv.Value + Environment.NewLine;
                    }
                }
                return headers;
            }
        }
        public string Class
        {
            get { return _className; }
            set { _className = value; }
        }
        public string Function
        {
            get { return _function; }
            set { _function = value; }
        }
        public ExceptionType Type
        {
            get { return _exceptionType; }
            set { _exceptionType = value; }
        }
        public string ExceptionMessage
        {
            get { return _exceptionMessage; }
            set { _exceptionMessage = value; }
        }
        public string RequestString
        {
            get { return _requestString; }
            set { _requestString = value; }
        }
        public string ResponseString
        {
            get { return _responseString; }
            set { _responseString = value; }
        }
        public Dictionary<string, string> RequestHeaders
        {
            get { return _requestHeaders; }
            set { _requestHeaders = value; }
        }
        public enum ExceptionType
        {
            Validation = 0,
            Internal = 1,
            General = 3,
            GraphApi = 4
        }
    }
}
