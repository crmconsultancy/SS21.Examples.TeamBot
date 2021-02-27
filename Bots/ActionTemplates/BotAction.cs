using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;

using Microsoft.PowerPlatform.Cds.Client;

namespace SS21.Examples.TeamBot.Bots
{
    /// <summary>
    /// Base Class for a Bot Action
    /// </summary>
    public abstract class BotAction
    {
        private string _teamId = string.Empty;
        private string _commandName = string.Empty;
        private string _fullCommandText = string.Empty;
        private string _commandValueTitle = string.Empty;
        private string _commandValueDescription = string.Empty;

        public string CommandText
        {
            get
            {
                return _fullCommandText;
            }
        }

        public string ValueTitle
        {
            get
            {
                return _commandValueTitle;
            }
        }

        public string ValueDescription
        {
            get
            {
                return _commandValueDescription;
            }
        }

        /// <summary>
        /// The Channel ID in Teams that the Command took place in
        /// </summary>
        internal string ChannelId = string.Empty;
        /// <summary>
        /// The Unique Message ID of the Command
        /// </summary>
        internal string MessageId = string.Empty;
        /// <summary>
        /// The full Conversation ID in Teams (including Channel ID and ;messageid=xxxxxx
        /// </summary>
        internal string ConversationId = string.Empty;
        /// <summary>
        /// Unique ID of the Thread that the Command was posted in
        /// </summary>
        internal string ConversationMessageId = string.Empty;

        internal TeamsToDynamicsConnection ToDynamicsConnection = null;
        internal ITurnContext<IMessageActivity> TurnContext = null;

        private CdsServiceClient _service = null;

        public string WarmUpMsg = null;

        public BotAction(string warmUpMsg = null, string commandName = null)
        {
            this.WarmUpMsg = warmUpMsg;
            this._commandName = commandName;
        }

        public void Finish()
        {
            ToDynamicsConnection = null;
            TurnContext = null;
        }

        public void Setup(ITurnContext<IMessageActivity> _turnContext, 
            TeamsToDynamicsConnection c)
        {
            TurnContext = _turnContext;
            ToDynamicsConnection = c;

            if ( !String.IsNullOrEmpty(WarmUpMsg))
            {
                TurnContext.SendActivityAsync(MessageFactory.Text(WarmUpMsg));
            }
            
            var conv = TurnContext.Activity.GetConversationReference();

            ConversationId = conv.Conversation.Id;
            ChannelId = ConversationId.Split(";")[0];
            ConversationMessageId = ConversationId.Split(";")[1];
            MessageId = TurnContext.Activity.Id;
            ConversationMessageId = ConversationMessageId.Replace("messageid=", "");

            _fullCommandText = TurnContext.Activity.Text;

            // remove the Command Name
            string commandText = _fullCommandText;

            if (!String.IsNullOrEmpty(_commandName))
            {
                commandText = commandText.Replace(_commandName, "");
                commandText = commandText.Replace(_commandName.ToLower(), "");
                commandText = commandText.Replace(_commandName.Replace(" ", ""), "");
                commandText = commandText.Replace(_commandName.ToLower().Replace(" ", ""), "");
            }

            if (commandText.Contains(","))
            {
                string[] commandSegments = commandText.Split(",");

                if (commandSegments.Length == 1)
                {
                    _commandValueTitle = commandText;
                }
                else if (commandSegments.Length > 1)
                {
                    _commandValueTitle = commandSegments[0];
                    _commandValueDescription = commandSegments[1];
                }
            }
            else if ( commandText.Contains(";"))
            {
                string[] commandSegments = commandText.Split(";");

                if (commandSegments.Length == 1)
                {
                    _commandValueTitle = commandText;
                }
                else if (commandSegments.Length > 1)
                {
                    _commandValueTitle = commandSegments[0];
                    _commandValueDescription = commandSegments[1];
                }
            }
            else
            {
                _commandValueTitle = commandText;
            }

            if ( !String.IsNullOrEmpty(_commandValueTitle))
            {
                _commandValueTitle = _commandValueTitle.Trim();
            }
            if (!String.IsNullOrEmpty(_commandValueDescription))
            {
                _commandValueDescription = _commandValueDescription.Trim();
            }

        }

        internal string TeamId
        {
            get
            {
                if (String.IsNullOrEmpty(_teamId))
                {
                    var teamInfo = TeamsInfo.GetTeamDetailsAsync(TurnContext).Result;
                    _teamId = teamInfo.AadGroupId;
                }

                return _teamId;
            }
        }

        public abstract Task Execute(CancellationToken cancellationToken);

        public void UseDynamics(string dynClientID, string dynClientSecret)
        {            
            _service = new CdsServiceClient(new Uri(ToDynamicsConnection.dynamicsUrl), dynClientID, dynClientSecret, true, "");
        }

        internal CdsServiceClient DynamicsService
        {
            get
            {
                if ( _service == null )
                {
                    throw new Exception("Connection CDS/Dynamics not initialised for this action");
                }

                return _service;
            }
        }
    }
}
