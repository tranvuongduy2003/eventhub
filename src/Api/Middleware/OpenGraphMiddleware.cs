using System.Text;
using System.Text.RegularExpressions;
using EventHub.Application.Events.Queries;
using MediatR;

namespace EventHub.Api.Middleware;

public sealed partial class OpenGraphMiddleware(RequestDelegate next)
{
    private const string SiteName = "EventHub";
    private const int MaxDescriptionLength = 200;

    private static readonly Regex EventSlugRoutePattern = EventSlugRoute();

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsHtmlRequest(context) && TryGetSlug(context, out var slug))
        {
            await WriteOgResponseAsync(context, slug!);
            return;
        }

        await next(context);
    }

    private static bool IsHtmlRequest(HttpContext context) =>
        context.Request.Method == "GET"
        && context.Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase);

    private static bool TryGetSlug(HttpContext context, out string? slug)
    {
        slug = null;
        var match = EventSlugRoutePattern.Match(context.Request.Path);
        if (!match.Success) return false;

        slug = match.Groups["slug"].Value;
        return !string.IsNullOrEmpty(slug);
    }

    private async Task WriteOgResponseAsync(HttpContext context, string slug)
    {
        var sender = context.RequestServices.GetRequiredService<ISender>();
        var result = await sender.Send(new GetPublicEventQuery(slug));

        if (!result.IsSuccess)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync(BuildHtml(null, context));
            return;
        }

        var eventResponse = result.Value!;
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(BuildHtml(eventResponse, context));
    }

    private static string BuildHtml(Contracts.Events.PublicEventResponse? eventResponse, HttpContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");

        if (eventResponse is not null)
        {
            var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
            var eventUrl = $"{baseUrl}/events/{eventResponse.Slug}";
            var description = FormatDateLocation(
                eventResponse.StartsAt,
                eventResponse.PhysicalAddress,
                eventResponse.IsOnline);

            sb.AppendLine($"<title>{Escape(eventResponse.Title)}</title>");

            // Open Graph
            sb.AppendLine($"<meta property=\"og:title\" content=\"{Escape(eventResponse.Title)}\">");
            sb.AppendLine($"<meta property=\"og:description\" content=\"{Escape(Truncate(description, MaxDescriptionLength))}\">");
            sb.AppendLine($"<meta property=\"og:type\" content=\"event\">");
            sb.AppendLine($"<meta property=\"og:url\" content=\"{Escape(eventUrl)}\">");
            sb.AppendLine($"<meta property=\"og:site_name\" content=\"{Escape(SiteName)}\">");

            if (!string.IsNullOrEmpty(eventResponse.CoverImageUrl))
            {
                sb.AppendLine($"<meta property=\"og:image\" content=\"{Escape(eventResponse.CoverImageUrl)}\">");
                sb.AppendLine($"<meta property=\"og:image:alt\" content=\"{Escape($"Cover image for {eventResponse.Title}")}\">");
            }

            // Twitter Card
            var twitterCard = string.IsNullOrEmpty(eventResponse.CoverImageUrl) ? "summary" : "summary_large_image";
            sb.AppendLine($"<meta name=\"twitter:card\" content=\"{twitterCard}\">");
            sb.AppendLine($"<meta name=\"twitter:title\" content=\"{Escape(eventResponse.Title)}\">");
            sb.AppendLine($"<meta name=\"twitter:description\" content=\"{Escape(Truncate(description, MaxDescriptionLength))}\">");

            if (!string.IsNullOrEmpty(eventResponse.CoverImageUrl))
            {
                sb.AppendLine($"<meta name=\"twitter:image\" content=\"{Escape(eventResponse.CoverImageUrl)}\">");
            }
        }
        else
        {
            sb.AppendLine($"<title>{Escape(SiteName)}</title>");
        }

        sb.AppendLine("</head>");
        sb.AppendLine("<body></body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static string FormatDateLocation(
        DateTimeOffset? startsAt,
        string? physicalAddress,
        bool isOnline)
    {
        var parts = new List<string>();

        if (startsAt.HasValue)
        {
            var date = startsAt.Value.ToString("ddd, MMM d, yyyy");
            var time = startsAt.Value.ToString("h:mm tt");
            parts.Add($"{date}, {time}");
        }

        if (!string.IsNullOrEmpty(physicalAddress))
        {
            parts.Add(physicalAddress);
        }
        else if (isOnline)
        {
            parts.Add("Online event");
        }

        return string.Join(" · ", parts) ?? "Event on EventHub";
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..(maxLength - 1)].TrimEnd() + "…";

    private static string Escape(string text) =>
        text.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;");

    [GeneratedRegex(@"^/api/events/(?<slug>[^/]+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex EventSlugRoute();
}
