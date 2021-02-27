using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Newtonsoft.Json.Converters;

namespace SS21.Examples.TeamBot.Bots.ActionTemplates
{
    public class FlowRequest
    {
        public string TeamId;
        public string ChannelId;
        public string ConversationId;
        public string UserId;
        public string Content;
        public string Description;

        private string JSONAttribute(string key, string value, bool addComma)
        {
            string json = "\"[attributeName]\": [value]";

            json = json.Replace("[attributeName]", key);

            if ( String.IsNullOrEmpty(value))
            {
                json = json.Replace("[value]", "\"\"");
            }
            else
            {
                json = json.Replace("[value]", "\"" + value + "\"");
            }

            if ( addComma)
            {
                json += ",";
            }

            return json;
        }

        public string ToJSON()
        {
            string json = "{";
            json += JSONAttribute("TeamId", this.TeamId, true);
            json += JSONAttribute("ChannelId", this.ChannelId, true);
            json += JSONAttribute("ConversationId", this.ConversationId, true);
            json += JSONAttribute("UserId", this.UserId, true);
            json += JSONAttribute("Content", this.Content, true);
            json += JSONAttribute("Description", this.Description, false);
            json += "}";

            return json;
        }
    }

    public class FlowResponse
    {
        public string Status;
        public string OpenUrl;
    }

    public class RunFlow : BotAction
    {
        public string FlowHttpTrigger = @"https://prod-43.westus.logic.azure.com:443/workflows/15a1d5a1f45d4bc18f21942c9909a12b/triggers/manual/paths/invoke?api-version=2016-06-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=IL5t3bbrJyGJvgFf8LKbjhclVa7OGCxv3q3oWI1a2Pk";
        private string _startingMessage = null;
        private string _finishingMessage = null;
        private bool _debugMode = false;

        public RunFlow(string commandName, string httpTrigger, string startingMessage = null, string finishMessage = null, bool debugMode = false) : base(string.Empty)
        {
            FlowHttpTrigger = httpTrigger;

            _startingMessage = startingMessage;
            _finishingMessage = finishMessage;
            _debugMode = debugMode;
        }

        public override async Task Execute(CancellationToken cancellationToken)
        {
            // our first step is to parse the user's input from Teams
            // we can do this in multiple ways
            // and ideally could use a Task in Teams or a Simple Form to collect the information
            // but if we want a quick flowing command from a Bot that avoids Fields and Data Types, we can do it this way
            // Parse the input from the User in the following steps:
            // (1) Remove the Command Name from the incoming Command
            // (2) Divide the command by a comma (,) or semi-colon (;) 
            // (3) Interpret the first section of the Command as the Title
            // (4) Interpret the second section as the Description
            // This is not as good as it could be - we could use better data in Teams - or use follow-up commands to further detail the Record
            // we are creating in Dynamics (and so allowing a CREATE then UPDATE style approach)
            // but gives a simple interface to use when say capturing Tasks or Cases from a Meeting in quick time

            HttpClient client = new HttpClient();
            HttpRequestMessage webRequest =
                new HttpRequestMessage(new System.Net.Http.HttpMethod("POST"), FlowHttpTrigger);
            string requestJSON = string.Empty;
            try
            {
                FlowRequest request = new FlowRequest();

                request.TeamId = base.TeamId;
                request.ChannelId = base.ChannelId;
                request.UserId = string.Empty;
                request.ConversationId = base.ConversationId;
                request.Content = base.ValueTitle;
                request.Description = base.ValueDescription;

                requestJSON = request.ToJSON();
                if (!string.IsNullOrEmpty(requestJSON))
                {
                    webRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    //webRequest.Headers.Add("content-type", "application/json");
                    webRequest.Content = new StringContent(requestJSON, Encoding.UTF8, "application/json");
                }

                if ( !String.IsNullOrEmpty(_startingMessage))
                {
                    TurnContext.SendActivityAsync(MessageFactory.Text(_startingMessage), cancellationToken);
                }
            }
            catch(Exception prepEx)
            {
                // error calling out to the Flow Http Trigger
                await TurnContext.SendActivityAsync(MessageFactory.Text("Error whilst building the Request: " + prepEx.Message), cancellationToken);
            }

            try
            {
                HttpResponseMessage webResponse = await client.SendAsync(webRequest).ConfigureAwait(false);
                FlowResponse response = null;

                string responseContent = "No Response";

                if (webResponse.Content != null)
                {
                    responseContent = await webResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                    // response = Newtonsoft.Json.JsonConvert.DeserializeObject<FlowResponse>(responseContent);
                }

                if ( response != null )
                {
                    // do something with the response from outcome of our Flow?
                }

                if ( _debugMode)
                    await TurnContext.SendActivityAsync(MessageFactory.Text("Response from Flow: " + responseContent + " - RequestJSON: " + requestJSON), cancellationToken);
            }
            catch(Exception ex)
            {
                // error calling out to the Flow Http Trigger
                await TurnContext.SendActivityAsync(MessageFactory.Text("Error when placing the Request to Flow: " + ex.Message), cancellationToken);
            }

            if ( !String.IsNullOrEmpty(_finishingMessage))
            {
                TurnContext.SendActivityAsync(MessageFactory.Text(_finishingMessage), cancellationToken);
            }
        }
    }
}
