using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;

namespace UDS.FeatureRequest
{
    public class SendUserEmailNotification : CodeActivity
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
                throw new InvalidPluginExecutionException("An error occured in UDS.ErrorRequest.SendUserEmailNotification activity. Failed to retrieve workflow context.");
            }

            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            EntityReference emailRef = EmailReference.Get<EntityReference>(executionContext);
            Entity email = service.Retrieve(emailRef.LogicalName, emailRef.Id, new ColumnSet(true));

            Entity settings = GetSettingsRecord(service);
            if (settings == null || !settings.Attributes.Contains("uds_sendfromuserid"))
                throw new InvalidPluginExecutionException("An error occured in UDS.ErrorRequest.SendUserEmailNotification activity. Please check the Error Handler Settings record.");

            EntityReference sendFromUser = settings.GetAttributeValue<EntityReference>("uds_sendfromuserid");

            if (email != null && sendFromUser != null)
            {
                //from
                Entity party = new Entity("activityparty");
                party["partyid"] = new EntityReference(sendFromUser.LogicalName, sendFromUser.Id); //new EntityReference("systemuser", new Guid("{784C5C1A-E0D7-EA11-8133-00155D06FD02}"));

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

    }
}
