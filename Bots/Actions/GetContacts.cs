using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using SS21.Examples.TeamBot.Helpers;

namespace SS21.Examples.TeamBot.Bots.Actions
{
    public class GetContacts : BotAction
    {
        public GetContacts() : base("Pulling Contacts from Dynamics", "Contacts")
        {
        }

        public override async Task Execute(CancellationToken cancellationToken)
        {
            QueryExpression q = new QueryExpression("contact");
            q.ColumnSet = new ColumnSet("fullname", "firstname", "lastname", "jobtitle", "mobilephone", "emailaddress1", "telephone1");
            q.Criteria.FilterOperator = LogicalOperator.And;

            // get the contacts for the Account that matches our current Channel
            LinkEntity le = new LinkEntity("contact", "account", "parentcustomerid", "accountid", JoinOperator.Inner);
            le.LinkCriteria.AddCondition("crmcs_msteams_channelid", ConditionOperator.Equal, base.ChannelId);
            q.LinkEntities.Add(le);

            if (!String.IsNullOrEmpty(base.ValueTitle))
            {
                FilterExpression f = new FilterExpression();
                f.FilterOperator = LogicalOperator.Or;
                f.AddCondition("fullname", ConditionOperator.Like, "%" + base.ValueTitle + "%");
                q.Criteria.AddFilter(f);
            }
            else
            {
                // just get all the Contacts back
            }

            EntityCollection contacts = base.DynamicsService.RetrieveMultiple(q);

            if (contacts.Entities.Count == 0)
            {
                // nothing to return
                await TurnContext.SendActivityAsync(MessageFactory.Text("Sorry - I couldn't find any matching contacts"), cancellationToken);
            }
            else
            {
                var card = new ListCard();

                card.content = new Content();
                List<Item> items = new List<Item>();
                int itemNo = 0;

                // loop through the information we have and display into Teams
                foreach (Microsoft.Xrm.Sdk.Entity c in contacts.Entities)
                {
                    string openUrl = string.Empty;
                    string subTitle = string.Empty;
                    openUrl = base.ToDynamicsConnection.dynamicsUrl + "/main.aspx?forceUCI=1&etn=" + "contact" + "&id=" + c.Id + "&pagetype=entityrecord";

                    var item = new Item();
                    item.id = itemNo.ToString();
                    item.type = "resultItem";
                    item.title = c.GetValue<string>("fullname", string.Empty);

                    // quick routine to summarise the attributes from Dynamics as an Info List in Teams
                    if (c.Contains("jobtitle"))
                        subTitle += "<p><b>Job Title:</b> " + c.GetValue("jobtitle", string.Empty) + "</p>";
                    if (c.Contains("mobilephone"))
                    {
                        subTitle += "<p><b>Mobille:</b> " + c.GetValue("mobilephone", string.Empty) + "</p>";
                    }
                    else
                    {
                        // only show telephone if mobile is blank
                        if (c.Contains("telephone1"))
                            subTitle += "<p><b>Telephone:</b> " + c.GetValue("telephone1", string.Empty) + "</p>";
                    }
                    if ( c.Contains("emailaddress1"))                    
                        subTitle += "<p><b>Email Address:</b> " + c.GetValue("emailaddress1", string.Empty) + "</p>";
                    item.subtitle = subTitle;

                    openUrl = openUrl.Replace(" ", "%20");

                    item.tap = new Tap()
                    {
                        type = "openUrl",
                        value = openUrl
                    };

                    itemNo++;

                    items.Add(item);
                }

                card.content.items = items.ToArray();

                Attachment attachment = new Attachment();
                attachment.ContentType = card.contentType;
                attachment.Content = card.content;

                await TurnContext.SendActivityAsync(MessageFactory.Attachment(attachment));
            }
        }
    }
}
