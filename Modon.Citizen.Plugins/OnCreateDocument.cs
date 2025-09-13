using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modon.Citizen.Plugins
{
    public class OnCreateDocument : PluginBase
    {
        protected override void ExecutePlugin(IServiceProvider serviceProvider)
        {
            if (Context.InputParameters.Contains("Target") && Context.InputParameters["Target"] is Entity entity)
            {
                var citizenRef = entity.GetAttributeValue<EntityReference>("ldv_citizen");
                var documentType = entity.GetAttributeValue<OptionSetValue>("ldv_documenttype");

                if (citizenRef == null || documentType == null)
                    return;

                var query = new QueryExpression("ldv_document")
                {
                    ColumnSet = new ColumnSet("ldv_documentid", "statuscode", "statecode"),
                    Criteria =
                    {
                        Conditions =
                        {
                            new ConditionExpression("ldv_citizen", ConditionOperator.Equal, citizenRef.Id),
                            new ConditionExpression("ldv_documenttype", ConditionOperator.Equal, documentType.Value),
                            new ConditionExpression("statecode", ConditionOperator.Equal, 0), // Active,
                            new ConditionExpression("ldv_documentid", ConditionOperator.NotEqual, entity.Id) // exclude current

                        }
                    }
                };

                var existingDocs = Service.RetrieveMultiple(query);

                foreach (var doc in existingDocs.Entities)
                {
                    // Update state & status
                    var update = new Entity("ldv_document", doc.Id)
                    {
                        ["statecode"] = new OptionSetValue(1), // Inactive
                        ["statuscode"] = new OptionSetValue(2) // Expired 
                    };
                    Service.Update(update);
                }
            }
        }
    }
}
