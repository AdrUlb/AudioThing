#include "Client.hpp"

extern "C" __declspec(dllexport) Client * AudioThing_Client_Create(WAVEFORMATEX * format)
{
	const auto client = new Client(format);

	if (FAILED(client->GetError()))
	{
		delete client;
		return nullptr;
	}

	return client;
}

extern "C" __declspec(dllexport) void AudioThing_Client_Destroy(Client * client)
{
	delete client;
}

extern "C" __declspec(dllexport) void AudioThing_Client_Start(Client * client)
{
	client->Start();
}

extern "C" __declspec(dllexport) void AudioThing_Client_Stop(Client * client)
{
	client->Stop();
}

extern "C" __declspec(dllexport) UINT32 AudioThing_Client_GetBufferFrames(Client * client)
{
	return client->GetBufferFrames();
}

extern "C" __declspec(dllexport) inline UINT32 AudioThing_Client_GetPaddingFrames(Client* client)
{
	return client->GetPaddingFrames();
}

extern "C" __declspec(dllexport) inline BYTE* AudioThing_Client_GetBuffer(Client* client, UINT32 requestFrameCount)
{
	return client->GetBuffer(requestFrameCount);
}

extern "C" __declspec(dllexport) inline void AudioThing_Client_ReleaseBuffer(Client* client, UINT32 writtenFrameCount)
{
	client->ReleaseBuffer(writtenFrameCount);
}
