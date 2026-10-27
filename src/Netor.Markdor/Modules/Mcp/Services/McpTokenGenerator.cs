using System.Security.Cryptography;

namespace Netor.Markdor.Modules.Mcp.Services;

public static class McpTokenGenerator
{
    public static string Generate()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
