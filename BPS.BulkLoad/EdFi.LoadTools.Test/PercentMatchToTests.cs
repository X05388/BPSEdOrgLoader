using System;
using System.Collections.Generic;
using System.Linq;
using EdFi.LoadTools.Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EdFi.LoadTools.Test
{
    [TestClass]
    public class PercentMatchToTests
    {
        private class Map
        {
            public string X;
            public string J;
            public double M;
        }

        private static Map[] CreateMappings(IEnumerable<string> xml, IEnumerable<string> json)
        {
            return xml.SelectMany(x => json.Select(j => new Map { X = x, J = j, M = x.PercentMatchTo(j) }))
                .OrderByDescending(m => m.M).ToArray();
        }

        private static IEnumerable<Map> CompressMappings(IEnumerable<Map> mappings)
        {
            var maps = mappings.Where(m => m.M > 0).ToList();
            while (maps.Count > 0)
            {
                var map = maps.First();
                yield return map;
                maps.RemoveAll(m => m.X == map.X || m.J == map.J);
            }
        }

        private static void WriteMaps(string description, IEnumerable<Map> maps)
        {
            Console.WriteLine($"---{description}---");
            foreach (var map in maps)
            {
                Console.WriteLine($"\t{map.M}\r\t\t{map.X}\r\t\t{map.J}");
            }
        }

        private static void PerformTest(IReadOnlyList<string> xml, IReadOnlyList<string> json)
        {
            var mappings = CreateMappings(xml, json);
            WriteMaps("Unmatched", mappings);

            // it is important that all the json properties are mapped
            var maps = CompressMappings(mappings).ToList();
            WriteMaps("Matched", maps);
            for (var i = 0; i < json.Count; i++)
            {
                var map = maps.SingleOrDefault(m => m.X == xml[i] && m.J == json[i]);
                Assert.IsNotNull(map);
                Assert.IsTrue(map.M > 0);
            }
        }

        [TestMethod]
        public void Descriptor_Values()
        {
            string[] xml = { "AcademicSubjectMap", "CodeValue", "Description", "EffectiveBeginDate", "EffectiveEndDate", "Namespace", "ShortDescription" };
            string[] json = { "academicSubjectType", "codeValue", "description", "effectiveBeginDate", "effectiveEndDate", "namespace", "shortDescription" };
            PerformTest(xml, json);
        }

        [TestMethod]
        public void AssessmentFamilyTitle_to_title()
        {
            string[] xml = { "AssessmentTitle", "AssessmentFamilyReference/AssessmentFamilyIdentity/AssessmentFamilyTitle" };
            string[] json = { "title", "assessmentFamilyReference/title" };
            PerformTest(xml, json);
        }

        [TestMethod]
        public void CohortScope_to_scopeType()
        {
            string[] xml = { "CohortScope", "scopeType" };
            string[] json = { "CohortType", "type" };
            PerformTest(xml, json);
        }

        [TestMethod]
        public void AssessmentReference_to_AssessmentItemReference()
        {
            string[] xml = { "AssessmentItemReference/AssessmentReference/AcademicSubject",
                "AssessmentReference/AcademicSubject" };
            string[] json = { "assessmentItems/assessmentItemReference/academicSubjectDescriptor", "assessmentReference/academicSubjectDescriptor" };
            PerformTest(xml, json);
        }

        [TestMethod]
        public void GradingPeriodGradingPeriodDescriptor_to_GradeGradingPeriodDescriptor()
        {
            string[] xml = { "GradeReference/GradeIdentity/GradingPeriodReference/GradingPeriodIdentity/GradingPeriod",
                "GradingPeriodReference/GradingPeriodIdentity/GradingPeriod" };
            string[] json = { "grades/gradeReference/gradingPeriodDescriptor", "gradingPeriodReference/descriptor" };
            PerformTest(xml, json);
        }

        [TestMethod]
        public void AssessmentReferenceAssessmentTitle_to_AssessmentItemAssessmentTitle()
        {
            string[] xml =
                {
                    "AssessmentReference/AssessmentTitle",
                    "StudentObjectiveAssessment/ObjectiveAssessmentReference/AssessmentReference/AssessmentTitle",
                    "StudentAssessmentItem/AssessmentItemReference/AssessmentReference/AssessmentTitle"
                };
            string[] json =
            {
                "assessmentReference/title",
                "studentObjectiveAssessments/objectiveAssessmentReference/assessmentTitle",
                "items/assessmentItemReference/assessmentTitle"
            };
            PerformTest(xml, json);
        }

        [TestMethod]
        public void KeyConsolidation()
        {
            string[] xml = {
                "StudentSectionAssociationReference/StudentSectionAssociationIdentity/SectionReference/SectionIdentity/CourseOfferingReference/CourseOfferingIdentity/SessionReference/SessionIdentity/SchoolYear",
                "StudentSectionAssociationReference/StudentSectionAssociationIdentity/SectionReference/SectionIdentity/ClassPeriodReference/ClassPeriodIdentity/SchoolReference/SchoolIdentity/SchoolId",
                "StudentSectionAssociationReference/StudentSectionAssociationIdentity/SectionReference/SectionIdentity/CourseOfferingReference/CourseOfferingIdentity/SessionReference/SessionIdentity/SchoolReference/SchoolIdentity/SchoolId",
                "StudentSectionAssociationReference/StudentSectionAssociationIdentity/SectionReference/SectionIdentity/CourseOfferingReference/CourseOfferingIdentity/SchoolReference/SchoolIdentity/SchoolId"
            };
            string[] json = { "studentSectionAssociationReference/schoolYear" };
            PerformTest(xml, json);
        }

        [TestMethod]
        public void CalendarDateReference()
        {
            string[] xml =
            {
                "CalendarDateReference/CalendarDateIdentity/SchoolReference/SchoolIdentity/SchoolId",
                "SchoolReference/SchoolIdentity/SchoolId",
            };
            string[] json = { "calendarDateReference/schoolId" };
            PerformTest(xml, json);
        }

        [TestMethod]
        public void URI_to_uri()
        {
            string[] xml = { "URI" };
            string[] json = { "uri" };
            PerformTest(xml, json);
        }
    }
}

