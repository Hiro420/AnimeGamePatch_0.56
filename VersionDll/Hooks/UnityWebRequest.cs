using System.Runtime.InteropServices;
using System;

namespace HsrPatch.Hooks;

internal class UnityWebRequest
{
	const string _redirectIP = "127.0.0.1";
	const int _redirectPort = 3000; // KazusaHSR's default port
	
	public static NativeDetour<SetUrl_delegate>? SetUrl_hook;

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void SetUrl_delegate(nint instance, nint ptr);


	private static void SetUrl_reimpl(nint instance, nint urlStr)
	{
		string originalUrl = Utils.Il2CppToString(urlStr);
		Console.WriteLine($"[Hook] Original URL: {originalUrl}");

		var uri = new Uri(originalUrl);
		string newUrl = $"http://{_redirectIP}:{_redirectPort}{uri.PathAndQuery}";

		Console.WriteLine($"[Hook] Redirected to: {newUrl}");

		IntPtr newUrlPtr = HsrPatch.Reflection.Convert.PtrToStringAnsi(newUrl);

		SetUrl_hook!.Trampoline(instance, newUrlPtr);
	}

	public static void Init()
	{
		SetUrl_hook = new(
			Program.ModuleHandle + Program.SetUrl_RVA,
			SetUrl_reimpl
		);

		SetUrl_hook!.Attach();
	}
}