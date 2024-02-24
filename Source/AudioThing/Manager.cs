using System.Reflection;
using System.Runtime.InteropServices;

namespace AudioThing;

internal static class Manager
{
	private static bool _isInit;
	private static readonly object _initLock = new();

	private static nint _libAudioThingWasapiHandle = 0;

	private static nint AudioThingWasapiImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
	{
		if (libraryName != AudioThingWasapi.LibraryName)
			return 0;

		if (_libAudioThingWasapiHandle != 0)
			return _libAudioThingWasapiHandle;

		var (ridOs, libName) =
			OperatingSystem.IsWindows() ? ("win", "AudioThing.Wasapi.dll") :
			throw new PlatformNotSupportedException();

		var ridPlatform = RuntimeInformation.ProcessArchitecture switch
		{
			Architecture.X64 => "x64",
			Architecture.X86 => "x86",
			_ => throw new PlatformNotSupportedException()
		};

		var rid = $"{ridOs}-{ridPlatform}";
		var libPath = Path.Combine(AppContext.BaseDirectory, "runtimes", rid, "native", libName);

		_libAudioThingWasapiHandle = NativeLibrary.Load(libPath);

		return _libAudioThingWasapiHandle;
	}

	private static void AppDomain_CurrentDomain_ProcessExit(object? sender, EventArgs args)
	{
		Clean();
	}

	internal static void Init()
	{
		lock (_initLock)
		{
			if (_isInit)
				return;

			try
			{
				NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), AudioThingWasapiImportResolver);
			}
			catch { }

			AppDomain.CurrentDomain.ProcessExit += AppDomain_CurrentDomain_ProcessExit;

			_isInit = true;
		}
	}

	internal static void Clean()
	{
		lock (_initLock)
		{
			if (!_isInit)
				return;

			AppDomain.CurrentDomain.ProcessExit -= AppDomain_CurrentDomain_ProcessExit;

			_isInit = false;
		}
	}
}
