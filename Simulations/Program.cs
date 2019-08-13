using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;

namespace Simulations
{
    internal class Program
    {
        static Program()
        {
            CrmServiceClient conn = new CrmServiceClient(connectionString);
            _service = (IOrganizationService) conn.OrganizationWebProxyClient ?? conn.OrganizationServiceProxy;
        }

        private static readonly string connectionString =
            @"AuthType=Office365;Url=https://dutpd41v7.crm4.dynamics.com/;Username=non_user@dutpd41v7.onmicrosoft.com;Password=D1028erm1997";

        private static IOrganizationService _service;
        
        public static void Main(string[] args)
        {
        }
        
        private static void NewStudentRequest()
        {
            var entity = new Entity("ds_studentrequest")
            {
                ["ds_firstname"] = "Dmitry",
                ["ds_lastname"] = "Shevchenko",
                ["ds_mobilephone"] = "0990049919",
                ["emailaddress"] = "dima946762@gmail.com",
                ["ds_universitygroupid"] = new EntityReference("ds_universitygroup", Guid.Parse("1cbdeda8-ba83-e911-a81e-000d3aba3dc1")),
                ["ds_otherspecialityinfo"] = "C#, ASP.net core, Angular, React, SQL",
            };
            _service.Create(entity);
        }
        private static void GetRightAnsvers(Guid TestId)
        {
            var testquestionid = _service.RetrieveMultiple(new QueryExpression("ds_ds_test_ds_testquestion")
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression()
                {
                    Conditions = {new ConditionExpression("ds_testid", ConditionOperator.Equal, TestId)}
                }
            }).Entities.Select(x => x.GetAttributeValue<Guid>("ds_testquestionid")).ToList();

            var questionsAnswersObjects = _service.RetrieveMultiple(new QueryExpression("ds_testquestion")
            {
                ColumnSet = new ColumnSet("ds_correctvar"),
                Criteria = new FilterExpression()
                {
                    Conditions = {new ConditionExpression("ds_testquestionid", ConditionOperator.In, testquestionid)}
                }
            }).Entities.Select(x => new AnswersQuestion()
            {
                QuestionId = x.Id,
                AnswerCode = x.GetAttributeValue<OptionSetValueCollection>("ds_correctvar").Select(y => y.Value)
                    .ToList()
            }).ToList();

            var testresultId = _service.RetrieveMultiple(new QueryExpression("ds_testresult")
            {
                Criteria = new FilterExpression()
                    {Conditions = {new ConditionExpression("ds_testid", ConditionOperator.Equal, TestId)}} ////
            }).Entities.First().Id;

            var ent = new Entity("ds_testresult")
            {
                Id = testresultId,
                ["ds_jsonanswers"] = questionsAnswersObjects.Serialize(),
                ["ds_status"] = true,
            };
            _service.Update(ent);
        }

        private static void DeleteAllRecords(string recordName)
        {
            while (true)
            {
                var multipleRequest = new ExecuteMultipleRequest()
                {
                    // Assign settings that define execution behavior: continue on error, return responses. 
                    Settings = new ExecuteMultipleSettings()
                    {
                        ContinueOnError = false,
                        ReturnResponses = true
                    },
                    // Create an empty organization request collection.
                    Requests = new OrganizationRequestCollection()
                };

                // Create several (local, in memory) entities in a collection. 
                var input = _service.RetrieveMultiple(new QueryExpression(recordName)).Entities
                    .Select(x => new EntityReference(x.LogicalName, x.Id)).Take(1000).ToList(); //ds_object

                if (input.Count == 0)
                {
                    break;
                }

                // Add a CreateRequest for each entity to the request collection.
                foreach (var entity in input)
                {
                    DeleteRequest deleteRequest = new DeleteRequest {Target = entity};
                    multipleRequest.Requests.Add(deleteRequest);
                }

                ExecuteMultipleResponse multipleResponse = (ExecuteMultipleResponse) _service.Execute(multipleRequest);
            }
        }

