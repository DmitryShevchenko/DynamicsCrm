using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Extensions;
using Microsoft.Xrm.Sdk.Query;

/*

namespace TestGeneration
{
    public enum TestType
    {
        Other = 717590002,
        EntranceTest = 717590000,
        EducationModuleTest = 717590001,
        EducationCourseTest = 717590003,
    }

    public class TestParams
    {
        public int EasyQuestionCount { get; set; }
        public int MiddleQuestionCount { get; set; }
        public int HardQuestionCount { get; set; }

        public const int EasyCode = 717590000;
        public const int MiddleCode = 717590001;
        public const int HardCode = 717590002;
    }

    public class Class1 : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.Get<IPluginExecutionContext>();
            var service = serviceProvider.GetOrganizationService(context.UserId);

            try
            {
                if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity entity)
                {
                    switch (entity.LogicalName)
                    {
                        case "contact":
                            EntranceTestGeneration(service, entity);
                            break;
                        case "ds_educationcourse":
                            EducationCourseTestGeneration(service, entity);
                            break;
                    }
                }

                if (context.MessageName == "Associate" && context.InputParameters.Contains("Relationship"))
                {
                    if (context.InputParameters["Relationship"].ToString() !=
                        "ds_ds_educationcourse_ds_educationmodule")
                    {
                        if (context.InputParameters.Contains("RelatedEntities") &&
                            context.InputParameters["RelatedEntities"] is EntityReferenceCollection relatedEntities)
                        {
                            EducationCourseTestGeneration(service,
                                new Entity(relatedEntities[0].LogicalName, relatedEntities[0].Id));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new InvalidPluginExecutionException(e.Message);
            }
        }


        private void EntranceTestGeneration(IOrganizationService service, Entity entity)
        {
            if (entity.Attributes.ContainsKey("ds_contacttype") &&
                entity.GetAttributeValue<OptionSetValue>("ds_contacttype").Value == 717590000)
            {
                var groups = service.RetrieveMultiple(new QueryExpression("ds_universitygroup")
                {
                    ColumnSet = new ColumnSet(true)
                });

                var groupsId = new List<Guid>();

                if (groups.Entities.All(x =>
                    x.Attributes.Contains("ds_educationyearstart") && x.Attributes.Contains("ds_educationyearend")))
                {
                    groupsId = groups.Entities.Where(x => IsInRange(DateTime.Now,
                        x.GetAttributeValue<DateTime>("ds_educationyearstart"),
                        x.GetAttributeValue<DateTime>("ds_educationyearstart").AddYears(1))).Select(x => x.Id).ToList();
                }

                ///
                var groupsWhereContact = service.RetrieveMultiple(new QueryExpression("ds_ds_universitygroup_contact")
                {
                    ColumnSet = new ColumnSet(true),
                    Criteria = new FilterExpression()
                    {
                        Conditions =
                        {
                            new ConditionExpression("contactid", ConditionOperator.Equal, entity.Id),
                            new ConditionExpression("ds_universitygroupid", ConditionOperator.In, groupsId)
                        }
                    }
                }).Entities.Select(x => x.GetAttributeValue<Guid>("ds_universitygroupid")).ToList();

                var uepmIdList = service.RetrieveMultiple(new QueryExpression("ds_universityeducationprogrammanager")
                {
                    ColumnSet = new ColumnSet(true),
                    Criteria = new FilterExpression()
                    {
                        Conditions =
                            {new ConditionExpression("ds_universitygroupid", ConditionOperator.In, groupsWhereContact)}
                    }
                }).Entities.Select(x => x.Id).ToList();

                var themeIdList = new List<Guid>();

                service.RetrieveMultiple(
                    new QueryExpression("ds_ds_universityeducationprogrammanager_ds")
                    {
                        ColumnSet = new ColumnSet(true),
                        Criteria =
                        {
                            Conditions =
                            {
                                new ConditionExpression("ds_universityeducationprogrammanagerid",
                                    ConditionOperator.In, uepmIdList)
                            }
                        }
                    }).Entities.ToList().ForEach(item =>
                {
                    if (item.Attributes.Contains("ds_themeid"))
                        themeIdList.Add(item.GetAttributeValue<Guid>("ds_themeid"));
                });

                TestGeneration(service, TestType.EntranceTest, themeIdList, 20, out var testEntityId);

                var testResult = new Entity("ds_testresult")
                {
                    ["ds_name"] = string.Join(" ", "Date:", DateTime.Now, "Contact_id:", entity.Id, "Test_id:",
                        testEntityId),
                    ["ds_contactid"] = new EntityReference(entity.LogicalName, entity.Id),
                    ["ds_testid"] = new EntityReference("ds_test", testEntityId),
                };
                service.Create(testResult);
            }
        }

        private void EducationCourseTestGeneration(IOrganizationService service, Entity entity)
        {
            var educationCourseid = entity.Id;

            var customEducationModulesId = service.RetrieveMultiple(new QueryExpression("ds_educationmodule")
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                    {
                        new ConditionExpression("ds_moduletype", ConditionOperator.Equal, 717590001),
                        new ConditionExpression("ds_educationcourseid", ConditionOperator.Equal, educationCourseid),
                    }
                }
            }).Entities.Select(x => x.Id).ToList();

            var defaultEducationModulesId = service.RetrieveMultiple(
                new QueryExpression("ds_ds_educationcourse_ds_educationmodule")
                {
                    ColumnSet = new ColumnSet(true),
                    Criteria = new FilterExpression()
                    {
                        Conditions =
                        {
                            new ConditionExpression("ds_educationcourseid", ConditionOperator.Equal, educationCourseid),
                        }
                    }
                }).Entities.Select(x => x.GetAttributeValue<Guid>("ds_educationmoduleid")).ToList();

            var allModulesList = customEducationModulesId.Concat(defaultEducationModulesId).ToList();

            var themeList = service.RetrieveMultiple(new QueryExpression("ds_ds_educationmodule_ds_theme")
            {
                ColumnSet = new ColumnSet(true),
            });

            allModulesList.ForEach(id =>
            {
                var themeListId = themeList.Entities.Where(x => x.GetAttributeValue<Guid>("ds_educationmoduleid") == id)
                    .Select(x => x.GetAttributeValue<Guid>("ds_themeid")).ToList();
                //themeListId = service.RetrieveMultiple(new QueryExpression("ds_ds_educationmodule_ds_theme")
                //{
                //    ColumnSet = new ColumnSet(true),
                //    Criteria = new FilterExpression(LogicalOperator.Or)
                //    {
                //        Conditions =
                //        {
                //            new ConditionExpression("ds_educationmoduleid", ConditionOperator.Equal, id),
                //        }
                //    }
                //}).Entities.Select(x => x.GetAttributeValue<Guid>("ds_themeid")).ToList();

                if (themeListId.Count > 0)
                {
                    var testQuestions = service.RetrieveMultiple(new QueryExpression("ds_testquestion")
                    {
                        Criteria = new FilterExpression()
                        {
                            Conditions =
                            {
                                new ConditionExpression("ds_theme", ConditionOperator.In, themeListId)
                            }
                        }
                    });

                    TestGeneration(service, TestType.EducationModuleTest, themeListId, 20, out var testEntityId);
                    var testResult = new Entity("ds_testresult")
                    {
                        ["ds_name"] = string.Join(" ", "Date:", DateTime.Now, "EducationCourse_id:", educationCourseid,
                            "Test_id:", testEntityId),
                        ["ds_educationcourseid"] = new EntityReference("ds_educationcourse", educationCourseid),
                        ["ds_educationmoduleid"] = new EntityReference("ds_educationmodule", id),
                        ["ds_testid"] = new EntityReference("ds_test", testEntityId),
                    };
                    service.Create(testResult);
                }
            });

            TestGeneration(service, TestType.EducationCourseTest, themeList.Entities.Select(x => x.Id).ToList(), 50,
                out var tEntityId);
            var tResult = new Entity("ds_testresult")
            {
                ["ds_name"] = string.Join(" ", "Date:", DateTime.Now, "EducationCourse_id:", educationCourseid,
                    "Test_id:", tEntityId),
                ["ds_educationcourseid"] = new EntityReference("ds_educationcourse", educationCourseid),
                ["ds_testid"] = new EntityReference("ds_test", tEntityId),
            };
            service.Create(tResult);
        }


        private void TestGeneration(IOrganizationService service, TestType type, List<Guid> themeIdList,
            int questionCount, out Guid testEntityId)
        {
            var questionsList = service.RetrieveMultiple(new QueryExpression("ds_testquestion")
            {
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                    {
                        new ConditionExpression("ds_theme", ConditionOperator.In, themeIdList),
                        new ConditionExpression("ds_questiondestination", ConditionOperator.ContainValues,
                            (int) type)
                    }
                },
                ColumnSet = new ColumnSet() {Columns = {"ds_questionlevel"}}
            });

            var newTest = new Entity("ds_test")
            {
                ["ds_testdestination"] = new OptionSetValue((int) type), //Entrance test
                ["ds_creationtype"] = new OptionSetValue(717590000), //Auto
            };

            testEntityId = service.Create(newTest);

            service.Associate("ds_test", testEntityId, new Relationship("ds_ds_test_ds_testquestion"),
                GetRandomCollection(questionsList, GetQuestionTypeCount(questionCount)));

            var newTestUpdeteName = new Entity("ds_test", testEntityId)
            {
                ["ds_name"] = string.Join("", "Test_Id {" + testEntityId + "}, Test_Destination_Id {" + type + "}"),
            };
            service.Update(newTestUpdeteName);
        }

        private EntityReferenceCollection GetRandomCollection(EntityCollection questionsList, TestParams testParams)
        {
            var questionTypeList = questionsList.Entities
                .Select(x => x.GetAttributeValue<OptionSetValue>("ds_questionlevel").Value)
                .ToList();

            if (NumContains(questionTypeList, 717590000) < testParams.EasyQuestionCount ||
                NumContains(questionTypeList, 717590001) < testParams.MiddleQuestionCount ||
                NumContains(questionTypeList, 717590002) <
                testParams.HardQuestionCount)
            {
                throw new Exception("Not enough questions to make a test");
            }

            var testPropertyInfos = testParams.GetType().GetProperties().Select(x => x.GetValue(testParams)).ToList();
            var testFieldsInfos = testParams.GetType().GetFields().Select(x => x.GetValue(testParams)).ToList();
            var testDictionary = testFieldsInfos.Zip(testPropertyInfos, (k, v) => new {k, v})
                .ToDictionary(x => x.k, x => x.v);

            var entityReferenceCollection = new EntityReferenceCollection();

            foreach (var item in testDictionary)
            {
                var entities = questionsList.Entities
                    .Where(x => x.Attributes.Values.Contains(new OptionSetValue((int) item.Key))).ToList();

                var random = new List<int>();

                for (var i = 0; i < (int) item.Value; i++)
                {
                    int rand;
                    do
                    {
                        rand = new Random().Next(entities.Count);
                    } while (random.Contains(rand));

                    entityReferenceCollection.Add(new EntityReference(entities[rand].LogicalName, entities[rand].Id));
                    random.Add(rand);
                }
            }

            return entityReferenceCollection;
        }

        public static bool IsInRange(DateTime dateToCheck, DateTime startDate, DateTime endDate)
        {
            return dateToCheck >= startDate && dateToCheck < endDate;
        }

        private TestParams GetQuestionTypeCount(int x)
        {
            var easyQuestionCount = (int) Math.Round(x * 0.25);
            var middleQuestionCount = (int) Math.Round(x * 0.6);
            var hardQuestionCount = (int) Math.Round(x * 0.15);

            return new TestParams()
            {
                EasyQuestionCount = easyQuestionCount,
                MiddleQuestionCount = middleQuestionCount,
                HardQuestionCount = hardQuestionCount,
            };
        }

        private int NumContains<T>(List<T> listA, T value)
        {
            var containsTimes = 0;
            foreach (var t in listA)
            {
                if (t.Equals(value))
                    containsTimes++;
            }

            return containsTimes;
        }
    }*/


