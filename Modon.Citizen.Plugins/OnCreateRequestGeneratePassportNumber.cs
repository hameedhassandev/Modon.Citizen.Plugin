using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;

namespace Modon.Citizen.Plugins
{
    public class OnCreateRequestGeneratePassportNumber : PluginBase
    {
        protected override void ExecutePlugin(IServiceProvider serviceProvider)
        {
            if (Context.InputParameters.Contains("Target") && Context.InputParameters["Target"] is Entity entity)
            {
                
                const int NEW_REQUEST_TYPE = 753240000;
                const int PASSPORT_DOCUMENT_TYPE = 753240001;

                if (entity.Contains("ldv_requesttype") && entity.GetAttributeValue<OptionSetValue>("ldv_requesttype").Value == NEW_REQUEST_TYPE
                    && entity.Contains("ldv_documenttype") && entity.GetAttributeValue<OptionSetValue>("ldv_documenttype").Value == PASSPORT_DOCUMENT_TYPE)
                {
                    string passportNo;
                    bool exists;
                    Random rnd = new Random();
                    do
                    {
                        passportNo = GeneratePassportNumber(rnd);
                        var fetch = new QueryExpression("ldv_request")
                        {
                            ColumnSet = new ColumnSet("ldv_passportnumber"),
                            Criteria = new FilterExpression
                            {
                                Conditions =
                                {
                                    new ConditionExpression("ldv_passportnumber", ConditionOperator.Equal, passportNo)
                                }
                            }
                        };
                        exists = Service.RetrieveMultiple(fetch).Entities.Any();
                    }
                    while (exists);

                    entity["ldv_passportnumber"] = passportNo;
                }
            }

        }

        private string GeneratePassportNumber(Random rnd)
        {
            char letter = (char)rnd.Next('A', 'Z' + 1);
            string digits = rnd.Next(0, 99999999).ToString("D8");
            return $"{letter}{digits}";
        }
    }
}