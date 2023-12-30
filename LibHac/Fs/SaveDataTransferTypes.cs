﻿using LibHac.Common.FixedArrays;

namespace LibHac.Fs;

public struct RsaEncryptedKey
{
    public Array256<byte> Value;
}

public struct AesKey
{
    public Array16<byte> Value;
}

public struct InitialDataVersion2
{
    public Array8192<byte> Value;
}