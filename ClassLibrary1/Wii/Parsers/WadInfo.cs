using System.Security.Cryptography;
using System.Text;
namespace RomManagerShared.Wii.Parsers;
//taken from showmiiwads
public class WadInfo
{
    public const int Headersize = 64;
    public string[] RegionCode = ["Japan", "USA", "Europe", "Region Free"];    /// <summary>
    /// Adds a padding to the next 64 bytes, if necessary
    /// </summary>
    /// <param name="size"></param>
    /// <returns></returns>
    public static int AddPadding(int value)
    {
        return AddPadding(value, 64);
    }
    /// <summary>
    /// Adds a padding to the given value, if necessary
    /// </summary>
    /// <param name="value"></param>
    /// <param name="padding"></param>
    /// <returns></returns>
    public static int AddPadding(int value, int padding)
    {
        if (value % padding != 0)
        {
            value += padding - (value % padding);
        }        return value;
    }
    /// <summary>
    /// Returns the Header of a Wadfile
    /// </summary>
    /// <param name="wadfile"></param>
    /// <returns></returns>
    public static byte[] GetHeader(byte[] wadfile)
    {
        byte[] Header = new byte[0x20];        for (int i = 0; i < Header.Length; i++)
        {
            Header[i] = wadfile[i];
        }        return Header;
    }    /// <summary>
    /// Returns the size of the Certificate
    /// </summary>
    /// <param name="wadfile"></param>
    /// <returns></returns>
    public static int GetCertSize(byte[] wadfile)
    {
        int size = int.Parse(wadfile[0x08].ToString("x2") + wadfile[0x09].ToString("x2") + wadfile[0x0a].ToString("x2") + wadfile[0x0b].ToString("x2"), System.Globalization.NumberStyles.HexNumber);
        return size;
    }    /// <summary>
    /// Returns the size of the Ticket
    /// </summary>
    /// <param name="wadfile"></param>
    /// <returns></returns>
    public static int GetTikSize(byte[] wadfile)
    {
        int size = int.Parse(wadfile[0x10].ToString("x2") + wadfile[0x11].ToString("x2") + wadfile[0x12].ToString("x2") + wadfile[0x13].ToString("x2"), System.Globalization.NumberStyles.HexNumber);
        return size;
    }    /// <summary>
    /// Returns the size of the TMD
    /// </summary>
    /// <param name="wadfile"></param>
    /// <returns></returns>
    public static int GetTmdSize(byte[] wadfile)
    {
        int size = int.Parse(wadfile[0x14].ToString("x2") + wadfile[0x15].ToString("x2") + wadfile[0x16].ToString("x2") + wadfile[0x17].ToString("x2"), System.Globalization.NumberStyles.HexNumber);
        return size;
    }    /// <summary>
    /// Returns the size of all Contents
    /// </summary>
    /// <param name="wadfile"></param>
    /// <returns></returns>
    public static int GetContentSize(byte[] wadfile)
    {
        int size = int.Parse(wadfile[0x18].ToString("x2") + wadfile[0x19].ToString("x2") + wadfile[0x1a].ToString("x2") + wadfile[0x1b].ToString("x2"), System.Globalization.NumberStyles.HexNumber);
        return size;
    }    /// <summary>
    /// Returns the size of the Footer
    /// </summary>
    /// <param name="wadfile"></param>
    /// <returns></returns>
    public static int GetFooterSize(byte[] wadfile)
    {
        int size = int.Parse(wadfile[0x1c].ToString("x2") + wadfile[0x1d].ToString("x2") + wadfile[0x1e].ToString("x2") + wadfile[0x1f].ToString("x2"), System.Globalization.NumberStyles.HexNumber);
        return size;
    }    /// <summary>
    /// Returns the position of the tmd in the wad file
    /// </summary>
    /// <param name="wadfile"></param>
    /// <returns></returns>
    public static int GetTmdPos(byte[] wadfile)
    {
        return Headersize + AddPadding(GetCertSize(wadfile)) + AddPadding(GetTikSize(wadfile));
    }    /// <summary>
    /// Returns the position of the ticket in the wad file, ticket or tmd
    /// </summary>
    /// <param name="wadfile"></param>
    /// <returns></returns>
    public static int GetTikPos(byte[] wadfile)
    {
        return Headersize + AddPadding(GetCertSize(wadfile));
    }
    /// <summary>
    /// Loads a file into a Byte Array
    /// </summary>
    /// <param name="sourcefile"></param>
    /// <returns></returns>
    public static byte[] LoadFileToByteArray(string sourcefile)
    {
        if (File.Exists(sourcefile))
        {
            using FileStream fs = new(sourcefile, FileMode.Open);
            byte[] filearray = new byte[fs.Length];
            fs.Read(filearray, 0, filearray.Length);
            return filearray;
        }
        else throw new FileNotFoundException("File couldn't be found:\r\n" + sourcefile);
    }    /// <summary>
    /// Returns the title ID of the wad file.
    /// </summary>
    /// <param name="wadfile"></param>
    /// <param name="type">0 = Tik, 1 = Tmd</param>
    /// <returns></returns>
    public string GetTitleID(string wadtiktmd, int type)
    {
        byte[] temp = LoadFileToByteArray(wadtiktmd);
        return GetTitleID(temp, type);
    }    /// <summary>
    /// Converts a Hex-String to Int
    /// </summary>
    /// <param name="hexstring"></param>
    /// <returns></returns>
    public static int HexStringToInt(string hexstring)
    {
        try { return int.Parse(hexstring, System.Globalization.NumberStyles.HexNumber); }
        catch { throw new Exception("An Error occured, maybe the Wad file is corrupt!"); }
    }
    /// <summary>
    /// Returns the title ID of the wad file.
    /// </summary>
    /// <param name="wadfile"></param>
    /// <param name="type">0 = Tik, 1 = Tmd</param>
    /// <returns></returns>
    public string GetTitleID(byte[] wadtiktmd, int type)
    {
        string channeltype = GetChannelType(wadtiktmd, type);
        int tikpos = 0;
        int tmdpos = 0;        if (IsThisWad(wadtiktmd) == true)
        {
            //It's a wad
            tikpos = GetTikPos(wadtiktmd);
            tmdpos = GetTmdPos(wadtiktmd);
        }        if (type == 1)
        {
            if (!channeltype.Contains("System:"))
            {
                string tmdid = Convert.ToChar(wadtiktmd[tmdpos + 0x190]).ToString() + Convert.ToChar(wadtiktmd[tmdpos + 0x191]).ToString() + Convert.ToChar(wadtiktmd[tmdpos + 0x192]).ToString() + Convert.ToChar(wadtiktmd[tmdpos + 0x193]).ToString();
                return tmdid;
            }
            else if (channeltype.Contains("IOS"))
            {
                int tmdid = HexStringToInt(wadtiktmd[tmdpos + 0x190].ToString("x2") + wadtiktmd[tmdpos + 0x191].ToString("x2") + wadtiktmd[tmdpos + 0x192].ToString("x2") + wadtiktmd[tmdpos + 0x193].ToString("x2"));
                return "IOS" + tmdid;
            }
            else return channeltype.Contains("System") ? "SYSTEM" : "";
        }
        else
        {
            if (!channeltype.Contains("System:"))
            {
                string tikid = Convert.ToChar(wadtiktmd[tikpos + 0x1e0]).ToString() + Convert.ToChar(wadtiktmd[tikpos + 0x1e1]).ToString() + Convert.ToChar(wadtiktmd[tikpos + 0x1e2]).ToString() + Convert.ToChar(wadtiktmd[tikpos + 0x1e3]).ToString();
                return tikid;
            }
            else if (channeltype.Contains("MIOS"))
            {
                return "MIOS";
            }
            else if (channeltype.Contains("BC"))
            {
                return "BC";
            }
            else if (channeltype.Contains("IOS"))
            {
                int tikid = HexStringToInt(wadtiktmd[tikpos + 0x1e0].ToString("x2") + wadtiktmd[tikpos + 0x1e1].ToString("x2") + wadtiktmd[tikpos + 0x1e2].ToString("x2") + wadtiktmd[tikpos + 0x1e3].ToString("x2"));
                return "IOS" + tikid;
            }
            else return channeltype.Contains("System") ? "SYSTEM" : "";
        }
    }    /// <summary>
    /// Returns the full title ID of the wad file as a hex string.
    /// </summary>
    /// <param name="wadfile"></param>
    /// <param name="type">0 = Tik, 1 = Tmd</param>
    /// <returns></returns>
    public string GetFullTitleID(byte[] wadtiktmd, int type)
    {
        int tikpos = 0;
        int tmdpos = 0;        if (IsThisWad(wadtiktmd) == true)
        {
            //It's a wad
            tikpos = GetTikPos(wadtiktmd);
            tmdpos = GetTmdPos(wadtiktmd);
        }        if (type == 1)
        {
            string tmdid = wadtiktmd[tmdpos + 0x18c].ToString("x2") +
                wadtiktmd[tmdpos + 0x18d].ToString("x2") +
                wadtiktmd[tmdpos + 0x18e].ToString("x2") +
                wadtiktmd[tmdpos + 0x18f].ToString("x2") +
                wadtiktmd[tmdpos + 0x190].ToString("x2") +
                wadtiktmd[tmdpos + 0x191].ToString("x2") +
                wadtiktmd[tmdpos + 0x192].ToString("x2") +
                wadtiktmd[tmdpos + 0x193].ToString("x2");
            return tmdid;
        }
        else
        {
            string tikid = wadtiktmd[tikpos + 0x1dc].ToString() +
                wadtiktmd[tikpos + 0x1dd].ToString() +
                wadtiktmd[tikpos + 0x1de].ToString() +
                wadtiktmd[tikpos + 0x1df].ToString() +
                wadtiktmd[tikpos + 0x1e0].ToString() +
                wadtiktmd[tikpos + 0x1e1].ToString() +
                wadtiktmd[tikpos + 0x1e2].ToString() +
                wadtiktmd[tikpos + 0x1e3].ToString();
            return tikid;
        }
    }    /// <summary>
    /// Returns the title for each language of a wad file.
    /// Order: Jap, Eng, Ger, Fra, Spa, Ita, Dut
    /// </summary>
    /// <param name="wadfile"></param>
    /// <returns></returns>
    public string[] GetChannelTitles(string wadfile)
    {
        byte[] wadarray = LoadFileToByteArray(wadfile);
        return GetChannelTitles(wadarray);
    }    /// <summary>
    /// Creates the Common Key
    /// </summary>
    /// <param name="fat">Must be "45e"</param>
    /// <param name="destination">Destination Path</param>
    public static void CreateCommonKey(string fat)
    {
        //What an effort, lol
        byte[] encryptedwater = [0x4d, 0x89, 0x21, 0x34, 0x62, 0x81, 0xe4, 0x02, 0x37, 0x36, 0xc4, 0xb4, 0xde, 0x40, 0x32, 0xab];
        byte[] key = [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, byte.Parse(fat.Remove(2), System.Globalization.NumberStyles.HexNumber), byte.Parse(fat.Remove(0, 2) + "0", System.Globalization.NumberStyles.HexNumber)];
        byte[] decryptedwater = new byte[10];        using var decryptkey = Aes.Create();
        decryptkey.Mode = CipherMode.CBC;
        decryptkey.Padding = PaddingMode.None;
        decryptkey.KeySize = 128;
        decryptkey.BlockSize = 128;
        decryptkey.Key = key;
        Array.Reverse(key);
        decryptkey.IV = key;        ICryptoTransform cryptor = decryptkey.CreateDecryptor();        using (MemoryStream memory = new(encryptedwater))
        {
            using CryptoStream crypto = new(memory, cryptor, CryptoStreamMode.Read);
            crypto.Read(decryptedwater, 0, 10);
        }        string water = BitConverter.ToString(decryptedwater).Replace("-", "").ToLower() + " ";        water = water.Insert(0, fat[2].ToString());
        water = water.Insert(2, fat[2].ToString());
        water = water.Insert(7, fat[2].ToString());
        water = water.Insert(11, fat[2].ToString());        water = water.Insert(7, fat[1].ToString());
        water = water.Insert(10, fat[1].ToString());
        water = water.Insert(18, fat[1].ToString());
        water = water.Insert(19, fat[1].ToString());        water = water.Insert(3, fat[0].ToString());
        water = water.Insert(15, fat[0].ToString());
        water = water.Insert(16, fat[0].ToString());
        water = water.Insert(22, fat[0].ToString());        byte[] cheese = new byte[16];
        int count = -1;        for (int i = 0; i < 32; i += 2)
            cheese[++count] = byte.Parse(water.Remove(0, i).Remove(2), System.Globalization.NumberStyles.HexNumber);
        Directory.CreateDirectory(Path.GetDirectoryName(RomManagerConfiguration.GetWiiWadCommonKeyPath()));
        using FileStream keystream = new(RomManagerConfiguration.GetWiiWadCommonKeyPath(), FileMode.Create);
        keystream.Write(cheese, 0, cheese.Length);
    }
    /// <summary>
    /// Decrypts the given content
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    public static byte[] DecryptContent(byte[] wadfile, int contentcount, byte[] titlekey)
    {
        var wadinfo = new WadInfo();
        int tmdpos = GetTmdPos(wadfile);
        byte[] iv = new byte[16];
        string[,] continfo = wadinfo.GetContentInfo(wadfile);
        int contentsize = Convert.ToInt32(continfo[contentcount, 3]);
        int paddedsize = AddPadding(contentsize, 16);        int contentpos = 64 + AddPadding(GetCertSize(wadfile)) + AddPadding(GetTikSize(wadfile)) + AddPadding(WadInfo.GetTmdSize(wadfile));        for (int x = 0; x < contentcount; x++)
        {
            contentpos += AddPadding(Convert.ToInt32(continfo[x, 3]));
        }        iv[0] = wadfile[tmdpos + 0x1e8 + (0x24 * contentcount)];
        iv[1] = wadfile[tmdpos + 0x1e9 + (0x24 * contentcount)];        using var decrypt = Aes.Create();        decrypt.Mode = CipherMode.CBC;
        decrypt.Padding = PaddingMode.None;
        decrypt.KeySize = 128;
        decrypt.BlockSize = 128;
        decrypt.Key = titlekey;
        decrypt.IV = iv;        ICryptoTransform cryptor = decrypt.CreateDecryptor();        MemoryStream memory = new(wadfile, contentpos, paddedsize);
        CryptoStream crypto = new(memory, cryptor, CryptoStreamMode.Read);        bool fullread = false;
        byte[] buffer = new byte[16384];
        byte[] cont = new byte[1];        using (MemoryStream ms = new())
        {
            while (fullread == false)
            {
                int len = 0;
                if ((len = crypto.Read(buffer, 0, buffer.Length)) <= 0)
                {
                    fullread = true;
                    cont = ms.ToArray();
                }
                ms.Write(buffer, 0, len);
            }
        }        memory.Close();
        crypto.Close();        Array.Resize(ref cont, contentsize);        return cont;
    }    /// <summary>
    /// Returns the title for each language of a wad file.
    /// Order: Jap, Eng, Ger, Fra, Spa, Ita, Dut
    /// </summary>
    /// <param name="wadfile"></param>
    /// <returns></returns>
    public string[] GetChannelTitles(byte[] wadfile)
    {
        if (!File.Exists(RomManagerConfiguration.GetWiiWadCommonKeyPath()))
        { CreateCommonKey("45e"); }        if (File.Exists(RomManagerConfiguration.GetWiiWadCommonKeyPath()))
        {
            string channeltype = GetChannelType(wadfile, 0);            if (!channeltype.Contains("System:"))
            {
                if (!channeltype.Contains("Hidden"))
                {
                    string[] titles = new string[8];                    string[,] conts = GetContentInfo(wadfile);
                    byte[] titlekey = GetTitleKey(wadfile);
                    int nullapp = 0;                    for (int i = 0; i < conts.GetLength(0); i++)
                    {
                        if (conts[i, 1] == "00000000")
                            nullapp = i;
                    }                    byte[] contenthandle = DecryptContent(wadfile, nullapp, titlekey);
                    int imetpos = 0;                    if (contenthandle.Length < 400) return new string[7];                    if (!channeltype.Contains("Downloaded"))
                    {
                        for (int z = 0; z < 400; z++)
                        {
                            if (Convert.ToChar(contenthandle[z]) == 'I')
                                if (Convert.ToChar(contenthandle[z + 1]) == 'M')
                                    if (Convert.ToChar(contenthandle[z + 2]) == 'E')
                                        if (Convert.ToChar(contenthandle[z + 3]) == 'T')
                                        {
                                            imetpos = z;
                                            break;
                                        }
                        }                        int jappos = imetpos + 29;
                        int count = 0;                        for (int i = jappos; i < jappos + 588; i += 84)
                        {
                            for (int j = 0; j < 40; j += 2)
                            {
                                if (contenthandle[i + j] != 0x00)
                                {
                                    char temp = BitConverter.ToChar([contenthandle[i + j], contenthandle[i + j - 1]], 0);
                                    titles[count] += temp;
                                }
                            }                            count++;
                        }                        for (int i = jappos + (9 * 84); i < jappos + (9 * 84) + 40; i += 2)
                        {
                            if (contenthandle[i] != 0x00)
                            {
                                char temp = BitConverter.ToChar([contenthandle[i], contenthandle[i - 1]], 0);
                                titles[count] += temp;
                            }
                        }                        return titles;
                    }
                    else
                    {
                        //DLC's
                        for (int j = 97; j < 97 + 40; j += 2)
                        {
                            if (contenthandle[j] != 0x00)
                            {
                                char temp = BitConverter.ToChar([contenthandle[j], contenthandle[j - 1]], 0);
                                titles[0] += temp;
                            }
                        }                        for (int i = 1; i < 8; i++)
                            titles[i] = titles[0];                        return titles;
                    }
                }
                else return new string[8];
            }
            else return new string[8];
        }
        else return new string[8];
    }    /// <summary>
    /// Returns the title for each language of a 00.app file
    /// Order: Jap, Eng, Ger, Fra, Spa, Ita, Dut
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static string[] GetChannelTitlesFromApp(string app)
    {
        byte[] tmp = LoadFileToByteArray(app);
        return GetChannelTitlesFromApp(tmp);
    }    /// <summary>
    /// Returns the title for each language of a 00.app file
    /// Order: Jap, Eng, Ger, Fra, Spa, Ita, Dut
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static string[] GetChannelTitlesFromApp(byte[] app)
    {
        string[] titles = new string[8];        int imetpos = 0;
        int length = 400;        if (app.Length < 400) length = app.Length - 4;        for (int z = 0; z < length; z++)
        {
            if (Convert.ToChar(app[z]) == 'I')
                if (Convert.ToChar(app[z + 1]) == 'M')
                    if (Convert.ToChar(app[z + 2]) == 'E')
                        if (Convert.ToChar(app[z + 3]) == 'T')
                        {
                            imetpos = z;
                            break;
                        }
        }        if (imetpos != 0)
        {
            int jappos = imetpos + 29;
            int count = 0;            for (int i = jappos; i < jappos + 588; i += 84)
            {
                for (int j = 0; j < 40; j += 2)
                {
                    if (app[i + j] != 0x00)
                    {
                        char temp = BitConverter.ToChar([app[i + j], app[i + j - 1]], 0);
                        titles[count] += temp;
                    }
                }                count++;
            }            for (int i = jappos + (9 * 84); i < jappos + (9 * 84) + 40; i += 2)
            {
                if (app[i] != 0x00)
                {
                    char temp = BitConverter.ToChar([app[i], app[i - 1]], 0);
                    titles[count] += temp;
                }
            }
        }        return titles;
    }    /// <summary>
    /// Returns the Type of the Channel as a string
    /// Wad or Tik needed for WiiWare / VC detection!
    /// </summary>
    /// <param name="wadfile"></param>
    /// <returns></returns>
    public string GetChannelType(byte[] wadtiktmd, int type)
    {
        int tikpos = 0;
        int tmdpos = 0;        if (IsThisWad(wadtiktmd) == true)
        {
            //It's a wad
            tikpos = GetTikPos(wadtiktmd);
            tmdpos = GetTmdPos(wadtiktmd);
        }        string thistype = type == 0
            ? wadtiktmd[tikpos + 0x1dc].ToString("x2") + wadtiktmd[tikpos + 0x1dd].ToString("x2") + wadtiktmd[tikpos + 0x1de].ToString("x2") + wadtiktmd[tikpos + 0x1df].ToString("x2")
            : wadtiktmd[tmdpos + 0x18c].ToString("x2") + wadtiktmd[tmdpos + 0x18d].ToString("x2") + wadtiktmd[tmdpos + 0x18e].ToString("x2") + wadtiktmd[tmdpos + 0x18f].ToString("x2");        string channeltype = "Unknown";        if (thistype == "00010001")
        {
            channeltype = CheckWiiWareVC(wadtiktmd, type);
        }
        else if (thistype == "00010002") channeltype = "System Channel";
        else if (thistype is "00010004" or "00010000") channeltype = "Game Channel";
        else if (thistype == "00010005") channeltype = "Downloaded Content";
        else if (thistype == "00010008") channeltype = "Hidden Channel";
        else if (thistype == "00000001")
        {
            channeltype = "System: IOS";            string thisid = type == 0
                ? wadtiktmd[tikpos + 0x1e0].ToString("x2") + wadtiktmd[tikpos + 0x1e1].ToString("x2") + wadtiktmd[tikpos + 0x1e2].ToString("x2") + wadtiktmd[tikpos + 0x1e3].ToString("x2")
                : wadtiktmd[tmdpos + 0x190].ToString("x2") + wadtiktmd[tmdpos + 0x191].ToString("x2") + wadtiktmd[tmdpos + 0x192].ToString("x2") + wadtiktmd[tmdpos + 0x193].ToString("x2");
            if (thisid == "00000001") channeltype = "System: Boot2";
            else if (thisid == "00000002") channeltype = "System: Menu";
            else if (thisid == "00000100") channeltype = "System: BC";
            else if (thisid == "00000101") channeltype = "System: MIOS";
        }        return channeltype;
    }    /// <summary>
    /// Returns the amount of included Contents (app-files)
    /// </summary>
    /// <param name="wadfile"></param>
    /// <returns></returns>
    public int GetContentNum(byte[] wadtmd)
    {
        int tmdpos = 0;        if (IsThisWad(wadtmd) == true)
        {
            //It's a wad file, so get the tmd position
            tmdpos = GetTmdPos(wadtmd);
        }        int contents = HexStringToInt(wadtmd[tmdpos + 0x1de].ToString("x2") + wadtmd[tmdpos + 0x1df].ToString("x2"));        return contents;
    }    /// <summary>
    /// Returns the boot index specified in the tmd
    /// </summary>
    /// <param name="wadfile"></param>
    /// <returns></returns>
    public int GetBootIndex(byte[] wadtmd)
    {
        int tmdpos = 0;        if (IsThisWad(wadtmd))
            tmdpos = GetTmdPos(wadtmd);        int bootIndex = HexStringToInt(wadtmd[tmdpos + 0x1e0].ToString("x2") + wadtmd[tmdpos + 0x1e1].ToString("x2"));        return bootIndex;
    }    /// <summary>
    /// Returns the approx. destination size on the Wii
    /// </summary>
    /// <param name="wadfile"></param>
    /// <returns></returns>
    public string GetNandSize(byte[] wadtmd, bool ConvertToMB)
    {
        int tmdpos = 0;
        int minsize = 0;
        int maxsize = 0;
        int numcont = GetContentNum(wadtmd);        if (IsThisWad(wadtmd) == true)
        {
            //It's a wad
            tmdpos = GetTmdPos(wadtmd);
        }        for (int i = 0; i < numcont; i++)
        {
            int cont = 36 * i;
            int contentsize = HexStringToInt(wadtmd[tmdpos + 0x1e4 + 8 + cont].ToString("x2") +
                wadtmd[tmdpos + 0x1e5 + 8 + cont].ToString("x2") +
                wadtmd[tmdpos + 0x1e6 + 8 + cont].ToString("x2") +
                wadtmd[tmdpos + 0x1e7 + 8 + cont].ToString("x2") +
                wadtmd[tmdpos + 0x1e8 + 8 + cont].ToString("x2") +
                wadtmd[tmdpos + 0x1e9 + 8 + cont].ToString("x2") +
                wadtmd[tmdpos + 0x1ea + 8 + cont].ToString("x2") +
                wadtmd[tmdpos + 0x1eb + 8 + cont].ToString("x2"));            string type = wadtmd[tmdpos + 0x1e4 + 6 + cont].ToString("x2") + wadtmd[tmdpos + 0x1e5 + 6 + cont].ToString("x2");            if (type == "0001")
            {
                minsize += contentsize;
                maxsize += contentsize;
            }
            else if (type == "8001")
                maxsize += contentsize;
        }        string size = maxsize == minsize ? maxsize.ToString() : minsize.ToString() + " - " + maxsize.ToString();        if (ConvertToMB == true)
        {
            if (size.Contains("-"))
            {
                string min = size.Remove(size.IndexOf(' '));
                string max = size.Remove(0, size.IndexOf('-') + 2);                min = Convert.ToString(Math.Round(Convert.ToDouble(min) * 0.0009765625 * 0.0009765625, 2));
                max = Convert.ToString(Math.Round(Convert.ToDouble(max) * 0.0009765625 * 0.0009765625, 2));
                if (min.Length > 4) { min = min.Remove(4); }
                if (max.Length > 4) { max = max.Remove(4); }
                size = min + " - " + max + " MB";
            }
            else
            {
                size = Convert.ToString(Math.Round(Convert.ToDouble(size) * 0.0009765625 * 0.0009765625, 2));
                if (size.Length > 4) { size = size.Remove(4); }
                size += " MB";
            }
        }        return size.Replace(",", ".");
    }    /// <summary>
    /// Returns the approx. destination block on the Wii
    /// </summary>
    /// <param name="wadfile"></param>
    /// <returns></returns>
    public string GetNandBlocks(string wadtmd)
    {
        using FileStream fs = new(wadtmd, FileMode.Open);
        byte[] temp = new byte[fs.Length];
        fs.Read(temp, 0, temp.Length);
        return GetNandBlocks(temp);
    }    /// <summary>
    /// Returns the approx. destination block on the Wii
    /// </summary>
    /// <param name="wadfile"></param>
    /// <returns></returns>
    public string GetNandBlocks(byte[] wadtmd)
    {
        string size = GetNandSize(wadtmd, false);        if (size.Contains('-'))
        {
            string size1 = size.Remove(size.IndexOf(' '));
            string size2 = size.Remove(0, size.LastIndexOf(' ') + 1);            double blocks1 = (double)(Convert.ToDouble(size1) / 1024 / 128);
            double blocks2 = (double)(Convert.ToDouble(size2) / 1024 / 128);            return Math.Ceiling(blocks1) + " - " + Math.Ceiling(blocks2);
        }
        else
        {
            double blocks = (double)(Convert.ToDouble(size) / 1024 / 128);            return Math.Ceiling(blocks).ToString();
        }
    }    /// <summary>
    /// Returns the title version of the wad file
    /// </summary>
    /// <param name="wadfile"></param>
    /// <returns></returns>
    public int GetTitleVersion(string wadtmd)
    {
        byte[] temp = LoadFileToByteArray(wadtmd, 0, 10000);
        return GetTitleVersion(temp);
    }
    public static byte[] LoadFileToByteArray(string sourcefile, int offset, int length)
    {
        if (File.Exists(sourcefile))
        {
            using FileStream fs = new(sourcefile, FileMode.Open);
            if (fs.Length < length) length = (int)fs.Length;
            byte[] filearray = new byte[length];
            fs.Seek(offset, SeekOrigin.Begin);
            fs.Read(filearray, 0, length);
            return filearray;
        }
        else throw new FileNotFoundException("File couldn't be found:\r\n" + sourcefile);
    }    /// <summary>
    /// Returns the title version of the wad file
    /// </summary>
    /// <param name="wadfile"></param>
    /// <returns></returns>
    public int GetTitleVersion(byte[] wadtmd)
    {
        int tmdpos = 0;        if (IsThisWad(wadtmd) == true) { tmdpos = GetTmdPos(wadtmd); }
        return HexStringToInt(wadtmd[tmdpos + 0x1dc].ToString("x2") + wadtmd[tmdpos + 0x1dd].ToString("x2"));
    }    /// <summary>
    /// Returns the IOS that is needed by the wad file
    /// </summary>
    /// <param name="wadfile"></param>
    /// <returns></returns>
    public string GetIosFlag(byte[] wadtmd)
    {
        string type = GetChannelType(wadtmd, 1);        if (!type.Contains("IOS") && !type.Contains("BC"))
        {
            int tmdpos = 0;
            if (IsThisWad(wadtmd) == true) { tmdpos = GetTmdPos(wadtmd); }
            return "IOS" + HexStringToInt(wadtmd[tmdpos + 0x188].ToString("x2") + wadtmd[tmdpos + 0x189].ToString("x2") + wadtmd[tmdpos + 0x18a].ToString("x2") + wadtmd[tmdpos + 0x18b].ToString("x2"));
        }
        else return "";
    }    /// <summary>
    /// Returns the region of the wad file
    /// </summary>
    /// <param name="wadfile"></param>
    /// <returns></returns>
    public string GetRegionFlag(byte[] wadtmd)
    {
        int tmdpos = 0;
        string channeltype = GetChannelType(wadtmd, 1);        if (IsThisWad(wadtmd) == true) { tmdpos = GetTmdPos(wadtmd); }        if (!channeltype.Contains("System:"))
        {
            int region = HexStringToInt(wadtmd[tmdpos + 0x19d].ToString("x2"));
            return RegionCode[region];
        }
        else return "";
    }    /// <summary>
    /// Returns the Path where the wad will be installed on the Wii
    /// </summary>
    /// <param name="wadfile"></param>
    /// <returns></returns>
    public string GetNandPath(string wadfile)
    {
        byte[] wad = LoadFileToByteArray(wadfile);
        return GetNandPath(wad, 0);
    }    /// <summary>
    /// Returns the Path where the wad will be installed on the Wii
    /// </summary>
    /// <param name="wadfile"></param>
    /// <param name="type">0 = Tik, 1 = Tmd</param>
    /// <returns></returns>
    public string GetNandPath(byte[] wadtiktmd, int type)
    {
        int tikpos = 0;
        int tmdpos = 0;        if (IsThisWad(wadtiktmd) == true)
        {
            tikpos = GetTikPos(wadtiktmd);
            tmdpos = GetTmdPos(wadtiktmd);
        }        string thispath = type == 0
            ? wadtiktmd[tikpos + 0x1dc].ToString("x2") +
                wadtiktmd[tikpos + 0x1dd].ToString("x2") +
                wadtiktmd[tikpos + 0x1de].ToString("x2") +
                wadtiktmd[tikpos + 0x1df].ToString("x2") +
                wadtiktmd[tikpos + 0x1e0].ToString("x2") +
                wadtiktmd[tikpos + 0x1e1].ToString("x2") +
                wadtiktmd[tikpos + 0x1e2].ToString("x2") +
                wadtiktmd[tikpos + 0x1e3].ToString("x2")
            : wadtiktmd[tmdpos + 0x18c].ToString("x2") +
                wadtiktmd[tmdpos + 0x18d].ToString("x2") +
                wadtiktmd[tmdpos + 0x18e].ToString("x2") +
                wadtiktmd[tmdpos + 0x18f].ToString("x2") +
                wadtiktmd[tmdpos + 0x190].ToString("x2") +
                wadtiktmd[tmdpos + 0x191].ToString("x2") +
                wadtiktmd[tmdpos + 0x192].ToString("x2") +
                wadtiktmd[tmdpos + 0x193].ToString("x2");        thispath = thispath.Insert(8, "\\");
        return thispath;
    }    /// <summary>
    /// Returns true, if the wad file is a WiiWare / VC title.
    /// </summary>
    /// <param name="wadtiktmd"></param>
    /// <param name="type">0 = Tik, 1 = Tmd</param>
    /// <returns></returns>
    public string CheckWiiWareVC(byte[] wadtiktmd, int type)
    {
        int tiktmdpos = 0;
        int offset = 0x221;
        int idoffset = 0x1e0;        if (type == 1) { offset = 0x197; idoffset = 0x190; }
        if (IsThisWad(wadtiktmd) == true)
        {
            tiktmdpos = type == 1 ? GetTmdPos(wadtiktmd) : GetTikPos(wadtiktmd);
        }        if (wadtiktmd[tiktmdpos + offset] == 0x01)
        {
            char idchar = Convert.ToChar(wadtiktmd[tiktmdpos + idoffset]);
            char idchar2 = Convert.ToChar(wadtiktmd[tiktmdpos + idoffset + 1]);            if (idchar == 'H') return "System Channel";
            else if (idchar == 'W') return "WiiWare";
            else
            {
                if (idchar == 'C') return "C64";
                else if (idchar == 'E' && idchar2 == 'A') return "NeoGeo";
                else if (idchar == 'E') return "VC - Arcade";
                else if (idchar == 'F') return "NES";
                else if (idchar == 'J') return "SNES";
                else if (idchar == 'L') return "Sega Master System";
                else if (idchar == 'M') return "Sega Genesis";
                else if (idchar == 'N') return "Nintendo 64";
                else return idchar == 'P' ? "Turbografx" : idchar == 'Q' ? "Turbografx CD" : "Channel Title";
            }
        }
        else return "Channel Title";
    }    /// <summary>
    /// Returns all information stored in the tmd for all contents in the wad file.
    /// [x, 0] = Content ID, [x, 1] = Index, [x, 2] = Type, [x, 3] = Size, [x, 4] = Sha1
    /// </summary>
    /// <param name="wadfile"></param>
    /// <returns></returns>
    public string[,] GetContentInfo(byte[] wadtmd)
    {
        int tmdpos = 0;        if (IsThisWad(wadtmd) == true) { tmdpos = GetTmdPos(wadtmd); }
        int contentcount = GetContentNum(wadtmd);
        string[,] contentinfo = new string[contentcount, 5];        for (int i = 0; i < contentcount; i++)
        {
            contentinfo[i, 0] = wadtmd[tmdpos + 0x1e4 + (36 * i)].ToString("x2") +
                wadtmd[tmdpos + 0x1e5 + (36 * i)].ToString("x2") +
                wadtmd[tmdpos + 0x1e6 + (36 * i)].ToString("x2") +
                wadtmd[tmdpos + 0x1e7 + (36 * i)].ToString("x2");
            contentinfo[i, 1] = "0000" +
                wadtmd[tmdpos + 0x1e8 + (36 * i)].ToString("x2") +
                wadtmd[tmdpos + 0x1e9 + (36 * i)].ToString("x2");
            contentinfo[i, 2] = wadtmd[tmdpos + 0x1ea + (36 * i)].ToString("x2") +
                wadtmd[tmdpos + 0x1eb + (36 * i)].ToString("x2");
            contentinfo[i, 3] = HexStringToInt(
                wadtmd[tmdpos + 0x1ec + (36 * i)].ToString("x2") +
                wadtmd[tmdpos + 0x1ed + (36 * i)].ToString("x2") +
                wadtmd[tmdpos + 0x1ee + (36 * i)].ToString("x2") +
                wadtmd[tmdpos + 0x1ef + (36 * i)].ToString("x2") +
                wadtmd[tmdpos + 0x1f0 + (36 * i)].ToString("x2") +
                wadtmd[tmdpos + 0x1f1 + (36 * i)].ToString("x2") +
                wadtmd[tmdpos + 0x1f2 + (36 * i)].ToString("x2") +
                wadtmd[tmdpos + 0x1f3 + (36 * i)].ToString("x2")).ToString();            for (int j = 0; j < 20; j++)
            {
                contentinfo[i, 4] += wadtmd[tmdpos + 0x1f4 + (36 * i) + j].ToString("x2");
            }
        }        return contentinfo;
    }    /// <summary>
    /// Returns the Tik of the wad file as a Byte-Array
    /// </summary>
    /// <param name="wadfile"></param>
    /// <returns></returns>
    public byte[] ReturnTik(byte[] wadfile)
    {
        int tikpos = GetTikPos(wadfile);
        int tiksize = GetTikSize(wadfile);        byte[] tik = new byte[tiksize];        for (int i = 0; i < tiksize; i++)
        {
            tik[i] = wadfile[tikpos + i];
        }        return tik;
    }    /// <summary>
    /// Returns the Tmd of the wad file as a Byte-Array
    /// </summary>
    /// <param name="wadfile"></param>
    /// <returns></returns>
    public byte[] ReturnTmd(byte[] wadfile)
    {
        int tmdpos = GetTmdPos(wadfile);
        int tmdsize = GetTmdSize(wadfile);        byte[] tmd = new byte[tmdsize];        for (int i = 0; i < tmdsize; i++)
        {
            tmd[i] = wadfile[tmdpos + i];
        }        return tmd;
    }    /// <summary>
    /// Checks, if the given file is a wad
    /// </summary>
    /// <param name="wadtiktmd"></param>
    /// <returns></returns>
    public static bool IsThisWad(byte[] wadtiktmd)
    {
        return wadtiktmd[0] == 0x00 &&
            wadtiktmd[1] == 0x00 &&
            wadtiktmd[2] == 0x00 &&
            wadtiktmd[3] == 0x20 &&
            wadtiktmd[4] == 0x49 &&
            wadtiktmd[5] == 0x73;    }    /// <summary>
    /// Returns the decrypted TitleKey
    /// </summary>
    /// <param name="wadtik"></param>
    /// <returns></returns>
    public byte[] GetTitleKey(byte[] wadtik)
    {
        byte[] commonkey = new byte[16];        if (File.Exists(RomManagerConfiguration.GetWiiWadCommonKeyPath()))
        { commonkey = LoadFileToByteArray(RomManagerConfiguration.GetWiiWadCommonKeyPath()); }
        else
        {
            CreateCommonKey("45e");
            commonkey = LoadFileToByteArray(RomManagerConfiguration.GetWiiWadCommonKeyPath());
        }        byte[] encryptedkey = new byte[16];
        byte[] iv = new byte[16];
        int tikpos = 0;        if (IsThisWad(wadtik) == true)
        {
            //It's a wad file, so get the tik position
            tikpos = GetTikPos(wadtik);
        }        for (int i = 0; i < 16; i++)
        {
            encryptedkey[i] = wadtik[tikpos + 0x1bf + i];
        }        for (int j = 0; j < 8; j++)
        {
            iv[j] = wadtik[tikpos + 0x1dc + j];
            iv[j + 8] = 0x00;
        }        using var decrypt = Aes.Create(); decrypt.Mode = CipherMode.CBC;
        decrypt.Padding = PaddingMode.None;
        decrypt.KeySize = 128;
        decrypt.BlockSize = 128;
        decrypt.Key = commonkey;
        decrypt.IV = iv;        ICryptoTransform cryptor = decrypt.CreateDecryptor();        MemoryStream memory = new(encryptedkey);
        CryptoStream crypto = new(memory, cryptor, CryptoStreamMode.Read);        byte[] decryptedkey = new byte[16];
        crypto.Read(decryptedkey, 0, decryptedkey.Length);        crypto.Close();
        memory.Close();        return decryptedkey;
    }    /// <summary>
    /// Decodes the Timestamp in the Trailer, if available.
    /// Returns null if no Timestamp was found.
    /// </summary>
    /// <param name="trailer"></param>
    /// <returns></returns>
    public static DateTime GetCreationTime(string trailer)
    {
        byte[] bTrailer = LoadFileToByteArray(trailer);
        return GetCreationTime(bTrailer);
    }    /// <summary>
    /// Decodes the Timestamp in the Trailer, if available.
    /// Returns null if no Timestamp was found.
    /// </summary>
    /// <param name="trailer"></param>
    /// <returns></returns>
    public static DateTime GetCreationTime(byte[] footer)
    {
        DateTime result = new(1970, 1, 1);        if ((footer[0] == 'C' &&
            footer[1] == 'M' &&
            footer[2] == 'i' &&
            footer[3] == 'i' &&
            footer[4] == 'U' &&
            footer[5] == 'T') ||
            (footer[0] == 'T' &&
            footer[1] == 'm' &&
            footer[2] == 'S' &&
            footer[3] == 't' &&
            footer[4] == 'm' &&
            footer[5] == 'p'))
        {
            ASCIIEncoding enc = new();
            string stringSeconds = enc.GetString(footer, 6, 10);
            int seconds = 0;            if (int.TryParse(stringSeconds, out seconds))
            {
                result = result.AddSeconds(seconds);
                return result;
            }
            else return result;
        }        return result;
    }
}