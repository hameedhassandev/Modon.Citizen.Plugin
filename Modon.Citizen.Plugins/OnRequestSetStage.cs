using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Modon.Citizen.Plugins.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;

namespace Modon.Citizen.Plugins
{
    public class OnRequestSetStage : PluginBase
    {
        protected override void ExecutePlugin(IServiceProvider serviceProvider)
        {
            if (Context.InputParameters.Contains("Target") && Context.InputParameters["Target"] is Entity entity)
            {
                // prevent recursion
                if (Context.Depth > 1)
                    return;

                var target = (Entity)Context.InputParameters["Target"];
                var requestId = target.Id;

                //on create from portal
                if (Context.MessageName.Equals("Create", StringComparison.OrdinalIgnoreCase))
                {
                    MoveToFirstApprovalStage(requestId, target.LogicalName);
                }

                //on update
                else if (Context.MessageName.Equals("Update", StringComparison.OrdinalIgnoreCase))
                {
                    // if update came from portal, field flag will be true
                    var isSubmitted = (bool)target["ldv_issubmittedfromportal"];
                    if (isSubmitted)
                    {
                        MoveToFirstApprovalStage(requestId, target.LogicalName);
                    }
                }

            }
        }


        private void MoveToFirstApprovalStage(Guid requestId, string requestLogicalName)
        {
            Guid FIRST_APPROVAL_STAGE_ID = new Guid("e225429f-2123-4f6a-9a81-ae6278c889b5");

            BpfHelper.MoveToStage(Service, "ldv_citizenrequestapprovalbpf", "bpf_ldv_requestid", requestId, FIRST_APPROVAL_STAGE_ID);

            // reset flag
            var updateRequest = new Entity(requestLogicalName, requestId)
            {
                ["ldv_issubmittedfromportal"] = false
            };
            Service.Update(updateRequest);
        }
    }
}
