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
    /// Example of an Action to run a Flow that will create a New Task in Dynamics
    /// </summary>
    public class NewTask : ActionTemplates.RunFlow
    {
        public NewTask() : base("New Task",
            "https://prod-43.westus.logic.azure.com:443/workflows/15a1d5a1f45d4bc18f21942c9909a12b/triggers/manual/paths/invoke?api-version=2016-06-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=IL5t3bbrJyGJvgFf8LKbjhclVa7OGCxv3q3oWI1a2Pk",
            "Launching the Flow",
            "All Done!",
            false)
        {

        }

        public override async Task Execute(CancellationToken cancellationToken)
        {
            await TurnContext.SendActivityAsync(MessageFactory.Text("Finished the Example Action!"), cancellationToken);
        }
    }
}
