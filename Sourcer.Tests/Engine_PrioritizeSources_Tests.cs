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

    [Fact(DisplayName = "Given data from the same source when prioritize then merge in lifo order")]
    public void MergeSameSource()
    {
        engine.ApplySource(new SourceEvent("id1", "source1", "{\"Name\":\"Name1\",\"Value\":1}"));
        engine.ApplySource(new SourceEvent("id1", "source1", "{\"Name\":\"Name2\",\"Value\":2}"));

        var prioritized = engine.Prioritize(new PrioritizationCollection
        {
            { new Identifier("default"), new EntityPrioritization { { "Name", new Source("source1") }, { "Value", new Source("source2") } } }
        });
        prioritized.Should().Be("{\"Name\":\"Name2\",\"Value\":2}");
    }

    [Fact(DisplayName = "Given data from the same source when prioritize then overwrite null")]
    public void MergeNullShouldOverwrite()
    {
        engine.ApplySource(new SourceEvent("id1", "source1", "{\"Name\":\"Name1\",\"Value\":1}"));
        engine.ApplySource(new SourceEvent("id1", "source1", "{\"Name\":null,\"Value\":null}"));

        var prioritized = engine.Prioritize(new PrioritizationCollection
        {
            { new Identifier("default"), new EntityPrioritization { { "Name", new Source("Source1") }, { "Value", new Source("Source2") } } }
        });

        prioritized.Should().Be("{\"Name\":null,\"Value\":null}");
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

    [Theory(DisplayName =
        "Given a lot of data from one source and another at next to last when prioritize then respect prioritization")]
    [AutoData]
    public void GivenAlotOfDataPrioSourceAtEnd([Range(10, 30)] int numberOfDataVersion)
    {
        var fixture = new Fixture();

        bool IsNextToLast(int index)
        {
            return index != numberOfDataVersion - 1;
        }

        var data = fixture.CreateMany<TestData>(numberOfDataVersion)
            .Select((d, i) =>
                (Event: new SourceEvent("id1", IsNextToLast(i) ? "source1" : "source2", JsonSerializer.Serialize(d)),
                    Data: d))
            .ToArray();

        foreach (var d in data)
        {
            engine.ApplySource(d.Event);
        }

        var prioritized = engine.Prioritize(new PrioritizationCollection
        {
            { new Identifier("default"), new EntityPrioritization { { "Name", new Source("source1") }, { "Value", new Source("source2") } } }
        });

        var lastSource2     = data.Last().Data;
        var nextLastSource1 = data.SkipLast(1).Last().Data;

        prioritized.Should().Be($"{{\"Name\":\"{nextLastSource1.Name}\",\"Value\":\"{lastSource2.Value}\"}}");
    }


    [Theory(DisplayName =
        "Given a lot of data from one source and another at the middle when prioritize then respect prioritization")]
    [AutoData]
    public void GivenAlotOfDataPrioSourceInTheMiddle([Range(10, 30)] int numberOfDataVersion)
    {
        var fixture = new Fixture();
        var middle  = numberOfDataVersion / 2;

        bool IsTheMiddle(int index)
        {
            return index == middle;
        }

        (SourceEvent Event, TestData Data)[]? data = fixture.CreateMany<TestData>(numberOfDataVersion)
            .Select((d, i) =>
                (Command: new SourceEvent("id1",
                        IsTheMiddle(i) ? "source1" : "source2",
                        JsonSerializer.Serialize(d)),
                    Data: d))
            .ToArray();

        foreach (var d in data)
        {
            engine.ApplySource(d.Event);
        }

        var prioritized = engine.Prioritize(new PrioritizationCollection
        {
            { new Identifier("default"), new EntityPrioritization { { "Name", new Source("source1") }, { "Value", new Source("source2") } } }
        });

        var lastSource2 = data.Last().Data;
        var source1     = data.First(c => c.Event.Source == "source1").Data;

        prioritized.Should().Be($"{{\"Name\":\"{source1.Name}\",\"Value\":\"{lastSource2.Value}\"}}");
    }
}