using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace SS21.Examples.Integration.GraphAPI.MicrosoftTeams.Chat
{
    public class RetrieveMessagesResponse : Response
    {
        private Messages _messages;

        public RetrieveMessagesResponse(string data) : base(data)
        {
            var messages = JsonConvert.DeserializeObject<MicrosoftTeams.Chat.BaseObjects.RetrieveMessagesResponseBase>(data);

            if (messages != null)
            {
                _messages = new Messages(messages.value);
            }
        }

        public Messages Messages
        {
            get
            {
                return _messages;
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
