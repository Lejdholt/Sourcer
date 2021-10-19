using System.Text.Json;
using System.Text.Json.Serialization;
using AutoFixture.Xunit2;
using Xunit;
using Xunit.Abstractions;

namespace Sourcer.Tests
{
    public class Class1
    {
        private readonly ITestOutputHelper helper;

        public Class1(ITestOutputHelper helper)
        {
            this.helper = helper;
        }

        [Fact]
        public void FactMethodName2()
        {
            var t1 = new TestData("Bill", null);
            var t2 = new TestData("Bill", "Hus");

            var d1 = JsonSerializer.SerializeToDocument(t1);
            var d2 = JsonSerializer.SerializeToDocument(t2);
            foreach (var VARIABLE in d1.RootElement.EnumerateObject())
            {
                helper.WriteLine(VARIABLE.Name.ToString());
            }
            foreach (var VARIABLE in d2.RootElement.EnumerateObject())
            {
                helper.WriteLine(VARIABLE.Name.ToString());
            }

            helper.WriteLine(JsonSerializer.Serialize(t1));

        }

        [Theory]
        [AutoData]
        public void FactMethodName(string entityId, string source, TestData testData)
        {
            var sourceData = System.Text.Json.JsonSerializer.Serialize(testData);

            helper.WriteLine(sourceData);

            var cmd = new SourceCommand(entityId, source, sourceData);
        }

        [Theory]
        [AutoData]
        public void FactMethodName1(string entityId, string source)
        {
            var sourceData = System.Text.Json.JsonSerializer
                .Serialize(new TestData(null, null), new JsonSerializerOptions()
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
                });

            helper.WriteLine(sourceData);

            var cmd = new SourceCommand(entityId, source, sourceData);
        }


      
    }
    public record TestData(string? Name, string? Value);
    public record SourceCommand(string EntityId, string Source, string SourceData);
}