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
        var page = this.GetLogicalAncestors().FirstOrDefault(a => a.GetType().GetCustomAttribute<SamplePageAttribute>() is not null);
        PageTitle.Text = page is null ? null : SamplePageCatalog.TitleOf(page.GetType());
    }
}
