using RomManagerShared.Base;
namespace RomManagerShared.PSVita;

public interface IPSVitaRom : IFolderLayout
{
    // <title>/sce_sys/param.sfo
    int IFolderLayout.FolderLevelsAboveMarker(string marker) => marker == "param.sfo" ? 2 : 0;
}
public class PSVitaGame : Game, IPSVitaRom
{
    public PSVitaGame() : base()
    {
    }
}
public class PSVitaDLC : DLC, IPSVitaRom
{
    public PSVitaDLC() : base()
    {
    }
}
public class PSVitaUpdate : Update, IPSVitaRom
{
    public PSVitaUpdate() : base()
    {
    }
}

public class PSVitaLicense : License
{
    public PSVitaLicense(string path) : base(path)
    {
    }
}