using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using HsrPatch;

internal static unsafe class VersionProxy
{
	[DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern uint GetSystemDirectoryW(char* lpBuffer, uint uSize);

	[DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern nint LoadLibraryW(char* lpFileName);

	[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
	private static extern nint GetProcAddress(nint hModule, string procName);

	[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
	private static extern uint GetLastError();

	private static nint _real;
	private static readonly nint[] _fp = new nint[15];
	private static int _init;
	private static readonly object _lock = new();

	private static void EnsureInit()
	{
		if (Volatile.Read(ref _init) == 1)
			return;

		lock (_lock)
		{
			if (_init == 1)
				return;

			Span<char> buf = stackalloc char[260];
			fixed (char* p = buf)
			{
				uint len = GetSystemDirectoryW(p, (uint)buf.Length);
				if (len == 0 || len + "\\version.dll".Length + 1 >= buf.Length)
					Environment.FailFast("VersionProxy: GetSystemDirectoryW failed.");

				Span<char> s = buf.Slice((int)len);
				"\\version.dll".AsSpan().CopyTo(s);
				s["\\version.dll".Length] = '\0';

				_real = LoadLibraryW(p);
			}

			if (_real == 0)
			{
				Environment.FailFast(
					$"VersionProxy: failed to load real version.dll from System32 (GLE={GetLastError()}).");
			}

			_fp[0] = Must("GetFileVersionInfoA");
			_fp[1] = Must("GetFileVersionInfoByHandle");
			_fp[2] = Must("GetFileVersionInfoExW");
			_fp[3] = Must("GetFileVersionInfoSizeA");
			_fp[4] = Must("GetFileVersionInfoSizeExW");
			_fp[5] = Must("GetFileVersionInfoSizeW");
			_fp[6] = Must("GetFileVersionInfoW");
			_fp[7] = Must("VerFindFileA");
			_fp[8] = Must("VerFindFileW");
			_fp[9] = Must("VerInstallFileA");
			_fp[10] = Must("VerInstallFileW");
			_fp[11] = Must("VerLanguageNameA");
			_fp[12] = Must("VerLanguageNameW");
			_fp[13] = Must("VerQueryValueA");
			_fp[14] = Must("VerQueryValueW");

			Volatile.Write(ref _init, 1);
		}

		nint Must(string name)
		{
			nint p = GetProcAddress(_real, name);
			if (p == 0)
				Environment.FailFast($"VersionProxy: failed to resolve {name} (GLE={GetLastError()}).");
			return p;
		}

		//LoadLibraryW_String("Cb2Patch.dll");
	}

	private static nint ResolveExportProc(int index)
	{
		EnsureInit();
		if ((uint)index >= _fp.Length)
			Environment.FailFast($"VersionProxy: invalid export index {index}.");
		return _fp[index];
	}

	[UnmanagedCallersOnly(EntryPoint = "GetFileVersionInfoA", CallConvs = new[] { typeof(CallConvStdcall) })]
	public static int GetFileVersionInfoA(byte* lptstrFilename, uint dwHandle, uint dwLen, void* lpData)
	{
		var p = (delegate* unmanaged[Stdcall]<byte*, uint, uint, void*, int>)ResolveExportProc(0);
		return p(lptstrFilename, dwHandle, dwLen, lpData);
	}

	[UnmanagedCallersOnly(EntryPoint = "GetFileVersionInfoByHandle", CallConvs = new[] { typeof(CallConvStdcall) })]
	public static int GetFileVersionInfoByHandle(uint dwFlags, nint hFile, void** lplpData, uint* pdwLen)
	{
		var p = (delegate* unmanaged[Stdcall]<uint, nint, void**, uint*, int>)ResolveExportProc(1);
		return p(dwFlags, hFile, lplpData, pdwLen);
	}

	[UnmanagedCallersOnly(EntryPoint = "GetFileVersionInfoExW", CallConvs = new[] { typeof(CallConvStdcall) })]
	public static int GetFileVersionInfoExW(uint dwFlags, char* lpwstrFilename, uint dwHandle, uint dwLen, void* lpData)
	{
		var p = (delegate* unmanaged[Stdcall]<uint, char*, uint, uint, void*, int>)ResolveExportProc(2);
		return p(dwFlags, lpwstrFilename, dwHandle, dwLen, lpData);
	}

	[UnmanagedCallersOnly(EntryPoint = "GetFileVersionInfoSizeA", CallConvs = new[] { typeof(CallConvStdcall) })]
	public static uint GetFileVersionInfoSizeA(byte* lptstrFilename, uint* lpdwHandle)
	{
		var p = (delegate* unmanaged[Stdcall]<byte*, uint*, uint>)ResolveExportProc(3);
		return p(lptstrFilename, lpdwHandle);
	}

	[UnmanagedCallersOnly(EntryPoint = "GetFileVersionInfoSizeExW", CallConvs = new[] { typeof(CallConvStdcall) })]
	public static uint GetFileVersionInfoSizeExW(uint dwFlags, char* lpwstrFilename, uint* lpdwHandle)
	{
		var p = (delegate* unmanaged[Stdcall]<uint, char*, uint*, uint>)ResolveExportProc(4);
		return p(dwFlags, lpwstrFilename, lpdwHandle);
	}

	[UnmanagedCallersOnly(EntryPoint = "GetFileVersionInfoSizeW", CallConvs = new[] { typeof(CallConvStdcall) })]
	public static uint GetFileVersionInfoSizeW(char* lptstrFilename, uint* lpdwHandle)
	{
		var p = (delegate* unmanaged[Stdcall]<char*, uint*, uint>)ResolveExportProc(5);
		return p(lptstrFilename, lpdwHandle);
	}

	[UnmanagedCallersOnly(EntryPoint = "GetFileVersionInfoW", CallConvs = new[] { typeof(CallConvStdcall) })]
	public static int GetFileVersionInfoW(char* lptstrFilename, uint dwHandle, uint dwLen, void* lpData)
	{
		var p = (delegate* unmanaged[Stdcall]<char*, uint, uint, void*, int>)ResolveExportProc(6);
		return p(lptstrFilename, dwHandle, dwLen, lpData);
	}

	[UnmanagedCallersOnly(EntryPoint = "VerFindFileA", CallConvs = new[] { typeof(CallConvStdcall) })]
	public static uint VerFindFileA(
		uint uFlags,
		byte* szFileName,
		byte* szWinDir,
		byte* szAppDir,
		byte* szCurDir,
		uint* puCurDirLen,
		byte* szDestDir,
		uint* puDestDirLen)
	{
		var p = (delegate* unmanaged[Stdcall]<uint, byte*, byte*, byte*, byte*, uint*, byte*, uint*, uint>)
			ResolveExportProc(7);
		return p(uFlags, szFileName, szWinDir, szAppDir, szCurDir, puCurDirLen, szDestDir, puDestDirLen);
	}

	[UnmanagedCallersOnly(EntryPoint = "VerFindFileW", CallConvs = new[] { typeof(CallConvStdcall) })]
	public static uint VerFindFileW(
		uint uFlags,
		char* szFileName,
		char* szWinDir,
		char* szAppDir,
		char* szCurDir,
		uint* puCurDirLen,
		char* szDestDir,
		uint* puDestDirLen)
	{
		var p = (delegate* unmanaged[Stdcall]<uint, char*, char*, char*, char*, uint*, char*, uint*, uint>)
			ResolveExportProc(8);
		return p(uFlags, szFileName, szWinDir, szAppDir, szCurDir, puCurDirLen, szDestDir, puDestDirLen);
	}

	[UnmanagedCallersOnly(EntryPoint = "VerInstallFileA", CallConvs = new[] { typeof(CallConvStdcall) })]
	public static uint VerInstallFileA(
		uint uFlags,
		byte* szSrcFileName,
		byte* szDestFileName,
		byte* szSrcDir,
		byte* szDestDir,
		byte* szCurDir,
		byte* szTmpFile,
		uint* puTmpFileLen)
	{
		var p = (delegate* unmanaged[Stdcall]<uint, byte*, byte*, byte*, byte*, byte*, byte*, uint*, uint>)
			ResolveExportProc(9);
		return p(uFlags, szSrcFileName, szDestFileName, szSrcDir, szDestDir, szCurDir, szTmpFile, puTmpFileLen);
	}

	[UnmanagedCallersOnly(EntryPoint = "VerInstallFileW", CallConvs = new[] { typeof(CallConvStdcall) })]
	public static uint VerInstallFileW(
		uint uFlags,
		char* szSrcFileName,
		char* szDestFileName,
		char* szSrcDir,
		char* szDestDir,
		char* szCurDir,
		char* szTmpFile,
		uint* puTmpFileLen)
	{
		var p = (delegate* unmanaged[Stdcall]<uint, char*, char*, char*, char*, char*, char*, uint*, uint>)
			ResolveExportProc(10);
		return p(uFlags, szSrcFileName, szDestFileName, szSrcDir, szDestDir, szCurDir, szTmpFile, puTmpFileLen);
	}

	[UnmanagedCallersOnly(EntryPoint = "VerLanguageNameA", CallConvs = new[] { typeof(CallConvStdcall) })]
	public static uint VerLanguageNameA(uint wLang, byte* szLang, uint cchLang)
	{
		var p = (delegate* unmanaged[Stdcall]<uint, byte*, uint, uint>)ResolveExportProc(11);
		return p(wLang, szLang, cchLang);
	}

	[UnmanagedCallersOnly(EntryPoint = "VerLanguageNameW", CallConvs = new[] { typeof(CallConvStdcall) })]
	public static uint VerLanguageNameW(uint wLang, char* szLang, uint cchLang)
	{
		var p = (delegate* unmanaged[Stdcall]<uint, char*, uint, uint>)ResolveExportProc(12);
		return p(wLang, szLang, cchLang);
	}

	[UnmanagedCallersOnly(EntryPoint = "VerQueryValueA", CallConvs = new[] { typeof(CallConvStdcall) })]
	public static int VerQueryValueA(void* pBlock, byte* lpSubBlock, void** lplpBuffer, uint* puLen)
	{
		var p = (delegate* unmanaged[Stdcall]<void*, byte*, void**, uint*, int>)ResolveExportProc(13);
		return p(pBlock, lpSubBlock, lplpBuffer, puLen);
	}

	[UnmanagedCallersOnly(EntryPoint = "VerQueryValueW", CallConvs = new[] { typeof(CallConvStdcall) })]
	public static int VerQueryValueW(void* pBlock, char* lpSubBlock, void** lplpBuffer, uint* puLen)
	{
		var p = (delegate* unmanaged[Stdcall]<void*, char*, void**, uint*, int>)ResolveExportProc(14);
		return p(pBlock, lpSubBlock, lplpBuffer, puLen);
	}

	[DllImport("kernel32.dll", EntryPoint = "LoadLibraryW",
		ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern nint LoadLibraryW_String(string lpFileName);
}