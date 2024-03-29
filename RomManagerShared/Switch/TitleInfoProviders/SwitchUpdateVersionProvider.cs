﻿using RomManagerShared.Base;using RomManagerShared.Interfaces;using RomManagerShared.Switch.Configuration;using RomManagerShared.Utils;using System.Text.Json;
namespace RomManagerShared.Switch.TitleInfoProviders;

public class SwitchUpdateVersionProvider : IUpdateVersionProvider<SwitchConsole>
{
    public string Source { get; set; }    private Dictionary<string, Dictionary<string, DateTime>> versionDatabase;
    public SwitchUpdateVersionProvider(string versionFilepath)
    {
        Source = versionFilepath;
        if (string.IsNullOrEmpty(versionFilepath))
        {
            FileUtils.Log("switch version filepath is null");
            return;
        }    }    public async Task LoadVersionDatabaseAsync()
    {
        if (versionDatabase is not null)
            return;
        if (Source == null)
        {
            return;
        }
        if (!File.Exists(Source))
            await FileDownloader.DownloadSwitchVersionsFile();
        try
        {
            string jsonContent = File.ReadAllText(Source);
            versionDatabase = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, DateTime>>>(jsonContent);
        }
        catch (Exception ex)
        {
            FileUtils.Log($"Error loading versions.json: {ex.Message}");
            throw;
        }        versionDatabase ??= [];
    }
    public async Task<Update> GetLatestVersion(string titleId)
    {
        string commonTitleId = titleId[..^3];
        commonTitleId += "000";
        string latestVersion = "0";
        commonTitleId = commonTitleId.ToLower();
        var titleidexists = versionDatabase.TryGetValue(commonTitleId, out var versions);
        if (titleidexists)
        {            foreach (var version in versions)
            {
                if (string.Compare(version.Key.ToString(), latestVersion) > 0)
                {
                    latestVersion = version.Key.ToString();
                }
            }
        }
        SwitchUpdate su = new()
        {
            Version = latestVersion
        };
        return su; // Return "-1" if title ID not found
    }}
