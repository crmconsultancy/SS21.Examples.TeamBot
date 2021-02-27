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

namespace SS21.Examples.TeamBot.Bots.ActionTemplates
{
    public class ColumnDefinition
    {
        public string DatabaseName;
        public string DisplayName;
        public bool Searchable = false;
        public bool Title = false;
    }

    public class GetRecords : BotAction
    {
        public string Table;
        public string RelationshipToAccount;
        public List<ColumnDefinition> Columns = new List<ColumnDefinition>();

        public GetRecords(string table, string relationshipToAccount) : base("Gotcha - getting the information back from CRM", "Tasks")
        {
            this.Table = table;
            this.RelationshipToAccount = relationshipToAccount;
        }

        public void AddColumn(string databaseName, string displayName, bool isSearchable = false, bool isTitle = false)
        {
            ColumnDefinition cd = new ColumnDefinition();
            cd.DatabaseName = databaseName;
            cd.DisplayName = displayName;
            cd.Searchable = isSearchable;
            cd.Title = isTitle;

            Columns.Add(cd);
        }

        public override async Task Execute(CancellationToken cancellationToken)
        {
            QueryExpression q = new QueryExpression(this.Table);

            // determine what columns to pull back from Dynamics
            q.ColumnSet = new ColumnSet();
            foreach(ColumnDefinition ra in this.Columns)
            {
                q.ColumnSet.AddColumn(ra.DatabaseName);
            }

            q.Criteria.FilterOperator = LogicalOperator.And;
            q.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);

            // get the contacts for the Account that matches our current Channel
            LinkEntity le = new LinkEntity(this.Table, "account", this.RelationshipToAccount, "accountid", JoinOperator.Inner);
            le.LinkCriteria.AddCondition("crmcs_msteams_channelid", ConditionOperator.Equal, base.ChannelId);
            q.LinkEntities.Add(le);

            if (!String.IsNullOrEmpty(base.ValueTitle))
            {
                FilterExpression f = new FilterExpression();
                f.FilterOperator = LogicalOperator.Or;

                foreach(ColumnDefinition ra in this.Columns)
                {
                    if ( ra.Searchable)
                    {
                        f.AddCondition(ra.DatabaseName, ConditionOperator.Like, "%" + base.ValueTitle + "%");
                    }
                }
                q.Criteria.AddFilter(f);
            }
            else
            {
                // just get all the Contacts back
            }

            EntityCollection records = base.DynamicsService.RetrieveMultiple(q);

            if (records.Entities.Count == 0)
            {
                // nothing to return
                await TurnContext.SendActivityAsync(MessageFactory.Text("Sorry - I couldn't find any matching records"), cancellationToken);
            }
            else
            {
                var card = new ListCard();

                card.content = new Content();
                List<Item> items = new List<Item>();
                int itemNo = 0;

                // loop through the information we have and display into Teams
                foreach (Microsoft.Xrm.Sdk.Entity c in records.Entities)
                {
                    string openUrl = string.Empty;
                    string title = string.Empty;
                    string subTitle = string.Empty;
                    openUrl = base.ToDynamicsConnection.dynamicsUrl + "/main.aspx?forceUCI=1&etn=" + this.Table + "&id=" + c.Id + "&pagetype=entityrecord";

                    // note - this is simplifed to be a good example
                    // if you were taking this to production - you would look at the Metadata Service to avoid manual entry of the 
                    foreach (ColumnDefinition ra in this.Columns)
                    {
                        if (ra.Title == true)
                        {
                            if (!String.IsNullOrEmpty(title))
                            {
                                title += ", ";
                            }
                            title += c.GetValue<string>(ra.DatabaseName, string.Empty);
                        }
                        else
                        {
                            // quick routine to summarise the attributes from Dynamics as an Info List in Teams
                            if (c.Contains(ra.DatabaseName))
                            {
                                if (!String.IsNullOrEmpty(ra.DisplayName))
                                {
                                    subTitle += "<p><b>" + ra.DisplayName + ":</b> " + c.GetValue(ra.DatabaseName, string.Empty) + "</p>";
                                }
                                else
                                {
                                    // sometimes we may want a description field shown as a Memo and avoid a Column Title
                                    subTitle += "<p>" + c.GetValue(ra.DatabaseName, string.Empty) + "</p>";
                                }
                            }
                        }
                    }

                    var item = new Item();
                    item.id = itemNo.ToString();
                    item.type = "resultItem";
                    item.title = title;
                    item.subtitle = subTitle;

                    // needed for Teams to process the URL as an Action correctly
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
