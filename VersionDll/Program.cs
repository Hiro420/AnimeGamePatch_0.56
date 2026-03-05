using HsrPatch.Hooks;
using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using static HsrPatch.Win32Api;

namespace HsrPatch;

class Program
{
	public static IntPtr ModuleHandle = IntPtr.Zero;

	public const int PtrToStringAnsi_RVA = 0x1A64980;
	public const int targetFrameRateSetter_RVA = 0x2290D70;
	public const int SetUrl_RVA = 0x2CE2240; // 0.56 HSR uses url setter instead of internal method :/

	private delegate void targetFrameRateSetter_delegate(int value);
	private static targetFrameRateSetter_delegate? _getFullNameDelegate;

	private static CancellationTokenSource? _cts;
	private static Task? _periodicTask;

	private static uint RunThread()
	{
		Run();
		return 0;
	}

	private static void Run()
	{
		GCSettings.LatencyMode = GCLatencyMode.Batch;
		Win32Api.AllocConsole();

		AppDomain.CurrentDomain.UnhandledException += (s, e) =>
		{
			Console.WriteLine($"Unhandled Exception: {e.ExceptionObject}");
		};

		while (ModuleHandle == IntPtr.Zero)
		{
			var h = GetModuleHandle("GameAssembly.dll");
			if (h != IntPtr.Zero)
			{
				ModuleHandle = h;
				break;
			}

			Console.WriteLine("Waiting for GameAssembly.dll to load...");
			Thread.Sleep(2000);
		}

		UnityWebRequest.Init();
		Console.WriteLine("HsrPatch initialized successfully.");

		_getFullNameDelegate = Marshal.GetDelegateForFunctionPointer<targetFrameRateSetter_delegate>(ModuleHandle + targetFrameRateSetter_RVA);

		_cts = new CancellationTokenSource();
		_periodicTask = PeriodicLoopAsync(_cts.Token);
	}

	private static async Task PeriodicLoopAsync(CancellationToken token)
	{
		using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

		while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
		{
			try
			{
				DoAction();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Periodic action error: {ex}");
			}
		}
	}

	private static void DoAction()
	{
		if (_getFullNameDelegate != null)
		{
			_getFullNameDelegate(360); // Set target frame rate to 360
			//Console.WriteLine("Set target frame rate to 360.");
		}
	}

	[UnmanagedCallersOnly(EntryPoint = "DllMain", CallConvs = [typeof(CallConvStdcall)])]
	public static bool DllMain(IntPtr hinstDLL, uint fdwReason, IntPtr lpvReserved)
	{
		switch (fdwReason)
		{
			case 1: // DLL_PROCESS_ATTACH
				{
					IntPtr threadHandle = Win32Api.CreateThread(IntPtr.Zero, 0, RunThread, IntPtr.Zero, 0, out _);
					if (threadHandle != IntPtr.Zero)
						Win32Api.CloseHandle(threadHandle);
					break;
				}
			case 0: // DLL_PROCESS_DETACH
				{
					try { _cts?.Cancel(); } catch { }
					break;
				}
		}

		return true;
	}
}
