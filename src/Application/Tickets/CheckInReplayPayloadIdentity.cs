using System.Security.Cryptography;
using System.Text;
using EventHub.Domain.Tickets;

namespace EventHub.Application.Tickets;

internal sealed record CheckInReplayPayloadIdentity(
    string CodeFingerprint,
    DateTimeOffset ScannedAtUtc)
{
    public static CheckInReplayPayloadIdentity Create(TicketCode code, DateTimeOffset scannedAt) =>
        new(CreateCodeFingerprint(code), PostgresTimestampPrecision.NormalizeUtc(scannedAt));

    public bool Matches(CheckInReplayRecord replay) =>
        ScannedAtUtc == replay.ScannedAtUtc
        && CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(CodeFingerprint),
            Encoding.UTF8.GetBytes(replay.CodeFingerprint));

    public static bool HasSameCodeFingerprint(
        CheckInReplayRecord replay,
        CheckInReplayPayloadIdentity identity) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(replay.CodeFingerprint),
            Encoding.UTF8.GetBytes(identity.CodeFingerprint));

    private static string CreateCodeFingerprint(TicketCode code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code.Value))).ToLowerInvariant();
}
