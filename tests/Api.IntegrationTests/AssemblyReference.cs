using System.Reflection;

namespace EventHub.Api.IntegrationTests;

public static class AssemblyReference
{
    public static Assembly Assembly { get; } = typeof(AssemblyReference).Assembly;
}
