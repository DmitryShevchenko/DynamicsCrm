using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Simulations
{
    [DataContract]
    public class AnswersQuestion
    {
        [DataMember] public Guid QuestionId { get; set; }
        [DataMember] public List<int> AnswerCode { get; set; }
    }
}