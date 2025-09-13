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
    public class OnSecondApprovalStatusChanged : PluginBase
    {
        protected override void ExecutePlugin(IServiceProvider serviceProvider)
        {
            if (Context.InputParameters.Contains("Target") && Context.InputParameters["Target"] is Entity entity)
            {
                Entity request = Service.Retrieve(entity.LogicalName, entity.Id, new ColumnSet(true));

                var secondfirstApprovalStatus = request.GetAttributeValue<OptionSetValue>("ldv_secondapprovalstatus")?.Value;


                if (secondfirstApprovalStatus != null)
                {
                    var update = new Entity(request.LogicalName, request.Id);

                    switch (secondfirstApprovalStatus)
                    {
                        case (int)SecondApprovalStatus.Approved:
                            HandleSecondApproval(Service, request, update);
                            break;

                        case (int)SecondApprovalStatus.Rejected:
                            HandleSecondApprovalReject(Service, request, update);
                            break;

                        case (int)SecondApprovalStatus.SentBack:
                            HandleSecondApprovalBackToCitizen(Service, request, update);
                            break;
                    }

                    Service.Update(update);
                }

            }
        }


        private void HandleSecondApproval(IOrganizationService service, Entity request, Entity update)
        {
            //Set second approver
            update["ldv_secondapprovaldate"] = DateTime.UtcNow;


            // Update statuses
            update["ldv_portalstatus"] = new OptionSetValue((int)PortalStatus.Done);
            update["ldv_crmstatus"] = new OptionSetValue((int)CrmStatus.Done);

            //Deactivate
            update["statecode"] = new OptionSetValue((int)HeaderStateCode.Inactive);
            update["statuscode"] = new OptionSetValue((int)HeaderStatusCode.Completed);



            //Create related document
            CreateCitizenDocument(service, request);

            // Log for external portal
            var regarding = new EntityReference(request.LogicalName, request.Id);
            var secondApprovalComment = request.GetAttributeValue<string>("ldv_secondapprovalcommentreason");
            var owner = request.GetAttributeValue<EntityReference>("ownerid");

            GeneralLog.Create(
                service,
                regarding,
                "Done",
                LogType.Audit,
                LogVisibility.ExternalPortal,
                "Employee",
                "Your request has been successfully approved and the document has been issued"
            );

            // Log for internal CRM when second approver approve
            string description = !string.IsNullOrWhiteSpace(secondApprovalComment)
                 ? secondApprovalComment
                 : $"The request was approved by the second approver ({owner?.Name ?? "Employee"}), and the document has been issued";


            GeneralLog.Create(
                service,
                regarding,
                "Second Approval - Completed",
                LogType.Audit,
                LogVisibility.InternalCRM,
                owner?.Name ?? "Employee",
                description
            );
        }

        private void HandleSecondApprovalReject(IOrganizationService service, Entity request, Entity update)
        {
            //Set second approver
            update["ldv_secondapprovaldate"] = DateTime.UtcNow;

            // Update statuses
            update["ldv_portalstatus"] = new OptionSetValue((int)PortalStatus.Rejected);
            update["ldv_crmstatus"] = new OptionSetValue((int)CrmStatus.Rejected);

            //Deactivate
            update["statecode"] = new OptionSetValue((int)HeaderStateCode.Inactive);
            update["statuscode"] = new OptionSetValue((int)HeaderStatusCode.Cancelled);

            // Log for external portal
            var regarding = new EntityReference(request.LogicalName, request.Id);
            var seondApprovalComment = request.GetAttributeValue<string>("ldv_secondapprovalcommentreason");
            var owner = request.GetAttributeValue<EntityReference>("ownerid");

            GeneralLog.Create(
                service,
                regarding,
                "Rejected",
                LogType.Audit,
                LogVisibility.ExternalPortal,
                "Employee",
                seondApprovalComment
            );

            // Log for internal CRM when first approver rejection

            GeneralLog.Create(
                service,
                regarding,
                "Second Approval - Rejected",
                LogType.Audit,
                LogVisibility.InternalCRM,
                owner?.Name ?? "Employee",
                seondApprovalComment
            );
        }


        private void HandleSecondApprovalBackToCitizen(IOrganizationService service, Entity request, Entity update)
        {
            // Reset fields
            update["ldv_portalstatus"] = new OptionSetValue((int)PortalStatus.ActionRequired);
            update["ldv_crmstatus"] = new OptionSetValue((int)CrmStatus.SendBackToCitizen);

            update["ldv_issubmittedfromportal"] = false;

            // Log for external portal
            var regarding = new EntityReference(request.LogicalName, request.Id);
            var secondApprovalComment = request.GetAttributeValue<string>("ldv_secondapprovalcommentreason");
            var owner = request.GetAttributeValue<EntityReference>("ownerid");

            GeneralLog.Create(
                service,
                regarding,
                "Action Required",
                LogType.Audit,
                LogVisibility.ExternalPortal,
                "Employee",
                secondApprovalComment
            );

            // Log for internal CRM when first approver SendBackToCitizen
            GeneralLog.Create(
                service,
                regarding,
                "Second Approval - Send Back to Citizen",
                LogType.Audit,
                LogVisibility.InternalCRM,
                owner?.Name ?? "Employee",
                secondApprovalComment
            );

            //handle reset and assign request to first approver in AssignBackToFirstApproverAsynccplugin

            // Move back BPF stage to Submitted
            MoveToSubmittedStage(request.Id);
        }

        private void CreateCitizenDocument(IOrganizationService service, Entity request)
        {
            // Ensure document type exists
            var docType = request.GetAttributeValue<OptionSetValue>("ldv_documenttype")?.Value;
            if (docType == null)
                throw new InvalidPluginExecutionException("Document type is required before creating document.");

            // Create new document
            var doc = new Entity("ldv_document")
            {
                ["ldv_citizen"] = request.GetAttributeValue<EntityReference>("ldv_citizen"),
                ["ldv_relatedrequest"] = request.ToEntityReference(),
                ["ldv_documenttype"] = new OptionSetValue(docType.Value)
            };

            // Assign document number depending on type
            if (docType == (int)DocumentType.ID)
            {
                doc["ldv_idnumber"] = request.GetAttributeValue<string>("ldv_citizenidnumber");
            }
            else if (docType == (int)DocumentType.Passport)
            {
                doc["ldv_passportnumber"] = request.GetAttributeValue<string>("ldv_passportnumber");
            }

            var issueDate = DateTime.UtcNow;
            doc["ldv_issuancedate"] = issueDate;
            doc["ldv_expirydate"] = issueDate.AddYears(7);

            service.Create(doc);


            if (docType == (int)DocumentType.ID)
            {
                var citizenRef = request.GetAttributeValue<EntityReference>("ldv_citizen");
                if (citizenRef != null)
                {
                    var citizenUpdate = new Entity(citizenRef.LogicalName, citizenRef.Id)
                    {
                        ["statuscode"] = new OptionSetValue((int)CitizenStatus.Verified)
                    };

                    service.Update(citizenUpdate);
                }
            }
        }

        private void MoveToSubmittedStage(Guid requestId)
        {
            Guid SUBMITTED_STAGE_ID = new Guid("0a7390a0-81f2-4afd-8ce5-5d1e1b20fb6f");

            BpfHelper.MoveToStage(Service, "ldv_citizenrequestapprovalbpf", "bpf_ldv_requestid", requestId, SUBMITTED_STAGE_ID);
        }
    }

}