namespace TestGeneration
{
    public enum TestType
    {
        Other = 717590002,
        EntranceTest = 717590000,
        EducationModuleTest = 717590001,
        EducationCourseTest = 717590003,
    }

    public class TestParams
    {
        public int EasyQuestionCount { get; set; }
        public int MiddleQuestionCount { get; set; }
        public int HardQuestionCount { get; set; }

        public const int EasyCode = 717590000;
        public const int MiddleCode = 717590001;
        public const int HardCode = 717590002;
    }

    public class Class1 : IPlugin
    {
        public int EntranceTestQuestionCount { get; set; }
        public int EducationModuleTestQuestionCount { get; set; }
        public int EducationCourseTestQuestionCount { get; set; }


        private void GetQuestionSettings(IOrganizationService service)
        {
            var questionSettings = service.Retrieve("ds_testsettings",
                Guid.Parse("f53f6233-0b86-e911-a81e-000d3aba3dc1"), new ColumnSet(true));

            EntranceTestQuestionCount = questionSettings.GetAttributeValue<int>("ds_entrancetestquestioncount");
            EducationModuleTestQuestionCount =
                questionSettings.GetAttributeValue<int>("ds_educationmoduletestquestioncount");
            EducationCourseTestQuestionCount =
                questionSettings.GetAttributeValue<int>("ds_educationcoursetestquestioncount");
        }


