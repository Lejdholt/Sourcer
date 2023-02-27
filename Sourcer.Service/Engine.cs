using System.Buffers;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Sourcer.Service.Exceptions;

namespace Sourcer.Service;

public class Engine
{

    private ImmutableArray<SourceData> data = ImmutableArray.Create<SourceData>();
    private ImmutableHashSet<Source> sources = ImmutableHashSet.Create<Source>();
    private string? id = null;

    private record SourceData(Source Source, string Data);

    public void ApplySource(SourceEvent cmd)
    {

        if (id == null)
        {
            id = cmd.EntityId;
        }
        else if (id != cmd.EntityId)
        {
            throw new IdentifiersNotSameException($"Incoming id {cmd.EntityId} must be same as first given id {id}");
        }

        var source = new Source(cmd.Source);

        if (sources.Contains(source))
        {
            throw new SourceAlreadyPresentException($"Incoming source {cmd.Source} already has source data associated with it");
        }

        data = data.Add(new SourceData(source, cmd.SourceData));
    }

    public string Prioritize(PrioritizationCollection prioritization)
    {


        PropertySpecificPrioritization propertySpecificPrioritization = new PropertySpecificPrioritization();
        var sourcePrioritization = Array.Empty<SourcePrioritization>();

        if (prioritization.TryGetValue(new(new("default")), out var defaultEntityPrioritization))
        {
            propertySpecificPrioritization = defaultEntityPrioritization.SpecificPrioritization;
            sourcePrioritization = defaultEntityPrioritization.SourcePrioritization.ToArray();
        }

        ReorderSourcesToPrio(sourcePrioritization);


        if (prioritization.TryGetValue(new(id), out var entityPrioritization))
        {

            ReorderSourcesToPrio(entityPrioritization.SourcePrioritization.ToArray());


            foreach (var pair in entityPrioritization.SpecificPrioritization)
            {
                propertySpecificPrioritization[pair.Key] = pair.Value;
            }
        }

        var prioritizedObject = Prioritize(propertySpecificPrioritization);

        var outputBuffer = new ArrayBufferWriter<byte>();

        using (var jsonWriter = new Utf8JsonWriter(outputBuffer, new JsonWriterOptions { Indented = false }))
        {
            jsonWriter.WriteStartObject();

            foreach (var (key, (_, property)) in prioritizedObject)
            {
                jsonWriter.WritePropertyName(key);
                if (property != null)
                {
                    property.WriteTo(jsonWriter);
                }
                else
                {
                    jsonWriter.WriteNullValue();
                }
            }

            jsonWriter.WriteEndObject();
        }
        return Encoding.UTF8.GetString(outputBuffer.WrittenSpan);
    }

    private void ReorderSourcesToPrio(SourcePrioritization[] sourcePrioritization)
    {
        if (sourcePrioritization.Length == 0)   
        {
            return;
        }

        var prioSources = ImmutableArray.Create<SourceData>();
        foreach (var VARIABLE in sourcePrioritization)
        {
            var s = data.FirstOrDefault(s => s.Source == VARIABLE.Source);

            if (s != null)
            {
                prioSources = prioSources.Add(s);
                data        = data.Remove(s);
            }
        }

        foreach (var sourceData in data)
        {
            prioSources = prioSources.Add(sourceData);
        }

        data = prioSources;
    }


    private Dictionary<string, (Source Source, JsonNode? Value)> Prioritize(PropertySpecificPrioritization propertySpecificPrioritization)
    {
        Dictionary<string, (Source Source, JsonNode? Value)> prioritizedObject = new();

        Prioritize(propertySpecificPrioritization, prioritizedObject, data[0], data[1..]);

        return prioritizedObject;
    }

    private void Prioritize(PropertySpecificPrioritization prio,
        Dictionary<string, (Source Source, JsonNode? Value)> prioritizedObject,
        SourceData sourceData,
        ImmutableArray<SourceData> rest)
    {
        var document = (JsonObject)JsonNode.Parse(sourceData.Data)!;
        var source = sourceData.Source;

        foreach (var (key, obj) in document)
        {
            if (prioritizedObject.TryGetValue(key, out var current))
            {

                if (!prio.TryGetValue(key, out var prioSource))
                {
                    continue;
                }

                if (current.Source == prioSource && current.Source != source)
                {
                    continue;
                }
            }

            prioritizedObject[key] = (Source: source, obj);
        }

        document.Clear();
        if (rest.Length is 0)
        {
            return;
        }
        
        Prioritize(prio, prioritizedObject, rest[0], rest[1..]);
    }
}