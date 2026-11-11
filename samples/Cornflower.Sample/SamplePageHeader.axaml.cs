using System.Reflection;
using Avalonia.Controls;
using Avalonia.LogicalTree;

namespace Cornflower.Sample;

/// <summary>
/// A sample page's header: "CORNFLOWER", then the title of the page it is on, read from the page's
/// <see cref="SamplePageAttribute"/>, so a page states its title once.
/// </summary>
public partial class SamplePageHeader : UserControl
{
    public SamplePageHeader()
    {
        InitializeComponent();
    }

    /// <summary>The title shown: the nearest page's, once the header is on one.</summary>
    public string? Title => PageTitle.Text;

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        // One reflection per ancestor, ending at the page's: its attribute is its title.
        PageTitle.Text = this.GetLogicalAncestors()
            .Select(ancestor => ancestor.GetType().GetCustomAttribute<SamplePageAttribute>())
            .FirstOrDefault(page => page is not null)?.Title;
    }
}
