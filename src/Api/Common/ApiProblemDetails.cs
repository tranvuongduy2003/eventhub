using Microsoft.AspNetCore.Mvc;

namespace Solution.Api.Common;

public sealed class ApiProblemDetails : ProblemDetails
{
    public string? Code { get; set; }
}
