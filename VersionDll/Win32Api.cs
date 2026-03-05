using System.Runtime.InteropServices;
using System;

namespace HsrPatch;

public static class Win32Api
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	public delegate uint ThreadStartRoutine();

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern IntPtr CreateThread(
		IntPtr lpThreadAttributes,
		uint dwStackSize,
		ThreadStartRoutine lpStartAddress,
		IntPtr lpParameter,
		uint dwCreationFlags,
		out uint lpThreadId
	);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool CloseHandle(IntPtr hObject);

	[DllImport("kernel32.dll")]
	public static extern bool AllocConsole();

	[DllImport("kernel32.dll")]
	public static extern IntPtr GetModuleHandle(string lpModuleName);

	[DllImport("user32.dll")]
	public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

	[DllImport("user32.dll")]
	public static extern bool IsWindowVisible(IntPtr hWnd);

	[DllImport("user32.dll")]
	public static extern bool IsWindowEnabled(IntPtr hWnd);

	[DllImport("user32.dll")]
	public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

	[DllImport("user32.dll")]
	public static extern bool IsIconic(IntPtr hWnd);

	public struct RECT
	{
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;
	}
}