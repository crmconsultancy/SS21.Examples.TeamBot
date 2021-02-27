using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS21.Examples.Integration.GraphAPI.MicrosoftTeams.Chat
{
    public enum RetrievalScope
    {
        Channel = 0,
        Replies = 1
    }
    public class RetrieveMessagesRequest : Request
    {
        public RetrieveMessagesRequest(Credentials credentials, string teamId, string channelId, RetrievalScope retrievalScope, string messageId = null) : base(credentials)
        {
            base.Name = "Team.RetrieveMessagesRequest";
            base.HttpMethod = new System.Net.Http.HttpMethod("Get");

            if (retrievalScope == RetrievalScope.Channel)
            { 
                base.Function = "beta/teams/" + teamId + "/channels/" + channelId + "/messages";
            }
            else if(retrievalScope == RetrievalScope.Replies && !string.IsNullOrEmpty(messageId))
            {
                base.Function = "beta/teams/" + teamId + "/channels/" + channelId + "/messages/" + messageId + "/replies";
            }
        }
       
        public override SS21.Examples.Integration.Response ResponseObject(string data)
        {
            return new RetrieveMessagesResponse(data);
        }
        public override string ReturnPayload()
        {
            return string.Empty;
        }
    }
}
