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
    public class AutoNumberSingleton : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            QueryExpression query = new QueryExpression("uds_autonumbersfeaturerequest")
            {
                ColumnSet = new ColumnSet(false),
                NoLock = false,
                Criteria = new FilterExpression() { }
            };
            Entity autoNumber = service.RetrieveMultiple(query)?.Entities?.FirstOrDefault();

            if (autoNumber != null)
                throw new InvalidPluginExecutionException("AUTONUMBER record already exists.");

        }
    }
}
