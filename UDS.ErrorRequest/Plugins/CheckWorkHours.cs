using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UDS.FeatureRequest
{
    public class CheckWorkHours : IPlugin
    {
        public void Execute(IServiceProvider provider)
        {
            ITracingService tracer = (ITracingService)provider.GetService(typeof(ITracingService));
            IPluginExecutionContext context = (IPluginExecutionContext)provider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory factory = (IOrganizationServiceFactory)provider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = factory.CreateOrganizationService(context.UserId);

            try
            {
                if (!(context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity))
                    return;
                Entity target = (Entity)context.InputParameters["Target"];
                Entity preImage = context.PreEntityImages.Contains("PreImage") ? context.PreEntityImages["PreImage"] : new Entity();
                int? workHours = target.Attributes.Contains("uds_workhours") ? target.GetAttributeValue<int>("uds_workhours") : (int?)null;
                EntityReference featureRequest = target.Attributes.Contains("uds_featurerequestid") ? target.GetAttributeValue<EntityReference>("uds_featurerequestid")
                    : preImage.Attributes.Contains("uds_featurerequestid") ? preImage.GetAttributeValue<EntityReference>("uds_featurerequestid")
                    : null;
                if (workHours == null || featureRequest == null)
                    return;
                Entity featureRequestEntity = service.Retrieve("uds_featurerequest", featureRequest.Id, new ColumnSet("uds_estimate"));
                int? estimate = featureRequestEntity.Attributes.Contains("uds_estimate") ? featureRequestEntity.GetAttributeValue<int>("uds_estimate") : (int?)null;
                if (estimate == null)
                    return;
                List<Entity> requestWorkHoursList = Logic.GetRelatedWorkHours(service, featureRequest.Id);
                int workHoursSum = (int)workHours;
                foreach (Entity requestWorkHours in requestWorkHoursList)
                {
                    workHoursSum += requestWorkHours.Contains("uds_workhours") ? requestWorkHours.GetAttributeValue<int>("uds_workhours") : 0;
                }
                if (workHoursSum > estimate)
                    target["uds_featuremessage"] = "Estimate";
            }
            catch (InvalidPluginExecutionException ex)
            {
                tracer.Trace(ex.Message);
                throw;
            }
        }
    }

    internal static class Logic
    {
        internal static List<Entity> GetRelatedWorkHours(IOrganizationService service, Guid featureRequestId)
        {
            QueryExpression query = new QueryExpression()
            {
                Distinct = false,
                EntityName = "uds_requestworkhours",
                ColumnSet = new ColumnSet("uds_workhours"),
                Criteria = new FilterExpression()
                {
                    Filters =
                    {
                        new FilterExpression()
                        {
                            FilterOperator = LogicalOperator.And,

                            Conditions =
                            {
                                new ConditionExpression("statecode", ConditionOperator.Equal, 0),
                                new ConditionExpression("uds_featurerequestid", ConditionOperator.Equal, featureRequestId)
                            }
                        }
                    }
                }
            };
            return service.RetrieveMultiple(query)?.Entities?.ToList();
        }
    }
}
