using FluentAssertions;
using Sourcer.Service;
using Xunit;

namespace Sourcer.Tests;

public class Aggregate_ArrayTests
{
    [Fact(DisplayName = "Given property with array when new command with larger array then prioritize should return larger array")]
    public void NewLargerArray()
    {
        var agg = new Aggregate();

        agg.ApplySource(new SourceCommand("id1", "source1", "{\"Names\":[\"Name1\"]}"));
        agg.ApplySource(new SourceCommand("id1", "source1", "{\"Names\":[\"Name1\",\"Name2\"]}"));

        var result = agg.Prioritize(new(){ {new Identifier("default"),new EntityPrioritization()}});

        result.Should().Be("{\"Names\":[\"Name1\",\"Name2\"]}");
    } 
    
    [Fact(DisplayName = "Given property with array when new command with smaller array then prioritize should return smaller array")]
    public void NewSmallerArray()
    {
        var agg = new Aggregate();

        agg.ApplySource(new SourceCommand("id1", "source1", "{\"Names\":[\"Name1\",\"Name2\"]}"));
        agg.ApplySource(new SourceCommand("id1", "source1", "{\"Names\":[\"Name1\"]}"));
      
        var result = agg.Prioritize(new(){ {new Identifier("default"),new EntityPrioritization()}});

        result.Should().Be("{\"Names\":[\"Name1\"]}");
    }
}