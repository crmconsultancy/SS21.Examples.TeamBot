using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace SS21.Examples.Integration.GraphAPI
{
    public abstract class Response : SS21.Examples.Integration.Response
    {
        public Response(string data) : base(data)
        {

        }
        public abstract void ProcessResponseHeaders(HttpResponseHeaders responseHeaders);
    }
}
