using System.Collections.ObjectModel;

namespace CargoTransport.Desktop.ViewModels;

public sealed class RoleDashboardSectionViewModel
{
    public RoleDashboardSectionViewModel(
        string heroTitle,
        string heroDescription,
        string focusTitle,
        string focusDescription)
    {
        HeroTitle = heroTitle;
        HeroDescription = heroDescription;
        FocusTitle = focusTitle;
        FocusDescription = focusDescription;
    }

    public string HeroTitle { get; }
    public string HeroDescription { get; }
    public string FocusTitle { get; }
    public string FocusDescription { get; }
    public ObservableCollection<AdminStatTileViewModel> Metrics { get; } = [];
    public ObservableCollection<AdminQuickActionCardViewModel> QuickActions { get; } = [];

    public void Apply(IEnumerable<AdminStatTileViewModel> metrics, IEnumerable<AdminQuickActionCardViewModel> quickActions)
    {
        AdminCollectionHelper.ReplaceWith(Metrics, metrics);
        AdminCollectionHelper.ReplaceWith(QuickActions, quickActions);
    }
}

public sealed class RoleChecklistItemViewModel
{
    public RoleChecklistItemViewModel(string title, string description, string status)
    {
        Title = title;
        Description = description;
        Status = status;
    }

    public string Title { get; }
    public string Description { get; }
    public string Status { get; }
}

public sealed class RoleChecklistSectionViewModel
{
    public RoleChecklistSectionViewModel(
        string title,
        string subtitle,
        string calloutTitle,
        string calloutDescription,
        IEnumerable<RoleChecklistItemViewModel> items)
    {
        Title = title;
        Subtitle = subtitle;
        CalloutTitle = calloutTitle;
        CalloutDescription = calloutDescription;

        foreach (RoleChecklistItemViewModel item in items)
        {
            Items.Add(item);
        }
    }

    public string Title { get; }
    public string Subtitle { get; }
    public string CalloutTitle { get; }
    public string CalloutDescription { get; }
    public ObservableCollection<RoleChecklistItemViewModel> Items { get; } = [];
}
