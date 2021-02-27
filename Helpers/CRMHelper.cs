using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Cds.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel.Description;
using System.Threading.Tasks;

namespace SS21.Examples.TeamBot
{
    public class CRMHelper
    {
       public static void AssignRecordToUser(CdsServiceClient service, Guid userId, Guid recordId, string recordLogicalName)
        {
            var assign = new AssignRequest
            {
                Assignee = new EntityReference("systemuser",
            userId),
                Target = new EntityReference(recordLogicalName,
            recordId)
            };

            service.Execute(assign);
        }
        public static Entity RetrieveUser(CdsServiceClient service, string azureObjectId, ColumnSet cs = null)
        {
            QueryExpression qe = new QueryExpression("systemuser");
            qe.Criteria.AddCondition(new ConditionExpression("azureactivedirectoryobjectid", ConditionOperator.Equal, azureObjectId));
            qe.ColumnSet = new ColumnSet("systemuserid");

            if(cs != null)
            {
                qe.ColumnSet = cs;
            }

            EntityCollection ec = service.RetrieveMultiple(qe);

            if(ec.Entities.Count == 1)
            {
                return ec.Entities[0];
            }
            return null;
        }

        public static Entity RetrieveUserByName(CdsServiceClient service, string fullname, ColumnSet cs = null)
        {
            QueryExpression qe = new QueryExpression("systemuser");
            qe.Criteria.AddCondition(new ConditionExpression("fullname", ConditionOperator.Equal, fullname));
            qe.ColumnSet = new ColumnSet("systemuserid");

            if (cs != null)
            {
                qe.ColumnSet = cs;
            }

            EntityCollection ec = service.RetrieveMultiple(qe);

            if (ec.Entities.Count == 1)
            {
                return ec.Entities[0];
            }
            return null;
        }

        public static ExecuteMultipleResponse SaveMultipleRecords(CdsServiceClient service, List<Entity> records)
        {
            ExecuteMultipleRequest executeMultipleRequest = new ExecuteMultipleRequest()
            {
                Settings = new ExecuteMultipleSettings()
                {
                    ContinueOnError = true,
                    ReturnResponses = true
                },
                Requests = new OrganizationRequestCollection()
            };

            foreach (Entity record in records)
            {
                CreateRequest createRequest = new CreateRequest { Target = record };
                executeMultipleRequest.Requests.Add(createRequest);
            }

            ExecuteMultipleResponse responseWithResults =
            (ExecuteMultipleResponse)service.Execute(executeMultipleRequest);

            return responseWithResults;
        }
    }
}
