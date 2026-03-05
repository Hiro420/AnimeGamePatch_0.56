using System.Runtime.CompilerServices;
using System;

namespace HsrPatch;

public static class Utils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe string Il2CppToString(IntPtr ptr)
    {
        int length = *(int*)(ptr + 0x10);
        return new string((char*)(ptr + 0x14), 0, length);
    }
}