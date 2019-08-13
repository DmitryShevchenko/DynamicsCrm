using System;
using System.Activities;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;

namespace UEPM_Disassociate_NtoN_Relationship_CWFA
{
    public class Class1 : CodeActivity
    {
        protected override void Execute(CodeActivityContext executionContext)
        {
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();

            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            
            var relationships = service.RetrieveMultiple(
                new QueryExpression("ds_ds_universityeducationprogrammanager_ds")
                {
                    ColumnSet = new ColumnSet(true), Criteria = new FilterExpression()
                    {
                        Conditions =
                        {
                            new ConditionExpression("ds_universityeducationprogrammanagerid", ConditionOperator.Equal,
                                Guid.Parse("eb9c2d43-ec77-e911-a819-000d3a22bb6b"))
                        }
                    }
                });
            
            var entityReferenceCollection = new EntityReferenceCollection();

            foreach (var relationshipsEntity in relationships.Entities)
            {
                var objects = relationshipsEntity.Attributes.Where(x => x.Key == "ds_themeid")
                    .ToDictionary(x => x.Key, x => x.Value);
                

                foreach (var keyValuePair in objects)
                {
                    entityReferenceCollection.Add(new EntityReference(keyValuePair.Key, (Guid) keyValuePair.Value));
                }
            }
            service.Disassociate("ds_universityeducationprogrammanager", Guid.Parse("eb9c2d43-ec77-e911-a819-000d3a22bb6b"), 
                new Relationship("ds_ds_universityeducationprogrammanager_ds_t"), entityReferenceCollection);
            
        }
    }
}