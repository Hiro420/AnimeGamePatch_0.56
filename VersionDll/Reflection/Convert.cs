using System.Runtime.InteropServices;
using System;

namespace HsrPatch.Reflection;

public class Convert
{
	public delegate IntPtr _MarshalPtrToStringAnsi(IntPtr ptr);
	public static IntPtr PtrToStringAnsi(string str)
	{
		IntPtr methodPtr = Program.ModuleHandle + Program.PtrToStringAnsi_RVA;
		var func = Marshal.GetDelegateForFunctionPointer<_MarshalPtrToStringAnsi>(methodPtr);
		var cStr = Marshal.StringToHGlobalAnsi(str);
		var ptr = func(cStr);
		Marshal.FreeHGlobal(cStr);
		return ptr;
	}
}