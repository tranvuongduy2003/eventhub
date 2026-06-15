using System.Reflection;

namespace EventHub.Domain.UnitTests;

public static class AssemblyReference
{
    public static Assembly Assembly { get; } = typeof(AssemblyReference).Assembly;
}
