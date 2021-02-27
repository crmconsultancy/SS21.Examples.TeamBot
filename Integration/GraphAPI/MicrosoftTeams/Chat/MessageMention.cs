using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS21.Examples.Integration.GraphAPI.MicrosoftTeams.Chat
{
    public class MessageMention
    {
        public string DisplayName;
        public string Id;
        public string UserIdentityType;

        public MessageMention(string displayName, string id, string userIdentityType)
        {
            this.DisplayName = displayName;
            this.Id = id;
            this.UserIdentityType = userIdentityType;
        }

        public string ToJSON()
        {
            string json = "\"user\": { \"displayName\": \"[displayName]\", \"id\": \"[id]\", \"userIdentityType\": \"[userIdentityType]\" }";
            json = json.Replace("[displayName]", this.DisplayName);
            json = json.Replace("[id]", this.Id);
            json = json.Replace("[userIdentityType]", this.UserIdentityType);
            return json;
        }
    }
}
