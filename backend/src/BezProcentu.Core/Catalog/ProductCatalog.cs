namespace BezProcentu.Core.Catalog;

/// <summary>Top-level product group shown in the storefront navigation.</summary>
public sealed record CategoryGroup(string Slug, string Name, int SortOrder);

/// <summary>Product category inside a group. Slugs are stable public identifiers used by the API.</summary>
public sealed record Category(string Slug, string Name, string GroupSlug, int SortOrder);

/// <summary>
/// Fixed product taxonomy. The focus is computer equipment, large home appliances and consumer electronics;
/// store categories are mapped onto these slugs in <see cref="BezProcentu.Core.Stores.StoreSources"/>.
/// </summary>
public static class ProductCatalog
{
    public const string Datortehnika = "datortehnika";
    public const string LielaSadzivesTehnika = "liela-sadzives-tehnika";
    public const string Elektrotehnika = "elektrotehnika";

    public static IReadOnlyList<CategoryGroup> Groups { get; } =
    [
        new(Datortehnika, "Datortehnika", 1),
        new(LielaSadzivesTehnika, "Lielā sadzīves tehnika", 2),
        new(Elektrotehnika, "Elektrotehnika", 3),
    ];

    public static IReadOnlyList<Category> Categories { get; } =
    [
        new("portativie-datori", "Portatīvie datori", Datortehnika, 1),
        new("stacionarie-datori", "Stacionārie datori", Datortehnika, 2),
        new("monitori", "Monitori", Datortehnika, 3),
        new("plansetdatori", "Planšetdatori", Datortehnika, 4),
        new("printeri", "Printeri", Datortehnika, 5),
        new("datoru-komponentes", "Datoru komponentes", Datortehnika, 6),
        new("tikla-iekartas", "Tīkla iekārtas", Datortehnika, 7),

        new("ledusskapji", "Ledusskapji un saldētavas", LielaSadzivesTehnika, 1),
        new("velas-masinas", "Veļas mašīnas un žāvētāji", LielaSadzivesTehnika, 2),
        new("trauku-mazgajamas-masinas", "Trauku mazgājamās mašīnas", LielaSadzivesTehnika, 3),
        new("plitis-un-cepeskrasnis", "Plītis un cepeškrāsnis", LielaSadzivesTehnika, 4),
        new("tvaika-nosuceji", "Tvaika nosūcēji", LielaSadzivesTehnika, 5),
        new("iebuvejama-tehnika", "Iebūvējamā tehnika", LielaSadzivesTehnika, 6),
        new("kondicionieri", "Kondicionieri", LielaSadzivesTehnika, 7),

        new("televizori", "Televizori", Elektrotehnika, 1),
        new("audio", "Audio un austiņas", Elektrotehnika, 2),
        new("viedtalruni", "Viedtālruņi", Elektrotehnika, 3),
        new("viedpulksteni", "Viedpulksteņi", Elektrotehnika, 4),
        new("spelu-konsoles", "Spēļu konsoles", Elektrotehnika, 5),
        new("projektori", "Projektori", Elektrotehnika, 6),
        new("puteklu-suceji", "Putekļsūcēji", Elektrotehnika, 7),
        new("virtuves-tehnika", "Virtuves tehnika", Elektrotehnika, 8),
    ];

    private static readonly Dictionary<string, Category> BySlug =
        Categories.ToDictionary(c => c.Slug, StringComparer.Ordinal);

    private static readonly HashSet<string> GroupSlugs =
        Groups.Select(g => g.Slug).ToHashSet(StringComparer.Ordinal);

    public static bool IsCategory(string slug) => BySlug.ContainsKey(slug);

    public static bool IsGroup(string slug) => GroupSlugs.Contains(slug);

    public static Category GetCategory(string slug) =>
        BySlug.TryGetValue(slug, out var c) ? c : throw new KeyNotFoundException($"Unknown category '{slug}'.");

    public static IEnumerable<string> CategoriesInGroup(string groupSlug) =>
        Categories.Where(c => c.GroupSlug == groupSlug).Select(c => c.Slug);
}
