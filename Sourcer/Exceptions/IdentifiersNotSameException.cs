﻿namespace Sourcer.Exceptions;

[Serializable]
public class IdentifiersNotSameException : SourcerException
{
    public IdentifiersNotSameException(string message) : base(message)
    {
    }
}