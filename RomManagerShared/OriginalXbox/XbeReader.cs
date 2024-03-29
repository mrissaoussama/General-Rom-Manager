﻿using System.Text;
namespace RomManagerShared.OriginalXbox;
//https://github.com/feudalnate/Original-Xbox-Data-Structures/blob/master/Xbox%20Executable/XBE.cs#L62
public class XbeReader
{//constants
    public static readonly uint  CONST_XBE_MAGIC = 0x48454258; //"XBEH" Xbox Executable identifier
    public static readonly uint  CONST_BASE_ADDRESS = 0x10000;    //address in memory a standard XBE file is loaded
    public static readonly uint  CONST_TITLE_NAME_LENGTH = 0x28;       //game/app name (unicode)
    public static readonly uint  CONST_MAX_ALTERNATE_COUNT = 0x10;       //maximum alternate title id's/title signature keys
    public static readonly uint  CONST_KEY_SIZE = 0x10;       //title signature key/lan key size
    public static readonly uint  CONST_HASH_SIZE = 0x14;       //SHA-1 hash size
    public static readonly uint  CONST_RSA_SIGNATURE_SIZE = 0x100;      //2048-bit RSA signature size
    public static readonly uint  CONST_LIB_NAME_LENGTH = 0x8;        //max length of imported library name

    //region flags
    public static readonly uint  GAME_REGION_NA = 0x00000001;
    public static readonly uint  GAME_REGION_JAPAN = 0x00000002;
    public static readonly uint  GAME_REGION_RESTOFWORLD = 0x00000004;
    public static readonly uint  GAME_REGION_INTERNAL_TEST = 0x40000000; //never seen this used, flag does exist in later kernel revisions
    public static readonly uint  GAME_REGION_MANUFACTURING = 0x80000000;

    //media type flags
    public static readonly uint  MEDIA_TYPE_HARD_DRIVE = 0x00000001;
    public static readonly uint  MEDIA_TYPE_DVD_X2 = 0x00000002;
    public static readonly uint  MEDIA_TYPE_DVD_CD = 0x00000004;
    public static readonly uint  MEDIA_TYPE_CD = 0x00000008;
    public static readonly uint  MEDIA_TYPE_DVD_5_RO = 0x00000010;
    public static readonly uint  MEDIA_TYPE_DVD_9_RO = 0x00000020;
    public static readonly uint  MEDIA_TYPE_DVD_5_RW = 0x00000040;
    public static readonly uint  MEDIA_TYPE_DVD_9_RW = 0x00000080;
    public static readonly uint  MEDIA_TYPE_DONGLE = 0x00000100;
    public static readonly uint  MEDIA_TYPE_MEDIA_BOARD = 0x00000200;
    public static readonly uint  MEDIA_TYPE_NONSECURE_HARD_DISK = 0x40000000;
    public static readonly uint  MEDIA_TYPE_NONSECURE_MODE = 0x80000000;
    public static readonly uint  MEDIA_TYPE_MASK = 0x00FFFFFF;

    //section flags
    public static readonly uint  SECTION_WRITEABLE = 0x00000001;
    public static readonly uint  SECTION_PRELOAD = 0x00000002;
    public static readonly uint  SECTION_EXECUTABLE = 0x00000004;
    public static readonly uint  SECTION_INSERTFILE = 0x00000008;
    public static readonly uint  SECTION_HEAD_PAGE_READONLY = 0x00000010;
    public static readonly uint  SECTION_TAIL_PAGE_READONLY = 0x00000020;

    //initialization flags
    public static readonly uint  INIT_MOUNT_UTILITY_DRIVE = 0x00000001;
    public static readonly uint  INIT_FORMAT_UTILITY_DRIVE = 0x00000002;
    public static readonly uint  INIT_LIMIT_DEVKIT_MEMORY = 0x00000004;
    public static readonly uint  INIT_NO_SETUP_HARD_DISK = 0x00000008;
    public static readonly uint  INIT_DONT_MODIFY_HARD_DISK = 0x00000010;
    public static readonly uint  INIT_UTILITY_DRIVE_CLUSTER_SIZE_MASK = 0xC0000000;
    public static readonly uint  INIT_UTILITY_DRIVE_16K_CLUSTER_SIZE = 0x00000000;
    public static readonly uint  INIT_UTILITY_DRIVE_32K_CLUSTER_SIZE = 0x40000000;
    public static readonly uint  INIT_UTILITY_DRIVE_64K_CLUSTER_SIZE = 0x80000000;
    public static readonly uint  INIT_UTILITY_DRIVE_128K_CLUSTER_SIZE = 0xC0000000;
    //INIT_UTILITY_DRIVE_CLUSTER_SIZE_SHIFT = 0x1E; //used when formatting utility drives i assume (x/y/z), not a flag

