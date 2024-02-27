using AudioThing;

using var client = new WasapiAudioClient(AudioFormat.IeeeFloat, 44100, 32, 2);
Console.WriteLine("Format: " + client.Format);
Console.WriteLine("Frames per second: " + client.FramesPerSecond);
Console.WriteLine("Bits per sample: " + client.BitsPerSample);
Console.WriteLine("Channels: " + client.Channels);
Console.WriteLine("Frame size: " + client.FrameSize);
Console.WriteLine();
Console.WriteLine("Buffer frames: " + client.BufferFrames);
Console.WriteLine("Buffer time: " + client.BufferFrames / (client.FramesPerSecond / 1000.0));

using var fs = File.OpenRead(@"C:\Users\Adrian\Desktop\Firepower.raw");
using var br = new BinaryReader(fs);

client.Start();
var lastWriteCount = 0;

const int maxFrameWriteCount = 2; // Write as little data at once as possible
var maxPadding = client.FramesPerSecond / 100; // Keeping the padding as low as possible will make written data play sooner

Console.WriteLine($"Writing a maximum of {maxFrameWriteCount} frames at once");
Console.WriteLine($"Maximum padding: {maxPadding} frames ({(double)maxPadding / (client.FramesPerSecond / 1000.0):N2}ms delay)");

while (br.BaseStream.Position < br.BaseStream.Length)
{
	var availableFrames = client.BufferFrames - client.PaddingFrames;

	if (availableFrames == 0 || client.PaddingFrames > maxPadding)
		continue;

	var buffer = client.GetBuffer<float>(availableFrames);

	int writtenSamples;
	for (writtenSamples = 0; writtenSamples / client.Channels < maxFrameWriteCount && writtenSamples < buffer.Length / 2 && br.BaseStream.Position < br.BaseStream.Length; writtenSamples += 2)
	{
		buffer[writtenSamples] = br.ReadSingle();
		buffer[writtenSamples + 1] = br.ReadSingle();
	}

	var writtenFrames = writtenSamples / client.Channels;
	client.ReleaseBuffer((uint)writtenFrames);

	lastWriteCount = writtenSamples;
}
Thread.Sleep((int)(lastWriteCount / 44100.0 * 1000.0)); // Make sure there is no audio left to play
client.Stop();
