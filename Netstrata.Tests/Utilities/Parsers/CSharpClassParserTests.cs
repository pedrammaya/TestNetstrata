using Netstrata.Utilities.Parsers;

namespace Netstrata.Tests.Utilities.Parsers
{
    public class CSharpClassParserTests
    {
        #region ParseClasses Tests

        [Fact]
        public void ParseClasses_SimpleClass_ReturnsCorrectProperties()
        {
            string input = @"
public class PersonDto
{
    public string Name { get; set; }
    public int Age { get; set; }
}";
            var result = CSharpClassParser.ParseClasses(input);

            Assert.NotNull(result);
            Assert.Equal("PersonDto", result.Name);
            Assert.Equal(2, result.Properties.Count);
            Assert.Equal("Name", result.Properties[0].Name);
            Assert.Equal("string", result.Properties[0].Type);
            Assert.Equal("Age", result.Properties[1].Name);
            Assert.Equal("int", result.Properties[1].Type);
        }

        [Fact]
        public void ParseClasses_ClassWithNested_ReturnsNestedClasses()
        {
            string input = @"
public class PersonDto
{
    public string Name { get; set; }

    public class Address
    {
        public string Street { get; set; }
    }
}";
            var result = CSharpClassParser.ParseClasses(input);

            Assert.NotNull(result);
            Assert.Single(result.NestedClasses);
            Assert.Equal("Address", result.NestedClasses[0].Name);
        }

        [Fact]
        public void ParseClasses_NoClass_ThrowsFormatException()
        {
            string input = @"// No class here";

            Assert.Throws<FormatException>(() =>
            {
                CSharpClassParser.ParseClasses(input);
            });
        }

        #endregion

        #region ParseProperties (tested indirectly via ParseClasses)

        [Fact]
        public void ParseProperties_ListAndNullableTypes_ParsedCorrectly()
        {
            string input = @"
public class Sample
{
    public List<int> Numbers { get; set; }
    public Nullable<long> OptionalValue { get; set; }
}";
            var result = CSharpClassParser.ParseClasses(input);

            Assert.NotNull(result);
            var numbersProp = result.Properties.Find(p => p.Name == "Numbers");
            var optionalProp = result.Properties.Find(p => p.Name == "OptionalValue");

            Assert.NotNull(numbersProp);
            Assert.Equal("List<int>", numbersProp.Type);
            Assert.False(numbersProp.IsNullable);

            Assert.NotNull(optionalProp);
            Assert.Equal("long", optionalProp.Type);
            Assert.True(optionalProp.IsNullable);
        }

        [Fact]
        public void ParseProperties_HandlesTrailingQuestionMark()
        {
            string input = @"
public class Sample
{
    public long? Id { get; set; }
}";
            var result = CSharpClassParser.ParseClasses(input);

            var prop = result.Properties[0];
            Assert.Equal("Id", prop.Name);
            Assert.Equal("long", prop.Type);
            Assert.True(prop.IsNullable);
        }

        [Fact]
        public void ParseProperties_HandlesNullableGeneric()
        {
            string input = @"
public class Sample
{
    public Nullable<int> Count { get; set; }
}";
            var result = CSharpClassParser.ParseClasses(input);

            var prop = result.Properties[0];
            Assert.Equal("Count", prop.Name);
            Assert.Equal("int", prop.Type);
            Assert.True(prop.IsNullable);
        }

        #endregion

        #region ParseNestedClasses (tested indirectly)

        [Fact]
        public void ParseNestedClasses_MultipleLevels_ReturnsCorrectHierarchy()
        {
            string input = @"
public class Parent
{
    public string Name { get; set; }

    public class Child
    {
        public int Age { get; set; }

        public class GrandChild
        {
            public string Info { get; set; }
        }
    }
}";
            var result = CSharpClassParser.ParseClasses(input);

            Assert.NotNull(result);
            Assert.Single(result.NestedClasses);
            var child = result.NestedClasses[0];
            Assert.Single(child.NestedClasses);
            var grandChild = child.NestedClasses[0];
            Assert.Single(grandChild.Properties);
            Assert.Equal("Info", grandChild.Properties[0].Name);
        }

        [Fact]
        public void ParseNestedClasses_SiblingNestedClasses_ReturnsAll()
        {
            string input = @"
public class Parent
{
    public class ChildA { public int A { get; set; } }
    public class ChildB { public int B { get; set; } }
}";
            var result = CSharpClassParser.ParseClasses(input);

            Assert.Equal(2, result.NestedClasses.Count);
            Assert.Contains(result.NestedClasses, c => c.Name == "ChildA");
            Assert.Contains(result.NestedClasses, c => c.Name == "ChildB");
        }

        #endregion

        #region FindMatchingBrace (tested indirectly)

        [Fact]
        public void FindMatchingBrace_CorrectlyMatchesBraces()
        {
            string input = @"public class Sample { public int X { get; set; } }";
            var result = CSharpClassParser.ParseClasses(input);
            Assert.Equal("Sample", result.Name);
        }

        [Fact]
        public void FindMatchingBrace_HandlesNestedBraces()
        {
            string input = @"
public class Sample
{
    public class Nested
    {
        public int X { get; set; }
    }
}";
            var result = CSharpClassParser.ParseClasses(input);
            Assert.Equal(1, result.NestedClasses.Count);
            Assert.Equal("Nested", result.NestedClasses[0].Name);
        }

        #endregion
    }
}
