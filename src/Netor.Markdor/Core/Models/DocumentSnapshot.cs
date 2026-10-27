using System.Text;

namespace Netor.Markdor.Core.Models;

public sealed record DocumentSnapshot(
    string? FilePath,
    string FileName,
    string Markdown,
    Encoding Encoding,
    bool IsNew);
