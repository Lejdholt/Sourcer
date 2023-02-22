namespace Sourcer.Service;

public record SourceEvent(string EntityId, string Source, string SourceData);
public record TestData(string? Name, string? Value);