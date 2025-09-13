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
    public class OnFirstApprovalStatusChanged : PluginBase
    {
        protected override void ExecutePlugin(IServiceProvider serviceProvider)
        {
            if (Context.InputParameters.Contains("Target") && Context.InputParameters["Target"] is Entity entity)
            {
                Entity request = Service.Retrieve(entity.LogicalName, entity.Id, new ColumnSet(true));

                var firstApprovalStatus = request.GetAttributeValue<OptionSetValue>("ldv_firstapprovalstatus")?.Value;

                if (firstApprovalStatus != null)
                {
                    var update = new Entity(request.LogicalName, request.Id);

                    switch (firstApprovalStatus)
                    {
                        case (int)FirstApprovalStatus.Approved:
                            HandleFirstApprovalApprove(Service, request, update);
                            break;

                        case (int)FirstApprovalStatus.Rejected:
                            HandleFirstApprovalReject(Service, request, update);
                            break;

                        case (int)FirstApprovalStatus.SentBack:
                            HandleFirstApprovalBackToCitizen(Service, request, update);
                            break;
                    }

                    Service.Update(update);
                }

            }
        }


        private void HandleFirstApprovalApprove(IOrganizationService service, Entity request, Entity update)
        {
            //Set first approver
            update["ldv_firstapprover"] = request.GetAttributeValue<EntityReference>("ownerid");
            update["ldv_firstapprovaldate"] = DateTime.UtcNow;

            // Update statuses
            update["ldv_portalstatus"] = new OptionSetValue((int)PortalStatus.InProgress);
            update["ldv_crmstatus"] = new OptionSetValue((int)CrmStatus.AdminApproval);

            //Get manager of first approver
            var owner = request.GetAttributeValue<EntityReference>("ownerid");
            var ownerRecord = service.Retrieve("systemuser", owner.Id, new ColumnSet("parentsystemuserid"));
            var manager = ownerRecord.GetAttributeValue<EntityReference>("parentsystemuserid");

            if (manager == null)
                throw new InvalidPluginExecutionException("This employee not have a mamanger set its manager first then proceed action");

            // Update owner with manager will be in AssignToManagerAsync plugin
            //update["ownerid"] = manager;
            update["ldv_secondapprover"] = manager;
            update["ldv_crmstatus"] = new OptionSetValue((int)CrmStatus.ManagerApproval);

            // Log for external portal
            var regarding = new EntityReference(request.LogicalName, request.Id);

            GeneralLog.Create(
                service,
                regarding,
                "In Progress",
                LogType.Audit,
                LogVisibility.ExternalPortal,
                "Employee",
                "Your current request is in progress of revision"
            );

            // Log for internal CRM when first approver approves
            var firstApprovalComment = request.GetAttributeValue<string>("ldv_firstapprovalcommentreason");
            string description = !string.IsNullOrWhiteSpace(firstApprovalComment)
                ? firstApprovalComment
                : $"First approval employee {owner.Name} has approved the request";

            GeneralLog.Create(
                service,
                regarding,
                "First Approval - Approved",
                LogType.Audit,
                LogVisibility.InternalCRM,
                owner.Name,
                description
            );

        }

        private void HandleFirstApprovalReject(IOrganizationService service, Entity request, Entity update)
        {
            //Set first approver
            update["ldv_firstapprover"] = request.GetAttributeValue<EntityReference>("ownerid");
            update["ldv_firstapprovaldate"] = DateTime.UtcNow;

            // Update statuses
            update["ldv_portalstatus"] = new OptionSetValue((int)PortalStatus.Rejected);
            update["ldv_crmstatus"] = new OptionSetValue((int)CrmStatus.Rejected);

            // Move BPF Finalization Stage
            MoveToFinalizationStage(request.Id);

            //Deactivate
            update["statecode"] = new OptionSetValue((int)HeaderStateCode.Inactive);
            update["statuscode"] = new OptionSetValue((int)HeaderStatusCode.Cancelled);

            // Log for external portal
            var regarding = new EntityReference(request.LogicalName, request.Id);
            var firstApprovalComment = request.GetAttributeValue<string>("ldv_firstapprovalcommentreason");
            var owner = request.GetAttributeValue<EntityReference>("ownerid");

            GeneralLog.Create(
                service,
                regarding,
                "Rejected",
                LogType.Audit,
                LogVisibility.ExternalPortal,
                "Employee",
                firstApprovalComment
            );

            // Log for internal CRM when first approver rejection

            GeneralLog.Create(
                service,
                regarding,
                "First Approval - Rejected",
                LogType.Audit,
                LogVisibility.InternalCRM,
                owner?.Name ?? "Employee",
                firstApprovalComment
            );
        }



        private void HandleFirstApprovalBackToCitizen(IOrganizationService service, Entity request, Entity update)
        {
            //Set first approver
            update["ldv_firstapprover"] = request.GetAttributeValue<EntityReference>("ownerid");
            update["ldv_firstapprovaldate"] = DateTime.UtcNow;


            // Reset fields
            update["ldv_portalstatus"] = new OptionSetValue((int)PortalStatus.ActionRequired);
            update["ldv_crmstatus"] = new OptionSetValue((int)CrmStatus.SendBackToCitizen);

            update["ldv_issubmittedfromportal"] = false;

            // Log for external portal
            var regarding = new EntityReference(request.LogicalName, request.Id);
            var firstApprovalComment = request.GetAttributeValue<string>("ldv_firstapprovalcommentreason");
            var owner = request.GetAttributeValue<EntityReference>("ownerid");

            GeneralLog.Create(
                service,
                regarding,
                "Action Required",
                LogType.Audit,
                LogVisibility.ExternalPortal,
                "Employee",
                firstApprovalComment
            );

            // Log for internal CRM when first approver SendBackToCitizen
            GeneralLog.Create(
                service,
                regarding,
                "First Approval - Send Back to Citizen",
                LogType.Audit,
                LogVisibility.InternalCRM,
                owner?.Name ?? "Employee",
                firstApprovalComment
            );
        }


        private void MoveToFinalizationStage(Guid requestId)
        {
            Guid FINALIZATION_STAGE_ID = new Guid("cbfad161-060d-4445-b4c8-d7189b50ff0c");

            BpfHelper.MoveToStage(Service, "ldv_citizenrequestapprovalbpf", "bpf_ldv_requestid", requestId, FINALIZATION_STAGE_ID);

        }
    }

}
