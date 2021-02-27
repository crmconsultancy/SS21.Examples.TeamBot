using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SS21.Examples.Integration.GraphAPI
{
    public abstract class Request : SS21.Examples.Integration.Request
    {
        public Request() : base()
        {
        }

        public Request(string url) : base(url)
        {

        }
        public Request(Credentials credentials) : base(credentials)
        {
     
        }
        public override SS21.Examples.Integration.Response ResponseObject(string data)
        {
            return null;
        }
    }
}
