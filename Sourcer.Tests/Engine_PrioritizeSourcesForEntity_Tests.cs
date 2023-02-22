using FluentAssertions;
using Sourcer.Service;
using Xunit;
using Xunit.Abstractions;

namespace Sourcer.Tests;

public class Engine_PrioritizeSourcesForEntity_Tests
{
    private readonly ITestOutputHelper helper;
    private readonly Engine agg;

    [Fact(DisplayName = "Given data from different sources when prioritize with prioritization for entity then merge according to prioritization source for entity")]
    public void MergeAccordingToPrioritizationForEntity()
    {
        agg.ApplySource(new SourceEvent("id1", "source1", "{\"Name\":\"Name1\",\"Value\":1}"));
        agg.ApplySource(new SourceEvent("id1", "source2", "{\"Name\":\"Name2\",\"Value\":2}"));

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
        agg.ApplySource(new SourceEvent("id1", "source1", "{\"Name\":\"Name1\",\"Value\":1}"));
        agg.ApplySource(new SourceEvent("id1", "source2", "{\"Name\":\"Name2\",\"Value\":2}"));
        
        string prioritized = agg.Prioritize(new()
        {
            { new("default"), new() { { "Name", new("source1") }, { "Value", new("Source2") }, } },
            { new("id1"), new() { { "Value", new("source2") }, } }
        });
        
        prioritized.Should().Be("{\"Name\":\"Name1\",\"Value\":2}");
    } 
    
    [Fact(DisplayName = "Given data from different sources when prioritize with prioritization for entity but prop is missing from prioritization and default then merge according to prioritization source for entity with missing prop as lifo prio")]
    public void MergeAccordingToPrioritizationForEntityUseFallback1()
    {
        agg.ApplySource(new SourceEvent("id1", "source1", "{\"Name\":\"Name1\",\"Value\":1}"));
        agg.ApplySource(new SourceEvent("id1", "source2", "{\"Name\":\"Name2\",\"Value\":2}"));
        
        string prioritized = agg.Prioritize(new()
        {
            { new("default"), new() { { "Value", new("Source2") }, } },
            { new("id1"), new() { { "Value", new("Source2") }, } }
        });
        
        prioritized.Should().Be("{\"Name\":\"Name2\",\"Value\":2}");
    }

    

    public Engine_PrioritizeSourcesForEntity_Tests(ITestOutputHelper helper)
    {
        this.helper = helper;
        agg         = new Engine();
    }
}

