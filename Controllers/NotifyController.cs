// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Configuration;
using Microsoft.Rest;

using SS21.Examples.TeamBot.BaseObjects;
using System.Xml;
using AdaptiveCards;
using Microsoft.PowerPlatform.Cds.Client;
using SS21.Examples.TeamBot.Helpers;
using Newtonsoft.Json.Linq;

namespace SS21.Examples.TeamBot.Controllers
{
    [Route("api/notify")]
    [ApiController]
    public class NotifyController : ControllerBase
    {
        private readonly IBotFrameworkHttpAdapter _adapter;
        private readonly ConcurrentDictionary<string, ConversationReference> _conversationReferences;

        private string _tenantId = string.Empty;
        private string _appId = string.Empty;
        private string _appPassword = string.Empty;

        private string _dynClientId;
        private string _dynClientSecret;
        private bool _debugMode = false;
        private bool _showConnMode = false;

        public NotifyController(
            IBotFrameworkHttpAdapter adapter, 
            IConfiguration config, 
            ConcurrentDictionary<string, ConversationReference> conversationReferences)
        {
            _adapter = adapter;
            _conversationReferences = conversationReferences;

            _tenantId = config["TenantId"];

            _appId = config["MicrosoftAppId"];
            _appPassword = config["MicrosoftAppPassword"];
            _dynClientId = config["DynamicsClientId"];
            _dynClientSecret = config["DynamicsClientSecret"];

            //_appId = configuration["MicrosoftAppId"];
            // If the channel is the Emulator, and authentication is not in use,
            // the AppId will be null.  We generate a random AppId for this case only.
            // This is not required for production, since the AppId will have a value.
            /*if (string.IsNullOrEmpty(_appId))
            {
                _appId = Guid.NewGuid().ToString(); //if no AppId, use a random Guid
            }*/
        }

        [Route("api/notify/{s}")]
        [HttpGet]
        public string Get(string s)
        {
            return "This is the Bots Notify API - " + s;
        }

        [HttpPost]
        public async Task<IActionResult> PostMessage([FromBody] NewMessage content)
        {
            int errPoint = 0;
            string errContent = string.Empty;
            string addingChannelNotification = string.Empty;

            string messageHeader = string.Empty;
            string messageContent = string.Empty;
            string messageFooter = string.Empty;
            bool mentionsPlaced = false;

            string orgUrl = string.Empty;

            // if the BotId or TenantId is left blank - we can use hard-coded values
            // for our Bot in Azure and our Client's Tenant
            if ( String.IsNullOrEmpty(content.botId))
            {
                content.botId = _appId;
            }
            if ( String.IsNullOrEmpty(content.tenantId) && !String.IsNullOrEmpty(_tenantId))
            {
                content.tenantId = _tenantId;
            }

            if ( String.IsNullOrEmpty(content.tenantId))
            {
                throw new InvalidOperationException("No Tenant Id is available to the Bot!");
            }
            // NOTE: This concept of embedding the Tenant Id in the Config File for the Bot
            // is great for demostration purposes or for a bespoke Bot - 
            // but would pose problems if the Bot was to be used in Multiple Tenants
            // or packaged as an App for multiple Clients

            try
            {
                errPoint = 101;

                // the following URL will change depending on what Region your Azure Bot Service is running from!
                // EUROPE: string serviceUrl = @"https://smba.trafficmanager.net/emea/";
                string serviceUrl = @"https://smba.trafficmanager.net/amer";
                MicrosoftAppCredentials.TrustServiceUrl(serviceUrl);

                errPoint = 102;
                var credentials = new MicrosoftAppCredentials(_appId, _appPassword);

                errPoint = 103;
                var connector = new ConnectorClient(new Uri(serviceUrl), credentials);

                errPoint = 104;
                TeamsChannelData tcd = new TeamsChannelData();
                tcd.Channel = new ChannelInfo(content.channelId);
                tcd.Team = new TeamInfo(content.teamId);
                tcd.Tenant = new TenantInfo(content.tenantId);
                string fullMessage = string.Empty;
                if (content.body != null)
                {
                    fullMessage = content.body.content;
                }
                else
                {
                    fullMessage = "Message passed to the Notify Controller: [no_body]";
                }

                bool posted = false;

                // create the conversation
                var conversationParameter = new ConversationParameters
                {
                    Activity = MessageFactory.Text(fullMessage),
                    Bot = new ChannelAccount
                    {
                        Id = content.botId,
                        Role = "Bot"
                    },
                    ChannelData = tcd,
                    IsGroup = true,
                    TenantId = content.tenantId
                };

                // Post as New Conversation or to Existing Conversation
                if (!string.IsNullOrWhiteSpace(content.conversationId))
                {
                    // Post to Existing Conversation
                    errPoint = 41;
                    errContent += "ConversationId: (" + content.conversationId + "), ";

                    try
                    {
                        // as conversation id has been supplied, this is a response to a thread
                        conversationParameter.Activity.Conversation = new ConversationAccount
                        {
                            Id = content.conversationId
                        };
                        var response = await connector.Conversations.SendToConversationAsync(
                            conversationParameter.Activity);
                        
                        posted = true;

                        return new JsonResult(response);
                    }
                    catch (Exception ex)
                    {
                        // if we fail to post to an existing conversation
                        // (say if the conversation has been deleted?)
                        // then still post out as a regular message
                        posted = false;
                        errContent += "FAILED to post to Existing Conversation: " + ex.Message + " | ";
                        conversationParameter.Activity.Conversation = null;
                    }
                }

                if ( posted == false)
                {
                    var response = await connector.Conversations.CreateConversationAsync(
                        conversationParameter);

                    return new JsonResult(response);
                }

                return null;
            }
            catch (Exception ex)
            {
                return new JsonResult("TeamBot --> NotifyController --> PostMessage[" + errPoint + "] :: " + ex.Message);
            }
        }
    }
}

namespace SS21.Examples.TeamBot.BaseObjects
{
    public class NewMessage
    {
        public string botId { get; set; }
        public string tenantId { get; set; }
        public string teamId { get; set; }
        public string channelId { get; set; }
        public Guid recordId { get; set; }
        public string recordType { get; set; }
        public string conversationId { get; set; }
        public string importance { get; set; }
        public Body body { get; set; }
        public Mention[] mentions { get; set; }
        public bool createSimpleAdaptiveCard { get; set; }
        public AdaptiveCard adaptiveCard { get; set; }
        public JObject adaptiveCardTemplate { get; set; }
    }
    public class Body
    {
        public string header { get; set; }
        public string content { get; set; }
        public string footer { get; set; }
    }
    public class Mention
    {
        public string id { get; set; }
        public string mentionText { get; set; }
        public Mentioned mentioned { get; set; }
    }
    public class Mentioned
    {
        public User user { get; set; }
    }
    public class User
    {
        public string displayName { get; set; }
        public string id { get; set; }
        public string userIdentityType { get; set; }
    }
}