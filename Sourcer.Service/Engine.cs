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
    private Identifier? id;
    private readonly ImmutableHashSet<Source> sources = ImmutableHashSet.Create<Source>();

    public void ApplySource(Identifier objectId, SourceData sourceData)
    {
        if (id == null)
        {
            id = objectId;
        }
        else if (id != objectId)
        {
            throw new IdentifiersNotSameException($"Incoming id {objectId} must be same as first given id {id}");
        }

        var source = sourceData.Source;

        if (sources.Contains(source))
        {
            throw new SourceAlreadyPresentException($"Incoming source {sourceData.Source} already has source data associated with it");
        }

        data = data.Add(sourceData);
    }

    public string Prioritize(PrioritizationCollection prioritization)
    {
        var propertySpecificPrioritization = new PropertySpecificPrioritization();
        var sourcePrioritization           = Array.Empty<Source>();

        if (prioritization.TryGetValue(new Identifier(new string("default")), out var defaultEntityPrioritization))
        {
            propertySpecificPrioritization = defaultEntityPrioritization.SpecificPrioritization;
            sourcePrioritization           = defaultEntityPrioritization.SourcePrioritization;
        }

        ReorderSourcesToPrioritization(sourcePrioritization);

        if (prioritization.TryGetValue(id!, out var entityPrioritization))
        {
            ReorderSourcesToPrioritization(entityPrioritization.SourcePrioritization);

            foreach (var pair in entityPrioritization.SpecificPrioritization)
            {
                propertySpecificPrioritization[pair.Key] = pair.Value;
            }
        }

        var prioritizedObject = Prioritize(propertySpecificPrioritization);

        return ResultToJson(prioritizedObject);
    }

    private void ReorderSourcesToPrioritization(Source[] sourcePrioritization)
    {
        if (sourcePrioritization.Length == 0)
        {
            return;
        }

        var prioSources = ImmutableArray.Create<SourceData>();

        foreach (var source in sourcePrioritization)
        {
            var sourceData = data.FirstOrDefault(s => s.Source == source);

            if (sourceData == null)
            {
                continue;
            }

            prioSources = prioSources.Add(sourceData);
            data        = data.Remove(sourceData);
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
        var source   = sourceData.Source;

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

    private static string ResultToJson(Dictionary<string, (Source Source, JsonNode? Value)> prioritizedObject)
    {
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
}