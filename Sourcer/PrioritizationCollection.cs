
namespace Sourcer;

public sealed class PrioritizationCollection : Dictionary<Identifier, Prioritization>
{
    public void Add(Identifier identifier, Sources sourcePrioritization, PropertySpecificPrioritization? propertySpecificPrioritization = null)
    {
        Add(identifier, new Prioritization(sourcePrioritization, propertySpecificPrioritization ?? new PropertySpecificPrioritization()));
    }

    public void Add(Identifier identifier, PropertySpecificPrioritization propertySpecificPrioritization)
    {
        Add(identifier, new Sources(), propertySpecificPrioritization);
    }
}


public record Prioritization(Sources SourcePrioritization, PropertySpecificPrioritization SpecificPrioritization);



public sealed class PropertySpecificPrioritization : Dictionary<string, Source>, IEquatable<PropertySpecificPrioritization>
{
    private static readonly CaseInsensitiveStringComparer _comparer = new();

    public PropertySpecificPrioritization() : base(_comparer)
    {

    }

    public PropertySpecificPrioritization(IDictionary<string, Source> dictionary) : base(dictionary, _comparer)
    {
    }

    public PropertySpecificPrioritization(IEnumerable<KeyValuePair<string, Source>> collection) : base(collection, _comparer)
    {
    }

    public new void Add(string attribute, Source source)
    {
        base.Add(attribute, source);
    }

    public bool Equals(PropertySpecificPrioritization? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.Keys.SequenceEqual(other.Keys) && this.Values.SequenceEqual(other.Values);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is PropertySpecificPrioritization other && Equals(other);
    }

    public override int GetHashCode()
    {
        var hashCode = 0;

        foreach (var source in this)
        {
            HashCode.Combine(hashCode, source.Key.GetHashCode(), source.Value.GetHashCode());
        }

        return hashCode;
    }
}

public sealed class Sources : List<Source>, IEquatable<Sources>
{

    public Sources(IEnumerable<Source> collection) : base(collection)
    {
    }

    public Sources()
    {
    }

    public bool Equals(Sources? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.SequenceEqual(other);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is Sources other && Equals(other);
    }

    public override int GetHashCode()
    {
        var hashCode = 0;

        foreach (var source in this)
        {
            HashCode.Combine(hashCode, source.GetHashCode());
        }

        return hashCode;
    }

}
public class CaseInsensitiveStringComparer : IEqualityComparer<string>
{
    public bool Equals(string x, string y)
    {
        return string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
    }

    public int GetHashCode(string obj)
    {
        return obj.ToLower().GetHashCode();
    }
}