using Netor.Markdor.Modules.Mcp.Models;

namespace Netor.Markdor.Modules.Mcp.Services;

public interface IMcpOperationAuditService
{
    IReadOnlyList<McpOperationRecord> GetRecent();

    void Record(McpOperationRecord record);
}
