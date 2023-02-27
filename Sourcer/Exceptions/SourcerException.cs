using System.Runtime.Serialization;

namespace Sourcer.Exceptions;

[Serializable]
public abstract class SourcerException : Exception
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