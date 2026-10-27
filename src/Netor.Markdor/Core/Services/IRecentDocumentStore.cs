using Netor.Markdor.Core.Models;

namespace Netor.Markdor.Core.Services;

public interface IRecentDocumentStore
{
    IReadOnlyList<RecentDocument> Load(int maxCount);

    void Save(IEnumerable<RecentDocument> documents);
}
