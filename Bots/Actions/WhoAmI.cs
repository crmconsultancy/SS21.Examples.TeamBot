using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;

using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams;

namespace SS21.Examples.TeamBot.Bots.Actions
{
    public class WhoAmI : BotAction
    {
        public WhoAmI() : base()
        {

        }

        public override async Task Execute(CancellationToken cancellationToken)
        {
            string returnText = "Your [GivenName] [Surname] working from Tenant: [TenantId]";

            try
            {
                IList<ChannelAccount> teamMembers = (await TurnContext.TurnState.Get<IConnectorClient>().Conversations
                    .GetConversationMembersAsync(TurnContext.Activity.GetChannelData<TeamsChannelData>().Team.Id)
                    .ConfigureAwait(false));

                foreach (ChannelAccount ca in teamMembers)
                {
                    MicrosoftTeamUser u = Newtonsoft.Json.JsonConvert.DeserializeObject<MicrosoftTeamUser>(ca.Properties.ToString());

                    if (u.ObjectId == TurnContext.Activity.From.AadObjectId)
                    {
                        returnText = returnText.Replace("[ObjectId]", u.ObjectId);
                        returnText = returnText.Replace("[UserPrincipalName]", u.UserPrincipalName);
                        returnText = returnText.Replace("[GivenName]", u.GivenName);
                        returnText = returnText.Replace("[Surname]", u.Surname);
                        returnText = returnText.Replace("[TenantId]", u.TenantId);
                    }
                }
            }
            catch (Exception ex)
            {
                returnText = "The Command returned the following error: " + ex.Message;
            }

            await TurnContext.SendActivityAsync(MessageFactory.Text(returnText), cancellationToken);
        }
    }
}
