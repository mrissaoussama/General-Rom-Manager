using SkyEditor.IO.Binary;
namespace DotNet3dsToolkit.Infrastructure;

/// <summary>
/// A <see cref="BinaryFile"/> that will delete the underlying file when disposed
/// </summary>
public class FileDeletingBinaryFile : BinaryFile
{
    public FileDeletingBinaryFile(string filename) : base(filename)
    {
    }    public override void Dispose()
    {
        base.Dispose();
        if (File.Exists(Filename))
        {
            File.Delete(Filename);
        }
    }
}
