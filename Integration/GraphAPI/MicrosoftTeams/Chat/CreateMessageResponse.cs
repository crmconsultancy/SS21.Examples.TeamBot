using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace SS21.Examples.Integration.GraphAPI.MicrosoftTeams.Chat
{
    public class CreateMessageResponse : Response
    {
        private string _messageId;

        public CreateMessageResponse(string data) : base(data)
        {
            var message = JsonConvert.DeserializeObject<MicrosoftTeams.Chat.BaseObjects.Message>(data);

            if (message != null)
            {
                _messageId = message.id;
            }
        }

        public string MessageId
        {
            get
            {
                return _messageId;
            }
        }

        public override void ProcessResponseHeaders(HttpResponseHeaders responseHeaders)
        {
            if(responseHeaders == null)
            {
                return;
            }
           
        }
    }
}
