using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Extensions;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;

namespace ContactGroupAssociation
{
    public class Class1 : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.Get<IPluginExecutionContext>();
            var service = serviceProvider.GetOrganizationService(context.UserId);

            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity entity)
            {
                if (entity.LogicalName == "contact" && entity.Attributes.Contains("ds_universitygroupid"))
                {
                    switch (context.MessageName)
                    {
                        case "Create":
                            AssociateRecord(service, entity.GetAttributeValue<EntityReference>("ds_universitygroupid"),
                                new Relationship("ds_ds_universitygroup_contact"),
                                new EntityReferenceCollection() {new EntityReference(entity.LogicalName, entity.Id)});
                            break;
                    }
                }
            }

            if (context.InputParameters.Contains("Relationship") &&
                context.InputParameters["Relationship"] is Relationship relationship &&
                relationship.SchemaName == "ds_ds_universitygroup_contact" &&
                context.InputParameters.Contains("RelatedEntities") &&
                context.InputParameters["RelatedEntities"] is EntityReferenceCollection entityReferenceCollection &&
                context.InputParameters.Contains("Target") &&
                context.InputParameters["Target"] is EntityReference group)
            {
                switch (context.MessageName)
                {
                    case "Associate":
                        
                        var specialityReference = service
                            .Retrieve(group.LogicalName, group.Id,
                                new ColumnSet("ds_universityspecialityid"))
                            .GetAttributeValue<EntityReference>("ds_universityspecialityid");

                        var chairReference = service.Retrieve(specialityReference.LogicalName, specialityReference.Id,
                                new ColumnSet("ds_universitychairid"))
                            .GetAttributeValue<EntityReference>("ds_universitychairid");

                        var updateEntity = new Entity(entityReferenceCollection.First().LogicalName)
                        {
                            ["ds_uchairid"] = chairReference,
                            ["ds_uspecialityid"] = specialityReference,
                            ["ds_universitygroupid"] = new EntityReference(group.LogicalName, group.Id),
                            Id = entityReferenceCollection.First().Id,
                        };
                        service.Update(updateEntity);
                        break;

                    case "Disassociate":

                        var contactUgRef = service.Retrieve(entityReferenceCollection.First().LogicalName,
                                entityReferenceCollection.First().Id, new ColumnSet("ds_universitygroupid"))
                            .GetAttributeValue<EntityReference>("ds_universitygroupid");
                        if (contactUgRef != null && group.Id == contactUgRef.Id)
                        {
                            updateEntity = new Entity(entityReferenceCollection.First().LogicalName)
                            {
                                ["ds_uchairid"] = null,
                                ["ds_uspecialityid"] = null,
                                ["ds_universitygroupid"] = null,
                                Id = entityReferenceCollection.First().Id,
                            };
                            service.Update(updateEntity);
                        }
                        break;
                }
            }
        }
       

        private void AssociateRecord(IOrganizationService service, EntityReference entityReference1,
            Relationship relationship, EntityReferenceCollection referenceCollection)
        {
            service.Associate(entityReference1.LogicalName, entityReference1.Id,
                relationship, referenceCollection);
        }
    }
}