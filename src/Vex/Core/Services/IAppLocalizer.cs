using System.Globalization;

namespace Vex.Core.Services;

public interface IAppLocalizer
{
    CultureInfo Culture { get; }

    string Get(string key);

    string Format(string key, params object?[] args);
}
