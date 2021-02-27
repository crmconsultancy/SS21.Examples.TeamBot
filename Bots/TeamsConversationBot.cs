// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;

using Microsoft.Bot.Connector.Authentication;

using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;

using Microsoft.PowerPlatform.Cds;
using Microsoft.PowerPlatform.Cds.Client;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

using SS21.Examples.Integration;
using SS21.Examples.Integration.GraphAPI;
using SS21.Examples.Integration.GraphAPI.MicrosoftTeams.Chat;

using Microsoft.Xrm.Sdk.Messages;

using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams;

namespace SS21.Examples.TeamBot.Bots
{
    using SS21.Examples.Integration.GraphAPI;
    using AdaptiveCards;

    public class TeamsToDynamicsConnection
    {
        public string userObjectId;
        public string tenantId;
        public string dynamicsUrl;
    }

    public class MicrosoftTeamUser
    {
        public string ObjectId { get; set; }
        public string GivenName { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string UserPrincipalName { get; set; }
        public string TenantId { get; set; }
    }

    public class TeamsToDynamicsConnections : List<TeamsToDynamicsConnection>
    {

        public TeamsToDynamicsConnection GetConnectionForUser(string userObjectId)
        {
            foreach (TeamsToDynamicsConnection c in this)
            {
                if (c.userObjectId.Equals(userObjectId))
                {
                    return c;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets a new connection to Dynamics based on the User and Tenant IDs
        /// </summary>
        /// <param name="userOjectId"></param>
        /// <param name="tennatId"></param>
        /// <returns></returns>
        public TeamsToDynamicsConnection NewConnection(
            string userOjectId, 
            string tennatId, 
            string dynamicsUrl)
        {
            try
            {
                TeamsToDynamicsConnection c = new TeamsToDynamicsConnection();
                c.userObjectId = userOjectId;
                c.tenantId = tennatId;

                // depending on your approach - you could put code here
                // to map the Instance, User and Tenant in Teams
                // to the different Environments of Power Apps and Dynamics
                // - for this example, we are keeping things simple with 
                // a hard-coded link from our Bot to a Dev Build of Power Apps
                // but this could be opened up to a series of links
                c.dynamicsUrl = dynamicsUrl;

                this.Add(c);

                return c;
            }
            catch (Exception ex)
            {
                throw new Exception("NewConnection :: " + ex.Message);
            }
        }
    }

    public class TeamsConversationBot : TeamsActivityHandler
    {
        private string _tenantId;
        private string _appId;
        private string _appPassword;
        private string _dynClientId;
        private string _dynClientSecret;
        private string _dynUrl;
        private TeamsToDynamicsConnections _connections = new TeamsToDynamicsConnections();

        private string _conn;

        // simple debug mode that will output details into the Team Channel
        private bool _debugMode = false;

        public TeamsConversationBot(IConfiguration config)
        {
            _tenantId = config["TenantId"];

            _appId = config["MicrosoftAppId"];
            _appPassword = config["MicrosoftAppPassword"];
            _dynClientId = config["DynamicsClientId"];
            _dynClientSecret = config["DynamicsClientSecret"];
            _dynUrl = config["DynamicsUrl"];

            string debugMode = config["DebugMode"];
            if (!String.IsNullOrEmpty(debugMode))
            {
                _debugMode = bool.Parse(debugMode);
            }
        }

        private CdsServiceClient GetCDSService(TeamsToDynamicsConnection c)
        {
            return new CdsServiceClient(new Uri(c.dynamicsUrl), _dynClientId, _dynClientSecret, true, "");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="turnContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            string errorPoint = string.Empty;
            turnContext.Activity.RemoveRecipientMention();

            // extract command text
            errorPoint = "Interpreting Message";
            string inputString = turnContext.Activity.Text.Trim().ToLower();
            string[] inputWords = inputString.Split();
            string selectedCommand = inputString.Split()[0];

            // if my list of connections has not been used and its null
            // prep up to get started
            if (_connections == null)
            {
                _connections = new TeamsToDynamicsConnections();
            }

            try
            {
                errorPoint = "Get Cached Connection";
                string userObjectId = turnContext.Activity.From.AadObjectId;
                TeamsToDynamicsConnection connection = _connections.GetConnectionForUser(userObjectId);

                if (connection == null)
                {
                    errorPoint = "No Cached Connection - Creating a New Connection (User: " + userObjectId + ", Tenant: TBC)";
                    string tenantId = string.Empty;
                    IList<ChannelAccount> teamMembers = (await turnContext.TurnState.Get<IConnectorClient>().Conversations
                        .GetConversationMembersAsync(turnContext.Activity.GetChannelData<TeamsChannelData>().Team.Id)
                        .ConfigureAwait(false));

                    foreach (ChannelAccount ca in teamMembers)
                    {
                        MicrosoftTeamUser u = Newtonsoft.Json.JsonConvert.DeserializeObject<MicrosoftTeamUser>(ca.Properties.ToString());

                        if (u.ObjectId == turnContext.Activity.From.AadObjectId)
                        {
                            tenantId = u.TenantId;
                        }
                    }

                    errorPoint = "No Cached Connection - Creating a New Connection (User: " + userObjectId + ", Tenant: " + tenantId + ")";
                    // no connection, create one
                    // await turnContext.SendActivityAsync(MessageFactory.Text("No Cached Connection for " + userObjectId + " - Creating anew"), cancellationToken);
                    connection = _connections.NewConnection(userObjectId, tenantId, _dynUrl);
                }

                if (connection == null)
                {
                    errorPoint = "No possible connection";
                    // no connection to DocDrive365
                    // log error?
                    throw new Exception("Failed to obtain connection to Dynamics.");
                }

                if (_debugMode)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(turnContext.Activity.From.Id), cancellationToken);
                }

                if (_debugMode)
                {
                    string details = "UserObjectId: " + connection.userObjectId + " ";
                    details += "TenantId: " + connection.tenantId + " ";
                    details += "DynamicsUrl: " + connection.dynamicsUrl + " ";
                    await turnContext.SendActivityAsync(MessageFactory.Text(details), cancellationToken);
                }

                selectedCommand = selectedCommand.ToLower();
                selectedCommand = selectedCommand.Replace(" ", "");
                errorPoint = "Running Command for " + selectedCommand;
                BotAction action = null;

                switch (selectedCommand)
                {
                    case "example":
                        action = new Actions.ExampleAction();
                        break;
                    case "tasks":
                        ActionTemplates.GetRecords getTasks = new ActionTemplates.GetRecords("task", "regardingobjectid");
                        getTasks.AddColumn("subject", string.Empty, true, true);
                        getTasks.AddColumn("ownerid", string.Empty, false, true);
                        getTasks.AddColumn("description", string.Empty, true, false);
                        getTasks.AddColumn("category", string.Empty, true, false);
                        getTasks.ToDynamicsConnection = connection;
                        getTasks.UseDynamics(_dynClientId, _dynClientSecret);

                        action = getTasks;
                        break;
                    case "contacts":
                        action = new Actions.GetContacts();
                        action.ToDynamicsConnection = connection;
                        action.UseDynamics(_dynClientId, _dynClientSecret);
                        break;
                    case "whoami":
                        action = new Actions.WhoAmI();
                        break;
                    case "onboarding":
                        action = new ActionTemplates.RunFlow
                            (string.Empty,
                            "https://prod-101.westus.logic.azure.com:443/workflows/4330f77be0f64bf9877d934c3668bc3f/triggers/manual/paths/invoke?api-version=2016-06-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=BoYm6PSfha5PMbiRhkHXf5fJK_OFlKMebQsaqbxZTHM",
                            "Starting the Onboarding!", "", false);
                        break;
                    case "helpme":
                        var card = new HeroCard
                        {
                            Text = "Hi there, you can ask me to do one of the following actions",
                            Buttons = new List<CardAction>
                        {
                            new CardAction
                            {
                                Type= ActionTypes.MessageBack,
                                Title = "Find Documents",
                                Text = "doc"
                            },
                            new CardAction
                            {
                                Type= ActionTypes.MessageBack,
                                Title = "Channel Details",
                                Text = "me"
                            },
                            new CardAction
                            {
                                Type= ActionTypes.MessageBack,
                                Title = "Raise Task",
                                Text = "raisetask"
                            },
                           new CardAction
                            {
                                Type = ActionTypes.MessageBack,
                                Title = "Save Chat",
                                Text = "HelpSaveChat"
                            },
                            new CardAction
                            {
                                Type = ActionTypes.OpenUrl,
                                Title = "About",
                                Value = "http://crmcs.co.uk/docdrive365"
                            }
                        }
                        };
                        await turnContext.SendActivityAsync(MessageFactory.Attachment(card.ToAttachment()));
                        break;
                    case ("adaptivecard"):
                        {
                            AdaptiveCard adaptiveCard = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0));

                            adaptiveCard.Body.Add(new AdaptiveTextBlock()
                            {
                                Text = "Hello",
                                Size = AdaptiveTextSize.ExtraLarge
                            });

                            adaptiveCard.Body.Add(new AdaptiveImage()
                            {
                                Url = new Uri("http://adaptivecards.io/content/cats/1.png")
                            });

                            // convert the adaptive card into an attachment object for the bot framework to translate into a chat message
                            Attachment attachment = new Attachment
                            {
                                ContentType = AdaptiveCard.ContentType,
                                Content = adaptiveCard
                            };
                            await turnContext.SendActivityAsync(MessageFactory.Attachment(attachment));
                            break;
                        }
                    default:
                        break;
                }

                if ( action != null )
                {
                    action.Setup(turnContext, connection);
                    await action.Execute(cancellationToken);
                    action.Finish();
                }
            }
            catch (Exception ex)
            {
                string errMsg = "Sorry, I encountered an error whilst processing your request.";
                if (_debugMode)
                {
                    errMsg += " - [" + errorPoint + "] " + ex.Message;
                }
                await turnContext.SendActivityAsync(MessageFactory.Text(errMsg), cancellationToken);
            }
        }
    }
}
