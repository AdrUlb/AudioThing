using System.Runtime.InteropServices;

namespace AudioThing;

public partial class AudioClient : IDisposable
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

	internal const string LibraryName = "AudioThing.Native";

	private readonly nint _handle;

	[LibraryImport(LibraryName, EntryPoint = "AudioThing_Client_Create")]
	private static partial nint NativeCreate(AudioFormat format, ushort channels, ushort bitsPerSample, ushort frameSize, uint framesPerSec);

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
	public readonly uint FramesPerSecond;
	public readonly ushort BitsPerSample;
	public readonly ushort Channels;
	public readonly uint BufferFrames;
	public readonly ushort FrameSize;

	public uint PaddingFrames => NativeGetPaddingFrames(_handle);

	public AudioClient(AudioFormat format, int framesPerSecond, int bitsPerSample, int channels)
	{
		Manager.Init();

		Format = format;
		FramesPerSecond = (uint)framesPerSecond;
		BitsPerSample = (ushort)bitsPerSample;
		Channels = (ushort)channels;

		FrameSize = (ushort)((channels * bitsPerSample) / 8);

		_format = new()
		{
			FormatTag = format,
			Channels = Channels,
			BitsPerSample = BitsPerSample,
			BlockAlign = FrameSize,
			SamplesPerSec = FramesPerSecond,
			AvgBytesPerSec = FrameSize * FramesPerSecond
		};

		_handle = NativeCreate(format, (ushort)channels, (ushort)bitsPerSample, FrameSize, FramesPerSecond);

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

	~AudioClient() => Dispose(false);

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
