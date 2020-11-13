using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;

namespace UDS.FeatureRequest
{
    public class SendDefaultEmailNotification : CodeActivity
    {
        [RequiredArgument]
        [Input("EmailReference")]
        [ReferenceTarget("email")]
        public InArgument<EntityReference> EmailReference { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();

            if (context == null)
            {
                throw new InvalidPluginExecutionException("An error occured in UDS.ErrorRequest.SendDefaultEmailNotification activity. Failed to retrieve workflow context.");
            }

            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            EntityReference emailRef = EmailReference.Get<EntityReference>(executionContext);
            Entity email = service.Retrieve(emailRef.LogicalName, emailRef.Id, new ColumnSet(true));

            Entity settings = GetSettingsRecord(service);
            if (settings == null || !settings.Attributes.Contains("uds_sendfromuserid"))
                throw new InvalidPluginExecutionException("An error occured in UDS.ErrorRequest.SendDefaultEmailNotification activity. Please check the Error Handler Settings record.");

            EntityReference sendFromUser = settings.GetAttributeValue<EntityReference>("uds_sendfromuserid");

            List<Entity> sendToUsers = GetUsersToNotify(service, settings.Id);

            List<Entity> sendToContacts = GetContactsToNotify(service, settings.Id);

            EntityCollection EntCol = new EntityCollection();

            if (sendToUsers?.Count > 0)
            {
                foreach (Entity user in sendToUsers)
                {

                    Entity ToActivityParty = new Entity("activityparty");
                    ToActivityParty["partyid"] = new EntityReference(user.LogicalName, user.Id);
                    EntCol.Entities.Add(ToActivityParty);
                }
            }
            if (sendToUsers?.Count > 0)
            {
                foreach (Entity contact in sendToContacts)
                {

                    Entity ToActivityParty = new Entity("activityparty");
                    ToActivityParty["partyid"] = new EntityReference(contact.LogicalName, contact.Id);
                    EntCol.Entities.Add(ToActivityParty);
                }
            }
            
            if (email != null && sendFromUser != null)
            {
                email["to"] = EntCol;

                //from
                Entity party = new Entity("activityparty");
                party["partyid"] = new EntityReference(sendFromUser.LogicalName, sendFromUser.Id); //new EntityReference("systemuser", new Guid("B4E5F5B6-6A6B-E511-80E0-3863BB358C70"));

                EntityCollection from = new EntityCollection();
                from.Entities.Add(party);

                email["from"] = from;

                service.Update(email);

                SendEmailRequest sendEmailreq = new SendEmailRequest
                {
                    EmailId = email.Id,
                    TrackingToken = "",
                    IssueSend = true
                };

                SendEmailResponse sendEmailresp = (SendEmailResponse)service.Execute(sendEmailreq);

            }
        }

        private static Entity GetSettingsRecord(IOrganizationService service)
        {
            QueryExpression query = new QueryExpression()
            {
                NoLock = true,
                EntityName = "uds_errorrequestbuttonsettings",
                ColumnSet = new ColumnSet("uds_sendfromuserid"),
            };

            List<Entity> response = service.RetrieveMultiple(query).Entities.ToList();

            if (response.Count == 1)
            {
                return response.FirstOrDefault();
            }

            return null;
        }

        private static List<Entity> GetUsersToNotify(IOrganizationService service, Guid settingsId)
        {
            QueryExpression query = new QueryExpression()
            {
                NoLock = true,
                EntityName = "systemuser",
                ColumnSet = new ColumnSet("internalemailaddress"),
                Criteria = new FilterExpression()
                {
                    FilterOperator = LogicalOperator.And,
                    Filters =
                    {
                        new FilterExpression()
                        {
                            FilterOperator = LogicalOperator.And,

                            Conditions =
                            {
                                new ConditionExpression("internalemailaddress", ConditionOperator.NotNull)
                            }
                        }
                    }
                },

                LinkEntities =
                {
                    new LinkEntity()
                    {
                        JoinOperator = JoinOperator.Inner,

                        LinkFromEntityName = "systemuser",
                        LinkFromAttributeName = "systemuserid",

                        LinkToEntityName = "uds_uds_errorrequestbuttonsettings_systemus",
                        LinkToAttributeName = "systemuserid",


                        LinkEntities =
                        {
                            new LinkEntity()
                            {
                                JoinOperator = JoinOperator.Inner,

                                LinkFromEntityName = "uds_uds_errorrequestbuttonsettings_systemus",
                                LinkFromAttributeName = "uds_errorrequestbuttonsettingsid",

                                LinkToEntityName = "uds_errorrequestbuttonsettings",
                                LinkToAttributeName = "uds_errorrequestbuttonsettingsid",

                                LinkCriteria = new FilterExpression()
                                {
                                    FilterOperator = LogicalOperator.And,
                                    Conditions =
                                    {
                                        new ConditionExpression("uds_errorrequestbuttonsettingsid", ConditionOperator.Equal, settingsId)
                                    }
                                },
                            }
                        }
                    }
                }
            };

            List<Entity> response = service.RetrieveMultiple(query).Entities.ToList();

            return response;
        }

        private static List<Entity> GetContactsToNotify(IOrganizationService service, Guid settingsId)
        {
            QueryExpression query = new QueryExpression()
            {
                NoLock = true,
                EntityName = "contact",
                ColumnSet = new ColumnSet("emailaddress1"),
                Criteria = new FilterExpression()
                {
                    FilterOperator = LogicalOperator.And,
                    Filters =
                    {
                        new FilterExpression()
                        {
                            FilterOperator = LogicalOperator.And,

                            Conditions =
                            {
                                new ConditionExpression("emailaddress1", ConditionOperator.NotNull)
                            }
                        }
                    }
                },

                LinkEntities =
                {
                    new LinkEntity()
                    {
                        JoinOperator = JoinOperator.Inner,

                        LinkFromEntityName = "contact",
                        LinkFromAttributeName = "contactid",

                        LinkToEntityName = "uds_uds_errorrequestbuttonsettings_contact",
                        LinkToAttributeName = "contactid",


                        LinkEntities =
                        {
                            new LinkEntity()
                            {
                                JoinOperator = JoinOperator.Inner,

                                LinkFromEntityName = "uds_uds_errorrequestbuttonsettings_contact",
                                LinkFromAttributeName = "uds_errorrequestbuttonsettingsid",

                                LinkToEntityName = "uds_errorrequestbuttonsettings",
                                LinkToAttributeName = "uds_errorrequestbuttonsettingsid",

                                LinkCriteria = new FilterExpression()
                                {
                                    FilterOperator = LogicalOperator.And,
                                    Conditions =
                                    {
                                        new ConditionExpression("uds_errorrequestbuttonsettingsid", ConditionOperator.Equal, settingsId)
                                    }
                                },
                            }
                        }
                    }
                }
            };

            List<Entity> response = service.RetrieveMultiple(query).Entities.ToList();

            return response;
        }
    }
}
