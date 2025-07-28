using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace darishplugin
{
    public class Contact_Update_RestrictMembershipID : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            tracingService.Trace("Execution started.");

            tracingService.Trace("Context: " + context);
            tracingService.Trace("Service Factory: " + serviceFactory);
            tracingService.Trace("Service: " + service);
            tracingService.Trace("Tracing Service: " + tracingService);

            tracingService.Trace("Depth: " + context.Depth);
            tracingService.Trace("Parent context: " + context.ParentContext);

            if (!(context.InputParameters["Target"] is Entity target) || target.LogicalName != "contact")
            {
                tracingService.Trace("Target is not contact.");
                return;
            }

            // Only act if membership ID is being updated
            if (!target.Attributes.Contains("dar_membershipid"))
            {
                tracingService.Trace("dar_membershipid not in target. Skipping.");
                return;
            }

            Guid contactId = target.Id;
            if (contactId == Guid.Empty)
            {
                tracingService.Trace("Contact ID is empty. Exiting.");
                return;
            }

            // Retrieve existing membership ID
            var existingContact = service.Retrieve("contact", contactId, new ColumnSet("dar_membershipid"));
            int? existingValue = existingContact.GetAttributeValue<int?>("dar_membershipid"); //int? is a nullable int
            int? newValue = target.GetAttributeValue<int?>("dar_membershipid");

            if (existingValue.HasValue && newValue.HasValue && existingValue.Value != newValue.Value)
            {
                tracingService.Trace($"Attempt to change membership ID from {existingValue.Value} to {newValue.Value} is not allowed.");
                throw new InvalidPluginExecutionException("Membership ID cannot be changed while updation once it has been set while creation.");
            }

            tracingService.Trace("Execution ended.");
        }
    }
}