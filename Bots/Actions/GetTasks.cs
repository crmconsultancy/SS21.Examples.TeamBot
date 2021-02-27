using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;

namespace SS21.Examples.TeamBot.Bots.Actions
{
    public class GetTasks : BotAction
    {
        public GetTasks() : base("Okay - running the GetAppointments Action for you")
        {

        }

        public override async Task Execute(CancellationToken cancellationToken)
        {
            await TurnContext.SendActivityAsync(MessageFactory.Text("Finished the GetAppointments Action!"), cancellationToken);
        }
    }
}
