using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Core.Extensions;
using GoatVaultCore.Models.Vault;

namespace GoatVaultClient.Services;

public class VaultFilterService
{
    public ObservableCollection<CategoryItem> FilterAndSortCategories(
        IEnumerable<CategoryItem> allCategories,
        string? searchText,
        bool sortAsc)
    {
        IEnumerable<CategoryItem> query = allCategories;

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            query = query.Where(x => x.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        query = sortAsc
            ? query.OrderBy(f => f.Name)
            : query.OrderByDescending(f => f.Name);

        return query.ToObservableCollection();
    }

    public ObservableCollection<VaultEntry> FilterAndSortEntries(
        IEnumerable<VaultEntry> allEntries,
        string? searchText,
        string? categoryFilter,
        bool sortAsc)
    {
        IEnumerable<VaultEntry> query = allEntries;

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            query = query.Where(x => x.Site.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }
        else if (!string.IsNullOrWhiteSpace(categoryFilter) && !categoryFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => x.Category == categoryFilter);
        }

        query = sortAsc
            ? query.OrderBy(f => f.Site)
            : query.OrderByDescending(f => f.Site);

        return query.ToObservableCollection();
    }
}
