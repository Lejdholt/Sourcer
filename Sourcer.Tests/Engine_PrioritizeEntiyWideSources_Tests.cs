using FluentAssertions;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Resources;
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
        @"Given data from different sources
          and rules default source prioritization
          when prioritize
          then merge according to default prioritization source")]
    public void MergeAccordingToDefaultSourcePrioritization()
    {
        engine.ApplySource(new Identifier("id1"), new SourceData(new Source("source1"), "{\"Name\":\"Name1\",\"Value\":1}"));
        engine.ApplySource(new Identifier("id1"), new SourceData(new Source("source2"), "{\"Name\":\"Name2\",\"Value\":2}"));


        var prioritized = engine.Prioritize(new PrioritizationCollection
        {
            {
                new Identifier("default"),
               new Sources
                    {
                        new("source1"),
                        new("source2")
                    }
                
            }
        });

        prioritized.Should().Be("{\"Name\":\"Name1\",\"Value\":1}");
    }

    [Fact(DisplayName =
        @"Given data from different sources 
          and rules default source prioritization
          and rules with source prioritization for object
          when prioritize 
          then merge according to objects prioritization source")]
    public void MergeAccordingToObjectPrioritization()
    {
        engine.ApplySource(new Identifier("id1"), new SourceData(new Source("source2"), "{\"Name\":\"Name2\",\"Value\":2}"));
        engine.ApplySource(new Identifier("id1"), new SourceData(new Source("source1"), "{\"Name\":\"Name1\",\"Value\":1}"));

        var prioritized = engine.Prioritize(new PrioritizationCollection
        {
            {
                new Identifier("default"),
                new Sources
                {
                    new Source("source1"),
                    new Source("source2")
                }
            },
            {
                new Identifier("id1"),
                new Sources
                {
                    new Source("source2"),
                    new Source("source1")
                }
            }
        });

        prioritized.Should().Be("{\"Name\":\"Name2\",\"Value\":2}");
    }


    [Fact(DisplayName =
        @"Given data from different sources 
          and rules default source prioritization not covering all sources
          and rules with source prioritization for object not covering all sources
          and rules default source prioritization
          when prioritize 
          then merge according to objects prioritization source")]
    public void MergeAccordingToObjectPrioritizationFirst()
    {
        engine.ApplySource(new Identifier("id1"), new SourceData(new Source("source2"), "{\"Name\":\"Name2\",\"Value\":2}"));
        engine.ApplySource(new Identifier("id1"), new SourceData(new Source("source1"), "{\"Name\":\"Name1\",\"Value\":1}"));

        var prioritized = engine.Prioritize(new PrioritizationCollection
        {
            {
                new Identifier("default"),
                new Sources
                {
                    new Source("source1")
                }
            },
            {
                new Identifier("id1"),
                new Sources
                {
                    new Source("source2")
                }
            }
        });

        prioritized.Should().Be("{\"Name\":\"Name2\",\"Value\":2}");
    }


    [Fact(DisplayName =
        @"Given data from different sources 
          and default source prioritization
          and source prioritization for object
          and default source property prioritization
          when prioritize 
          then merge according to default source property prioritization source")]
    public void MergeAccordingToDefaultSourcePropertyPrioritization()
    {
        engine.ApplySource(new Identifier("id1"), new SourceData(new Source("source2"), "{\"Name\":\"Name2\",\"Value\":2}"));
        engine.ApplySource(new Identifier("id1"), new SourceData(new Source("source1"), "{\"Name\":\"Name1\",\"Value\":1}"));

        var prioritized = engine.Prioritize(new PrioritizationCollection
        {
            {
                new Identifier("default"),
                new Sources
                {
                    new Source("source1"),
                    new Source("source2")
                },
                new PropertySpecificPrioritization
                {
                    { "Value", new("Source1") }
                }
            },
            {
                new Identifier("id1"),
                new Sources
                {
                    new Source("source2"),
                    new Source("source1")
                }
            }
        });

        prioritized.Should().Be("{\"Name\":\"Name2\",\"Value\":1}");
    }

    [Fact(DisplayName =
        @"Given data from different sources 
          and default source prioritization
          and source prioritization for object
          and default source property prioritization
          and source property prioritization for object
          when prioritize 
          then merge according to object source property prioritization source")]
    public void MergeAccordingToObjectSourcePropertyPrioritizationSource()
    {
        engine.ApplySource(new Identifier("id1"), new SourceData(new Source("source2"), "{\"Name\":\"Name2\",\"Value\":2}"));
        engine.ApplySource(new Identifier("id1"), new SourceData(new Source("source1"), "{\"Name\":\"Name1\",\"Value\":1}"));

        var prioritized = engine.Prioritize(new PrioritizationCollection
        {
            {
                new Identifier("default"),
                new Sources
                {
                    new Source("source1"),
                    new Source("source2")
                },
                new PropertySpecificPrioritization
                {
                    { "Value", new("Source1") }
                }
            },
            {
                new Identifier("id1"),
                new Sources
                {
                    new Source("source2"),
                    new Source("source1")
                },
                new PropertySpecificPrioritization
                {
                    { "Value", new("source2") }
                }
            }
        });

        prioritized.Should().Be("{\"Name\":\"Name2\",\"Value\":2}");
    }
}