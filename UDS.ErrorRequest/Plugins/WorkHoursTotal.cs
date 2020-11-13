using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace UDS.FeatureRequest
{
    public class WorkHoursTotal : IPlugin
    {
        public void Execute(IServiceProvider provider)
        {
            ITracingService tracer = (ITracingService)provider.GetService(typeof(ITracingService));
            IPluginExecutionContext context = (IPluginExecutionContext)provider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory factory = (IOrganizationServiceFactory)provider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = factory.CreateOrganizationService(context.UserId);

            try
            {

                if (!(context.InputParameters.Contains("Target")))
                    return;
                if (context.InputParameters["Target"] is Entity)
                {

                    Entity target = (Entity)context.InputParameters["Target"];
                    Entity preImage = context.PreEntityImages.Contains("PreImage") ? context.PreEntityImages["PreImage"] : new Entity();

                    int? workHours = target.Attributes.Contains("uds_workhours") ? target.GetAttributeValue<int>("uds_workhours") : (int?)null;
                    
                    EntityReference featureRequest = target.Attributes.Contains("uds_featurerequestid") ? target.GetAttributeValue<EntityReference>("uds_featurerequestid")
                        : preImage.Attributes.Contains("uds_featurerequestid") ? preImage.GetAttributeValue<EntityReference>("uds_featurerequestid")
                        : null;


                    if (featureRequest == null)
                        return;
                   
                    List<Entity> requestWorkHoursList = GetRelatedWorkHours(service, featureRequest.Id);

                    int workHoursSum = 0;
                    foreach (Entity requestWorkHours in requestWorkHoursList)
                    {
                        workHoursSum += requestWorkHours.Contains("uds_workhours") ? requestWorkHours.GetAttributeValue<int>("uds_workhours") : 0;
                    }

                    Entity featureRequestUpd = service.Retrieve("uds_featurerequest", featureRequest.Id, new ColumnSet(false));
                    featureRequestUpd["uds_workhourstotal"] = workHoursSum;
                    service.Update(featureRequestUpd);

                }
                else if (context.InputParameters["Target"] is EntityReference)
                {
                    EntityReference target = (EntityReference)context.InputParameters["Target"];
                    Entity preImage = context.PreEntityImages.Contains("PreImage") ? context.PreEntityImages["PreImage"] : new Entity();
                    EntityReference featureRequest = preImage.Attributes.Contains("uds_featurerequestid") ? preImage.GetAttributeValue<EntityReference>("uds_featurerequestid")
                        : null;
                    if (featureRequest == null)
                        return;
                    List<Entity> requestWorkHoursList = GetRelatedWorkHours(service, featureRequest.Id);
                    int workHoursSum = 0;
                    foreach (Entity requestWorkHours in requestWorkHoursList)
                    {
                        if (requestWorkHours.Id != target.Id)
                        {
                            workHoursSum += requestWorkHours.Contains("uds_workhours") ? requestWorkHours.GetAttributeValue<int>("uds_workhours") : 0;

                        }
                    }
                    Entity featureRequestUpd = service.Retrieve("uds_featurerequest", featureRequest.Id, new ColumnSet(false));
                    featureRequestUpd["uds_workhourstotal"] = workHoursSum;
                    service.Update(featureRequestUpd);
                }
            }
            catch (InvalidPluginExecutionException ex)
            {
                tracer.Trace("An feature occured in UDS.featureRequest.WorkHoursTotal plugin. " + ex.Message);
                throw new InvalidPluginExecutionException("An feature occured in UDS.featureRequest.WorkHoursTotal plugin. " + ex.Message);
            }
        }

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
