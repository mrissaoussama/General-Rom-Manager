using RomManagerShared.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RomManagerShared.Interfaces
{
    public interface IHasExternalHashSource
    {
        Task<IEnumerable<RomHash>> GetRomHashesFromExternalSource(Rom rom);
        Task<IEnumerable<RomHash>> CompareHashesToExternalDatabase(Rom rom);

    }

}