        private static void GenerateEducationData()
        {
            var subjects = _service.RetrieveMultiple(new QueryExpression("ds_subject")
            {
                ColumnSet = new ColumnSet(true),
            }).Entities.ToList();

            var themes = _service.RetrieveMultiple(new QueryExpression("ds_theme")
            {
                ColumnSet = new ColumnSet(true),
            });

            var ugrops = _service.RetrieveMultiple(new QueryExpression("ds_universitygroup")
                {ColumnSet = new ColumnSet("ds_name", "ds_universityspecialityid")}).Entities.ToList();


            ugrops.ForEach(group =>
            {
                var specialityRef = group.GetAttributeValue<EntityReference>("ds_universityspecialityid");
                var chairRef = _service
                    .Retrieve(specialityRef.LogicalName, specialityRef.Id, new ColumnSet("ds_universitychairid"))
                    .GetAttributeValue<EntityReference>("ds_universitychairid");


                var educationList = new List<Education>();

                var subject = subjects.Where(x =>
                    x.Attributes.Contains("ds_universitychairid") &&
                    x.GetAttributeValue<EntityReference>("ds_universitychairid").Id == chairRef.Id).ToList();

                subject.ForEach(item =>
                {
                    var themeIdRefCollection = new EntityReferenceCollection();
                    themes.Entities
                        .Where(x => x.Attributes.Contains("ds_subjectid") &&
                                    x.GetAttributeValue<EntityReference>("ds_subjectid").Id == item.Id)
                        .Select(x => new EntityReference(x.LogicalName, x.Id)).ToList().ForEach(reff =>
                        {
                            themeIdRefCollection.Add(reff);
                        });

                    educationList.Add(new Education()
                    {
                        Prop = item.GetAttributeValue<string>("ds_name"),
                        SubjectName = item.LogicalName,
                        Subjectid = item.Id,
                        ThemeName = themes.Entities.First().LogicalName,
                        ThemeId = themeIdRefCollection,
                    });
                });

                educationList.ForEach(education =>
                {
                    var UEPM = new Entity("ds_universityeducationprogrammanager")
                    {
                        ["ds_name"] = string.Join(" ", group.GetAttributeValue<string>("ds_name"), education.Prop),
                        ["ds_universitygroupid"] = new EntityReference(group.LogicalName, group.Id),
                        ["ds_subjectid"] = new EntityReference(education.SubjectName, education.Subjectid),
                    };

                    var uepmId = _service.Create(UEPM);

                    _service.Associate("ds_universityeducationprogrammanager", uepmId,
                        new Relationship("ds_ds_universityeducationprogrammanager_ds_t"), education.ThemeId);
                });
            });
        }

        private static void GenerateUData()
        {
            for (int i = 1; i <= 2; i++)
            {
                var faculty = new Entity("ds_universityfaculty")
                {
                    ["ds_name"] = string.Join(" ", "Факультет" + i)
                };

                var facultyId = _service.Create(faculty);

                for (int j = 1; j <= 3; j++)
                {
                    var chair = new Entity("ds_universitychair")
                    {
                        ["ds_name"] = string.Join(" ", "Факультет" + i + "Кафедра" + j),
                        ["ds_universityfacultyid"] = new EntityReference(faculty.LogicalName, facultyId),
                    };
                    var chairId = _service.Create(chair);

                    for (int k = 1; k <= 2; k++)
                    {
                        var speciality = new Entity("ds_universityspeciality")
                        {
                            ["ds_name"] = string.Join(" ", "Факультет" + i + "Кафедра" + j + "Специальность" + k),
                            ["ds_universitychairid"] = new EntityReference(chair.LogicalName, chairId),
                        };
                        var specialityId = _service.Create(speciality);

                        for (int l = 1; l <= 3; l++)
                        {
                            var group = new Entity("ds_universitygroup")
                            {
                                ["ds_name"] = string.Join(" ", "Кафедра" + j + "Специальность" + k + "Группа" + l),
                                ["ds_universityspecialityid"] =
                                    new EntityReference(speciality.LogicalName, specialityId),
                                ["ds_educationyearstart"] = DateTime.Parse("09/01/2018"),
                                ["ds_educationyearend"] = DateTime.Parse("05/30/2019"),
                            };
                            var groupId = _service.Create(group);
                        }

                        var subject = new Entity("ds_subject")
                        {
                            ["ds_name"] = string.Join(" ", "Кафедра" + j + " Предмет" + k),
                            ["ds_universitychairid"] = new EntityReference(chair.LogicalName, chairId),
                        };
                        var subjectId = _service.Create(subject);

                        for (int m = 1; m <= 4; m++)
                        {
                            var theme = new Entity("ds_theme")
                            {
                                ["ds_name"] = string.Join(" ", "Предмет" + k + "Тема" + m),
                                ["ds_subjectid"] = new EntityReference(subject.LogicalName, subjectId),
                            };
                            var themeId = _service.Create(theme);
                        }
                    }
                }
            }
        }

