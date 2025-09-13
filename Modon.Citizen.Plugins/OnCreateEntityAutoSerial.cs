using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modon.Citizen.Plugins
{
    public class OnCreateEntityAutoSerial : PluginBase
    {
        protected override void ExecutePlugin(IServiceProvider serviceProvider)
        {
            if (Context.InputParameters.Contains("Target") && Context.InputParameters["Target"] is Entity entity)
            {

                QueryExpression query = new QueryExpression("ldv_autonumbersettings")
                {
                    ColumnSet = new ColumnSet("ldv_prefix", "ldv_numberlength", "ldv_lastusednumber", "ldv_usedateinautonumber", "ldv_entitylogicalname"),
                    Criteria =
                    {
                        Conditions =
                        {
                            new ConditionExpression("ldv_entitylogicalname", ConditionOperator.Equal,entity.LogicalName )
                        }
                    }
                };

                var settings = Service.RetrieveMultiple(query).Entities.FirstOrDefault();
                if (settings == null) return;

                int lastNumber = settings.Contains("ldv_lastusednumber")
                    ? settings.GetAttributeValue<int>("ldv_lastusednumber")
                    : 0;

                if (lastNumber < 0)
                    lastNumber = 0;

                int length = settings.Contains("ldv_numberlength")
                    ? settings.GetAttributeValue<int>("ldv_numberlength")
                    : 4;

                string prefix = settings.GetAttributeValue<string>("ldv_prefix");
                bool useDate = settings.Contains("ldv_usedateinautonumber")
                    ? settings.GetAttributeValue<bool>("ldv_usedateinautonumber")
                    : false;

                int nextNumber = lastNumber + 1;
                string serial = nextNumber.ToString().PadLeft(length, '0');

                string autoNumber;
                if (useDate)
                {
                    autoNumber = $"{prefix}-{DateTime.UtcNow:yyyy-MM-dd}-{serial}";
                }
                else
                {
                    autoNumber = $"{prefix}-{serial}";
                }


                entity["ldv_name"] = autoNumber; // set the request number

                Entity updateSettings = new Entity(settings.LogicalName, settings.Id);
                updateSettings["ldv_lastusednumber"] = nextNumber;
                Service.Update(updateSettings);
            }

        }
    }
}
