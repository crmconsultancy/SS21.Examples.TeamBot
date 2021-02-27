using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SS21.Examples.Integration.GraphAPI.MicrosoftTeams.Chat
{
    public class Message
    {
        private string _id;
        private object _replyToId;
        private string _etag;
        private string _messageType;
        private DateTime _createdDateTime;
        private object _lastModifiedDateTime;
        private object _deletedDateTime;
        private object _subject;
        private object _summary;
        private string _importance;
        private string _locale;
        private string _webUrl;
        private object _policyViolation;
        private From _from;
        private Body _body;
        private object[] _attachments;
        //private Mention[] mentions;
        private object[] _reactions;
        private Messages _replies;
        public Message()
        {
            // default constructor
        }
        public Message(BaseObjects.Message baseMessage)
        {
            _id = baseMessage.id;
            _replyToId = baseMessage.replyToId;
            _etag = baseMessage.etag;
            _messageType = baseMessage.messageType;
            _createdDateTime = baseMessage.createdDateTime;
            _lastModifiedDateTime = baseMessage.lastModifiedDateTime;
            _deletedDateTime = baseMessage.deletedDateTime;
            _subject = baseMessage.subject;
            _summary = baseMessage.summary;
            _importance = baseMessage.importance;
            _locale = baseMessage.locale;
            _webUrl = baseMessage.webUrl;
            _policyViolation = baseMessage.policyViolation;
            _attachments = baseMessage.attachments;
            _reactions = baseMessage.reactions;
            _body = new Body(baseMessage.body);
            _from = new From(baseMessage.from);
            _replies = new Messages();
        }
        public Entity ToDynamicsNote(EntityReference regardingRecord = null)
        {
            // TODO: Optional regarding field dictated by Channel?

            Entity note = new Entity("annotation");
            note["notetext"] = _body.RawContent;

            if(regardingRecord != null)
            {
                note["regardingobjectid"] = regardingRecord;
            }

            // documentbody Edm.String - Contents of the note's attachment.
            // documentbody_binary - Edm.Binary - Contents of the note's attachment.
            // filename - Edm.String - File name of the note.
            // filesize - Edm.Int32 - File size of the note.
            // isdocument - Edm.Boolean - Specifies whether the note is an attachment.

            // Journal note implementation
            //Entity note = new Entity("crmcs_journalnote");
            //note["crmcs_name"] = _body.Content;
            //note["crmcs_notetext"] = _body.Content;
            //// defaults as 'other' note category
            //note["crmcs_notecategory"] = new OptionSetValue(651240003);
            return note;
        }
        public bool Contains(string value)
        {
            if (_body.Content.Contains(value))
            {
                return true;
            }
            return false;
        }
        public string Id
        {
            get
            {
                return _id;
            }
        }
        public object ReplyToId
        {
            get
            {
                return _replyToId;
            }
        }
        public string Etag
        {
            get
            {
                return _etag;
            }
        }
        public string MessageType
        {
            get
            {
                return _messageType;
            }
        }
        public DateTime CreatedDateTime
        {
            get
            {
                return _createdDateTime;
            }
        }
        public object LastModifiedDateTime
        {
            get
            {
                return _lastModifiedDateTime;
            }
        }
        public object DeletedDateTime
        {
            get
            {
                return _deletedDateTime;
            }
        }
        public object Subject
        {
            get
            {
                return _subject;
            }
        }
        public object Summary
        {
            get
            {
                return _subject;
            }
        }

        public string Importance
        {
            get
            {
                return _importance;
            }
        }
        public string Locale
        {
            get
            {
                return _locale;
            }
        }
        public string WebUrl
        {
            get
            {
                return _webUrl;
            }
        }
        public object PolicyViolation
        {
            get
            {
                return _policyViolation;
            }
        }
        public From From
        {
            get
            {
                return _from;
            }
        }
        public Body Body
        {
            get
            {
                return _body;
            }
        }
        public object[] Attachments
        {
            get
            {
                return _attachments;
            }
        }
        //public Mention[] mentions
        public object[] Reactions
        {
            get
            {
                return _reactions;
            }
        }
        public Messages Replies
        {
            get
            {
                return _replies;
            }
            set
            {
                _replies = value;
            }
        }
    }

    public class Messages : List<Message>
    {
        public Messages()
        {

        }
        public Messages(BaseObjects.Message[] baseMessages)
        {
            this.AddRange(baseMessages.Select(x => new Message(x)).ToList());
        }

        public bool Contains(string value, Messages collection)
        {
            Messages subset = new Messages();

            subset.AddRange(this.Where(x => x.Body.Content.Contains(value) || x.Replies.Contains(value, subset)));

            if (subset.Count > 0)
            {
                collection.AddRange(subset);
                return true;
            }
            return false;
        }
        public bool Contains(string messageId)
        {
            return this.Any(x => x.Id == messageId);
        }

        public Message ExtractConversationThread(string messageId)
        {
            Message parent = null;
            foreach (Message message in this)
            {
                // if the message contains the current message within the replies
                if (message.Replies.Contains(messageId))
                {
                    // key parent record has been detected
                    parent = message;
                    break;
                }
            }
            return parent;
        }
    }
    public class From
    {
        private User _user;
        public From(BaseObjects.From baseFrom)
        {
            if (baseFrom.user != null)
            {
                _user = new User(baseFrom.user);
            }
        }
        public object application { get; set; }
        public object device { get; set; }
        public object conversation { get; set; }
        public User User
        {
            get
            {
                return _user;
            }
            set
            {
                _user = value;
            }
        }
    }

    public class User
    {
        private string _id;
        private string _displayName;
        private string _userIdentityType;
        public User(string id, string displayName, string userIdentityType)
        {
            _id = id;
            _displayName = displayName;
            _userIdentityType = userIdentityType;
        }
        public User(BaseObjects.User baseUser)
        {
            _id = baseUser.id;
            _displayName = baseUser.displayName;
            _userIdentityType = baseUser.userIdentityType;
        }
        public string Id
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
            }
        }
        public string DisplayName
        {
            get
            {
                return _displayName;
            }
            set
            {
                _displayName = value;
            }
        }
        public string UserIdentityType
        {
            get
            {
                return _userIdentityType;
            }
            set
            {
                _userIdentityType = value;
            }
        }
    }

    public class Body
    {
        private string _contentType;
        private string _content;
        private string _rawContent;
        public Body(string content, string contentType)
        {
            _content = content;
            _rawContent = RemoveHTML(_content);
            _contentType = contentType;
        }
        public Body(BaseObjects.Body baseBody)
        {
            _content = baseBody.content;
            _rawContent = RemoveHTML(_content);
            _contentType = baseBody.contentType;
        }
        public string ContentType
        {
            get
            {
                return _contentType;
            }
            set
            {
                _contentType = value;
            }
        }
        public string Content
        {
            get
            {
                return _content;
            }
            set
            {
                _content = value;
            }
        }
        public string RawContent
        {
            get
            {
                return _rawContent;
            }
            set
            {
                _rawContent = value;
            }
        }
        private  string RemoveHTML(string inputString)
        {
            if (string.IsNullOrEmpty(inputString))
            {
                return string.Empty;
            }

            string rawContent = Regex.Replace(inputString, "<.*?>", string.Empty);

            return rawContent;
        }
    }
}
