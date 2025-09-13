using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Modon.Citizen.Plugins
{
    public abstract class PluginBase : IPlugin
    {
        protected IPluginExecutionContext Context { get; private set; }
        protected IOrganizationServiceFactory ServiceFactory { get; private set; }
        protected IOrganizationService Service { get; private set; }
        protected ITracingService Tracing { get; private set; }

        public void Execute(IServiceProvider serviceProvider)
        {
            try
            {
                Context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
                ServiceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                Service = ServiceFactory.CreateOrganizationService(Context.UserId);
                Tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

                ExecutePlugin(serviceProvider);
            }
            catch (Exception ex)
            {
                if (Tracing != null)
                    Tracing.Trace("Plugin Exception: {0}", ex.ToString());

                throw new InvalidPluginExecutionException("An error occurred in plugin: " + ex.Message, ex);
            }
        }

        protected abstract void ExecutePlugin(IServiceProvider serviceProvider);
    }

}
