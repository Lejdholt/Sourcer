using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Text.Json;
using AutoFixture;
using AutoFixture.Xunit2;
using FluentAssertions;
using Sourcer.Service;
using Xunit;
using Xunit.Abstractions;

namespace Sourcer.Tests;

public class Aggregate_PrioritizeSources_Tests
{
    private readonly ITestOutputHelper helper;

    [Fact(DisplayName =
        "Given data from different sources when prioritize then merge according to prioritization source")]
    public void MergeAccordingToPrioritization()
    {
        var agg = new Aggregate();

        agg.ApplySource(new SourceCommand("id1", "source1", "{\"Name\":\"Name1\",\"Value\":1}"));
        agg.ApplySource(new SourceCommand("id1", "source2", "{\"Name\":\"Name2\",\"Value\":2}"));

        string prioritized = agg.Prioritize(new()
        {
            { new("default"), new() { { "Name", new("Source1") }, { "Value", new("Source2") }, } }
        });

        prioritized.Should().Be("{\"Name\":\"Name1\",\"Value\":2}");
    }

    [Fact(DisplayName = "Given data from the same source when prioritize then merge in lifo order")]
    public void MergeSameSource()
    {
        var agg = new Aggregate();

        agg.ApplySource(new SourceCommand("id1", "source1", "{\"Name\":\"Name1\",\"Value\":1}"));
        agg.ApplySource(new SourceCommand("id1", "source1", "{\"Name\":\"Name2\",\"Value\":2}"));

        string prioritized = agg.Prioritize(new()
        {
            { new("default"), new() { { "Name", new("Source1") }, { "Value", new("Source2") }, } }
        });
        prioritized.Should().Be("{\"Name\":\"Name2\",\"Value\":2}");
    }

    [Fact(DisplayName = "Given data from the same source when prioritize then overwrite null")]
    public void MergeNullShouldOverwrite()
    {
        var agg = new Aggregate();

        agg.ApplySource(new SourceCommand("id1", "source1", "{\"Name\":\"Name1\",\"Value\":1}"));
        agg.ApplySource(new SourceCommand("id1", "source1", "{\"Name\":null,\"Value\":null}"));

        string prioritized = agg.Prioritize(new()
        {
            { new("default"), new() { { "Name", new("Source1") }, { "Value", new("Source2") }, } }
        });

        prioritized.Should().Be("{\"Name\":null,\"Value\":null}");
    }

    [Fact(DisplayName = "Given data when prioritize is missing sources in data treat all sources equal")]
    public void NoSourceTreatedEqually()
    {
        var agg = new Aggregate();

        agg.ApplySource(new SourceCommand("id1", "source1", "{\"Name\":\"Name1\",\"Value\":1}"));
        agg.ApplySource(new SourceCommand("id1", "source2", "{\"Name\":\"Name2\",\"Value\":2}"));
        string prioritized = agg.Prioritize(new()
        {
            { new("default"), new() }
        });

        prioritized.Should().Be("{\"Name\":\"Name2\",\"Value\":2}");
    }

    [Fact(DisplayName =
        "Given data when prioritize is missing a prioritization for property in data treat all sources equal for that property")]
    public void MissingSourceTreatedEqually()
    {
        var agg = new Aggregate();

        agg.ApplySource(new SourceCommand("id1", "source1", "{\"Name\":\"Name1\",\"Value\":1}"));
        agg.ApplySource(new SourceCommand("id1", "source2", "{\"Name\":\"Name2\",\"Value\":2}"));
       
        string prioritized = agg.Prioritize(new()
        {
            { new("default"), new() { { "Name", new("Source1") } } }
        });

        prioritized.Should().Be("{\"Name\":\"Name1\",\"Value\":2}");
    }

    [Theory(DisplayName =
        "Given a lot of data from one source and another at next to last when prioritize then respect prioritization")]
    [AutoData]
    public void GivenAlotOfDataPrioSourceAtEnd([Range(10, 30)] int numberOfDataVersion)
    {
        var fixture = new Fixture();

        bool IsNextToLast(int index) => index != numberOfDataVersion - 1;

        var cmds = fixture.CreateMany<TestData>(numberOfDataVersion)
            .Select((d, i) =>
                (Command: new SourceCommand("id1", IsNextToLast(i) ? "source1" : "source2", JsonSerializer.Serialize(d)),
                    Data: d))
            .ToArray();


        var agg = new Aggregate();
        foreach (var cmd in cmds)
        {
            agg.ApplySource(cmd.Item1);
        }
        
        string prioritized = agg.Prioritize(new()
        {
            { new("default"), new() { { "Name", new("Source1") }, { "Value", new("Source2") }, } }
        });
        
        var lastSource2 = cmds.Last().Data;
        var nextLastSource1 = cmds.SkipLast(1).Last().Data;

        prioritized.Should().Be($"{{\"Name\":\"{nextLastSource1.Name}\",\"Value\":\"{lastSource2.Value}\"}}");
    }


    [Theory(DisplayName =
        "Given a lot of data from one source and another at the middle when prioritize then respect prioritization")]
    [AutoData]
    public void GivenAlotOfDataPrioSourceInTheMiddle([Range(10, 30)] int numberOfDataVersion)
    {
        var fixture = new Fixture();
        var middle = (int)(numberOfDataVersion / 2);
        bool IsTheMiddle(int index) => index == middle;

        var cmds = fixture.CreateMany<TestData>(numberOfDataVersion)
            .Select((d, i) =>
                (Command: new SourceCommand("id1",
                        IsTheMiddle(i) ? "source1" : "source2",
                        JsonSerializer.Serialize(d)),
                    Data: d))
            .ToArray();


        var agg = new Aggregate();
        foreach (var cmd in cmds)
        {
            agg.ApplySource(cmd.Item1);
        }

        string prioritized = agg.Prioritize(new()
        {
            { new("default"), new() { { "Name", new("Source1") }, { "Value", new("Source2") }, } }
        });
        
        var lastSource2 = cmds.Last().Data;
        var source1 = cmds.First(c => c.Command.Source == "source1").Data;

        prioritized.Should().Be($"{{\"Name\":\"{source1.Name}\",\"Value\":\"{lastSource2.Value}\"}}");
    }

    public Aggregate_PrioritizeSources_Tests(ITestOutputHelper helper)
    {
        this.helper = helper;
    }
}