        public void Execute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.Get<IPluginExecutionContext>();
            var service = serviceProvider.GetOrganizationService(context.UserId);

            try
            {
                if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity entity)
                {
                    switch (entity.LogicalName)
                    {
                        case "contact":
                            if (context.MessageName == "Update" && entity.Attributes.ContainsKey("ds_entrancetestgenerationtrigger") && entity.GetAttributeValue<bool>("ds_entrancetestgenerationtrigger"))
                            {
                                EntranceTestGeneration(service, service.Retrieve(entity.LogicalName, entity.Id, new ColumnSet(true)));
                            }
                            break;
                        case "ds_educationcourse":
                            if (entity.GetAttributeValue<bool>("ds_donecode"))
                            {
                                GetQuestionSettings(service);
                                EducationCourseTestGeneration(service, entity);
                            }
                            break;
                    }
                }

                if (context.MessageName == "Associate" && context.InputParameters.Contains("Relationship"))
                {
                    if (context.InputParameters["Relationship"].ToString() !=
                        "ds_ds_educationcourse_ds_educationmodule")
                    {
                        if (context.InputParameters.Contains("RelatedEntities") &&
                            context.InputParameters["RelatedEntities"] is EntityReferenceCollection relatedEntities)
                        {
                            GetQuestionSettings(service);
                            EducationCourseTestGeneration(service,
                                new Entity(relatedEntities[0].LogicalName, relatedEntities[0].Id));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new InvalidPluginExecutionException(e.Message);
            }
        }


        private void EntranceTestGeneration(IOrganizationService service, Entity entity)
        {
            GetQuestionSettings(service);
            
            if (entity.Attributes.ContainsKey("ds_contacttype") &&
                entity.GetAttributeValue<OptionSetValue>("ds_contacttype").Value == 717590000)
            {
                var themeIdList = service.RetrieveMultiple(
                    new QueryExpression("ds_ds_universityeducationprogrammanager_ds")
                    {
                        ColumnSet = new ColumnSet(true),
                        Criteria =
                        {
                            Conditions =
                            {
                                new ConditionExpression("ds_universityeducationprogrammanagerid",
                                    ConditionOperator.In, service.RetrieveMultiple(
                                        new QueryExpression("ds_universityeducationprogrammanager")
                                        {
                                            Criteria = new FilterExpression()
                                            {
                                                Conditions =
                                                {
                                                    new ConditionExpression("ds_universitygroupid",
                                                        ConditionOperator.In,
                                                        service.RetrieveMultiple(
                                                            new QueryExpression("ds_universitygroup")
                                                            {
                                                                Criteria = new FilterExpression(LogicalOperator.And)
                                                                {
                                                                    Conditions =
                                                                    {
                                                                        new ConditionExpression("ds_universitygroupid",
                                                                            ConditionOperator.In,
                                                                            service.RetrieveMultiple(
                                                                                new QueryExpression(
                                                                                    "ds_ds_universitygroup_contact")
                                                                                {
                                                                                    ColumnSet = new ColumnSet(true),
                                                                                    Criteria = new FilterExpression()
                                                                                    {
                                                                                        Conditions =
                                                                                        {
                                                                                            new ConditionExpression(
                                                                                                "contactid",
                                                                                                ConditionOperator.Equal,
                                                                                                entity.Id),
                                                                                        }
                                                                                    }
                                                                                }).Entities.Select(x =>
                                                                                x.GetAttributeValue<Guid>(
                                                                                    "ds_universitygroupid")).ToList()),

                                                                        new ConditionExpression(
                                                                            "ds_universityspecialityid",
                                                                            ConditionOperator.Equal,
                                                                            entity.GetAttributeValue<EntityReference>(
                                                                                "ds_uspecialityid").Id),
                                                                    }
                                                                }
                                                            }).Entities.Select(x => x.Id).ToList())
                                                }
                                            }
                                        }).Entities.Select(x => x.Id).ToList())
                            }
                        }
                    }).Entities.Select(x => x.GetAttributeValue<Guid>("ds_themeid")).ToList();

                TestGeneration(service, TestType.EntranceTest, themeIdList, EntranceTestQuestionCount,
                    out var testEntityId);

                var testResult = new Entity("ds_testresult")
                {
                    ["ds_name"] = string.Join(" ", "Date:", DateTime.Now, "Contact_id:", entity.Id, "Test_id:",
                        testEntityId),
                    ["ds_contactid"] = new EntityReference(entity.LogicalName, entity.Id),
                    ["ds_testid"] = new EntityReference("ds_test", testEntityId),
                };
                service.Create(testResult);
            }
        }

        private void EducationCourseTestGeneration(IOrganizationService service, Entity entity)
        {
            //"ds_moduletype", 717590001 -- Custom 
            //"ds_moduletype", 717590000 -- Default
            var allModulesList = service.RetrieveMultiple(new QueryExpression("ds_educationmodule")
                {
                    ColumnSet = new ColumnSet(true),
                    Criteria = new FilterExpression()
                    {
                        Conditions =
                        {
                            new ConditionExpression("ds_moduletype", ConditionOperator.Equal, 717590001),
                            new ConditionExpression("ds_educationcourseid", ConditionOperator.Equal, entity.Id),
                        }
                    }
                }).Entities.Select(x => x.Id)
                .Concat(service.RetrieveMultiple(new QueryExpression("ds_educationmodule")
                {
                    ColumnSet = new ColumnSet(true),
                    Criteria = new FilterExpression(LogicalOperator.And)
                    {
                        Conditions =
                        {
                            new ConditionExpression("ds_moduletype", ConditionOperator.Equal, 717590000),
                            new ConditionExpression("ds_educationmoduleid", ConditionOperator.In, service
                                .RetrieveMultiple(
                                    new QueryExpression("ds_ds_educationcourse_ds_educationmodule")
                                    {
                                        ColumnSet = new ColumnSet(true),
                                        Criteria = new FilterExpression()
                                        {
                                            Conditions =
                                            {
                                                new ConditionExpression("ds_educationcourseid", ConditionOperator.Equal,
                                                    entity.Id),
                                            }
                                        }
                                    }).Entities.Select(x => x.GetAttributeValue<Guid>("ds_educationmoduleid")).ToList())
                        }
                    }
                }).Entities.Select(x => x.Id)).ToList();


            var themeList = service.RetrieveMultiple(new QueryExpression("ds_ds_educationmodule_ds_theme")
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression()
                {
                    Conditions =
                        {new ConditionExpression("ds_educationmoduleid", ConditionOperator.In, allModulesList)}
                }
            });

            allModulesList.ForEach(id =>
            {
                var themeListId = themeList.Entities.Where(x => x.GetAttributeValue<Guid>("ds_educationmoduleid") == id)
                    .Select(x => x.GetAttributeValue<Guid>("ds_themeid")).Distinct().ToList();


                if (themeListId.Count > 0)
                {
                    TestGeneration(service, TestType.EducationModuleTest, themeListId, EducationModuleTestQuestionCount,
                        out var testEntityId);
                    var testResult = new Entity("ds_testresult")
                    {
                        ["ds_name"] = string.Join(" ", "Date:", DateTime.Now, "EducationCourse_id:", entity.Id,
                            "Test_id:", testEntityId),
                        ["ds_educationcourseid"] = new EntityReference("ds_educationcourse", entity.Id),
                        ["ds_educationmoduleid"] = new EntityReference("ds_educationmodule", id),
                        ["ds_testid"] = new EntityReference("ds_test", testEntityId),
                    };
                    service.Create(testResult);
                }
            });

            TestGeneration(service, TestType.EducationModuleTest,
                themeList.Entities.Select(x => x.GetAttributeValue<Guid>("ds_themeid")).Distinct().ToList(),
                EducationCourseTestQuestionCount, out var tEntityId);
            
            //КОСТИЛЬ бо нет вопросов с типом  EducationCourseTest -- 717590003
            var testApdeteCostil = new Entity("ds_test")
            {
                Id = tEntityId,
                ["ds_testdestination"] = new OptionSetValue(717590003)
            };
            service.Update(testApdeteCostil);
            //
            
            var tResult = new Entity("ds_testresult")
            {
                ["ds_name"] = string.Join(" ", "Date:", DateTime.Now, "EducationCourse_id:", entity.Id, "Test_id:",
                    tEntityId),
                ["ds_educationcourseid"] = new EntityReference("ds_educationcourse", entity.Id),
                ["ds_testid"] = new EntityReference("ds_test", tEntityId),
            };
            service.Create(tResult);
        }


