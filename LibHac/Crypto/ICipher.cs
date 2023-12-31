﻿using System;
using LibHac.Common;

namespace LibHac.Crypto;

public interface ICipher
{
    int Transform(ReadOnlySpan<byte> input, Span<byte> output);
}

public interface ICipherWithIv : ICipher
{
    ref Buffer16 Iv { get; }
}