using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

public class Lead_CreateUpdateDelete_CRUD : IPlugin
{
    public void Execute(IServiceProvider serviceProvider)
    {
        var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
        var factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
        var service = factory.CreateOrganizationService(context.UserId);
        var tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

        tracing.Trace("Execution started.");

        try
        {
            // Only run for Lead Create
            if (context.MessageName != "Create" || !(context.InputParameters["Target"] is Entity lead) || lead.LogicalName != "lead")
                return;

            string leadSubject = lead.GetAttributeValue<string>("subject") ?? "No Subject";
            string topic = "Opportunity for " + leadSubject;

            // 1️ CREATE Opportunity
            Entity opportunity = new Entity("opportunity");
            opportunity["name"] = topic;
            //opportunity["customerid"] = new EntityReference("lead", (Guid)context.OutputParameters["id"]);
            Guid oppId = service.Create(opportunity);
            tracing.Trace($"Opportunity created with ID: {oppId}");

            // 2️ UPDATE Lead (custom bool field new_opportunitylinked = true)
            Entity leadUpdate = new Entity("lead", (Guid)context.OutputParameters["id"]);
            leadUpdate["new_opportunitylinked"] = true;
            service.Update(leadUpdate);
            tracing.Trace("Lead updated with new_opportunitylinked = true.");

            // 3️ DELETE Opportunity (for demo: if subject == "DELETE")
            if (leadSubject.ToUpper() == "DELETE")
            {
                service.Delete("opportunity", oppId);
                tracing.Trace("Opportunity deleted due to subject = DELETE.");
            }

            tracing.Trace("LeadOpportunityHandlerPlugin: Execution completed.");
        }
        catch (Exception ex)
        {
            tracing.Trace("Error: " + ex.ToString());
            throw new InvalidPluginExecutionException("Plugin failed: " + ex.Message);
        }
        tracing.Trace("Execution ended.");
    }
}
