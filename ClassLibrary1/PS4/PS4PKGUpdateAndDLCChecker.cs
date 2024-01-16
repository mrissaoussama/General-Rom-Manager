using HtmlAgilityPack;
using Microsoft.Extensions.Logging.Abstractions;
using PS4_Tools.LibOrbis.SFO;
using RomManagerShared.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RomManagerShared.PS4
{
    public class PS4PKGUpdateAndDLCChecker
    {
        public PS4PKGUpdateAndDLCChecker()
        {
            CheckedTitleIDUpdates = [];
            CheckedTitleIDDLC = [];
        }

        public HashSet<string> CheckedTitleIDUpdates { get; set; }
        public HashSet<string> CheckedTitleIDDLC { get; set; }


        public async Task<Rom?> CheckForUpdate(Rom ps4rom)
        {if(CheckedTitleIDUpdates.Contains(ps4rom.TitleID)) { return null; }
            CheckedTitleIDUpdates.Add(ps4rom.TitleID);
            //the pkg tools implementation for checking the update is very slow for some reason. 
            //TODO: try to reimplement it with httpclient instead of webclient
                var item = PS4_Tools.PKG.Official.CheckForUpdate(ps4rom.TitleID);
            if (item is not null)
            {
                PS4Update update = new();
                update.TitleID=item.Titleid;
                update.ProductCode = item.Tag.Package.Content_id;
                update.Size = item.Tag.Package.Manifest_item.originalFileSize;
                update.Version = item.Tag.Package.Version;
                update.TitleName = item.Tag.Package.Paramsfo.Title;
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
};

        //each product page contains jsons containing the game details and dlcs, so this can be expanded to get almost every other pkg property
        public async Task<List<Rom>> GetDLCList(Rom ps4rom)
        {
            string url = string.Empty;
            List<Rom> dlcs = new();

            if (CheckedTitleIDDLC.Contains(ps4rom.TitleID)) { return dlcs; }
            CheckedTitleIDDLC.Add(ps4rom.TitleID);

            var handler = new HttpClientHandler();
            handler.AllowAutoRedirect = false;

            HttpClient client = new HttpClient(handler);

            string[] priorityRegions;

            if (ps4rom.Region == "US")
            {
                priorityRegions = northAmericaRegions.Concat(europeRegions).ToArray();
            }
            else
            {
                priorityRegions = europeRegions.Concat(northAmericaRegions).ToArray();
            }

            foreach (var regioncode in priorityRegions)
            {
                url = $"https://store.playstation.com/{regioncode}/product/{ps4rom.ProductCode}";

                var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));

                if (response.IsSuccessStatusCode)
                {
                    break;
                }
                else if ((int)response.StatusCode >= 300 && (int)response.StatusCode < 399)
                {
                    continue;
                }
            }
            string html = await client.GetStringAsync(url);

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            HtmlNode scriptNode = doc.DocumentNode.SelectSingleNode("//div[@class='pdp-add-ons']//script[@type='application/json']");
            if (scriptNode != null)
            {
                string jsonData = scriptNode.InnerText; JsonDocument jsonDoc = JsonDocument.Parse(jsonData);
                JsonElement root = jsonDoc.RootElement;

                if (root.TryGetProperty("cache", out JsonElement cacheElement))
                {
                    foreach (JsonProperty cacheItem in cacheElement.EnumerateObject())
                    {
                        PS4DLC rom = new();

                        if (cacheItem.Name.StartsWith("Product:"))
                        {
                            var productData = cacheItem.Value;


                            rom.ProductCode = productData.GetProperty("id").GetString();
                            rom.TitleID = PS4Utils.GetTitleIDFromProductCode(rom.ProductCode);
                            rom.Images.Add(productData.GetProperty("boxArt").GetProperty("url").GetString());
                            rom.Rating = productData.GetProperty("contentRating").GetProperty("name").GetString();
                            rom.Type = productData.GetProperty("localizedStoreDisplayClassification").GetString();
                            rom.TitleName = productData.GetProperty("name").GetString();
                            //will eventually be used for ps5
                            //Platforms = productData.GetProperty("platforms").EnumerateArray().Select(p => p.GetString()).ToArray();

                            JsonElement localizedGenresElement = productData.GetProperty("localizedGenres");
                            if (localizedGenresElement.ValueKind == JsonValueKind.Array)
                            {
                                rom.Genres = localizedGenresElement.EnumerateArray()
                                    .Select(genreElement => genreElement.GetProperty("value").GetString())
                                    .ToList();
                            }

                           dlcs.Add(rom);
                        }
                    }

                }
            }

                    return dlcs;
        }
        public async Task<List<Rom>> GetMissingDLC(Rom romToCheck, List<Rom> localroms, List<Rom> DlcList)
        {
            List<Rom> relatedRoms = localroms
                .Where(rom => rom.TitleID == romToCheck.TitleID && rom != romToCheck && rom is PS4DLC)
                .ToList();

            List<Rom> missingDLCs = new List<Rom>();

            foreach (var dlc in DlcList)
            {
                bool productCodeExists = false;

                foreach (var relatedRom in relatedRoms)
                {
                    if (relatedRom.ProductCode == dlc.ProductCode)
                    {
                        productCodeExists = true;
                        break;
                    }
                }

                if (!productCodeExists)
                {
                    missingDLCs.Add(dlc);
                }
            }

            return missingDLCs;
        }


    }

}
