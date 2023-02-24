using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using AutoFixture;
using AutoFixture.Xunit2;
using FluentAssertions;
using Sourcer.Service;
using Xunit;
using Xunit.Abstractions;

namespace Sourcer.Tests;

public class Engine_PrioritizeSources_Tests
{
    private readonly ITestOutputHelper helper;
    private readonly Engine engine;

    public Engine_PrioritizeSources_Tests(ITestOutputHelper helper)
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
                new EntityPrioritization
                {
                    { "Name", new ("source1") },
                    { "Value", new ("source2") }
                }
            }
        });

        prioritized.Should().Be("{\"Name\":\"Name1\",\"Value\":2}");
    }

    [Fact(DisplayName = "Given data when prioritize is missing sources in data treat all sources equal")]
    public void NoSourceTreatedEqually()
    {
        engine.ApplySource(new SourceEvent("id1", "source1", "{\"Name\":\"Name1\",\"Value\":1}"));
        engine.ApplySource(new SourceEvent("id1", "source2", "{\"Name\":\"Name2\",\"Value\":2}"));
        var prioritized = engine.Prioritize(new PrioritizationCollection
        {
            { new Identifier("default"), new EntityPrioritization() }
        });

        prioritized.Should().Be("{\"Name\":\"Name2\",\"Value\":2}");
    }

    [Fact(DisplayName =
        "Given data when prioritize is missing a prioritization for property in data treat all sources equal for that property")]
    public void MissingSourceTreatedEqually()
    {
        engine.ApplySource(new SourceEvent("id1", "source1", "{\"Name\":\"Name1\",\"Value\":1}"));
        engine.ApplySource(new SourceEvent("id1", "source2", "{\"Name\":\"Name2\",\"Value\":2}"));

        var prioritized = engine.Prioritize(new PrioritizationCollection
        {
            { new Identifier("default"), new EntityPrioritization { { "Name", new Source("source1") } } }
        });

        prioritized.Should().Be("{\"Name\":\"Name1\",\"Value\":2}");
    }


}