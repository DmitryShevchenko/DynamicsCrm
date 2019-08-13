using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Extensions;
using Microsoft.Xrm.Sdk.Query;

namespace EntityNaming
{
    public class Class1 : IPlugin
    {
        private IOrganizationService _service;

        public void Execute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.Get<IPluginExecutionContext>();
            _service = serviceProvider.GetOrganizationService(context.UserId);

            try
            {
                if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity entity)
                {
                    switch (entity.LogicalName)
                    {
                        case "ds_technicaltaskmanager":
                            TechnicalTaskManagerGetName(entity);
                            break;
                        case "ds_testquestion":
                            QuestionGetName(entity);
                            break;
                        case "ds_hrinterview":
                            InterviewGetName(entity);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                throw new InvalidPluginExecutionException(e.Message);
            }
        }

        private void TechnicalTaskManagerGetName(Entity entity)
        {
            var retrieveEntity = _service.Retrieve(entity.LogicalName, entity.Id, new ColumnSet(true));
            
            var contactid = retrieveEntity.GetAttributeValue<EntityReference>("ds_contactid");
            var vacancyid = retrieveEntity.GetAttributeValue<EntityReference>("ds_vacancyid");
            var technicaltaskid = retrieveEntity.GetAttributeValue<EntityReference>("ds_technicaltaskid");
            
            var ety = new Entity(retrieveEntity.LogicalName)
            {
                ["ds_name"] = string.Join(" ", "Contact_id:", contactid.Id, "Vacancy_id:", vacancyid.Id, "TechnicalTask_id", technicaltaskid.Id),
                Id = retrieveEntity.Id,
            };
            _service.Update(ety);
        }
        
        private void QuestionGetName(Entity entity)
        {
            var retrieveEntity = _service.Retrieve(entity.LogicalName, entity.Id, new ColumnSet(true));
            
            var questionDestination = retrieveEntity.GetAttributeValue<OptionSetValueCollection>("ds_questiondestination")
                .Select(x => x.Value).ToList();
            var desCode = "";
            questionDestination.ForEach(item => { desCode += item.ToString() + ";"; });
            var subjectid = retrieveEntity.GetAttributeValue<EntityReference>("ds_subjectid");
            var themeid = retrieveEntity.GetAttributeValue<EntityReference>("ds_theme");
            
            var ety = new Entity(retrieveEntity.LogicalName)
            {
                ["ds_name"] = string.Join(" ", "DestinationCode: ", desCode, "Subject_id:", subjectid.Id, "Theme_id:", themeid.Id),
                Id = retrieveEntity.Id,
                
            };
            _service.Update(ety);
        }

        private void InterviewGetName(Entity entity)
        {
            var retrieveEntity = _service.Retrieve(entity.LogicalName, entity.Id, new ColumnSet(true));
            var createdon = retrieveEntity.GetAttributeValue<DateTime>("createdon");
            var contactid = retrieveEntity.GetAttributeValue<EntityReference>("ds_contactid");
            var ownerid = retrieveEntity.GetAttributeValue<EntityReference>("ownerid");
            
            var ety = new Entity(retrieveEntity.LogicalName)
            {
                ["ds_name"] = string.Join(" ", "Date: ", createdon, "Contact_id:", contactid.Id, "Owner_id:", ownerid.Id),
                Id = retrieveEntity.Id,
                
            };
            _service.Update(ety);
        }
    }
}