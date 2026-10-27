namespace Netor.Markdor.Modules.Mcp.Services;

public interface IMcpOperationConfirmationService
{
    Task<bool> ConfirmAsync(string toolName, string target, string summary);

    void ResetRememberedChoices();
}
