using DotNet3dsToolkit;
using LibHac.Tools.Fs;
using RomManagerShared;
using RomManagerShared.ThreeDS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RomManagerShared.ThreeDS.ThreeDSUtils;

namespace RomManagerShared.ThreeDS
{
    public class ThreeDsRomParser : IRomParser
    {
        public async Task<List<IRom>> ProcessFile(string path)
        {
            ThreeDsRom rom = new ThreeDsRom();
            Console.WriteLine(Path.GetFileName(path));
            try
            {
                await rom.OpenFile(path);
            }
            catch
            {
                FileUtils.Log($"error reading 3DS file {path}. make sure the file is valid and not encrypted ");
            }
            var titleid=rom.GetTitleID().ToString("X16");
            
            IRom game = GetRomType(titleid);
            game.ProductCode = rom.GetProductCode();
            game.Description = rom.GetShortDescription();
            game.TitleName = rom.GetLongDescription().Replace("\n"," ");
            game.Publisher = rom.GetPublisher();
            game.Region= rom.GetRegion();
            game.Path = path;
            game.TitleID = titleid;
            game.Version=rom.GetTitleVersion().ToString();
            game.MinimumFirmware=rom.GetSystemVersion().ToString();

            return new List<IRom>() { game };
        }

        private IRom GetRomType(string titleId)
        {
            var romType = DetectContentCategory(titleId);

            switch (romType)
            {
                case TidCategory.Normal:
                    ThreeDSGame game =new();
                    return game;
                case TidCategory.Update:
                    ThreeDSUpdate update = new();
                    return update;
                case TidCategory.Dlc:
                case TidCategory.AddOnContents:
                    ThreeDSDLC dlc = new ThreeDSDLC();
                    return dlc;
                default:
                    return new ThreeDSGame();
            }
        }
    }
}
