using System.Reflection;

namespace EventHub.Contracts;

public static class AssemblyReference
{
    public static Assembly Assembly { get; } = typeof(AssemblyReference).Assembly;
}