        private void TestGeneration(IOrganizationService service, TestType type, List<Guid> themeIdList,
            int questionCount, out Guid testEntityId)
        {
            var questionsList = service.RetrieveMultiple(new QueryExpression("ds_testquestion")
            {
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                    {
                        new ConditionExpression("ds_theme", ConditionOperator.In, themeIdList),
                        new ConditionExpression("ds_questiondestination", ConditionOperator.ContainValues,
                            (int) type)
                    }
                },
                ColumnSet = new ColumnSet() {Columns = {"ds_questionlevel"}}
            });

            if (questionsList.Entities.Count == 0)
            {
                throw new Exception("No questions to make a test");
            }

            var newTest = new Entity("ds_test")
            {
                ["ds_testdestination"] = new OptionSetValue((int) type), //Entrance test
                ["ds_creationtype"] = new OptionSetValue(717590000), //Auto
            };

            testEntityId = service.Create(newTest);

            service.Associate("ds_test", testEntityId, new Relationship("ds_ds_test_ds_testquestion"),
                GetRandomCollection(questionsList, GetQuestionTypeCount(questionCount)));

            var newTestUpdeteName = new Entity("ds_test", testEntityId)
            {
                ["ds_name"] = string.Join("", "Test_Id {" + testEntityId + "}, Test_Destination_Id {" + type + "}"),
            };
            service.Update(newTestUpdeteName);
        }

