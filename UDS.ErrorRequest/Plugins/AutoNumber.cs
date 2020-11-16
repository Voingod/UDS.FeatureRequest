using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace UDS.FeatureRequest
{
    public class AutoNumber : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            var crmContext = new OrganizationServiceContext(service);

            try
            {
                Entity target = (Entity)context.InputParameters["Target"];

                Entity config = GetCounterEntity(service);

                if (!config.Attributes.Contains("uds_value"))
                    throw new InvalidPluginExecutionException("An error occurred in UDS.FeatureRequest.AutoNumber plugin: Check AUTONUMBER record.");

                string entityid = string.Empty;

                //if lock key is equal to current key in process, then record is ready for writing 
                string currentid = context.PrimaryEntityId.ToString();
                while (entityid != currentid)
                {
                    // checking, if record is free for processing 
                    System.Threading.Thread.Sleep(25);
                    config = GetCounterEntity(service);
                    entityid = config.Attributes.Contains("uds_recordidbeingprocessed") ? config.GetAttributeValue<string>("uds_recordidbeingprocessed") : null;
                    //if entityid is empty, then record is free for processing. 
                    if (string.IsNullOrEmpty(entityid))
                    {
                        Entity updatedConfig = new Entity() { LogicalName = config.LogicalName, Id = config.Id, };
                        //only key value (entityid) will be saved, this key locking current record 
                        updatedConfig["uds_recordidbeingprocessed"] = currentid;
                        //create UpdateRequest, Update(_counterEntity) not working 

                        //UpdateRequest upd = new UpdateRequest() { Target = updatedConfig, ConcurrencyBehavior = ConcurrencyBehavior.AlwaysOverwrite };
                        //service.Execute(upd);
                        service.Update(updatedConfig);
                    }
                }


                int number = config.GetAttributeValue<int>("uds_value") + 1;

                while (HasDuplicatesByNumber("FR-" + number, target.Id, service))
                {
                    number++;
                }

                config["uds_value"] = number;
                config["uds_recordidbeingprocessed"] = null;

                service.Update(config);

                //set counter to field in callingEntity
                target["uds_number"] = "FR-" + number;

            }
            catch (InvalidPluginExecutionException ex)
            {
                throw new InvalidPluginExecutionException("An error occurred in UDS.FeatureRequest.AutoNumber plugin." + ex.Message);
            }
        }

        public Entity GetCounterEntity(IOrganizationService service)
        {
            QueryExpression query = new QueryExpression("uds_autonumbersfeaturerequest")
            {
                ColumnSet = new ColumnSet(true),
                NoLock = false,
                Criteria = new FilterExpression() { }
            };
            Entity autoNumber = service.RetrieveMultiple(query)?.Entities?.FirstOrDefault();

            if (autoNumber == null)
            {
                autoNumber = new Entity("uds_autonumbersfeaturerequest");
                autoNumber["uds_value"] = 0;
                autoNumber["uds_entity"] = "Feature request";
                service.Create(autoNumber);
            }

            return autoNumber;
        }

        public static bool HasDuplicatesByNumber(string number, Guid excludeId, IOrganizationService service)
        {
            QueryExpression query = new QueryExpression()
            {
                Distinct = false,
                EntityName = "uds_featurerequest",
                ColumnSet = new ColumnSet("uds_number"),
                Criteria = new FilterExpression()
                {
                    Filters =
                    {
                        new FilterExpression()
                        {
                            FilterOperator = LogicalOperator.And,

                            Conditions =
                            {
                                new ConditionExpression("uds_number", ConditionOperator.Equal, number),
                                new ConditionExpression("activityid", ConditionOperator.NotEqual, excludeId)
                            }
                        }
                    }
                }
            };

            var duplicates = service.RetrieveMultiple(query).Entities.ToList();

            return duplicates.Count != 0;
        }
    }
}