    public static XBE_CERTIFICATE ReadCertificate(byte[] xbeContent)
    {
        using MemoryStream stream = new(xbeContent);
        using BinaryReader reader = new(stream);
        XBE_HEADER header = ReadXbeHeader(reader);

        // Calculate the offset of the certificate
        uint rawOffset = header.Certificate - header.BaseAddress;

        // Seek to the certificate offset
        reader.BaseStream.Seek(rawOffset, SeekOrigin.Begin);

        // Read the certificate
        XBE_CERTIFICATE certificate = ReadXbeCertificate(reader);
        certificate.TitleNameString = ConvertUnicodeToString(certificate.TitleName);
        certificate.TitleIDHex = certificate.TitleID.ToString("X8");

        return certificate;
    }
    private static string ConvertUnicodeToString(ushort[] unicodeString)
    {
        StringBuilder stringBuilder = new();
        for (int i = 0; i < unicodeString.Length && unicodeString[i] != 0; i++)
        {
            stringBuilder.Append((char)unicodeString[i]);
        }
        return stringBuilder.ToString();
    }
    private static XBE_HEADER ReadXbeHeader(BinaryReader reader)
    {
        XBE_HEADER header = new()
        {
            Magic = reader.ReadUInt32(),
            Signature = reader.ReadBytes((int)CONST_RSA_SIGNATURE_SIZE),
            BaseAddress = reader.ReadUInt32(),
            SizeOfHeaders = reader.ReadUInt32(),
            SizeOfImage = reader.ReadUInt32(),
            SizeOfImageHeader = reader.ReadUInt32(),
            TimeStamp = reader.ReadUInt32(),
            Certificate = reader.ReadUInt32()
        };

        return header;
    }

    private static XBE_CERTIFICATE ReadXbeCertificate(BinaryReader reader)
    {
        XBE_CERTIFICATE certificate = new()
        {
            SizeOfCertificate = reader.ReadUInt32(),
            TimeStamp = reader.ReadUInt32(),
            TitleID = reader.ReadUInt32(),
            TitleName = ReadUnicodeString(reader, CONST_TITLE_NAME_LENGTH),
            AlternateTitleIDs = ReadUInt32Array(reader, CONST_MAX_ALTERNATE_COUNT)
        };

        return certificate;
    }

    private static ushort[] ReadUnicodeString(BinaryReader reader, uint length)
    {
        ushort[] unicodeString = new ushort[length];
        for (int i = 0; i < length; i++)
        {
            unicodeString[i] = reader.ReadUInt16();
        }
        return unicodeString;
    }

    private static  uint [] ReadUInt32Array(BinaryReader reader, uint count)
    {
        uint[] array = new uint[count];
        for (int i = 0; i < count; i++)
        {
            array[i] = reader.ReadUInt32();
        }
        return array;
    }


    //library approval enum (2 bits)
    public enum LIBRARY_APPROVAL : ushort
    {
        UNAPPROVED = 0,
        CONDITIONALLY_APPROVED,
        APPROVED
    }

    public struct XBE_CERTIFICATE
    {
        /*
        The original Xbox has had some revisioning of its executable certificate data over the years

        Games shipping with the 3803 SDK and earlier use version 1 certificates. Version 3 certificates were in use by mid/late 2002. When version 4 certificates started being used is unknown (~2003?)

        Certificate versions can be identified by the SizeOfCertificate field: 
        0xD0 for v1
        0x1D0 for v2
        0x1DC for v3
        0x1EC for v4

        Version 2 certificates include an extra field for additional signature keys which are used in conjuction with the alternate title ids
        (ex: if titleid[3] then use signaturekey[3])
        @NOTE: the purpose of multiple titleid/signature key fields could be to support multi-disc games or games that carry forward saves from a previous game (like mass effect did on Xbox 360) - not entirely sure for Microsofts reasoning

        Version 3 certificates include some extra fields:
        OriginalSizeOfCertificate (assume this was for when games were re-released and certs were upgraded? GOTY kind of games?)
        OnlineServiceName (for distinguishing between passport.net/partner.net?)
        RuntimeSecurityFlags (have observed 0x00000001 and 0x00000002 flags set in this field. its use is unknown)

        Version 4 certificates (final cert. revision) added 1 extra field:
        CodeEncryptionKey (v4 certs. have this filled in (assume auto-generated much like signature keys). its use is unknown)
        */
        public string TitleNameString;
        public string TitleIDHex;
        public uint SizeOfCertificate;
        public uint TimeStamp; //unix time
        public uint TitleID;
        public ushort[] TitleName; //unicode chars.
        public uint[] AlternateTitleIDs; //16 uint's
        public uint AllowedMediaTypes;
        public uint GameRegion;
        public uint GameRatings;
        public uint DiskNumber; //zero-based index
        public uint Version;
        public byte[] LANKey;
        public byte[] SignatureKey;

