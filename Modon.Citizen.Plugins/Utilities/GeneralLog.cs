using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modon.Citizen.Plugins.Utilities
{
    public class GeneralLog
    {
        public static void Create(
                            IOrganizationService service,
                            EntityReference regardingRequestId,
                            string subject,
                            LogType logType,
                            LogVisibility visibility,  
                            string actionBy,
                            string description)
        {
            // Create log record
            var log = new Entity("ldv_generallog");
            log["subject"] = subject;
            log["regardingobjectid"] = regardingRequestId;
            log["ldv_logtype"] = new OptionSetValue((int)logType);
            log["ldv_visibility"] = new OptionSetValue((int)visibility);
            log["ldv_actionby"] = actionBy;
            log["createdon"] = DateTime.UtcNow;
            log["description"] = description;

            // Create record
            var logId = service.Create(log);

            // Mark log as Completed (activity close)
            var close = new Entity("ldv_generallog")
            {
                Id = logId
            };
            close["statecode"] = new OptionSetValue(1);   // Completed
            close["statuscode"] = new OptionSetValue(2);  // Completed = 2

            service.Update(close);
        }

    }
}
