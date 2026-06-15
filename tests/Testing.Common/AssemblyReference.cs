using System.Reflection;

namespace EventHub.Testing.Common;

public static class AssemblyReference
{
    public static Assembly Assembly { get; } = typeof(AssemblyReference).Assembly;
}
