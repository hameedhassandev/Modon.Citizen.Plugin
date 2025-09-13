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
    public class AssignToManagerAsync : PluginBase
    {
        protected override void ExecutePlugin(IServiceProvider serviceProvider)
        {
            if (Context.InputParameters.Contains("Target") && Context.InputParameters["Target"] is Entity entity)
            {
                var request = Service.Retrieve("ldv_request", entity.Id, new ColumnSet("ownerid", "ldv_secondapprover", "ldv_firstapprovalstatus"));

                var firstApprovalStatus = request.GetAttributeValue<OptionSetValue>("ldv_firstapprovalstatus")?.Value;

                if (firstApprovalStatus != (int)FirstApprovalStatus.Approved)
                    return;

                var manager = request.GetAttributeValue<EntityReference>("ldv_secondapprover");

                if (manager == null)
                    return;

                // Assign record to manager
                var assign = new AssignRequest
                {
                    Assignee = manager,
                    Target = new EntityReference("ldv_request", request.Id)
                };
                Service.Execute(assign);

            }


        }
    }
}
