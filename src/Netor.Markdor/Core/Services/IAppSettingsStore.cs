using Netor.Markdor.Core.Models;

namespace Netor.Markdor.Core.Services;

public interface IAppSettingsStore
{
    AppSettings Current { get; }

    AppSettings Update(Func<AppSettings, AppSettings> update);
}
