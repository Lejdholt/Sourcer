﻿namespace Sourcer.Service.Exceptions;

[Serializable]
public class SourceAlreadyPresentException : SourcerException
{
    public SourceAlreadyPresentException(string message) : base(message)
    {
    }
}