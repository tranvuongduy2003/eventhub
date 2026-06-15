using System.Reflection;

namespace EventHub.Infrastructure;

public static class AssemblyReference
{
    public static Assembly Assembly { get; } = typeof(AssemblyReference).Assembly;
}
