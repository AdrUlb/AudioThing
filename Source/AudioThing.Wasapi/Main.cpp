#include <iostream>
#include <windows.h>
#include <mmdeviceapi.h>
#include <audioclient.h>

constexpr auto REFTIMES_PER_SEC = 100000;
constexpr auto REFTIMES_PER_MILLISEC = 200;

const CLSID CLSID_MMDeviceEnumerator = __uuidof(MMDeviceEnumerator);
const IID IID_IMMDeviceEnumerator = __uuidof(IMMDeviceEnumerator);
const IID IID_IAudioClient = __uuidof(IAudioClient);
const IID IID_IAudioRenderClient = __uuidof(IAudioRenderClient);

#define GOTO_END_IF_FAILED(hres) if (FAILED(hres)) { goto end; }

class Context
{
public:
	Context(IMMDeviceEnumerator* deviceEnumerator, IMMDevice* device, IAudioClient* client, IAudioRenderClient* renderClient, WAVEFORMATEX* format);
	~Context();
	void Play(void(*callback)(UINT32, BYTE*));
	void Stop();
private:
	IMMDeviceEnumerator* _deviceEnumerator;
	IMMDevice* _device;
	IAudioClient* _client;
	IAudioRenderClient* _renderClient;
	WAVEFORMATEX _format;
	volatile bool _playing = false;
};

Context::Context(IMMDeviceEnumerator* deviceEnumerator, IMMDevice* device, IAudioClient* client, IAudioRenderClient* renderClient, WAVEFORMATEX* format)
	: _deviceEnumerator(deviceEnumerator), _device(device), _client(client), _renderClient(renderClient), _format(*format)
{

}

void Context::Play(void(*callback)(UINT32, BYTE*))
{
	if (_playing)
		return;

	UINT32 bufferFrameCount;
	_client->GetBufferSize(&bufferFrameCount);
	_client->Start();

	REFERENCE_TIME bufferDuration = (double)REFTIMES_PER_SEC * bufferFrameCount / _format.nSamplesPerSec;

	UINT32 numFramesAvailable = bufferFrameCount;
	_playing = true;
	while (_playing)
	{
		BYTE* buffer;
		_renderClient->GetBuffer(numFramesAvailable, &buffer);
		callback(numFramesAvailable, buffer);
		_renderClient->ReleaseBuffer(numFramesAvailable, 0);

		Sleep((DWORD)(bufferDuration / REFTIMES_PER_MILLISEC / 4 * 3));

		UINT32 numFramesPadding;
		_client->GetCurrentPadding(&numFramesPadding);
		numFramesAvailable = bufferFrameCount - numFramesPadding;
	}

	_client->Stop();
}

void Context::Stop()
{
	_playing = false;
}

Context::~Context()
{
	_deviceEnumerator->Release();
	_device->Release();
	_client->Release();
	_renderClient->Release();
}

Context* CreateContext(WAVEFORMATEX* format)
{
	IMMDeviceEnumerator* deviceEnumerator = nullptr;
	IMMDevice* device = nullptr;
	IAudioClient* client = nullptr;
	IAudioRenderClient* renderClient = nullptr;

	// Create IMM device enumerator to find audio device
	GOTO_END_IF_FAILED(CoCreateInstance(CLSID_MMDeviceEnumerator, nullptr, CLSCTX_ALL, IID_IMMDeviceEnumerator, (void**)&deviceEnumerator));

	// Get default playback device
	GOTO_END_IF_FAILED(deviceEnumerator->GetDefaultAudioEndpoint(eRender, eConsole, &device));

	// Create an audio client using the device
	GOTO_END_IF_FAILED(device->Activate(IID_IAudioClient, CLSCTX_ALL, nullptr, (void**)&client));

	// Initialize audio client
	GOTO_END_IF_FAILED(client->Initialize(AUDCLNT_SHAREMODE_SHARED, AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM | AUDCLNT_STREAMFLAGS_SRC_DEFAULT_QUALITY, REFTIMES_PER_SEC, 0, format, NULL));

	// Get audio playback service from audio client
	GOTO_END_IF_FAILED(client->GetService(IID_IAudioRenderClient, (void**)&renderClient));

	return new Context(deviceEnumerator, device, client, renderClient, format);

end:
	if (deviceEnumerator)
		deviceEnumerator->Release();
	if (device)
		device->Release();
	if (client)
		client->Release();
	if (renderClient)
		renderClient->Release();

	return nullptr;
}

void DestroyContext(Context* context)
{
	delete context;
}

extern "C" __declspec(dllexport) Context * AudioThing_CreateContext(WAVEFORMATEX * format)
{
	return CreateContext(format);
}

extern "C" __declspec(dllexport) void AudioThing_DestroyContext(Context * context)
{
	DestroyContext(context);
}

extern "C" __declspec(dllexport) void AudioThing_Play(Context * context, void(*callback)(UINT32, BYTE*))
{
	context->Play(callback);
}

extern "C" __declspec(dllexport) void AudioThing_Stop(Context * context)
{
	context->Stop();
}