        private static void QuestionGenaration()
        {
            var themes = _service.RetrieveMultiple(new QueryExpression("ds_theme") {ColumnSet = new ColumnSet(true)})
                .Entities.ToList();


            themes.ForEach(item =>
            {
                for (int i = 1; i <= 30; i++)
                {
                    var correctvar = new OptionSetValueCollection();
                    var correctvarInt = new List<int>();
                    var random = new List<int>();
                    int rand;

                    for (int j = 0; j < new Random().Next(1, 4); j++)
                    {
                        var list = new List<int>() {717590000, 717590001, 717590002, 717590003, 717590004};

                        do
                        {
                            rand = new Random().Next(list.Count);
                        } while (random.Contains(rand));

                        if (correctvarInt.Count != 0 && list[rand] == 717590000)
                        {
                            continue;
                        }

                        if (correctvarInt.Count == 0 && list[rand] == 717590000)
                        {
                            correctvarInt.Add(list[rand]);
                            correctvar.Add(new OptionSetValue(list[rand]));
                            continue;
                        }


                        correctvarInt.Add(list[rand]);
                        correctvar.Add(new OptionSetValue(list[rand]));
                        random.Add(rand);
                    }

                    var question = string.Join(" ", "Question", new Random().Next(50000));


                    if (i < 10)
                    {
                        var entity = new Entity("ds_testquestion")
                        {
                            ["ds_name"] = question,
                            ["ds_questionlevel"] = new OptionSetValue(717590000),
                            ["ds_questiondestination"] = new OptionSetValueCollection()
                                {new OptionSetValue(717590000), new OptionSetValue(717590001)},
                            ["ds_subjectid"] = item.GetAttributeValue<EntityReference>("ds_subjectid"),
                            ["ds_theme"] = new EntityReference(item.LogicalName, item.Id),
                            ["ds_question"] = question,
                            ["ds_correctvar"] = correctvar,
                        };
                        _service.Create(entity);
                    }

                    if (i >= 10 && i < 20)
                    {
                        var entity = new Entity("ds_testquestion")
                        {
                            ["ds_name"] = question,
                            ["ds_questionlevel"] = new OptionSetValue(717590001),
                            ["ds_questiondestination"] = new OptionSetValueCollection()
                                {new OptionSetValue(717590000), new OptionSetValue(717590001)},
                            ["ds_subjectid"] = item.GetAttributeValue<EntityReference>("ds_subjectid"),
                            ["ds_theme"] = new EntityReference(item.LogicalName, item.Id),
                            ["ds_question"] = question,
                            ["ds_correctvar"] = correctvar,
                        };
                        _service.Create(entity);
                    }
                    else if (i >= 20)
                    {
                        var entity = new Entity("ds_testquestion")
                        {
                            ["ds_name"] = question,
                            ["ds_questionlevel"] = new OptionSetValue(717590002),
                            ["ds_questiondestination"] = new OptionSetValueCollection()
                                {new OptionSetValue(717590000), new OptionSetValue(717590001)},
                            ["ds_subjectid"] = item.GetAttributeValue<EntityReference>("ds_subjectid"),
                            ["ds_theme"] = new EntityReference(item.LogicalName, item.Id),
                            ["ds_question"] = question,
                            ["ds_correctvar"] = correctvar,
                        };
                        _service.Create(entity);
                    }
                }
            });
        }
    }
}