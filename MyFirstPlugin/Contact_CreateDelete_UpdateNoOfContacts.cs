using System;
using System.ServiceModel.Channels;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace darishplugin
{
    public class Contact_CreateDelete_UpdateNoOfContacts : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);
            
            tracingService.Trace("Execution started.");
                      
            EntityReference accountRef = null;

            if (context.MessageName == "Create")
            {
                if (!(context.InputParameters["Target"] is Entity contact) || contact.LogicalName != "contact")
                {
                    tracingService.Trace("Target is not contact.");
                    return;
                }

                if (!contact.Attributes.Contains("parentcustomerid"))
                {
                    tracingService.Trace("No parent account linked. Skipping count update.");
                    return;
                }

                EntityReference parentRef = contact.GetAttributeValue<EntityReference>("parentcustomerid");
                if (parentRef.LogicalName == "account")
                {
                    accountRef = parentRef;
                }
            }
            else if (context.MessageName == "Delete")
            {
                if (!(context.InputParameters["Target"] is EntityReference targetRef) || targetRef.LogicalName != "contact")
                {
                    tracingService.Trace("Target is not contact (delete).");
                    return;
                }

                // Get PreImage to find parent account
                if (!context.PreEntityImages.Contains("ParentCustomerIdPreImage") || !(context.PreEntityImages["ParentCustomerIdPreImage"] is Entity preImage))
                {
                    tracingService.Trace("ParentCustomerIdPreImage missing for delete.");
                    return;
                }

                if (preImage.Contains("parentcustomerid"))
                {
                    EntityReference parentRef = preImage.GetAttributeValue<EntityReference>("parentcustomerid");
                    if (parentRef.LogicalName == "account")
                    {
                        accountRef = parentRef;
                    }
                }
            }

            if (accountRef == null)
            {
                tracingService.Trace("No account to update. Exiting.");
                return;
            }

            // Query total contacts under this account
            var query = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet("contactid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("parentcustomerid", ConditionOperator.Equal, accountRef.Id),
                        new ConditionExpression("parentcustomeridtype", ConditionOperator.Equal, "account")
                    }
                }
            };

            var results = service.RetrieveMultiple(query); //get all contacts under a single account
            int totalContacts = results.Entities.Count; //count number of contacts in selected account

            // Update the account with the count
            var updateAccount = new Entity("account", accountRef.Id);
            updateAccount["dar_contactcount"] = totalContacts;

            service.Update(updateAccount);

            if (context.MessageName == "Delete" && context.PreEntityImages.Contains("ParentCustomerIdPreImage"))
            {
                var deletedEntity = context.PreEntityImages["ParentCustomerIdPreImage"];
                string fullname = deletedEntity.GetAttributeValue<string>("fullname") ?? "Unknown";
                tracingService.Trace($"Deleted contact: {fullname}");
            }


            tracingService.Trace($"Updated account ({accountRef.Name}) with contact count: {totalContacts}");
            tracingService.Trace("Execution ended.");
        }
    }
}