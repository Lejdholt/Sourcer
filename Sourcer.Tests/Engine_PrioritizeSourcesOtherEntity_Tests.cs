using FluentAssertions;
using Sourcer.Service;
using Xunit;

namespace Sourcer.Tests;

public class Engine_PrioritizeSourcesOtherEntity_Tests
{
    private readonly Engine engine;

    public Engine_PrioritizeSourcesOtherEntity_Tests()
    {
        engine = new Engine();
    }

    [Fact(DisplayName = "Given data from different sources when prioritize with prioritization for another entity do not use prioritization for another entity")]
    public void DoNotYouPrioritizationForOtherEntity()
    {
        engine.ApplySource(new ("id1"), new ( new ("source1"), "{\"Name\":\"Name1\",\"Value\":1}"));
        engine.ApplySource(new ("id1"), new ( new ("source2"), "{\"Name\":\"Name2\",\"Value\":2}"));

        string prioritized = engine.Prioritize(new()
        {
            { new("default"), new() { { "Name", new("source1") }, { "Value", new("source1") }, } },
            { new("id2"), new() { { "Name", new("source2") }, { "Value", new("source2") }, } }
        });
        
        prioritized.Should().Be("{\"Name\":\"Name1\",\"Value\":1}");
    }
}