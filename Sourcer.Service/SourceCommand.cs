namespace Sourcer.Service;

public record SourceCommand(string EntityId, string Source, string SourceData);
public record TestData(string? Name, string? Value);