using System;
using System.Runtime.InteropServices;
using Cetours.Hooking;

namespace HsrPatch;

public class NativeDetour<T> where T : Delegate
{
	private Hook _hook;
	private nint _targetHandle;
	private nint _detourHandle;
	private nint _trampolineHandle;
	private T _trampoline;

	public nint Target
	{
		get => _targetHandle;

		set
		{
			if (value == nint.Zero)
				throw new ArgumentNullException(nameof(value));

			_targetHandle = value;
		}
	}

	public nint Detour
	{
		get => _detourHandle;

		set
		{
			if (value == nint.Zero)
				throw new ArgumentNullException(nameof(value));

			_detourHandle = value;
		}
	}

	public T Trampoline
	{
		get
		{
			if (_trampolineHandle == nint.Zero)
				throw new NullReferenceException("_trampolineHandle");

			if (_trampoline == null)
			{
				_trampoline = Marshal.GetDelegateForFunctionPointer<T>(_trampolineHandle);
			}

			return _trampoline;
		}
	}

	public bool IsHooked { get; private set; }

	public NativeDetour() { }

	public NativeDetour(nint target, T detour, bool autoAttach = true)
	{
		if (target == nint.Zero)
			throw new ArgumentNullException(nameof(target));

		if (detour == null)
			throw new ArgumentNullException(nameof(detour));

		_targetHandle = target;
		_detourHandle = Marshal.GetFunctionPointerForDelegate(detour);

		_hook = Cetours.Cetour.Create(
			_targetHandle,
			_detourHandle
		);

		_trampolineHandle = _hook.New;

		if (autoAttach)
			Attach();
	}

	public NativeDetour(nint target, nint detour, bool autoAttach = true)
	{
		if (target == nint.Zero)
			throw new ArgumentNullException(nameof(target));

		if (detour == nint.Zero)
			throw new ArgumentNullException(nameof(detour));

		_targetHandle = target;
		_detourHandle = detour;

		_hook = Cetours.Cetour.Create(
			_targetHandle,
			_detourHandle
		);

		_trampolineHandle = _hook.New;

		if (autoAttach)
			Attach();
	}

	public void Attach()
	{
		if (IsHooked)
			return;

		if (_targetHandle == nint.Zero)
			throw new NullReferenceException("The Native Detour's target has not been set!");

		if (_detourHandle == nint.Zero)
			throw new NullReferenceException("The Native Detour's detour has not been set!");

		_hook.Attach();

		IsHooked = true;
	}

	public void Detach()
	{
		if (!IsHooked)
			return;

		if (_targetHandle == nint.Zero)
			throw new NullReferenceException("The Native Detour's target has not been set!");

		_hook.Detach();

		IsHooked = false;

		_trampoline = null;
		_trampolineHandle = nint.Zero;
	}
}