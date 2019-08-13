using System;
using Microsoft.Xrm.Sdk;

namespace Simulations
{
    public class Education
    {
        public string Prop { get; set; }
        public string SubjectName { get; set; }
        public Guid Subjectid { get; set; }
        public string ThemeName { get; set; }
        public EntityReferenceCollection ThemeId { get; set; }
    }
}