using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;

namespace darishplugin
{
    public class Case_Create_SendMailOnHighPriority : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);

            tracingService.Trace("Execution started.");

            if (!(context.InputParameters["Target"] is Entity caseEntity) || caseEntity.LogicalName != "incident")
            {
                tracingService.Trace("Target is not a case (incident).");
                return;
            }

            // Check if priority is High (OptionSet value 1)
            if (!caseEntity.Attributes.Contains("prioritycode"))
            {
                tracingService.Trace("No prioritycode set.");
                return;
            }

            OptionSetValue priority = caseEntity.GetAttributeValue<OptionSetValue>("prioritycode");
            if (priority.Value != 1) // 1 = High
            {
                tracingService.Trace("Priority is not high. Skipping email.");
                return;
            }

            // Get owner (typically a systemuser)
            EntityReference ownerRef = caseEntity.GetAttributeValue<EntityReference>("ownerid");
            if (ownerRef == null || ownerRef.LogicalName != "systemuser")
            {
                tracingService.Trace("Owner is null or not a systemuser.");
                return;
            }

            // Retrieve owner's email
            var owner = service.Retrieve("systemuser", ownerRef.Id, new ColumnSet("internalemailaddress"));
            string email = owner.GetAttributeValue<string>("internalemailaddress");

            if (string.IsNullOrWhiteSpace(email))
            {
                tracingService.Trace("Owner has no email address.");
                return;
            }

            // Create email entity
            Entity emailEntity = new Entity("email");
            emailEntity["subject"] = "New High Priority Case Assigned";
            emailEntity["description"] = "A high-priority case has been created and assigned to you.";
            emailEntity["directioncode"] = true; // outgoing
            emailEntity["to"] = new EntityCollection(new[]
            {
            new Entity("activityparty")
            {
                ["partyid"] = new EntityReference("systemuser", ownerRef.Id)
            }
        });

            emailEntity["from"] = new EntityCollection(new[]
            {
            new Entity("activityparty")
            {
                ["partyid"] = new EntityReference("systemuser", context.InitiatingUserId)
            }
        });

            // Create email record
            Guid emailId = service.Create(emailEntity);
            tracingService.Trace($"Email record created with ID: {emailId}");

            // Send email
            var sendEmailReq = new SendEmailRequest
            {
                EmailId = emailId,
                TrackingToken = "",
                IssueSend = true
            };

            service.Execute(sendEmailReq);
            tracingService.Trace("Email sent successfully to the owner.");
            tracingService.Trace("Execution ended.");
        }
    }
}
