using Avalonia;

namespace Vex.Core.Models;

public sealed class OutlineItem
{
    public OutlineItem(int level, string title, int line)
    {
        Level = level;
        Title = title;
        Line = line;
        IndentMargin = new Thickness(Math.Max(0, level - 1) * 14, 0, 0, 0);
    }

    public int Level { get; }

    public string Title { get; }

    public int Line { get; }

    public Thickness IndentMargin { get; }
}
