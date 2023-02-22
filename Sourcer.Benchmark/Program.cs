// See https://aka.ms/new-console-template for more information


using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Sourcer.Service;

var summary = BenchmarkRunner.Run<Md5VsSha256>();

[MemoryDiagnoser]
public class Md5VsSha256
{
    [Benchmark]
    public void Sha256()
    {
        var agg = new Engine();

        agg.ApplySource(new SourceEvent("id1", "source1", "{\"Name\":\"Name1\",\"Value\":1}"));
        agg.ApplySource(new SourceEvent("id1", "source2", "{\"Name\":\"Name2\",\"Value\":2}"));
        agg.ApplySource(new SourceEvent("id1", "source3", "{\"Name\":\"Name3\",\"Value\":2}"));
        agg.ApplySource(new SourceEvent("id1", "source1", "{\"Name\":\"Name1\",\"Value\":2}"));
        agg.ApplySource(new SourceEvent("id1", "source2", "{\"Name\":\"Name2\",\"Value\":2}"));
        agg.ApplySource(new SourceEvent("id1", "source3", "{\"Name\":\"Name3\",\"Value\":2}"));
        agg.ApplySource(new SourceEvent("id1", "source1", "{\"Name\":\"Name1\",\"Value\":2}"));
        agg.ApplySource(new SourceEvent("id1", "source2", "{\"Name\":\"Name2\",\"Value\":2}"));
        agg.ApplySource(new SourceEvent("id1", "source3", "{\"Name\":\"Name3\",\"Value\":2}"));
        agg.ApplySource(new SourceEvent("id1", "source1", "{\"Name\":\"Name1\",\"Value\":2}"));
        agg.ApplySource(new SourceEvent("id1", "source2", "{\"Name\":\"Name2\",\"Value\":2}"));
        agg.ApplySource(new SourceEvent("id1", "source3", "{\"Name\":\"Name3\",\"Value\":2}"));
        agg.Prioritize(new()
            {
                { new("default"), new() { { "Name", new("Source1") }, { "Value", new("Source2") },} },
                { new("id1"), new() { { "Name", new("Source2") }, { "Value", new("Source2") },{ "Missing", new("Source3") }} },
                { new("id2"), new() { { "Name", new("Source3") }, { "Value", new("Source1") },} }
            });
    }
}