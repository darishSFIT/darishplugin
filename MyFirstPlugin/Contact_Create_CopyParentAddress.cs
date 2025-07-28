using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace darishplugin
{
    public class Contact_Create_CopyParentAddress : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Get tracing service
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            tracingService.Trace("Execution started.");

            // Get context
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            // Get org service
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);

            // Get target entity
            if (!(context.InputParameters["Target"] is Entity contact) || contact.LogicalName != "contact")
            {
                tracingService.Trace("Target is not contact.");
                return;
            }

            // Get target contact name i.e. when creating a new contact
            string contactFirstName = contact.GetAttributeValue<string>("firstname");
            string contactLastName = contact.GetAttributeValue<string>("lastname");

            // Check if parent customer is set
            if (!contact.Attributes.Contains("parentcustomerid"))
            {
                tracingService.Trace("No parentcustomerid found. Skipping address copy.");
                return;
            }

            // Get parent reference
            EntityReference parentRef = contact.GetAttributeValue<EntityReference>("parentcustomerid"); //use EntityReference only for lookup
            tracingService.Trace($"Parent type: {parentRef.LogicalName}, ID: {parentRef.Id}.");

            // Copy address fields to contact
            if (parentRef.LogicalName == "account") //if not empty. empty means no account is selected
            {
                // Retrieve parent account's address
                var parentAccount = service.Retrieve("account", parentRef.Id, new ColumnSet(
                    "name",
                    "address1_line1",
                    "address1_line2",
                    "address1_line3",
                    "address1_city",
                    "address1_stateorprovince",
                    "address1_postalcode",
                    "address1_country"));

                string companyName = parentAccount.GetAttributeValue<string>("name");

                // If parent account contains address line then contact address line field is same as coreressponding parent address line field
                // below are the relevant address fields visible when creating a contact

                if (parentAccount.Contains("address1_line1"))
                    contact["address1_line1"] = parentAccount["address1_line1"];

                if (parentAccount.Contains("address1_line2"))
                    contact["address1_line2"] = parentAccount["address1_line2"];

                if (parentAccount.Contains("address1_line3"))
                    contact["address1_line3"] = parentAccount["address1_line3"];

                if (parentAccount.Contains("address1_city"))
                    contact["address1_city"] = parentAccount["address1_city"];

                if (parentAccount.Contains("address1_stateorprovince"))
                    contact["address1_stateorprovince"] = parentAccount["address1_stateorprovince"];

                if (parentAccount.Contains("address1_postalcode"))
                    contact["address1_postalcode"] = parentAccount["address1_postalcode"];

                if (parentAccount.Contains("address1_country"))
                    contact["address1_country"] = parentAccount["address1_country"];

                tracingService.Trace($"Address of Account ({companyName}) was successfully copied to new contact {contactFirstName} {contactLastName}");

            }
            else if (parentRef.LogicalName == "contact")
            {
                var parentContact = service.Retrieve("contact", parentRef.Id, new ColumnSet(
                    "firstname",
                    "lastname",
                    "address1_line1",
                    "address1_line2",
                    "address1_line3",
                    "address1_city",
                    "address1_stateorprovince",
                    "address1_postalcode",
                    "address1_country"));

                string parentFirstName = parentContact.GetAttributeValue<string>("firstname");
                string parentLastName = parentContact.GetAttributeValue<string>("lastname");

                if (parentContact.Contains("address1_line1"))
                    contact["address1_line1"] = parentContact["address1_line1"];

                if (parentContact.Contains("address1_line2"))
                    contact["address1_line2"] = parentContact["address1_line2"];

                if (parentContact.Contains("address1_line3"))
                    contact["address1_line3"] = parentContact["address1_line3"];

                if (parentContact.Contains("address1_city"))
                    contact["address1_city"] = parentContact["address1_city"];

                if (parentContact.Contains("address1_stateorprovince"))
                    contact["address1_stateorprovince"] = parentContact["address1_stateorprovince"];

                if (parentContact.Contains("address1_postalcode"))
                    contact["address1_postalcode"] = parentContact["address1_postalcode"];

                if (parentContact.Contains("address1_country"))
                    contact["address1_country"] = parentContact["address1_country"];

                tracingService.Trace($"Address of contact {parentFirstName} {parentLastName} was successfully copied to new contact {contactFirstName} {contactLastName}");
            }
            else
            {
                tracingService.Trace("Parent is neither account nor contact. No address copied.");
            }
            tracingService.Trace("Execution ended.");
        }
    }
}