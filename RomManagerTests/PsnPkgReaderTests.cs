using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using RomManagerShared.Utils.PKGUtils;

namespace RomManagerTests;

public class PsnPkgReaderTests
{
    private static readonly byte[] Ps3Key = Convert.FromBase64String("Lntx18nJoU6jIh8YiCi4+A==");
    private static readonly byte[] PspKey = Convert.FromBase64String("B/LGgpC1DSwzgY1wm2DmKw==");

    /// <summary>Builds a retail PKG3 with the given items, encrypted with AES-CTR.</summary>
    private static string BuildPkg(ushort type, int contentType, string contentId, byte[] key, int keyIndex,
        params (string Name, byte[] Data)[] items)
    {
        const int dataOffset = 0x200;
        var riv = RandomNumberGenerator.GetBytes(16);

        // Body: entries, names (16-aligned), data (16-aligned).
        var body = new MemoryStream();
        var entries = new byte[items.Length * 0x20];
        body.Write(entries);
        var names = new List<(int Offset, int Size)>();
        foreach (var (name, _) in items)
        {
            Pad(body);
            names.Add(((int)body.Length, Encoding.UTF8.GetByteCount(name)));
            body.Write(Encoding.UTF8.GetBytes(name));
        }
        var dataSlots = new List<(long Offset, long Size)>();
        foreach (var (_, data) in items)
        {
            Pad(body);
            dataSlots.Add((body.Length, data.Length));
            body.Write(data);
        }
        Pad(body);
        var plain = body.ToArray();
        for (int i = 0; i < items.Length; i++)
        {
            var e = plain.AsSpan(i * 0x20, 0x20);
            BinaryPrimitives.WriteUInt32BigEndian(e, (uint)names[i].Offset);
            BinaryPrimitives.WriteUInt32BigEndian(e[0x04..], (uint)names[i].Size);
            BinaryPrimitives.WriteUInt64BigEndian(e[0x08..], (ulong)dataSlots[i].Offset);
            BinaryPrimitives.WriteUInt64BigEndian(e[0x10..], (ulong)dataSlots[i].Size);
            BinaryPrimitives.WriteUInt32BigEndian(e[0x18..], 0x80000003u | ((uint)keyIndex << 28));
        }

        // Independent CTR: encrypt counter blocks one at a time.
        using var aes = Aes.Create();
        aes.Key = key;
        var counter = (byte[])riv.Clone();
        for (int i = 0; i < plain.Length; i += 16)
        {
            var ks = aes.EncryptEcb(counter, PaddingMode.None);
            for (int j = 0; j < 16; j++) plain[i + j] ^= ks[j];
            for (int k = 15; k >= 0 && ++counter[k] == 0; k--) { }
        }

        var head = new byte[dataOffset];
        BinaryPrimitives.WriteUInt32BigEndian(head, 0x7F504B47);
        BinaryPrimitives.WriteUInt16BigEndian(head.AsSpan(0x04), 0x8000);
        BinaryPrimitives.WriteUInt16BigEndian(head.AsSpan(0x06), type);
        BinaryPrimitives.WriteUInt32BigEndian(head.AsSpan(0x08), 0x100);
        BinaryPrimitives.WriteUInt32BigEndian(head.AsSpan(0x0C), 1);
        BinaryPrimitives.WriteUInt32BigEndian(head.AsSpan(0x14), (uint)items.Length);
        BinaryPrimitives.WriteUInt64BigEndian(head.AsSpan(0x20), dataOffset);
        Encoding.ASCII.GetBytes(contentId).CopyTo(head, 0x30);
        riv.CopyTo(head, 0x70);
        BinaryPrimitives.WriteUInt32BigEndian(head.AsSpan(0x100), 0x02);
        BinaryPrimitives.WriteUInt32BigEndian(head.AsSpan(0x104), 4);
        BinaryPrimitives.WriteUInt32BigEndian(head.AsSpan(0x108), (uint)contentType);

        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pkg");
        File.WriteAllBytes(path, [.. head, .. plain]);
        return path;
    }

    private static void Pad(Stream s)
    {
        while (s.Length % 16 != 0) s.WriteByte(0);
    }

    [Fact]
    public void Read_Ps3Game_ReadsSfoFromRoot()
    {
        var sfo = SfoTestData.Build(("TITLE", "WipEout HD Fury"), ("APP_VER", "02.00"), ("CATEGORY", "HG"));
        var pkg = BuildPkg(1, 0x5, "EP9000-BCES00664_00-WIPEOUTHDFURY000", Ps3Key, 0,
            ("USRDIR/EBOOT.BIN", new byte[100]), ("PARAM.SFO", sfo));
        try
        {
            var info = PsnPkgReader.Read(pkg);

            info.TitleId.Should().Be("BCES00664");
            info.Platform.Should().Be(PsnPlatform.PS3);
            info.Kind.Should().Be(PsnPkgKind.Game);
            info.RegionCode.Should().Be('E');
            info.Sfo!.GetString("TITLE").Should().Be("WipEout HD Fury");
        }
        finally
        {
            File.Delete(pkg);
        }
    }

    [Fact]
    public void Read_PspGame_ReadsSfoFromEbootPbp()
    {
        var sfo = SfoTestData.Build(("TITLE", "Aces of War"), ("CATEGORY", "EG"), ("DISC_ID", "NPEH00001"));
        var pbp = new byte[0x28 + sfo.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(pbp, 0x50425000);
        BinaryPrimitives.WriteUInt32LittleEndian(pbp.AsSpan(0x08), 0x28);
        BinaryPrimitives.WriteUInt32LittleEndian(pbp.AsSpan(0x0C), (uint)(0x28 + sfo.Length));
        sfo.CopyTo(pbp, 0x28);
        var pkg = BuildPkg(2, 0x7, "UP0001-NPUH00001_00-0000000000000000", PspKey, 1,
            ("USRDIR/CONTENT/EBOOT.PBP", pbp));
        try
        {
            var info = PsnPkgReader.Read(pkg);

            info.Platform.Should().Be(PsnPlatform.PSP);
            info.Kind.Should().Be(PsnPkgKind.Game);
            info.TitleId.Should().Be("NPUH00001");
            info.Sfo!.GetString("TITLE").Should().Be("Aces of War");
        }
        finally
        {
            File.Delete(pkg);
        }
    }

    [Fact]
    public void Read_NonPkg_Throws()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, new byte[0x400]);
            var act = () => PsnPkgReader.Read(path);
            act.Should().Throw<InvalidDataException>();
        }
        finally
        {
            File.Delete(path);
        }
    }
}
