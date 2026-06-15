using System.Reflection;

namespace EventHub.AppHost;

public static class AssemblyReference
{
    public static Assembly Assembly { get; } = typeof(AssemblyReference).Assembly;
}
