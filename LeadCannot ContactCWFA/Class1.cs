using System;
using System.Activities;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;

namespace LeadCannot_ContactCWFA
{
    public class Class1 : CodeActivity
    {
        
        protected override void Execute(CodeActivityContext executionContext)
        {
            
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();

            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            if (context.InputParameters.Contains("Target") &&
                context.InputParameters["Target"] is Entity entity)
            {
                    
                var studentRequestList = service.RetrieveMultiple(new QueryExpression("ds_studentrequest")
                {
                    Criteria = new FilterExpression()
                    {
                        Conditions =
                            {new ConditionExpression("ds_leadid", ConditionOperator.Equal, entity.Id)}
                    }
                }).Entities.Select(x => x).ToList();
                
                var studentRequest = new Entity(studentRequestList[0].LogicalName)
                {
                    ["statecode"] = new OptionSetValue(0),
                    ["statuscode"] = new OptionSetValue(717590000),
                };
                studentRequest.Id = studentRequestList[0].Id;
                
                service.Update(studentRequest);
            }
        }
    }
}