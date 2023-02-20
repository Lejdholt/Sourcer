using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Sourcer.Service;

public record Source(string Value);

public record Identifier(string Value);

public sealed class EntityPrioritization : Dictionary<string, Source>
{
}

public sealed class PrioritizationCollection : Dictionary<Identifier, EntityPrioritization>
{
}

public class Aggregate
{
    private List<SourceData> data = new();
    private string id = string.Empty;

    private record SourceData(Source Source, string Data);

    public void ApplySource(SourceCommand cmd)
    {
        id = cmd.EntityId;
        data.Add(new SourceData(new(cmd.Source), cmd.SourceData));
    }

    public string Prioritize(PrioritizationCollection prioritization)
    {
        EntityPrioritization @default = prioritization[new("default")];
        if (prioritization.TryGetValue(new(id), out var entityPrioritization))
        {
            foreach (var pair in entityPrioritization)
            {
                @default[pair.Key] = pair.Value;
            }
        }

        var prioritizedObject = Prioritize(@default);

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


    private Dictionary<string, (Source Source, JsonNode? Value)> Prioritize(EntityPrioritization entityPrioritization)
    {
        Dictionary<string, (Source Source, JsonNode? Value)> prioritizedObject = new();

        Passe(entityPrioritization, prioritizedObject, data[0], data.Skip(1).ToArray());

        return prioritizedObject;
    }

    private void Passe(EntityPrioritization prio,
        Dictionary<string, (Source Source, JsonNode? Value)> prioritizedObject,
        SourceData sourceData,
        SourceData[] rest)
    {
        var document = (JsonObject)JsonNode.Parse(sourceData.Data)!;
        var source = sourceData.Source;

        foreach (var (key, obj) in document)
        {
            if (prioritizedObject.TryGetValue(key, out var current) &&
                prio.TryGetValue(key, out var prioSource))
            {
                var (currentSource, _) = current;
                if (prioSource == currentSource && currentSource == source)
                {
                    continue;
                }
            }

            prioritizedObject[key] = (Source: source, obj);
        }

        if (rest.Length is 0)
        {
            return;
        }

        Passe(prio, prioritizedObject, rest[0], rest.Skip(1).ToArray());
    }
}