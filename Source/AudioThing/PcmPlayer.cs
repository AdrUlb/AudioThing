using System.Runtime.InteropServices;

namespace AudioThing;

public unsafe partial class PcmPlayer : IDisposable
{
	public delegate int DataCallback(Span<byte> buffer);

	private delegate void WaveOutProc(nint handle, WaveOutMsg msg, nint dwInstance, nint dwParam1, nint dwParam2);

	enum WaveOutMsg : uint
	{
		Open = 955,
		Close = 956,
		Done = 957,
	}

	private struct WAVEFORMATEX
	{
		public ushort FormatTag;
		public ushort Channels;
		public uint SamplesPerSec;
		public uint AvgBytesPerSec;
		public ushort BlockAlign;
		public ushort BitsPerSample;
		public ushort Size;
	}

	private struct WAVEHDR
	{
		public nint Data;
		public uint BufferLength;
		public uint BytesRecorded;
		public uint* User;
		public uint Flags;
		public uint Loops;
		public WAVEHDR* Next;
		public nint Reserved;
	};

	[LibraryImport("winmm", EntryPoint = "waveOutOpen")]
	private static partial int waveOutOpen(out nint handle, int deviceId, in WAVEFORMATEX format, WaveOutProc callback, nint dwInstance, int openFlags);

	[LibraryImport("winmm", EntryPoint = "waveOutClose")]
	private static partial int waveOutClose(nint handle);

	[LibraryImport("winmm", EntryPoint = "waveOutPrepareHeader")]
	private static partial int waveOutPrepareHeader(nint handle, ref WAVEHDR hdr, uint hdrSize);

	[LibraryImport("winmm", EntryPoint = "waveOutUnprepareHeader")]
	private static partial int waveOutUnprepareHeader(nint handle, ref WAVEHDR hdr, uint hdrSize);

	[LibraryImport("winmm", EntryPoint = "waveOutWrite")]
	private static partial int waveOutWrite(nint handle, ref WAVEHDR hdr, uint hdrSize);

	private const int CALLBACK_FUNCTION = 0x30000;
	private const int WAVE_MAPPER = -1;

	private DataCallback _dataCallback;
	private nint _handle;

	// Looks stupid but is required so the GC doesn't delete the callback because it is only referenced in unmanaged code
#pragma warning disable IDE0052 // Remove unread private members
	private readonly WaveOutProc _waveOutCallback;
#pragma warning restore IDE0052 // Remove unread private members
	private int _blockSize;
	private readonly WAVEHDR[] _blocks = new WAVEHDR[3];
	private int _currentBlock = 0;

	private volatile int _playingBlocksCount = 0;

	public bool Playing { get; private set; } = false;

	public PcmPlayer(int samplesPerSecond, int bitsPerSample, int channels, DataCallback dataCallback, int blockCount = -1)
	{
		_dataCallback = dataCallback;

		var blockAlign = bitsPerSample * channels / 8;

		WAVEFORMATEX format;
		format.Size = (ushort)sizeof(WAVEFORMATEX);
		format.FormatTag = 1; // PCM audio
		format.Channels = (ushort)channels; // How many audio channels (how many samples per "block")
		format.SamplesPerSec = (uint)samplesPerSecond; // How many samples per second
		format.BitsPerSample = (ushort)bitsPerSample; // How many bits per sample
		format.BlockAlign = (ushort)blockAlign; // Number of bytes per "block"
		format.AvgBytesPerSec = format.BlockAlign * format.SamplesPerSec;

		if (waveOutOpen(out _handle, WAVE_MAPPER, format, _waveOutCallback = Callback, 0, CALLBACK_FUNCTION) != 0)
			throw new("Failed to open waveOut device.");

		// 50ms of audio data per buffer if block size not specified
		_blockSize = blockAlign * samplesPerSecond / 20;

		if (blockCount != -1)
		{
			_blockSize = blockCount * blockAlign;
		}

		for (var i = 0; i < _blocks.Length; i++)
		{
			var bufferPtr = Marshal.AllocHGlobal((int)_blockSize);
			var buffer = new Span<byte>((void*)bufferPtr, (int)_blockSize);

			_blocks[i].Data = bufferPtr;
			_blocks[i].BufferLength = (uint)_blockSize;
		}
	}

	private void Callback(nint handle, WaveOutMsg msg, nint dwInstance, nint dwParam1, nint dwParam2)
	{
		switch (msg)
		{
			case WaveOutMsg.Open:
				break;
			case WaveOutMsg.Done:
				{
					_playingBlocksCount--;
					if (!Playing)
						break;

					PlayNextBlock();
				}
				break;
		}
	}

	private void PlayNextBlock()
	{
		var byteCount = Math.Clamp(_dataCallback(new Span<byte>((void*)_blocks[_currentBlock].Data, (int)_blockSize)), 0, _blockSize);
		_blocks[_currentBlock].BufferLength = (uint)byteCount;

		if (waveOutPrepareHeader(_handle, ref _blocks[_currentBlock], (uint)sizeof(WAVEHDR)) != 0)
			throw new("Failed to prepare header.");

		if (waveOutWrite(_handle, ref _blocks[_currentBlock], (uint)sizeof(WAVEHDR)) != 0)
			throw new("Failed to write audio data.");

		_currentBlock++;
		_currentBlock %= _blocks.Length;

		_playingBlocksCount++;
	}

	public void Play()
	{
		if (Playing)
			return;

		PlayNextBlock();
		PlayNextBlock();
		Playing = true;
	}

	public void Stop()
	{
		Playing = false;
		SpinWait.SpinUntil(() => _playingBlocksCount == 0);
	}

	~PcmPlayer() => Dispose(false);

	public void Dispose()
	{
		GC.SuppressFinalize(this);
		Dispose(true);
	}

	private void Dispose(bool disposing)
	{
		for (var i = 0; i < _blocks.Length; i++)
		{
			Marshal.FreeHGlobal(_blocks[i].Data);
		}

		if (waveOutClose(_handle) != 0)
			throw new("Failed to close waveOut device.");
	}
}
