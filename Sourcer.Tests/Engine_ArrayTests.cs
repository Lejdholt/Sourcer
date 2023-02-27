using FluentAssertions;
using Sourcer.Service;
using Xunit;

namespace Sourcer.Tests;

public class Engine_ArrayTests
{
    private readonly Engine engine;

    public Engine_ArrayTests()
    {
        engine = new Engine();
    }

    [Fact(DisplayName = "Given property with array when new command with larger array then prioritize should return larger array")]
    public void NewLargerArray()
    {
        engine.ApplySource(new SourceEvent("id1", "source1", "{\"Names\":[\"Name1\"]}"));
        engine.ApplySource(new SourceEvent("id1", "source2", "{\"Names\":[\"Name1\",\"Name2\"]}"));

        var result = engine.Prioritize(new(){ {new Identifier("default"),new PropertySpecificPrioritization()}});

        result.Should().Be("{\"Names\":[\"Name1\",\"Name2\"]}");
    } 
    
    [Fact(DisplayName = "Given property with array when new command with smaller array then prioritize should return smaller array")]
    public void NewSmallerArray()
    {
        engine.ApplySource(new SourceEvent("id1", "source1", "{\"Names\":[\"Name1\",\"Name2\"]}"));
        engine.ApplySource(new SourceEvent("id1", "source2", "{\"Names\":[\"Name1\"]}"));
      
        var result = engine.Prioritize(new(){ {new Identifier("default"),new PropertySpecificPrioritization()}});

        result.Should().Be("{\"Names\":[\"Name1\"]}");
    }
}