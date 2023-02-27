namespace Sourcer.Service;

public sealed class PrioritizationCollection : Dictionary<Identifier, Prioritization>
{
    public void Add(Identifier identifier, SourcePrioritization[] sourcePrioritization, PropertySpecificPrioritization? propertySpecificPrioritization = null)
    {
        this.Add(identifier, new Prioritization(sourcePrioritization, propertySpecificPrioritization ?? new PropertySpecificPrioritization()));
    }

    public void Add(Identifier identifier, PropertySpecificPrioritization propertySpecificPrioritization)
    {
        this.Add(identifier, Array.Empty<SourcePrioritization>(), propertySpecificPrioritization);
    }
}

public record Prioritization(IEnumerable<SourcePrioritization> SourcePrioritization, PropertySpecificPrioritization SpecificPrioritization);

public sealed class PropertySpecificPrioritization : Dictionary<string, Source>
{
}

