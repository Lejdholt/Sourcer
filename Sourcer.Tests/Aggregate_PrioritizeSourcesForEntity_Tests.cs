using FluentAssertions;
using Sourcer.Service;
using Xunit;
using Xunit.Abstractions;

namespace Sourcer.Tests;

public class Aggregate_PrioritizeSourcesForEntity_Tests
{
    private readonly ITestOutputHelper helper;

    [Fact(DisplayName = "Given data from different sources when prioritize with prioritization for entity then merge according to prioritization source for entity")]
    public void MergeAccordingToPrioritizationForEntity()
    {
        var agg = new Aggregate();

        agg.ApplySource(new SourceCommand("id1", "source1", "{\"Name\":\"Name1\",\"Value\":1}"));
        agg.ApplySource(new SourceCommand("id1", "source2", "{\"Name\":\"Name2\",\"Value\":2}"));

        string prioritized = agg.Prioritize(new()
        {
            { new("default"), new() { { "Name", new("Source1") }, { "Value", new("Source2") }, } },
            { new("id1"), new() { { "Name", new("Source2") }, { "Value", new("Source2") }, } }
        });

        prioritized.Should().Be("{\"Name\":\"Name2\",\"Value\":2}");
    } 
    
    [Fact(DisplayName = "Given data from different sources when prioritize with prioritization for entity but prop is missing then merge according to prioritization source for entity with fallback to default")]
    public void MergeAccordingToPrioritizationForEntityUseFallback()
    {
        var agg = new Aggregate();

        agg.ApplySource(new SourceCommand("id1", "source1", "{\"Name\":\"Name1\",\"Value\":1}"));
        agg.ApplySource(new SourceCommand("id1", "source2", "{\"Name\":\"Name2\",\"Value\":2}"));
        
        string prioritized = agg.Prioritize(new()
        {
            { new("default"), new() { { "Name", new("Source1") }, { "Value", new("Source2") }, } },
            { new("id1"), new() { { "Value", new("Source2") }, } }
        });
        
        prioritized.Should().Be("{\"Name\":\"Name1\",\"Value\":2}");
    } 
    
    [Fact(DisplayName = "Given data from different sources when prioritize with prioritization for entity but prop is missing from prioritization and default then merge according to prioritization source for entity with missing prop as lifo prio")]
    public void MergeAccordingToPrioritizationForEntityUseFallback1()
    {
        var agg = new Aggregate();

        agg.ApplySource(new SourceCommand("id1", "source1", "{\"Name\":\"Name1\",\"Value\":1}"));
        agg.ApplySource(new SourceCommand("id1", "source2", "{\"Name\":\"Name2\",\"Value\":2}"));
        
        string prioritized = agg.Prioritize(new()
        {
            { new("default"), new() { { "Value", new("Source2") }, } },
            { new("id1"), new() { { "Value", new("Source2") }, } }
        });
        
        prioritized.Should().Be("{\"Name\":\"Name2\",\"Value\":2}");
    }

    

    public Aggregate_PrioritizeSourcesForEntity_Tests(ITestOutputHelper helper)
    {
        this.helper = helper;
    }
}

