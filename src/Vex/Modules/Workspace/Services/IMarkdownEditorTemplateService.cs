namespace Vex.Modules.Workspace.Services;

public interface IMarkdownEditorTemplateService
{
    string BoldPlaceholder { get; }

    string ItalicPlaceholder { get; }

    string InlineCodePlaceholder { get; }

    string LinkPlaceholder { get; }

    string ImageInsertion { get; }

    string CodeFencePlaceholder { get; }

    string TableInsertion { get; }

    string MathPlaceholder { get; }
}
