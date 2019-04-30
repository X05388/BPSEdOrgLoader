using EdFi.LoadTools.Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EdFi.LoadTools.Test
{
    public class ExtensionMethodsTests
    {
        [TestClass]
        public class InitialUpperCase
        {
            const string LowerCase = "test";
            const string UpperCase = "Test";

            [TestMethod]
            public void Should_capitolize_initial_lower_case()
            {
                Assert.AreEqual(LowerCase.InitialUpperCase(), UpperCase);
            }

            [TestMethod]
            public void Should_not_change_initial_upper_case()
            {
                Assert.AreEqual(UpperCase.InitialUpperCase(), UpperCase);
            }
        }

        [TestClass]
        public class AreMatchingSimpleTypes
        {
            [TestMethod]
            public void Should_match_overlapping_types()
            {
                Assert.IsTrue(ExtensionMethods.AreMatchingSimpleTypes("string", "String"));
                Assert.IsTrue(ExtensionMethods.AreMatchingSimpleTypes("string", "Token"));
            }

            [TestMethod]
            public void Should_identify_unmatched_types()
            {
                Assert.IsFalse(ExtensionMethods.AreMatchingSimpleTypes("string", "Int"));
            }

            [TestMethod]
            public void Should_not_match_simple_to_complex_types()
            {
                Assert.IsFalse(ExtensionMethods.AreMatchingSimpleTypes("string", "ComplexType"));
            }
        }
    }
}
