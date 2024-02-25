using HtmlAgilityPack;
using RomManagerShared.Base;
using System.Text.Json;
namespace RomManagerShared.PS4;

public class PS4PKGUpdateAndDLCChecker
{
    public PS4PKGUpdateAndDLCChecker()
    {
        CheckedTitleIDUpdates = [];
        CheckedTitleIDDLC = [];
    }    public List<string> CheckedTitleIDUpdates { get; set; }
    public List<string> CheckedTitleIDDLC { get; set; }    public async Task<Rom?> CheckForUpdate(Rom ps4rom)
    {
        if (CheckedTitleIDUpdates.Contains(ps4rom.TitleID)) { return null; }
        CheckedTitleIDUpdates.Add(ps4rom.TitleID);
        //the pkg tools implementation for checking the update is very slow for some reason. 
        //TODO: try to reimplement it with httpclient instead of webclient
        var item = PS4_Tools.PKG.Official.CheckForUpdate(ps4rom.TitleID);
        if (item is not null)
        {
            PS4Update update = new()
            {
                TitleID = item.Titleid,
                ProductCode = item.Tag.Package.Content_id,
                Size = item.Tag.Package.Manifest_item.originalFileSize,
                Version = item.Tag.Package.Version
            };
            update.AddTitleName(item.Tag.Package.Paramsfo.Title);
            long minfirm = Convert.ToInt64(item.Tag.Package.System_ver);
            update.MinimumFirmware = PS4Utils.SystemFirmwareLongToString(minfirm);
            return update;
        }
        return null;
    }
    //used to iterate every region to find the correct page.
    //can probably be done with knowing the pkg region then try a country from it, but i'm not sure if this is consistent
    string[] northAmericaRegions = { "en-us", "en-ca" };
    string[] europeRegions = {
"en-mt", "en-gb", "en-fi", "en-hr", "en-pl", "en-no", "en-cy", "en-cz",
"en-ie", "en-se", "en-ro", "en-sk", "en-si", "en-is", "de-at", "nl-be",
"fr-be", "fr-ca", "da-dk", "en-dk", "fi-fi", "fr-fr", "de-de", "en-gr",
"en-hu", "it-it", "fr-lu", "de-lu", "nl-nl", "no-no", "pl-pl", "pt-pt",
"es-es", "sv-se"
};    //each product page contains jsons containing the game details and dlcs, so this can be expanded to get almost every other pkg property
    public async Task<List<Rom>> GetDLCList(Rom ps4rom)
    {
        string url = string.Empty;
        List<Rom> dlcs = [];        if (CheckedTitleIDDLC.Contains(ps4rom.TitleID)) { return dlcs; }
        CheckedTitleIDDLC.Add(ps4rom.TitleID);        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false
        };
        HttpClient client = new(handler);        string[] priorityRegions = ps4rom.Regions.Contains(Region.USA)
            ? northAmericaRegions.Concat(europeRegions).ToArray()
            : europeRegions.Concat(northAmericaRegions).ToArray();        foreach (var regioncode in priorityRegions)
        {
            url = $"https://store.playstation.com/{regioncode}/product/{ps4rom.ProductCode}";            var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));            if (response.IsSuccessStatusCode)
            {
                break;
            }
            else if ((int)response.StatusCode is >= 300 and < 399)
            {
                continue;
            }
        }
        string html = await client.GetStringAsync(url);        HtmlDocument doc = new();
        doc.LoadHtml(html);        HtmlNode scriptNode = doc.DocumentNode.SelectSingleNode("//div[@class='pdp-add-ons']//script[@type='application/json']");
        if (scriptNode != null)
        {
            string jsonData = scriptNode.InnerText; JsonDocument jsonDoc = JsonDocument.Parse(jsonData);
            JsonElement root = jsonDoc.RootElement;            if (root.TryGetProperty("cache", out JsonElement cacheElement))
            {
                foreach (JsonProperty cacheItem in cacheElement.EnumerateObject())
                {
                    PS4DLC rom = new();                    if (cacheItem.Name.StartsWith("Product:"))
                    {
                        var productData = cacheItem.Value;                        rom.ProductCode = productData.GetProperty("id").GetString();
                        rom.TitleID = PS4Utils.GetTitleIDFromProductCode(rom.ProductCode);
                        rom.Images.Add(productData.GetProperty("boxArt").GetProperty("url").GetString());
                        //  rom.Rating = productData.GetProperty("contentRating").GetProperty("name").GetString();
                        //   rom.Type = productData.GetProperty("localizedStoreDisplayClassification").GetString();
                        rom.AddTitleName(productData.GetProperty("name").GetString());
                        //will eventually be used for ps5
                        //Platforms = productData.GetProperty("platforms").EnumerateArray().Select(p => p.GetString()).ToArray();
                        JsonElement localizedGenresElement = productData.GetProperty("localizedGenres");
                        if (localizedGenresElement.ValueKind == JsonValueKind.Array)
                        {
                            var Genres = localizedGenresElement.EnumerateArray()
                                .Select(genreElement => genreElement.GetProperty("value").GetString())
                                .ToList();
                            Console.WriteLine(Genres);
                        }
                        dlcs.Add(rom);
                    }
                }            }
        }
        return dlcs;
    }
    public async Task<List<Rom>> GetMissingDLC(Rom romToCheck, IEnumerable<Rom> localroms, List<Rom> DlcList)
    {
        List<Rom> relatedRoms = localroms
            .Where(rom => rom.TitleID == romToCheck.TitleID && rom != romToCheck && rom is PS4DLC)
            .ToList();        List<Rom> missingDLCs = [];        foreach (var dlc in DlcList)
        {
            bool productCodeExists = false;            foreach (var relatedRom in relatedRoms)
            {
                if (relatedRom.ProductCode == dlc.ProductCode)
                {
                    productCodeExists = true;
                    break;
                }
            }            if (!productCodeExists)
            {
                missingDLCs.Add(dlc);
            }
        }        return missingDLCs;
    }}