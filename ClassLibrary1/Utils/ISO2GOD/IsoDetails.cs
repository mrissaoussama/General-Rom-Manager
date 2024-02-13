using RomManagerShared.Utils.ISO2GOD.Chilano.Xbox360.Graphics;
using RomManagerShared.Utils.ISO2GOD.Chilano.Xbox360.IO;
using RomManagerShared.Utils.ISO2GOD.Chilano.Xbox360.Iso;
using RomManagerShared.Utils.ISO2GOD.Chilano.Xbox360.Xbe;
using RomManagerShared.Utils.ISO2GOD.Chilano.Xbox360.Xdbf;
using RomManagerShared.Utils.ISO2GOD.Chilano.Xbox360.Xex;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace RomManagerShared.Utils.ISO2GOD;

internal class IsoDetails
{
    private IsoDetailsArgs args;

    private FileStream f;

    private GDF iso;

    public IsoDetails()
    {

    }

    public IsoDetailsResults? IsoDetails_DoWork(DoWorkEventArgs e)
    {
        if (e.Argument == null)
        {
            throw new ArgumentNullException("A populated instance of IsoDetailsArgs must be passed.");
        }
        args = (IsoDetailsArgs)e.Argument;
        if (!openIso())
        {
            return null;
        }
        IsoDetailsPlatform isoDetailsPlatform;
        if (iso.Exists("default.xex"))
        {
            isoDetailsPlatform = IsoDetailsPlatform.Xbox360;
        }
        else
        {
            if (!iso.Exists("default.xbe"))
            {
                return null;
            }
            isoDetailsPlatform = IsoDetailsPlatform.Xbox;
        }
        return isoDetailsPlatform switch
        {
            IsoDetailsPlatform.Xbox => readXbeFromIso(e),
            IsoDetailsPlatform.Xbox360 => readXexFromIso(e),
            _ => null,
        };
    }

