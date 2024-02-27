using System.Runtime.InteropServices;

namespace AudioThing;

public partial class WasapiAudioClient : IDisposable
{
	private struct WaveFormat
	{
		public AudioFormat FormatTag;
		public ushort Channels;
		public uint SamplesPerSec;
		public uint AvgBytesPerSec;
		public ushort BlockAlign;
		public ushort BitsPerSample;
	}

	internal const string LibraryName = "AudioThing.Wasapi.Native";

	private readonly nint _handle;

	[LibraryImport(LibraryName, EntryPoint = "AudioThing_Client_Create")]
	private static partial nint NativeCreate(in WaveFormat format);

	[LibraryImport(LibraryName, EntryPoint = "AudioThing_Client_Destroy")]
	private static partial void NativeDestroy(nint handle);

	[LibraryImport(LibraryName, EntryPoint = "AudioThing_Client_Start")]
	private static partial void NativeStart(nint handle);

	[LibraryImport(LibraryName, EntryPoint = "AudioThing_Client_Stop")]
	private static partial void NativeStop(nint handle);

	[LibraryImport(LibraryName, EntryPoint = "AudioThing_Client_GetBufferFrames")]
	private static partial uint NativeGetBufferFrames(nint handle);

	[LibraryImport(LibraryName, EntryPoint = "AudioThing_Client_GetPaddingFrames")]
	private static partial uint NativeGetPaddingFrames(nint handle);

	[LibraryImport(LibraryName, EntryPoint = "AudioThing_Client_GetBuffer")]
	private static partial nint NativeGetBuffer(nint handle, uint requestFrameCount);

	[LibraryImport(LibraryName, EntryPoint = "AudioThing_Client_ReleaseBuffer")]
	private static partial void NativeReleaseBuffer(nint handle, uint writtenFrameCount);

	private readonly WaveFormat _format;

	public readonly AudioFormat Format;
	public readonly int FramesPerSecond;
	public readonly int BitsPerSample;
	public readonly int Channels;
	public readonly uint BufferFrames;
	public readonly int FrameSize;

	public uint PaddingFrames => NativeGetPaddingFrames(_handle);

	public WasapiAudioClient(AudioFormat format, int framesPerSecond, int bitsPerSample, int channels)
	{
		Manager.Init();

		Format = format;
		FramesPerSecond = framesPerSecond;
		BitsPerSample = bitsPerSample;
		Channels = channels;

		FrameSize = (channels * bitsPerSample) / 8;

		_format = new()
		{
			FormatTag = format,
			Channels = (ushort)channels,
			BitsPerSample = (ushort)bitsPerSample,
			BlockAlign = (ushort)FrameSize,
			SamplesPerSec = (uint)framesPerSecond,
			AvgBytesPerSec = (uint)(FrameSize * framesPerSecond)
		};

		_handle = NativeCreate(_format);

		if (_handle == 0)
			throw new("Failed to create audio client.");

		BufferFrames = NativeGetBufferFrames(_handle);
	}

	public void Start() => NativeStart(_handle);
	public void Stop() => NativeStop(_handle);
	public unsafe bool TryGetBuffer<T>(uint requestFrameCount, out Span<T> buffer) where T : unmanaged
	{
		var ptr = NativeGetBuffer(_handle, requestFrameCount);

		if (ptr == 0)
		{
			buffer = [];
			return false;
		}

		buffer = new Span<T>((T*)ptr, (int)requestFrameCount * Channels);
		return true;
	}

	public void ReleaseBuffer(uint writtenFrameCount)
	{
		NativeReleaseBuffer(_handle, writtenFrameCount);
	}

	~WasapiAudioClient() => Dispose(false);

	public void Dispose()
	{
		GC.SuppressFinalize(this);
		Dispose(true);
	}

	private void Dispose(bool disposing)
	{
		NativeDestroy(_handle);
	}
}
