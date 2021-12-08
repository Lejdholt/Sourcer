using System.Buffers;
using System.Text;
using System.Text.Json;

namespace Sourcer.Tests;

public class Aggregate
{
    private List<SourceData> data = new();
    private string id = string.Empty;

    private record SourceData(string Source, string Data);

    public void ApplySource(SourceCommand cmd)
    {
        id = cmd.EntityId;
        data.Add(new SourceData(cmd.Source, cmd.SourceData));
    }

    public string Prioritize(string priortization)
    {
        Dictionary<string, Dictionary<string, string>> Dictionary()
        {
            using var document = JsonDocument.Parse(priortization);
            var dictionary = document.RootElement.EnumerateObject()
                .ToDictionary(j => j.Name,
                    j => j.Value
                        .EnumerateObject()
                        .ToDictionary(p => p.Name, p => p.Value.ToString()));

            return dictionary;
        }

        var outputBuffer = new ArrayBufferWriter<byte>();

        var prio = Dictionary();

        var @default = prio["default"];
        if (prio.TryGetValue(id, out var idPrio))
        {
            foreach (var pair in idPrio)
            {
                @default[pair.Key] = pair.Value;
            }
        }

        Parse(outputBuffer, @default);

        return Encoding.UTF8.GetString(outputBuffer.WrittenSpan);
    }


    private void Parse(ArrayBufferWriter<byte> outputBuffer, IReadOnlyDictionary<string, string> prio)
    {
        Dictionary<string, (string Source, JsonProperty Property)> prioritizedObject = new();

        var sourceAndDocuments = data
            .Select(sourceData => (sourceData.Source, Document: JsonDocument.Parse(sourceData.Data)))
            .ToArray();

        foreach (var (source, document) in sourceAndDocuments)
        {
            foreach (var newProp in document.RootElement.EnumerateObject())
            {
                if (prioritizedObject.TryGetValue(newProp.Name, out var current) &&
                    prio.TryGetValue(newProp.Name, out var prioSource) &&
                    prioSource.Equals(current.Source, StringComparison.InvariantCultureIgnoreCase) &&
                    !current.Source.Equals(source, StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }

                prioritizedObject[newProp.Name] = (Source: source, newProp);
            }
        }

        using var jsonWriter = new Utf8JsonWriter(outputBuffer, new JsonWriterOptions { Indented = false });

        jsonWriter.WriteStartObject();

        foreach (var (_, property) in prioritizedObject.Values)
        {
            property.WriteTo(jsonWriter);
        }

        jsonWriter.WriteEndObject();

        foreach (var sourceAndDocument in sourceAndDocuments)
        {
            sourceAndDocument.Document.Dispose();
        }
    }
}