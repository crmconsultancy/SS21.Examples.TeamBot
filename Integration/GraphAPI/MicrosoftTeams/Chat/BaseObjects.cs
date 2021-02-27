using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS21.Examples.Integration.GraphAPI.MicrosoftTeams.Chat.BaseObjects
{
    public class RetrieveMessagesResponseBase
    {
        public string odatacontext { get; set; }
        public int odatacount { get; set; }
        public string odatanextLink { get; set; }
        public Message[] value { get; set; }
    }

    public class Message
    {
        public string id { get; set; }
        public object replyToId { get; set; }
        public string etag { get; set; }
        public string messageType { get; set; }
        public DateTime createdDateTime { get; set; }
        public object lastModifiedDateTime { get; set; }
        public object deletedDateTime { get; set; }
        public object subject { get; set; }
        public object summary { get; set; }
        public string importance { get; set; }
        public string locale { get; set; }
        public string webUrl { get; set; }
        public object policyViolation { get; set; }
        public From from { get; set; }
        public Body body { get; set; }
        public object[] attachments { get; set; }
        public Mention[] mentions { get; set; }
        public object[] reactions { get; set; }
    }

    public class From
    {
        public object application { get; set; }
        public object device { get; set; }
        public object conversation { get; set; }
        public User user { get; set; }
    }

    public class User
    {
        public string id { get; set; }
        public string displayName { get; set; }
        public string userIdentityType { get; set; }
    }

    public class Body
    {
        public string contentType { get; set; }
        public string content { get; set; }
    }

    public class Mention
    {
        public int id { get; set; }
        public string mentionText { get; set; }
        public Mentioned mentioned { get; set; }
    }

    public class Mentioned
    {
        public object device { get; set; }
        public object user { get; set; }
        public object conversation { get; set; }
        public Application application { get; set; }
    }

    public class Application
    {
        public string id { get; set; }
        public string displayName { get; set; }
        public string applicationIdentityType { get; set; }
    }
}
