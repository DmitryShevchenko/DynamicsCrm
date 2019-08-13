using System;
using System.Activities;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;

/*namespace TestValidation
{
    struct Grades
    {
        public const double Easy = 4.4;
        public const double Middle = 5.0;
        public const double Hard = 6;
    }

    public class Class1 : CodeActivity
    {
        protected override void Execute(CodeActivityContext executionContext)
        {
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();

            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            

            if (context.InputParameters.Contains("Target") &&
                context.InputParameters["Target"] is Entity entity) // ds_testresult
            {
                if (entity.LogicalName == "ds_testresult" && entity.GetAttributeValue<bool>("ds_status"))
                {
                    var answerList = entity.Attributes.Where(x => x.Key.Contains("ds_answer"))
                        .OrderBy(x => ExtractNumber(x.Key)).Select(x => x.Value as string).ToList();
                    var test = entity?.GetAttributeValue<EntityReference>("ds_testid");
                    var testQuestionRef = service.Retrieve(test.LogicalName, test.Id, new ColumnSet(true))
                        .Attributes
                        .Where(x => x.Key.Contains("ds_question")).OrderBy(x => ExtractNumber(x.Key))
                        .Select(x => x.Value as EntityReference).ToList();


                    var questionAnswerDictionary = testQuestionRef.Zip(answerList, (k, v) => new {key = k, value = v})
                        .ToDictionary(x => x.key, x => x.value);

                    double resultScore = 0.0;

                    foreach (var item in questionAnswerDictionary)
                    {
                        var question = service
                            .Retrieve("ds_testquestion", item.Key.Id,
                                new ColumnSet("ds_correctvar", "ds_questionlevel"));

                        var answerVarId = Regex.Matches(item.Value, @"\d+", RegexOptions.Multiline).Cast<Match>()
                            .Select(m => int.Parse(m.Value)).Distinct()
                            .ToList();

                        var questionLevel =
                            question?.GetAttributeValue<OptionSetValue>("ds_questionlevel")
                                .Value; //ds_questionlevel Easy 717590000; Middle 717590001; Hard 717590002;
                        var questionCorrectVar = question?.GetAttributeValue<OptionSetValueCollection>("ds_correctvar")
                            .Select(x => x.Value).ToList();


                        if (!(answerVarId.Count > questionCorrectVar?.Count))
                        {
                            var numContains = NumContains(questionCorrectVar, answerVarId);

                            if (!(answerVarId.Count > numContains))
                            {
                                if (questionCorrectVar != null)
                                {
                                    switch (questionLevel)
                                    {
                                        case 717590000:
                                            resultScore += Percent(Grades.Easy,
                                                numContains / questionCorrectVar.Count * 100);
                                            break;
                                        case 717590001:
                                            resultScore += Percent(Grades.Middle,
                                                numContains / questionCorrectVar.Count * 100);
                                            break;
                                        case 717590002:
                                            resultScore += Percent(Grades.Hard,
                                                numContains / questionCorrectVar.Count * 100);
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static double Percent(double number, double percent)
        {
            //return ((double) 80         *       25)/100;
            return ((double) number * percent) / 100;
        }

        private static int NumContains<T>(List<T> listA, List<T> listB)
        {
            var containsTimes = 0;
            foreach (var t in listB)
            {
                foreach (var t1 in listA)
                {
                    if (t1.Equals(t))
                        containsTimes++;
                }
            }

            return containsTimes;
        }


        private static int ExtractNumber(string wordNumber)
        {
            return int.Parse(Regex.Match(wordNumber, "\\d+").ToString());
        }
    }
}*/

namespace TestValidation
{
    [DataContract]
    public class AnswersQuestion
    {
        [DataMember] public Guid QuestionId { get; set; }
        [DataMember] public List<int> AnswerCode { get; set; }
    }

