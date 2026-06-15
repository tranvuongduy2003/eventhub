using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Common;

public sealed class ApiProblemDetails : ProblemDetails
{
    public string? Code { get; set; }
}