    private bool openIso()
    {
        try
        {
            f = new FileStream(args.PathISO, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            iso = new GDF(f);
        }
        catch (IOException ex)
        {
            FileUtils.Log(ex.Message);
        }
        return true;
    }
    public static IsoDetailsResults readXbeFromFile(string xbefilepath)
    {
        IsoDetailsResults isoDetailsResults = null;
        byte[] array = null;
        try
        {
            array = File.ReadAllBytes(xbefilepath);
        }
        catch (Exception ex)
        {
            FileUtils.Log(ex.Message);
            return null;
        }
        using (XbeInfo xbeInfo = new(array))
        {
            if (!xbeInfo.IsValid)
            {
                return null;
            }
            isoDetailsResults = new IsoDetailsResults(xbeInfo.Certifcate.TitleName, xbeInfo.Certifcate.TitleID, xbeInfo.Certifcate.DiskNumber != 0 ? xbeInfo.Certifcate.DiskNumber.ToString() : "1")
            {
                DiscCount = "1"
            };
            foreach (XbeSection section in xbeInfo.Sections)
            {
                if (!(section.Name == "$$XSIMAGE"))
                {
                    continue;
                }
                try
                {
                    XPR xPR = new(section.Data);
                    DDS dDS = xPR.ConvertToDDS(64, 64);
                    Bitmap bitmap = new(64, 64);
                    switch (xPR.Format)
                    {
                        case XPRFormat.ARGB:
                            bitmap = (Bitmap)dDS.GetImage(DDSType.ARGB);
                            break;
                        case XPRFormat.DXT1:
                            bitmap = (Bitmap)dDS.GetImage(DDSType.DXT1);
                            break;
                    }
                    MemoryStream memoryStream = new();
                    bitmap.Save(memoryStream, ImageFormat.Png);
                    isoDetailsResults.Thumbnail = (Image)bitmap.Clone();
                    isoDetailsResults.RawThumbnail = (byte[])memoryStream.ToArray().Clone();
                    bitmap.Dispose();
                    memoryStream.Dispose();

                }
                catch (Exception ex2)
                {
                    FileUtils.Log(ex2.Message);
                }
            }
            if (isoDetailsResults.Thumbnail == null)
            {
                foreach (XbeSection section2 in xbeInfo.Sections)
                {
                    if (!(section2.Name == "$$XTIMAGE"))
                    {
                        continue;
                    }
                    try
                    {
                        XPR xPR2 = new(section2.Data);
                        DDS dDS2 = xPR2.ConvertToDDS(128, 128);
                        Bitmap bitmap2 = new(128, 128);
                        switch (xPR2.Format)
                        {
                            case XPRFormat.ARGB:
                                bitmap2 = (Bitmap)dDS2.GetImage(DDSType.ARGB);
                                break;
                            case XPRFormat.DXT1:
                                bitmap2 = (Bitmap)dDS2.GetImage(DDSType.DXT1);
                                break;
                        }
                        Image image = new Bitmap(64, 64);
                        Graphics graphics = Graphics.FromImage(image);
                        graphics.DrawImage(bitmap2, 0, 0, 64, 64);
                        MemoryStream memoryStream2 = new();
                        image.Save(memoryStream2, ImageFormat.Png);
                        isoDetailsResults.Thumbnail = (Image)image.Clone();
                        isoDetailsResults.RawThumbnail = (byte[])memoryStream2.ToArray().Clone();
                        memoryStream2.Dispose();
                        bitmap2.Dispose();
                        graphics.Dispose();

                    }
                    catch (Exception ex3)
                    {
                        FileUtils.Log(ex3.Message);
                    }
                }
            }
        }
        return isoDetailsResults;
    }

    public IsoDetailsResults readXbeFromIso(DoWorkEventArgs e)
    {
        IsoDetailsResults isoDetailsResults = null;
        byte[] array = null;
        try
        {
            array = iso.GetFile("default.xbe");
        }
        catch (Exception ex)
        {
            FileUtils.Log(ex.Message);
            return null;
        }
        using (XbeInfo xbeInfo = new(array))
        {
            if (!xbeInfo.IsValid)
            {
                return null;
            }
            isoDetailsResults = new IsoDetailsResults(xbeInfo.Certifcate.TitleName, xbeInfo.Certifcate.TitleID, xbeInfo.Certifcate.DiskNumber != 0 ? xbeInfo.Certifcate.DiskNumber.ToString() : "1")
            {
                DiscCount = "1"
            };
            foreach (XbeSection section in xbeInfo.Sections)
            {
                if (!(section.Name == "$$XSIMAGE"))
                {
                    continue;
                }
                try
                {
                    XPR xPR = new(section.Data);
                    DDS dDS = xPR.ConvertToDDS(64, 64);
                    Bitmap bitmap = new(64, 64);
                    switch (xPR.Format)
                    {
                        case XPRFormat.ARGB:
                            bitmap = (Bitmap)dDS.GetImage(DDSType.ARGB);
                            break;
                        case XPRFormat.DXT1:
                            bitmap = (Bitmap)dDS.GetImage(DDSType.DXT1);
                            break;
                    }
                    MemoryStream memoryStream = new();
                    bitmap.Save(memoryStream, ImageFormat.Png);
                    isoDetailsResults.Thumbnail = (Image)bitmap.Clone();
                    isoDetailsResults.RawThumbnail = (byte[])memoryStream.ToArray().Clone();
                    bitmap.Dispose();
                    memoryStream.Dispose();

                }
                catch (Exception ex2)
                {
                    FileUtils.Log(ex2.Message);
                }
            }
            if (isoDetailsResults.Thumbnail == null)
            {
                foreach (XbeSection section2 in xbeInfo.Sections)
                {
                    if (!(section2.Name == "$$XTIMAGE"))
                    {
                        continue;
                    }
                    try
                    {
                        XPR xPR2 = new(section2.Data);
                        DDS dDS2 = xPR2.ConvertToDDS(128, 128);
                        Bitmap bitmap2 = new(128, 128);
                        switch (xPR2.Format)
                        {
                            case XPRFormat.ARGB:
                                bitmap2 = (Bitmap)dDS2.GetImage(DDSType.ARGB);
                                break;
                            case XPRFormat.DXT1:
                                bitmap2 = (Bitmap)dDS2.GetImage(DDSType.DXT1);
                                break;
                        }
                        Image image = new Bitmap(64, 64);
                        Graphics graphics = Graphics.FromImage(image);
                        graphics.DrawImage(bitmap2, 0, 0, 64, 64);
                        MemoryStream memoryStream2 = new();
                        image.Save(memoryStream2, ImageFormat.Png);
                        isoDetailsResults.Thumbnail = (Image)image.Clone();
                        isoDetailsResults.RawThumbnail = (byte[])memoryStream2.ToArray().Clone();
                        memoryStream2.Dispose();
                        bitmap2.Dispose();
                        graphics.Dispose();

                    }
                    catch (Exception ex3)
                    {
                        FileUtils.Log(ex3.Message);
                    }
                }
            }
        }
        return isoDetailsResults;
    }

    public IsoDetailsResults readXexFromIso(DoWorkEventArgs e)
    {
        IsoDetailsResults isoDetailsResults = null;
        byte[] array = null;
        string text = null;
        string text2 = null;
        try
        {
            array = iso.GetFile("default.xex");
            text2 = args.PathTemp;
            text = text2 + "default.xex";
            if (array == null || array.Length == 0)
            {
                return null;
            }
            File.WriteAllBytes(text, array);
        }
        catch (Exception ex)
        {
            FileUtils.Log(ex.Message);
            return null;
        }
        using (XexInfo xexInfo = new(array))
        {
            if (!xexInfo.IsValid)
            {
                return null;
            }
            if (xexInfo.Header.ContainsKey(XexInfoFields.ExecutionInfo))
            {
                XexExecutionInfo xexExecutionInfo = (XexExecutionInfo)xexInfo.Header[XexInfoFields.ExecutionInfo];
                isoDetailsResults = new IsoDetailsResults("", DataConversion.BytesToHexString(xexExecutionInfo.TitleID), DataConversion.BytesToHexString(xexExecutionInfo.MediaID), xexExecutionInfo.Platform.ToString(), xexExecutionInfo.ExecutableType.ToString(), xexExecutionInfo.DiscNumber.ToString(), xexExecutionInfo.DiscCount.ToString(), null);
            }
        }
        Process process = new()
        {
            EnableRaisingEvents = false
        };
        process.StartInfo.FileName = args.PathXexTool;
        if (!File.Exists(process.StartInfo.FileName))
        {
            return null;
        }
        process.StartInfo.WorkingDirectory = text2;
        process.StartInfo.Arguments = "-d . default.xex";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = false;
        process.StartInfo.CreateNoWindow = true;
        try
        {
            process.Start();
            process.WaitForExit();
            process.Close();
        }
        catch (Win32Exception)
        {
            return null;
        }
        if (File.Exists(text2 + isoDetailsResults.TitleID))
        {
            Xdbf xdbf = new(File.ReadAllBytes(text2 + isoDetailsResults.TitleID));
            try
            {
                byte[] resource = xdbf.GetResource(XdbfResource.Thumb, XdbfResourceType.TitleInfo);
                MemoryStream stream = new(resource);
                Image image = Image.FromStream(stream);
                isoDetailsResults.Thumbnail = (Image)image.Clone();
                isoDetailsResults.RawThumbnail = (byte[])resource.Clone();
                image.Dispose();
            }
            catch (Exception)
            {
                try
                {
                    byte[] resource2 = xdbf.GetResource(XdbfResource.Thumb, XdbfResourceType.Achievement);
                    MemoryStream stream2 = new(resource2);
                    Image image2 = Image.FromStream(stream2);
                    isoDetailsResults.Thumbnail = (Image)image2.Clone();
                    isoDetailsResults.RawThumbnail = (byte[])resource2.Clone();
                    image2.Dispose();
                }
                catch (Exception ex)
                {
                    FileUtils.Log(ex.Message);
                }
            }
            try
            {
                MemoryStream memoryStream = new(xdbf.GetResource(1u, 3));
                memoryStream.Seek(17L, SeekOrigin.Begin);
                int count = memoryStream.ReadByte();
                isoDetailsResults.Name = Encoding.UTF8.GetString(memoryStream.ToArray(), 18, count);
                memoryStream.Close();
            }
            catch (Exception)
            {
                try
                {
                    MemoryStream memoryStream2 = new(xdbf.GetResource(1u, 0));
                    memoryStream2.Seek(17L, SeekOrigin.Begin);
                    int count2 = memoryStream2.ReadByte();
                    isoDetailsResults.Name = Encoding.UTF8.GetString(memoryStream2.ToArray(), 18, count2);
                    memoryStream2.Close();
                }
                catch (Exception)
                {
                    isoDetailsResults.Name = "Unable to read name.";
                }
            }
        }
        return isoDetailsResults;
    }
    public static IsoDetailsResults readXexFromFile(string xexfilepath)
    {
        IsoDetailsResults isoDetailsResults = null;
        byte[] array = null;
        string text = null;
        try
        {
            array = File.ReadAllBytes(xexfilepath);
            if (array == null || array.Length == 0)
            {
                return null;
            }
        }
        catch (Exception ex)
        {
            FileUtils.Log(ex.Message);
            return null;
        }
        using (XexInfo xexInfo = new(array))
        {
            if (!xexInfo.IsValid)
            {
                return null;
            }
            if (xexInfo.Header.ContainsKey(XexInfoFields.ExecutionInfo))
            {
                XexExecutionInfo xexExecutionInfo = (XexExecutionInfo)xexInfo.Header[XexInfoFields.ExecutionInfo];
                isoDetailsResults = new IsoDetailsResults("", DataConversion.BytesToHexString(xexExecutionInfo.TitleID), DataConversion.BytesToHexString(xexExecutionInfo.MediaID), xexExecutionInfo.Platform.ToString(), xexExecutionInfo.ExecutableType.ToString(), xexExecutionInfo.DiscNumber.ToString(), xexExecutionInfo.DiscCount.ToString(), null);
            }
        }
        Process process = new()
        {
            EnableRaisingEvents = false
        };
        process.StartInfo.FileName = RomManagerConfiguration.GetXexToolPath();
        if (!File.Exists(process.StartInfo.FileName))
        {
            return null;
        }
        process.StartInfo.WorkingDirectory = Path.GetDirectoryName(xexfilepath);
        process.StartInfo.Arguments = "-d . default.xex";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = false;
        process.StartInfo.CreateNoWindow = true;
        try
        {
            process.Start();
            process.WaitForExit();
            process.Close();
        }
        catch (Win32Exception)
        {
            return null;
        }
        var titleidfile = Path.GetDirectoryName(xexfilepath) + "\\" + isoDetailsResults.TitleID;
        if (File.Exists(titleidfile))
        {
            Xdbf xdbf = new(File.ReadAllBytes(titleidfile));
            try
            {
                byte[] resource = xdbf.GetResource(XdbfResource.Thumb, XdbfResourceType.TitleInfo);
                MemoryStream stream = new(resource);
                Image image = Image.FromStream(stream);
                isoDetailsResults.Thumbnail = (Image)image.Clone();
                isoDetailsResults.RawThumbnail = (byte[])resource.Clone();
                image.Dispose();
            }
            catch (Exception)
            {
                try
                {
                    byte[] resource2 = xdbf.GetResource(XdbfResource.Thumb, XdbfResourceType.Achievement);
                    MemoryStream stream2 = new(resource2);
                    Image image2 = Image.FromStream(stream2);
                    isoDetailsResults.Thumbnail = (Image)image2.Clone();
                    isoDetailsResults.RawThumbnail = (byte[])resource2.Clone();
                    image2.Dispose();
                }
                catch (Exception ex)
                {
                    FileUtils.Log(ex.Message);
                }
            }
            try
            {
                MemoryStream memoryStream = new(xdbf.GetResource(1u, 3));
                memoryStream.Seek(17L, SeekOrigin.Begin);
                int count = memoryStream.ReadByte();
                isoDetailsResults.Name = Encoding.UTF8.GetString(memoryStream.ToArray(), 18, count);
                memoryStream.Close();
            }
            catch (Exception)
            {
                try
                {
                    MemoryStream memoryStream2 = new(xdbf.GetResource(1u, 0));
                    memoryStream2.Seek(17L, SeekOrigin.Begin);
                    int count2 = memoryStream2.ReadByte();
                    isoDetailsResults.Name = Encoding.UTF8.GetString(memoryStream2.ToArray(), 18, count2);
                    memoryStream2.Close();
                }
                catch (Exception)
                {
                    isoDetailsResults.Name = "Unable to read name.";
                }
            }
        }
        return isoDetailsResults;
    }

}
