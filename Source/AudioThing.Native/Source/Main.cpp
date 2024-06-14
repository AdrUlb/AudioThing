#include "Client.hpp"

#if defined(_MSC_VER)
#define EXPORT __declspec(dllexport)
#elif defined(__GNUC__)
#define EXPORT __attribute__((visibility("default")))
#else
#pragma error Platform not supported.
#endif

extern "C" EXPORT Client *AudioThing_Client_Create(uint16_t format, uint16_t channels, uint16_t bitsPerSample, uint16_t frameSize, uint32_t framesPerSec)
{
	const auto client = new Client(format, channels, bitsPerSample, frameSize, framesPerSec);

#if defined(_MSC_VER)
	if (FAILED(client->GetError()))
	{
		delete client;
		return nullptr;
	}
#endif

	return client;
}

extern "C" EXPORT void AudioThing_Client_Destroy(Client *client)
{
	delete client;
}

extern "C" EXPORT void AudioThing_Client_Start(Client *client)
{
	client->Start();
}

extern "C" EXPORT void AudioThing_Client_Stop(Client *client)
{
	client->Stop();
}

extern "C" EXPORT uint32_t AudioThing_Client_GetBufferFrames(Client *client)
{
	return client->GetBufferFrames();
}

extern "C" EXPORT uint32_t AudioThing_Client_GetPaddingFrames(Client *client)
{
	return client->GetPaddingFrames();
}

extern "C" EXPORT uint8_t *AudioThing_Client_GetBuffer(Client *client, uint32_t requestFrameCount)
{
	return client->GetBuffer(requestFrameCount);
}

extern "C" EXPORT void AudioThing_Client_ReleaseBuffer(Client *client, uint32_t writtenFrameCount)
{
	client->ReleaseBuffer(writtenFrameCount);
}
