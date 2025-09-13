using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modon.Citizen.Plugins
{
    public class AssignBackToFirstApproverAsync : PluginBase
    {
        protected override void ExecutePlugin(IServiceProvider serviceProvider)
        {
            if (Context.InputParameters.Contains("Target") && Context.InputParameters["Target"] is Entity entity)
            {

                var request = Service.Retrieve("ldv_request", entity.Id, new ColumnSet("ldv_secondapprovalstatus", "ldv_firstapprover"));

                var secondApprovalStatus = request.GetAttributeValue<OptionSetValue>("ldv_secondapprovalstatus")?.Value;

                if (secondApprovalStatus != (int)SecondApprovalStatus.SentBack)
                    return;

                var firstApprover = request.GetAttributeValue<EntityReference>("ldv_firstapprover");

                if (firstApprover == null)
                    return;

                // Assign record to first approver
                var assign = new AssignRequest
                {
                    Assignee = firstApprover,
                    Target = new EntityReference("ldv_request", request.Id)
                };

                Service.Execute(assign);

                //reset approvals

                var update = new Entity(entity.LogicalName, entity.Id);

                // Reset First Approval fields (but keep approver reference)
                update["ldv_firstapprovalstatus"] = null;
                update["ldv_firstapprovaldate"] = null;
                update["ldv_firstapprovalcommentreason"] = null;

                // Reset Second Approval fields (but keep approver reference)
                update["ldv_secondapprovalstatus"] = null;
                update["ldv_secondapprovaldate"] = null;
                update["ldv_secondapprovalcommentreason"] = null;

                Service.Update(update);
            }


        }
    }
}
