using System;
using System.Collections.Generic;
using System.Net;
using HtmlAgilityPack;
namespace RomManagerShared.WiiU;
public class TitleDTO
{
    public string? TitleID { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public string? Versions { get; set; }
    public string? Region { get; set; }
    public string? CompanyCode { get; set; }
    public string? ProductCode { get; set; }
    public string TitleType { get; set; }
}
public class TableData
{
    public List<string> ColumnNames { get; set; }
    public List<List<string>> Rows { get; set; }
}
public class WiiUWikiBrewScraper
{
    public static List<TitleDTO> titles;
    public async static Task<List<TitleDTO>> ScrapeTitles()
    {
        titles = [];

        // Download the HTML content of the page
        string htmlContent =await DownloadHtmlContent("http://wiiubrew.org/wiki/Title_database");

        if (!string.IsNullOrEmpty(htmlContent))
        {
            // Load the HTML content into HtmlAgilityPack's HtmlDocument
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            // Find all <table> elements
            var tables = doc.DocumentNode.SelectNodes("//table");

            if (tables != null)
            {
                // Loop through each <table> element
                foreach (var table in tables)
                {
                    // Extract data from each <table> element
                    var tableData = ExtractTableData(table);
                    // Add extracted data to the list of titles
                    titles.AddRange(ProcessTableData(tableData));
                }
            }
        }
        else
        {
            Console.WriteLine("Failed to retrieve HTML content from the URL.");
        }

        return titles;
    }

    private async static Task<string> DownloadHtmlContent(string url)
    {
        try
        {
            using HttpClient client = new HttpClient();
            string html = await client.GetStringAsync(url);            return html;        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading HTML content: {ex.Message}");
        }
        return await Task.FromResult(string.Empty);
    }


    private static List<TableData> ExtractTableData(HtmlNode table)
    {
        List<TableData> tablesData = new List<TableData>();

        // Find all <tr> elements within the table
        var rows = table.SelectNodes(".//tr");

        if (rows != null)
        {
            // Extract column names from the first row
            var headerRow = rows.First();
            var headerCells = headerRow.SelectNodes(".//th");
            List<string> columnNames = new List<string>();
            if (headerCells != null)
            {
                foreach (var cell in headerCells)
                {
                    columnNames.Add(cell.InnerText.Trim());
                }
            }

            TableData tableData = new TableData();
            tableData.ColumnNames = columnNames;

            // Extract data from subsequent rows
            List<List<string>> rowDataList = new List<List<string>>();
            foreach (var row in rows.Skip(1)) // Skip header row
            {
                var cells = row.SelectNodes(".//td");
                List<string> rowData = new List<string>();
                if (cells != null)
                {
                    foreach (var cell in cells)
                    {
                        rowData.Add(cell.InnerText.Trim());
                    }
                }
                rowDataList.Add(rowData);
            }
            tableData.Rows = rowDataList;

            tablesData.Add(tableData);
        }

        return tablesData;
    }

    private static List<TitleDTO> ProcessTableData(List<TableData> tablesData)
    {
        List<TitleDTO> titles = new List<TitleDTO>();

        foreach (var tableData in tablesData)
        {
            List<string> columnNames = tableData.ColumnNames;

            foreach (var rowData in tableData.Rows)
            {
                TitleDTO title = new TitleDTO();

                for (int i = 0; i < Math.Min(columnNames.Count, rowData.Count); i++)
                {
                    string columnName = columnNames[i];
                    string cellValue = rowData[i];

                    if (columnName.ToLower().Contains("Title ID".ToLower()))
                    {
                        title.TitleID = cellValue.Replace("-", "") ;

                        title.Description = cellValue;
                        switch (title.TitleID.Substring(0, 8))
                        {
                            case "00050010":
                                title.TitleType = "System Application Titles";
                                break;
                            case "0005001B":
                                title.TitleType = "System Data Archive Titles";
                                break;
                            case "00050030":
                                title.TitleType = "System Applet Titles";
                                break;
                            case "00050000":
                                title.TitleType = "eShop and disc titles";
                                break;
                            case "0005000C":
                                title.TitleType = "eShop title DLC";
                                break;
                            case "0005000E":
                                title.TitleType = "eShop title updates";
                                break;
                            case "00050002":
                                title.TitleType = "Kiosk Interactive Demo and eShop Demo";
                                break;
                            case "00000007":
                            case "000700":
                                title.TitleType = "Virtual Wii titles";
                                break;
                            default:
                                title.TitleType = "Unknown";
                                break;
                        }
                    }
                

                    if (columnName.Contains("Description"))
                        title.Description = cellValue.Replace("\n"," ");
                    if (columnName.Contains("Notes"))
                        title.Notes = cellValue;
                    if (columnName.Contains("Versions"))
                        title.Versions = cellValue;
                    if (columnName.Contains("Region"))
                        title.Region = cellValue;
                    if (columnName.Contains("Company Code"))
                        title.CompanyCode = cellValue;
                    if (columnName.Contains("Product Code"))
                    {if (cellValue.Length < 7)
                            title.ProductCode = null;
                    else
                        title.ProductCode = cellValue;
                    }
                }

                titles.Add(title);
            }
        }

        return titles;
    }
}
