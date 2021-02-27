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
    /// <summary>
    /// Example of an Action to run a Flow that will create a New Case in Dynamics
    /// </summary>
    public class NewCase : ActionTemplates.RunFlow
    {
        public NewCase() : base("New Case", 
            "Adding a new Case into CRM",
            "New Case raised",
            "")
        {

        }

        public override async Task Execute(CancellationToken cancellationToken)
        {
            await TurnContext.SendActivityAsync(MessageFactory.Text("Finished the Example Action!"), cancellationToken);
        }
    }
}