    internal static class SerializerWrapper
    {
        public static T Deserialize<T>(string jsonString)
        {
            using (var deserializeStream = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                var writer = new System.IO.StreamWriter(deserializeStream);
                writer.Write(jsonString);
                writer.Flush();
                deserializeStream.Position = 0;
                var deserializedObject = (T) serializer.ReadObject(deserializeStream);
                return deserializedObject;
            }
        }
    }

    class TestParams
    {
        public double EasyQuestionCost { get; set; }
        public double MiddleQuestionCost { get; set; }
        public double HardQuestionCost { get; set; }
    }

    public class Class1 : CodeActivity
    {
        protected override void Execute(CodeActivityContext executionContext)
        {
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();

            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);


            if (context.InputParameters.Contains("Target") &&
                context.InputParameters["Target"] is Entity entity) // ds_testresult
            {
                if (entity.LogicalName == "ds_testresult" && entity.GetAttributeValue<bool>("ds_status"))
                {
                    var jsonAnswers = entity.GetAttributeValue<string>("ds_jsonanswers");

                    if (jsonAnswers != null)
                    {
                        var deserializedAnswers = SerializerWrapper.Deserialize<List<AnswersQuestion>>(jsonAnswers);

                        var questionCost = GetQuestionTypeCost(deserializedAnswers.Count);

                        double resultScore = 0.0;

                        foreach (var item in deserializedAnswers)
                        {
                            var question = service
                                .Retrieve("ds_testquestion", item.QuestionId,
                                    new ColumnSet("ds_correctvar", "ds_questionlevel"));

                            var questionLevel =
                                question?.GetAttributeValue<OptionSetValue>("ds_questionlevel")
                                    .Value;
                            var questionCorrectVar = question
                                ?.GetAttributeValue<OptionSetValueCollection>("ds_correctvar")
                                .Select(x => x.Value).ToList();


                            if (!(item.AnswerCode.Count > questionCorrectVar?.Count))
                            {
                                var numContains = NumContains(questionCorrectVar, item.AnswerCode);

                                if (!(item.AnswerCode.Count > numContains))
                                {
                                    if (questionCorrectVar != null)
                                    {
                                        switch (questionLevel)
                                        {
                                            case 717590000:
                                                resultScore += questionCost.EasyQuestionCost;
                                                break;
                                            case 717590001:
                                                resultScore += questionCost.MiddleQuestionCost;
                                                break;
                                            case 717590002:
                                                resultScore += questionCost.HardQuestionCost;
                                                break;
                                        }
                                    }
                                }
                            }
                        }

                        service.Update(new Entity(entity.LogicalName)
                        {
                            Id = entity.Id,
                            ["ds_resultscore"] = Convert.ToInt32(resultScore),
                        });
                    }
                }
            }
        }

        private static TestParams GetQuestionTypeCost(int x)
        {
            var easyQuestionCount = (int) Math.Round(x * 0.25);
            var middleQuestionCount = (int) Math.Round(x * 0.6);
            var hardQuestionCount = (int) Math.Round(x * 0.15);

            var easyQuestionCost = ((double) easyQuestionCount * 80 / (easyQuestionCount + middleQuestionCount)) /
                                   easyQuestionCount;
            var middleQuestionCost = ((double) middleQuestionCount * 80 / (easyQuestionCount + middleQuestionCount)) /
                                     middleQuestionCount;
            var hardQuestionCost = (double) 20 / hardQuestionCount;

            return new TestParams()
            {
                EasyQuestionCost = easyQuestionCost,
                MiddleQuestionCost = middleQuestionCost,
                HardQuestionCost = hardQuestionCost,
            };
        }

        private static int NumContains<T>(List<T> listA, List<T> listB)
        {
            var containsTimes = 0;
            foreach (var t in listB)
            {
                foreach (var t1 in listA)
                {
                    if (t1.Equals(t))
                        containsTimes++;
                }
            }

            return containsTimes;
        }
    }
}