        private EntityReferenceCollection GetRandomCollection(EntityCollection questionsList, TestParams testParams)
        {
            var questionLevelList = questionsList.Entities
                .Select(x => x.GetAttributeValue<OptionSetValue>("ds_questionlevel").Value)
                .ToList();

            if (NumContains(questionLevelList, 717590000) < testParams.EasyQuestionCount ||
                NumContains(questionLevelList, 717590001) < testParams.MiddleQuestionCount ||
                NumContains(questionLevelList, 717590002) <
                testParams.HardQuestionCount)
            {
                throw new Exception("Not enough questions to make a test");
            }

            var testPropertyInfos = testParams.GetType().GetProperties().Select(x => x.GetValue(testParams)).ToList();
            var testFieldsInfos = testParams.GetType().GetFields().Select(x => x.GetValue(testParams)).ToList();
            var testDictionary = testFieldsInfos.Zip(testPropertyInfos, (k, v) => new {k, v})
                .ToDictionary(x => x.k, x => x.v);

            var entityReferenceCollection = new EntityReferenceCollection();

            foreach (var item in testDictionary)
            {
                var entities = questionsList.Entities
                    .Where(x => x.Attributes.Values.Contains(new OptionSetValue((int) item.Key))).ToList();

                var random = new List<int>();

                for (var i = 0; i < (int) item.Value; i++)
                {
                    int rand;
                    do
                    {
                        rand = new Random().Next(entities.Count);
                    } while (random.Contains(rand));

                    entityReferenceCollection.Add(new EntityReference(entities[rand].LogicalName, entities[rand].Id));
                    random.Add(rand);
                }
            }

            return entityReferenceCollection;
        }

        public static bool IsInRange(DateTime dateToCheck, DateTime startDate, DateTime endDate)
        {
            return dateToCheck >= startDate && dateToCheck < endDate;
        }

        private TestParams GetQuestionTypeCount(int x)
        {
            var easyQuestionCount = (int) Math.Round(x * 0.25);
            var middleQuestionCount = (int) Math.Round(x * 0.6);
            var hardQuestionCount = (int) Math.Round(x * 0.15);

            return new TestParams()
            {
                EasyQuestionCount = easyQuestionCount,
                MiddleQuestionCount = middleQuestionCount,
                HardQuestionCount = hardQuestionCount,
            };
        }

        private int NumContains<T>(List<T> listA, T value)
        {
            var containsTimes = 0;
            foreach (var t in listA)
            {
                if (t.Equals(value))
                    containsTimes++;
            }

            return containsTimes;
        }
    }
}