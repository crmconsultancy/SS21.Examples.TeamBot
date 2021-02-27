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
    public class ExampleAction : BotAction
    {
        public ExampleAction() : base("Okay - running the Example Action for you")
        {

        }

        public override async Task Execute(CancellationToken cancellationToken)
        {
            string output = "Finished the Example Action at " + DateTime.Now.ToString("dd/MM/YYYY") + ".";

            await TurnContext.SendActivityAsync(MessageFactory.Text(output), cancellationToken);
        }
    }
}
