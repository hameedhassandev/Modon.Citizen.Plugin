using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Modon.Citizen.Plugins.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modon.Citizen.Plugins
{
    public class OnRequestInitialization : PluginBase
    {
        protected override void ExecutePlugin(IServiceProvider serviceProvider)
        {
            if (Context.InputParameters.Contains("Target") && Context.InputParameters["Target"] is Entity entity)
            {
                var request = Service.Retrieve("ldv_request", entity.Id, new ColumnSet("ldv_citizen", "ldv_documenttype"));

                var citizenRef = request.GetAttributeValue<EntityReference>("ldv_citizen");
                if (citizenRef == null) return;

                var updateRequest = new Entity(request.LogicalName, request.Id);

                updateRequest["ldv_portalstatus"] = new OptionSetValue((int)PortalStatus.Draft);
                updateRequest["ldv_crmstatus"] = new OptionSetValue((int)CrmStatus.Draft);

                Service.Update(updateRequest);

                GeneralLog.Create(
                Service,
                new EntityReference(request.LogicalName, request.Id),
                "Draft",
                LogType.Audit,
                LogVisibility.ExternalPortal,
                citizenRef.Name ?? "Citizen",
                "Initial request created");

                var requestDetails = request.GetAttributeValue<string>("ldv_requestdetails");

                GeneralLog.Create(
                    Service,
                    new EntityReference(request.LogicalName, request.Id),
                    "Request Created",
                    LogType.Audit,
                    LogVisibility.InternalCRM,
                    citizenRef.Name ?? "Citizen",
                    $"A new request has been initiated by the citizen. Details: {requestDetails}"
                );


                //assign request to team 
                var documentType = request.GetAttributeValue<OptionSetValue>("ldv_documenttype")?.Value;
                string teamName = null;

                if (documentType == (int)DocumentType.ID)
                    teamName = "Civil Affairs Team";
                else if (documentType == (int)DocumentType.Passport)
                    teamName = "Passport  Administrations Team";

                if (!string.IsNullOrEmpty(teamName))
                {
                    var teamId = GetTeamId(Service, teamName);

                    if (teamId != Guid.Empty)
                    {
                        var assignRequest = new AssignRequest
                        {
                            Assignee = new EntityReference("team", teamId),
                            Target = new EntityReference(request.LogicalName, request.Id)
                        };

                        Service.Execute(assignRequest);
                    }
                }


            }
        }

        private Guid GetTeamId(IOrganizationService service, string teamName)
        {
            var query = new QueryExpression("team")
            {
                ColumnSet = new ColumnSet("teamid"),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression("name", ConditionOperator.Equal, teamName)
                    }
                }
            };

            var team = service.RetrieveMultiple(query).Entities.FirstOrDefault();
            return team?.Id ?? Guid.Empty;
        }
    }
}
