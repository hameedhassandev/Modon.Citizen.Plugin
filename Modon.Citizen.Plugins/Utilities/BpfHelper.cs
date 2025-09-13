using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modon.Citizen.Plugins.Utilities
{
    public static class BpfHelper
    {
        public static void MoveToStage(
            IOrganizationService service,
            string bpfEntityName,
            string bpfLookupAttribute,
            Guid regardingId,
            Guid targetStageId)
        {
            // get BPF instance
            var query = new QueryExpression(bpfEntityName)
            {
                ColumnSet = new ColumnSet("businessprocessflowinstanceid", "activestageid")
            };
            query.Criteria.AddCondition(bpfLookupAttribute, ConditionOperator.Equal, regardingId);

            var bpfInstances = service.RetrieveMultiple(query).Entities;
            if (!bpfInstances.Any())
                return;

            var bpfInstance = bpfInstances.First();

            // move to first approval stage
            var updateBpf = new Entity(bpfInstance.LogicalName, bpfInstance.Id)
            {
                ["activestageid"] = new EntityReference("processstage", targetStageId)
            };
            service.Update(updateBpf);
        }
    }
}
