using System.Buffers;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using AutoFixture;
using AutoFixture.Xunit2;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Sourcer.Tests;

public class Aggregate_PrioritizeSources_Tests
{
    private readonly ITestOutputHelper helper;

    [Fact(DisplayName = "Given data from different sources when prioritize then merge according to prioritization source")]
    public void MergeAccordingToPrioritization()
    {
        var agg = new Aggregate();

        agg.ApplySource(new SourceCommand("id1", "source1", "{\"Name\":\"Name1\",\"Value\":1}"));
        agg.ApplySource(new SourceCommand("id1", "source2", "{\"Name\":\"Name2\",\"Value\":2}"));
        string prioritized = agg.Prioritize("{\"Name\":\"Source1\",\"Value\":\"Source2\"}");

        prioritized.Should().Be("{\"Name\":\"Name1\",\"Value\":2}");
    }

    [Fact(DisplayName = "Given data from the same source when prioritize then merge in lifo order")]
    public void MergeSameSource()
    {
        var agg = new Aggregate();

        agg.ApplySource(new SourceCommand("id1", "source1", "{\"Name\":\"Name1\",\"Value\":1}"));
        agg.ApplySource(new SourceCommand("id1", "source1", "{\"Name\":\"Name2\",\"Value\":2}"));
        string prioritized = agg.Prioritize("{\"Name\":\"Source1\",\"Value\":\"Source2\"}");

        prioritized.Should().Be("{\"Name\":\"Name2\",\"Value\":2}");
    }

    [Fact(DisplayName = "Given data from the same source when prioritize then overwrite null")]
    public void MergeNullShouldOverwrite()
    {
        var agg = new Aggregate();

        agg.ApplySource(new SourceCommand("id1", "source1", "{\"Name\":\"Name1\",\"Value\":1}"));
        agg.ApplySource(new SourceCommand("id1", "source1", "{\"Name\":null,\"Value\":null}"));
        string prioritized = agg.Prioritize("{\"Name\":\"Source1\",\"Value\":\"Source2\"}");

        prioritized.Should().Be("{\"Name\":null,\"Value\":null}");
    }

    [Fact(DisplayName = "Given data when prioritize is missing sources in data threat all sources equal")]
    public void NoSourceThreadedEqually()
    {
        var agg = new Aggregate();

        agg.ApplySource(new SourceCommand("id1", "source1", "{\"Name\":\"Name1\",\"Value\":1}"));
        agg.ApplySource(new SourceCommand("id1", "source2", "{\"Name\":\"Name2\",\"Value\":2}"));
        string prioritized = agg.Prioritize("{}");


        prioritized.Should().Be("{\"Name\":\"Name2\",\"Value\":2}");
    }

    [Fact(DisplayName = "Given data when prioritize is missing a prioritization for property in data threat all sources equal for that property")]
    public void MissingSourceThreadedEqually()
    {
        var agg = new Aggregate();

        agg.ApplySource(new SourceCommand("id1", "source1", "{\"Name\":\"Name1\",\"Value\":1}"));
        agg.ApplySource(new SourceCommand("id1", "source2", "{\"Name\":\"Name2\",\"Value\":2}"));
        string prioritized = agg.Prioritize("{\"Name\":\"Source1\"}");


        prioritized.Should().Be("{\"Name\":\"Name1\",\"Value\":2}");
    }

    [Theory(DisplayName = "Given a lot of data from one source and another at next to last when prioritize then respect prioritization")]
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

        string prioritized = agg.Prioritize("{\"Name\":\"Source1\",\"Value\":\"Source2\"}");


        var lastSource2 = cmds.Last().Data;
        var nextLastSource1 = cmds.SkipLast(1).Last().Data;

        prioritized.Should().Be($"{{\"Name\":\"{nextLastSource1.Name}\",\"Value\":\"{lastSource2.Value}\"}}");
    }


    [Theory(DisplayName = "Given a lot of data from one source and another at the middle when prioritize then respect prioritization")]
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

        string prioritized = agg.Prioritize("{\"Name\":\"Source1\",\"Value\":\"Source2\"}");


        var lastSource2 = cmds.Last().Data;
        var source1 = cmds.First(c => c.Command.Source == "source1").Data;

        prioritized.Should().Be($"{{\"Name\":\"{source1.Name}\",\"Value\":\"{lastSource2.Value}\"}}");
    }

    public Aggregate_PrioritizeSources_Tests(ITestOutputHelper helper)
    {
        this.helper = helper;
    }
}

public class Aggregate
{
    private List<SourceData> data = new();
    private string id = string.Empty;

    private record SourceData(string Source, string Data);

    public void ApplySource(SourceCommand cmd)
    {
        id = cmd.EntityId;
        data.Add(new SourceData(cmd.Source, cmd.SourceData));
    }

    public string Prioritize(string priortization)
    {
        var outputBuffer = new ArrayBufferWriter<byte>();

        var objectEnumerator = JsonDocument.Parse(priortization).RootElement

            .EnumerateObject();

        Dictionary<string, Dictionary<string, string>> prio = objectEnumerator
               .ToDictionary(j => j.Name,
                   j => j.Value
                       .EnumerateObject()
                       .ToDictionary(j => j.Name, j => j.Value.ToString()));

        Parse(outputBuffer, prio);

        return Encoding.UTF8.GetString(outputBuffer.WrittenSpan);
    }


    private void Parse(ArrayBufferWriter<byte> outputBuffer, Dictionary<string, Dictionary<string, string>> prioTotal)
    {
        var prio = prioTotal.TryGetValue(id, out var idPrio) ? idPrio : prioTotal["default"];

        Dictionary<string, (string Source, JsonProperty Property)> prioritizedObject = new();

        var sourceAndDocuments = data
            .Select(sourceData => (sourceData.Source, Document: JsonDocument.Parse(sourceData.Data)))
            .ToArray();

        foreach (var sourceAndDocument in sourceAndDocuments)
        {

            foreach (var newProp in sourceAndDocument.Document.RootElement.EnumerateObject())
            {
                if (prioritizedObject.TryGetValue(newProp.Name, out var current) &&
                    prio.TryGetValue(newProp.Name, out var prioSource) &&
                    prioSource.Equals(current.Source, StringComparison.InvariantCultureIgnoreCase) &&
                    !current.Source.Equals(sourceAndDocument.Source, StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }


                prioritizedObject[newProp.Name] = (sourceAndDocument.Source, newProp);
            }
        }

        using var jsonWriter = new Utf8JsonWriter(outputBuffer, new JsonWriterOptions { Indented = false });


        jsonWriter.WriteStartObject();

        foreach (var sourceAndProperty in prioritizedObject.Values)
        {
            sourceAndProperty.Property.WriteTo(jsonWriter);
        }

        jsonWriter.WriteEndObject();

        foreach (var sourceAndDocument in sourceAndDocuments)
        {
            sourceAndDocument.Document.Dispose();
        }

    }
}