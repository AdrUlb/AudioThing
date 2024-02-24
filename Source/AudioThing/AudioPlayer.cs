using System.Runtime.InteropServices;

namespace AudioThing;

internal static partial class AudioThingWasapi
{
	public const string LibraryName = "AudioThing.Wasapi";

	internal struct WAVEFORMATEX
	{
		public ushort FormatTag;
		public ushort Channels;
		public uint SamplesPerSec;
		public uint AvgBytesPerSec;
		public ushort BlockAlign;
		public ushort BitsPerSample;
		public ushort Size;
	}

	internal delegate void PlayCallback(uint frames, nint buffer);

	[LibraryImport(LibraryName, EntryPoint = "AudioThing_CreateContext")]
	internal static partial nint CreateContext(in WAVEFORMATEX fmt);

	[LibraryImport(LibraryName, EntryPoint = "AudioThing_DestroyContext")]
	internal static partial void DestroyContext(nint context);

	[LibraryImport(LibraryName, EntryPoint = "AudioThing_Play")]
	internal static partial void Play(nint context, PlayCallback callback);

	[LibraryImport(LibraryName, EntryPoint = "AudioThing_Stop")]
	internal static partial void Stop(nint context);

}

public unsafe partial class AudioPlayer<T> : IDisposable
{
	public delegate int DataCallback(Span<T> buffer);

	private DataCallback _dataCallback;

#pragma warning disable IDE0052 // Remove unread private members
	private readonly AudioThingWasapi.PlayCallback _playCallback;
#pragma warning restore IDE0052 // Remove unread private members

	private nint _context;

	public bool Playing { get; private set; } = false;

	public readonly int SamplesPerSecond;
	public readonly int BitsPerSample;
	public readonly int Channels;
	public readonly AudioFormat AudioFormat;

	private int _bytesPerFrame;

	public AudioPlayer(int samplesPerSecond, int bitsPerSample, int channels, AudioFormat audioFormat, DataCallback dataCallback)
	{
		Manager.Init();

		SamplesPerSecond = samplesPerSecond;
		BitsPerSample = bitsPerSample;
		Channels = channels;
		AudioFormat = audioFormat;
		_playCallback = Callback;
		_dataCallback = dataCallback;

		_bytesPerFrame = bitsPerSample * channels / 8;

		AudioThingWasapi.WAVEFORMATEX format;
		format.Size = 0;
		format.FormatTag = audioFormat switch
		{
			AudioFormat.Pcm => 1,
			AudioFormat.Float => 3,
			_ => throw new ArgumentException(null, nameof(audioFormat))
		}; // PCM audio
		format.Channels = (ushort)channels; // How many audio channels (how many samples per frame)
		format.SamplesPerSec = (uint)samplesPerSecond; // How many samples per second
		format.BitsPerSample = (ushort)bitsPerSample; // How many bits per sample
		format.BlockAlign = (ushort)_bytesPerFrame; // Number of bytes per frame
		format.AvgBytesPerSec = format.BlockAlign * format.SamplesPerSec;

		_context = AudioThingWasapi.CreateContext(format);
		if (_context == 0)
			throw new();
	}

	private void Callback(uint frames, nint buffer)
	{
		var buf = new Span<T>((void*)buffer, (int)(frames * Channels));
		_dataCallback(buf);
	}

	public void Play()
	{
		if (Playing)
			return;

		Playing = true;
		new Thread(() => AudioThingWasapi.Play(_context, _playCallback)).Start();
		;
	}

	public void Stop()
	{
		if (!Playing)
			return;

		AudioThingWasapi.Stop(_context);
		Playing = false;
	}

	~AudioPlayer() => Dispose(false);

	public void Dispose()
	{
		GC.SuppressFinalize(this);
		Dispose(true);
	}

	private void Dispose(bool disposing)
	{
		AudioThingWasapi.DestroyContext(_context);
	}
}
