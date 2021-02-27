using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS21.Examples.Integration.GraphAPI
{
    public class AuthenticationRequest : Request
    {
        private const string _authorityFormat = "https://login.microsoftonline.com/{0}/v2.0";
        private const string _graphScope = "https://graph.microsoft.com/.default";

        public AuthenticationRequest(Credentials credentials) : base(credentials)
        {
            // default constructor
        }
        public string AuthorityFormat
        {
            get
            {
                return _authorityFormat;
            }
        }
        public string GraphScope
        {
            get
            {
                return _graphScope;
            }
        }
    }
}
