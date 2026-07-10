using System.Security.Cryptography;
using EventHub.Application.Abstractions.Tickets;

namespace EventHub.Infrastructure.Tickets;

internal sealed class RandomTicketCodeGenerator : ITicketCodeGenerator
{
    public string Generate()
    {
        Span<byte> bytes = stackalloc byte[24];
        RandomNumberGenerator.Fill(bytes);
        return $"tk_{Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=')}";
    }
}
