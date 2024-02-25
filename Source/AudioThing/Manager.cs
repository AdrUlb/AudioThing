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
		if (libraryName != WasapiAudioClient.LibraryName)
			return 0;

		if (_libAudioThingWasapiHandle != 0)
			return _libAudioThingWasapiHandle;

		var (ridOs, libName) =
			OperatingSystem.IsWindows() ? ("win", $"{libraryName}.dll") :
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

			_isInit = true;
		}
	}
}
