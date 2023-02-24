using System.Buffers;
using System.Collections.Immutable;
using System.Runtime.Serialization;
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

public class Engine
{
    private ImmutableArray<SourceData> data = ImmutableArray.Create<SourceData>();
    private string? id = null;

    private record SourceData(Source Source, string Data);

    public void ApplySource(SourceEvent cmd)
    {

        if (id == null)
        {
            id = cmd.EntityId;
        }
        else  if (id != cmd.EntityId)
        {
            throw new IdentifiersNotSameException($"Incoming id {cmd.EntityId} must be same as first given id {id}");
        }
        
        data = data.Add(new SourceData(new(cmd.Source), cmd.SourceData));
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


        Prioritize(entityPrioritization, prioritizedObject, data[0], data[1..]);

        return prioritizedObject;
    }

    private static void Prioritize(EntityPrioritization prio,
        Dictionary<string, (Source Source, JsonNode? Value)> prioritizedObject,
        SourceData sourceData,
        ImmutableArray< SourceData> rest)
    {
        var document = (JsonObject)JsonNode.Parse(sourceData.Data)!;
        var source = sourceData.Source;

        foreach (var (key, obj) in document)
        {
            if (prioritizedObject.TryGetValue(key, out var current) && current.Source != source &&
                prio.TryGetValue(key, out var prioSource) && current.Source == prioSource)
            {
                continue;
            }

            prioritizedObject[key] = (Source: source, obj);
        }

        if (rest.Length is 0)
        {
            return;
        }
        document.Clear();

        Prioritize(prio, prioritizedObject, rest[0], rest[1..]);
    }
}

[Serializable]
public class IdentifiersNotSameException : SourcerException
{
    public IdentifiersNotSameException(string message) : base(message)
    {
    }
}

[Serializable]
public abstract class SourcerException:Exception
{
    protected SourcerException()
    {
    }

    protected SourcerException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    protected SourcerException(string? message) : base(message)
    {
    }

    protected SourcerException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}