﻿using System;

namespace LibHac.Common.Keys;

/// <summary>
/// This is the exception that is thrown when an action requires a key that is not found in the provided keyset.
/// </summary>
public class MissingKeyException : LibHacException
{
    /// <summary>
    /// The name of the key that is missing.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The type of the key that is missing.
    /// </summary>
    public KeyType Type { get; }

    /// <summary>
    ///  Initializes a new instance of the <see cref="MissingKeyException"/> class with a specified error message,
    ///  information about the missing key and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="name">The name of the key that is missing, or the rights ID of the missing key if <paramref name="keyType"/> is <see cref="KeyType.Title"/></param>
    /// <param name="keyType">The <see cref="KeyType"/> of the key that is missing.</param>
    public MissingKeyException(string message, string name, KeyType keyType)
        : base(message)
    {
        Name = name;
        Type = keyType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MissingKeyException"/> class. 
    /// </summary>
    public MissingKeyException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MissingKeyException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public MissingKeyException(string message)
        : base(message) { }

    /// <summary>
    ///  Initializes a new instance of the <see cref="MissingKeyException"/> class with a specified error message
    ///  and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public MissingKeyException(string message, Exception innerException)
        : base(message, innerException) { }

    public override string Message
    {
        get
        {
            string s = base.Message;

            if (Type != KeyType.None)
            {
                s += $"{Environment.NewLine}Key Type: {Type}";
            }

            if (Name != null)
            {
                s += $"{Environment.NewLine}Key Name: {Name}";
            }

            return s;
        }
    }
}