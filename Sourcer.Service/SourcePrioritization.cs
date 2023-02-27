namespace Sourcer.Service;

public class SourcePrioritization
{
    public SourcePrioritization(string source)
    {
        Source = new Source(source);
    }

    public Source Source { get; }
}