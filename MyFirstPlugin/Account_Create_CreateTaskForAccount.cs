
using System;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;

namespace darishplugin
{
    public class Account_Create_CreateTaskForAccount : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Obtain the tracing service
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            //obtain the execution context from the service provider
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            
            // Obtain the IOrganizationService instance which you will need for web service calls 
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
                
            //trace in plugin log
            tracingService.Trace("Execution started.");

            // The target entity is the account that was created.
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                Entity entity = (Entity)context.InputParameters["Target"]; //Entity is a class from Xrm

                try
                {
                    // Create a task activity to follow up with the account customer in 7 days
                    Entity followup = new Entity("task");

                    followup["subject"] = "Send e-mail to the new customer.";
                    followup["description"] = "Follow up with the customer. Check if there are any new issues that need resolution.";
                    followup["scheduledstart"] = DateTime.Now.AddDays(7);
                    followup["scheduledend"] = DateTime.Now.AddDays(7);
                    followup["category"] = context.PrimaryEntityName;

                    // Refer to the account in the task activity
                    if (context.OutputParameters.Contains("id"))
                    {
                        Guid regardingobjectid = new Guid(context.OutputParameters["id"].ToString());
                        string regardingobjectidType = "account";

                        followup["regardingobjectid"] = new EntityReference(regardingobjectidType, regardingobjectid);
                    }

                    // Create the task in Microsoft Dynamics CRM
                    service.Create(followup);
                }
                catch (FaultException<OrganizationServiceFault> ex)
                {
                    throw new InvalidPluginExecutionException("An error occured in the followup" + ex);
                }
                //service.Create(task);
                catch (Exception ex)
                {
                    tracingService.Trace("Account_Create_CreateTaskForAccount: {0}", ex.ToString());
                    throw;
                }
            }
            else
            { return; }
            tracingService.Trace("Execution ended.");
        }
    }
}
