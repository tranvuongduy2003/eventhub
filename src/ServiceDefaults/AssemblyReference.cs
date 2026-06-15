using System.Reflection;

namespace EventHub.ServiceDefaults;

public static class AssemblyReference
{
    public static Assembly Assembly { get; } = typeof(AssemblyReference).Assembly;
}
