using System;
using System.Text.RegularExpressions;

namespace SmartTaskManagementAPI.Domain.ValueObjects;

public class Email : IEquatable<Email>
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled);
    
    public string Value { get; }
    
    private Email(string value)
    {
        
    }
}
