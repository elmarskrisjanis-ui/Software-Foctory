namespace BezProcentu.Core.Stores;

public enum StoreStatus
{
    /// <summary>Collected daily.</summary>
    Active = 1,

    /// <summary>Not collected: the store blocks automated access (bot protection) and no official feed is available.</summary>
    Unavailable = 2,
}

/// <summary>A store listing page and the storefront category its products belong to.</summary>
public sealed record ListingSource(string Url, string CategorySlug);

public sealed record StoreDefinition(
    string Slug,
    string Name,
    string HomepageUrl,
    StoreStatus Status,
    string? StatusNote,
    IReadOnlyCollection<string> SiteHosts,
    IReadOnlyCollection<string> ImageHosts,
    string? PolicyUrl,
    IReadOnlyList<ListingSource> Listings);

/// <summary>
/// The list of supported stores. Popular Riga e-shops that answer automated requests with a bot challenge are
/// listed as unavailable on purpose: the collector never tries to bypass such protection.
/// </summary>
public static class StoreSources
{
    public const string Euronics = "euronics";
    public const string M79 = "m79";
    public const string Tet = "tet";

    public static IReadOnlyList<StoreDefinition> All { get; } =
    [
        new(Euronics, "Euronics", "https://www.euronics.lv/", StoreStatus.Active, null,
            ["www.euronics.lv"], ["www.euronics.lv"], "https://www.euronics.lv/lizings",
            [
                new("https://www.euronics.lv/it/portativie-datori", "portativie-datori"),
                new("https://www.euronics.lv/it/galda-datori", "stacionarie-datori"),
                new("https://www.euronics.lv/it/monitori", "monitori"),
                new("https://www.euronics.lv/it/plansetdatori", "plansetdatori"),
                new("https://www.euronics.lv/it/printeri", "printeri"),
                new("https://www.euronics.lv/sadzives-tehnika/ledusskapji", "ledusskapji"),
                new("https://www.euronics.lv/sadzives-tehnika/velas-mazgajamas-un-zavejamas-masinas", "velas-masinas"),
                new("https://www.euronics.lv/sadzives-tehnika/trauku-mazgajamas-masinas", "trauku-mazgajamas-masinas"),
                new("https://www.euronics.lv/sadzives-tehnika/plitis-un-cepeskrasnis", "plitis-un-cepeskrasnis"),
                new("https://www.euronics.lv/sadzives-tehnika/tvaika-nosuceji", "tvaika-nosuceji"),
                new("https://www.euronics.lv/sadzives-tehnika/iebuvejama-tehnika", "iebuvejama-tehnika"),
                new("https://www.euronics.lv/tv/televizori", "televizori"),
                new("https://www.euronics.lv/tv/majas-kinozales", "audio"),
                new("https://www.euronics.lv/audio/austinas", "audio"),
                new("https://www.euronics.lv/telefoni/viedtalruni", "viedtalruni"),
                new("https://www.euronics.lv/telefoni/pulksteni", "viedpulksteni"),
                new("https://www.euronics.lv/izklaide/spelu-konsoles", "spelu-konsoles"),
                new("https://www.euronics.lv/maza-sadzives-tehnika/puteklu-suceji", "puteklu-suceji"),
                new("https://www.euronics.lv/virtuves-tehnika/kafijas-automati", "virtuves-tehnika"),
            ]),

        new(M79, "M79", "https://www.m79.lv/", StoreStatus.Active, null,
            ["www.m79.lv"], ["pictures.m79.lv"], "https://www.m79.lv/pages/bezprocentu-kredits",
            [
                new("https://www.m79.lv/c/category/Laptopi=portativie+datori", "portativie-datori"),
                new("https://www.m79.lv/c/category/Pcs=darbstacijas", "stacionarie-datori"),
                new("https://www.m79.lv/c/category/Monitori", "monitori"),
                new("https://www.m79.lv/c/category/Plansetdatori", "plansetdatori"),
                new("https://www.m79.lv/c/category/Lazerprinteri", "printeri"),
                new("https://www.m79.lv/c/category/Daudzfunkciju+printeri", "printeri"),
                new("https://www.m79.lv/c/category/Mates+plates", "datoru-komponentes"),
                new("https://www.m79.lv/c/category/Procesori", "datoru-komponentes"),
                new("https://www.m79.lv/c/category/SSD+diski", "datoru-komponentes"),
                new("https://www.m79.lv/c/category/Ledusskapji", "ledusskapji"),
                new("https://www.m79.lv/c/category/Saldetavas", "ledusskapji"),
                new("https://www.m79.lv/c/category/Velasmasinas", "velas-masinas"),
                new("https://www.m79.lv/c/category/Elektriskie+velas+zavetaji", "velas-masinas"),
                new("https://www.m79.lv/c/category/Trauku+mazgajamas+masinas", "trauku-mazgajamas-masinas"),
                new("https://www.m79.lv/c/category/Plitis", "plitis-un-cepeskrasnis"),
                new("https://www.m79.lv/c/category/Cepeskrasnis", "plitis-un-cepeskrasnis"),
                new("https://www.m79.lv/c/category/Tvaika+nosuceji", "tvaika-nosuceji"),
                new("https://www.m79.lv/c/category/Gaisa+kondicionieri", "kondicionieri"),
                new("https://www.m79.lv/c/category/Televizori", "televizori"),
                new("https://www.m79.lv/c/category/Majas+kinozales", "audio"),
                new("https://www.m79.lv/c/category/Mobilie+telefoni", "viedtalruni"),
                new("https://www.m79.lv/c/category/Viedpulksteni+un+sporta+pulksteni", "viedpulksteni"),
                new("https://www.m79.lv/c/category/Spelu+konsoles", "spelu-konsoles"),
                new("https://www.m79.lv/c/category/Projektori", "projektori"),
                new("https://www.m79.lv/c/category/Puteklusuceji", "puteklu-suceji"),
                new("https://www.m79.lv/c/category/Roboti-puteklsuceji", "puteklu-suceji"),
                new("https://www.m79.lv/c/category/Kafijas+automati", "virtuves-tehnika"),
                new("https://www.m79.lv/c/category/Mikrovilnu+krasnis", "virtuves-tehnika"),
            ]),

        new(Tet, "Tet", "https://www.tet.lv/veikals/", StoreStatus.Active, null,
            ["www.tet.lv"], ["cdn.tet.lv"], "https://www.tet.lv/veikals/nomaksa",
            [
                new("https://www.tet.lv/veikals/datortehnika/portativie-datori-un-piederumi/portativie-datori.html", "portativie-datori"),
                new("https://www.tet.lv/veikals/datortehnika/stacionarie-datori-un-piederumi/stacionarie-datori.html", "stacionarie-datori"),
                new("https://www.tet.lv/veikals/datortehnika/stacionarie-datori-un-piederumi/monitori.html", "monitori"),
                new("https://www.tet.lv/veikals/datortehnika/plansetdatori-un-aksesuari/plansetdatori.html", "plansetdatori"),
                new("https://www.tet.lv/veikals/datortehnika/tikla-iekartas/ruteri.html", "tikla-iekartas"),
                new("https://www.tet.lv/veikals/datortehnika/tikla-iekartas/komutatori.html", "tikla-iekartas"),
                new("https://www.tet.lv/veikals/sadzives-tehnika/liela-sadzives-tehnika/ledusskapji.html", "ledusskapji"),
                new("https://www.tet.lv/veikals/sadzives-tehnika/liela-sadzives-tehnika/saldetavas.html", "ledusskapji"),
                new("https://www.tet.lv/veikals/sadzives-tehnika/liela-sadzives-tehnika/velas-masinas.html", "velas-masinas"),
                new("https://www.tet.lv/veikals/sadzives-tehnika/liela-sadzives-tehnika/velas-zavetaji.html", "velas-masinas"),
                new("https://www.tet.lv/veikals/sadzives-tehnika/liela-sadzives-tehnika/trauku-mazgajamas-masinas-2.html", "trauku-mazgajamas-masinas"),
                new("https://www.tet.lv/veikals/sadzives-tehnika/liela-sadzives-tehnika/plitis.html", "plitis-un-cepeskrasnis"),
                new("https://www.tet.lv/veikals/sadzives-tehnika/liela-sadzives-tehnika/tvaika-nosuceji.html", "tvaika-nosuceji"),
                new("https://www.tet.lv/veikals/sadzives-tehnika/iebuvejama-sadzives-tehnika/cepeskrasnis.html", "iebuvejama-tehnika"),
                new("https://www.tet.lv/veikals/sadzives-tehnika/iebuvejama-sadzives-tehnika/ledusskapji-2.html", "iebuvejama-tehnika"),
                new("https://www.tet.lv/veikals/sadzives-tehnika/iebuvejama-sadzives-tehnika/plits-virsmas.html", "iebuvejama-tehnika"),
                new("https://www.tet.lv/veikals/sadzives-tehnika/iebuvejama-sadzives-tehnika/trauku-mazgajamas-masinas.html", "iebuvejama-tehnika"),
                new("https://www.tet.lv/veikals/televizori-un-audio/televizori-un-piederumi/televizori.html", "televizori"),
                new("https://www.tet.lv/veikals/televizori-un-audio/audio-2-3/soundbars.html", "audio"),
                new("https://www.tet.lv/veikals/televizori-un-audio/audio-2-3/skalruni.html", "audio"),
                new("https://www.tet.lv/veikals/viedierices/viedpulksteni-un-aproces/viedie-pulksteni.html", "viedpulksteni"),
                new("https://www.tet.lv/veikals/datortehnika/spelu-konsoles-un-piederumi/spelu-konsoles.html", "spelu-konsoles"),
                new("https://www.tet.lv/veikals/datortehnika/projektori-un-ekrani/projektori.html", "projektori"),
                new("https://www.tet.lv/veikals/sadzives-tehnika/maza-sadzives-tehnika/puteklu-suceji.html", "puteklu-suceji"),
                new("https://www.tet.lv/veikals/sadzives-tehnika/maza-sadzives-tehnika/roboti-puteklu-suceji.html", "puteklu-suceji"),
            ]),

        Unavailable("1a", "1a.lv", "https://www.1a.lv/"),
        Unavailable("rdveikals", "RD Electronics", "https://www.rdveikals.lv/"),
        Unavailable("dateks", "Dateks", "https://www.dateks.lv/"),
        Unavailable("ksenukai", "Ksenukai", "https://www.ksenukai.lv/"),
        Unavailable("220", "220.lv", "https://220.lv/"),
        Unavailable("balticdata", "Balticdata", "https://www.balticdata.lv/"),
    ];

    public static StoreDefinition? Find(string slug) => All.FirstOrDefault(s => s.Slug == slug);

    private static StoreDefinition Unavailable(string slug, string name, string homepage) =>
        new(slug, name, homepage, StoreStatus.Unavailable,
            "Veikals bloķē automatizētu piekļuvi (robotu aizsardzība), tāpēc dati netiek vākti.",
            [], [], null, []);
}
