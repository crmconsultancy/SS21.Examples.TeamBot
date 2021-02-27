using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace SS21.Examples.Integration.GraphAPI
{
    public class AuthenticationResponse
    {
        private string _accessToken;
        private DateTime _validTo;

        public AuthenticationResponse(AuthenticationResult authenticationResult)
        {
            _accessToken = authenticationResult.AccessToken;
            _validTo = authenticationResult.ExpiresOn.UtcDateTime.ToLocalTime();
        }

        public string AccessToken
        {
            get
            {
                return _accessToken;
            }
        }


    }
}
