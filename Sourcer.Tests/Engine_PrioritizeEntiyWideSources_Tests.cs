using FluentAssertions;
using Sourcer.Service;
using Xunit;
using Xunit.Abstractions;

namespace Sourcer.Tests;

public class Engine_PrioritizeEntiyWideSources_Tests
{
    private readonly Engine engine;
    private readonly ITestOutputHelper helper;

    public Engine_PrioritizeEntiyWideSources_Tests(ITestOutputHelper helper)
    {
        this.helper = helper;
        engine      = new Engine();
    }

    [Fact(DisplayName =
        "Given data from different sources when prioritize then merge according to prioritization source")]
    public void MergeAccordingToPrioritization()
    {
        engine.ApplySource(new SourceEvent("id1", "source1", "{\"Name\":\"Name1\",\"Value\":1}"));
        engine.ApplySource(new SourceEvent("id1", "source2", "{\"Name\":\"Name2\",\"Value\":2}"));

        var prioritized = engine.Prioritize(new PrioritizationCollection
        {
            {
                new Identifier("default"),
                new[]
                {
                    new SourcePrioritization("source1"),
                    new SourcePrioritization("source2")
                }
            }
        });

        prioritized.Should().Be("{\"Name\":\"Name1\",\"Value\":1}");
    }
}