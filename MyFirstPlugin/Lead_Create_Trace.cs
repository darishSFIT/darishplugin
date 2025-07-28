using System;
using Microsoft.Xrm.Sdk;

namespace darishplugin
{
    public class Lead_Create_Trace : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Get tracing service
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Get execution context
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            // Log plugin start
            tracingService.Trace("Execution started.");

            try
            {
                // Ensure the plugin is triggered on 'Create' of Lead
                if (context.MessageName.ToLower() != "create" || context.PrimaryEntityName.ToLower() != "lead")
                {
                    tracingService.Trace("LeadCreationLogger: Not a create message or not on lead entity.");
                    return;
                }

                // Get the target entity
                if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity entity)
                {
                    tracingService.Trace("LeadCreationLogger: A Lead has been created with Subject: {0}", entity.GetAttributeValue<string>("subject"));
                }
                else
                {
                    tracingService.Trace("LeadCreationLogger: Target entity not found or not of type 'Entity'.");
                }
            }
            catch (Exception ex)
            {
                tracingService.Trace("LeadCreationLogger: Exception {0}", ex.ToString());
                throw;
            }

            tracingService.Trace("Execution ended.");
        }
    }
}