        //v2 certificates
        public byte[][] AlternateSignatureKeys; //16 0x10 byte keys (0x100 bytes total)

        //v3 certificates
        public uint OriginalSizeOfCertificate;
        public uint OnlineServiceName; //"PART" or "PASS"
        public uint RuntimeSecurityFlags;

        //v3 certificates
        public byte[] CodeEncryptionKey; //0x10 bytes
    }

    public struct IMAGE_IMPORT_BY_NAME //win32 import format
    {
        public ushort Hint;
        public byte Name;
    }

    public struct IMAGE_THUNK_DATA //win32 import format
    {
        public uint ForwarderString; //memory address (PBYTE ForwarderString)
        public uint Function; //memory address of function (PDWORD Function)
        public uint Ordinal; //ord number
        public uint AddressOfData; //memory address of IMAGE_IMPORT_BY_NAME struct
    }

    public struct XBE_IMPORT_DESCRIPTOR
    {
        public uint ImageThunkData; //address in memory of thunk data struct
        public ushort[] ImageName; //unicode, null-term
    }

    public struct XBE_SECTION
    {
        public uint SectionFlags;
        public uint VirtualAddress; //address in memory of section
        public uint VirtualSize; //number of bytes allocated in memory for the section
        public uint RawData; //offset in .xbe file for the section (section zero padded to virtual size in file)
        public uint SizeOfRawData; //number of bytes in .xbe file allocated for the section
        public uint SectionName; //offset in .xbe file of the section name (utf8, null-term)
        public uint SectionReferenceCount; //number of references to the section (applies only when loaded in memory?)
        public uint HeadSharedPageReferenceCount; //address in memory of uint16 value that represents number of shared memory page references
        public uint TailSharedPageReferenceCount; //address in memory of uint16 value that represents number of shared memory page references
        public byte[] SectionDigest; //SHA-1 hash of section
    }

    public struct IMAGE_TLS_DIRECTORY //win32 thread local storage format
    {
        public uint StartAddressOfRawData;
        public uint EndAddressOfRawData;
        public uint AddressOfIndex; //memory address
        public uint AddressOfCallbacks; //memory address, typedef VOID (NTAPI *PIMAGE_TLS_CALLBACK) (PVOID DllHandle, DWORD Reason, PVOID Reserved);
        public uint SizeOfZeroFill;
        public uint Characteristics;
    }

    public struct LIBRARY_VERION
    {
        public char[] LibraryName; //utf8, zero-padded, null-term
        public ushort MajorVersion;
        public ushort MinorVersion;
        public ushort BuildVersion;

        //16bits split between 3 values
        public ushort QFEVersion; //first 13 bits
        public ushort ApprovedLibrary; //next 2 bits, certification status of library version
        public ushort DebugBuild; //last bit, true/false bit if library is debug version
    }

    public struct XBE_HEADER
    {
        public uint Magic; //"XBEH"
        public byte[] Signature; //2048-bit RSA1 signature
        public uint BaseAddress; //memory load address
        public uint SizeOfHeaders;
        public uint SizeOfImage;
        public uint SizeOfImageHeader;
        public uint TimeStamp; //unix time
        public uint Certificate; //memory address of certificate
        public uint NumberOfSections;
        public uint SectionHeaders; //memory address of section headers
        public uint InitFlags;
        public uint AddressOfEntryPoint; //memory address of entry point (typedef VOID (*PXBEIMAGE_ENTRY_POINT)(VOID)) - XOR ENCODED
        public uint TlsDirectory; //memory address of thread local storage (can be zeroed) (IMAGE_TLS_DIRECTORY*) - XOR ENCODED
        public uint SizeOfStackCommit; //default thread stack size
        public uint SizeOfHeapReserve;
        public uint SizeOfHeapCommit;
        public uint NtBaseOfDll; //memory address of PE header (can be zeroed)
        public uint NtSizeOfImage;
        public uint NtChecksum;
        public uint NtTimestamp;
        public uint DebugPathName; //memory address of string representing the debug executable file path (utf8)
        public uint DebugFileName; //memory address of string representing the debug executable file name (utf8)
        public uint DebugUnicodeFileName; //memory address of string representing the debug executable file name (unicode)
        public uint XboxKernelThunkData; //memory address of imported kernel thunks
        public uint ImportDirectory; //memory address of imported non-kernel thunks (zeroed on retail executables)
        public uint NumberOfLibraryVersions;
        public uint LibraryVersion; //memory address of library version structs
        public uint XboxKernelLibraryVersion; //memory address of kernel version library struct
        public uint XapiLibraryVersion; //memory address of XAPI version library struct
        public uint MicrosoftLogo; //memory address of the Microsoft logo
        public uint SizeOfMicrosoftLogo;
        //certificate follows (not part of header)
    }
}
