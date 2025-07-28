using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace darishplugin
{
    public class Contact_Create_QueryContacts : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);

            tracingService.Trace("Execution started.");

            try
            {
                // Define base query
                var query = new QueryExpression("contact")
                {
                    ColumnSet = new ColumnSet("firstname", "lastname", "jobtitle", "createdon")
                };

                // Filter: first name contains 'o' (LIKE)
                var nameFilter = new ConditionExpression("firstname", ConditionOperator.Like, "%o%");

                // Filter: created in last 2 days
                var dateFilter = new ConditionExpression("createdon", ConditionOperator.LastXDays, 2);

                // Filter: job title is Actor
                var managerFilter = new ConditionExpression("jobtitle", ConditionOperator.Equal, "Actor");


                // Join to account table to filter by city tiwelve islands (like SQL join)
                var accountLink = query.AddLink("account", "parentcustomerid", "accountid");
                accountLink.EntityAlias = "acc"; // not used in right now but can be used for complex joins
                accountLink.Columns = new ColumnSet("address1_city"); // used ColumnSet retrive city field from account table
                accountLink.LinkCriteria.AddCondition("address1_city", ConditionOperator.Equal, "tiwelve islands"); //filter by city field (only retrive city with value = tiwelve islands)

                // Combine filters 

                var andFilter = new FilterExpression(LogicalOperator.And); // AND expression filter for name and date
                andFilter.AddCondition(nameFilter);
                andFilter.AddCondition(dateFilter);

                var orFilter = new FilterExpression(LogicalOperator.Or); // OR expression that checks either AND filters or manager filter

                orFilter.AddFilter(andFilter); // AddFilter bcoz andFilter is having FilterExpression
                orFilter.AddCondition(managerFilter); // (firstname+date) OR jobtitle = Manager

                query.Criteria.AddFilter(orFilter); // Add this OR filter block into the top level querys criteria

                var results = service.RetrieveMultiple(query); // Execute query and fetch more than one records (retrive multiple)

                tracingService.Trace($"Found {results.Entities.Count} matching contacts."); // count of records retrived using query

                foreach (var contact in results.Entities)
                {
                    string fname = contact.GetAttributeValue<string>("firstname") ?? "";
                    string lname = contact.GetAttributeValue<string>("lastname") ?? "";
                    string title = contact.GetAttributeValue<string>("jobtitle") ?? "";

                    tracingService.Trace($"Contact: {fname} {lname}, Title: {title}");
                }

            }
            catch (Exception ex)
            {
                tracingService.Trace($"QueryContactsPlugin Error: {ex.Message}");
                throw;
            }

            tracingService.Trace("Execution ended.");
        }
    }
}
