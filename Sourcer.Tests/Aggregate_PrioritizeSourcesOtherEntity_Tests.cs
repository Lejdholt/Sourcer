﻿using FluentAssertions;
using Sourcer.Service;
using Xunit;

namespace Sourcer.Tests;

public class Aggregate_PrioritizeSourcesOtherEntity_Tests
{
    [Fact(DisplayName = "Given data from different sources when prioritize with prioritization for another entity do not use prioritization for another entity")]
    public void DoNotYouPrioritizationForOtherEntity()
    {
        var agg = new Aggregate();

        agg.ApplySource(new SourceCommand("id1", "source1", "{\"Name\":\"Name1\",\"Value\":1}"));
        agg.ApplySource(new SourceCommand("id1", "source2", "{\"Name\":\"Name2\",\"Value\":2}"));

        string prioritized = agg.Prioritize(new()
        {
            { new("default"), new() { { "Name", new("source1") }, { "Value", new("source1") }, } },
            { new("id2"), new() { { "Name", new("source2") }, { "Value", new("source2") }, } }
        });
        
        prioritized.Should().Be("{\"Name\":\"Name1\",\"Value\":1}");
    }
}