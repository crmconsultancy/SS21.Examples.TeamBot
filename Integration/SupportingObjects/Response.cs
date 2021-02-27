using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace SS21.Examples.Integration
{
    public abstract class Response
    {
        private string _data;
        public Response(string data)
        {
            _data = data;
        }
        public string Data
        {
            get
            {
                return _data;
            }
        }
    }
}
