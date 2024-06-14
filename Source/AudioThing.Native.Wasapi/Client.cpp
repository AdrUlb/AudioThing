#include "Client.hpp"

constexpr const GUID CLSID_MMDeviceEnumerator = __uuidof(MMDeviceEnumerator);
constexpr const IID IID_IMMDeviceEnumerator = __uuidof(IMMDeviceEnumerator);
constexpr const IID IID_IAudioClient = __uuidof(IAudioClient);
constexpr const IID IID_IAudioRenderClient = __uuidof(IAudioRenderClient);

Client::Client(WAVEFORMATEX* format) : _deviceEnumerator(nullptr), _device(nullptr), _audioClient(nullptr), _audioRenderClient(nullptr)
{
	constexpr REFERENCE_TIME bufferDuration = (REFERENCE_TIME)10'000 * 1; // 1 = 100ns, 10'000 = 1'000'000ns = 1'000µs = 1ms

	SET_ERROR_AND_RETURN_IF_FAILED(CoCreateInstance(CLSID_MMDeviceEnumerator, nullptr, CLSCTX_ALL, IID_IMMDeviceEnumerator, (void**)&_deviceEnumerator));
	SET_ERROR_AND_RETURN_IF_FAILED(_deviceEnumerator->GetDefaultAudioEndpoint(eRender, eConsole, &_device));
	SET_ERROR_AND_RETURN_IF_FAILED(_device->Activate(IID_IAudioClient, CLSCTX_ALL, nullptr, (void**)&_audioClient));
	SET_ERROR_AND_RETURN_IF_FAILED(_audioClient->Initialize(AUDCLNT_SHAREMODE_SHARED, AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM | AUDCLNT_STREAMFLAGS_SRC_DEFAULT_QUALITY, bufferDuration, 0, format, nullptr));
	SET_ERROR_AND_RETURN_IF_FAILED(_audioClient->GetService(IID_IAudioRenderClient, (void**)&_audioRenderClient));
}

Client::~Client()
{
	if (_deviceEnumerator)
	{
		_deviceEnumerator->Release();
		_deviceEnumerator = nullptr;
	}

	if (_device)
	{
		_device->Release();
		_device = nullptr;
	}

	if (_audioClient)
	{
		_audioClient->Release();
		_audioClient = nullptr;
	}

	if (_audioRenderClient)
	{
		_audioRenderClient->Release();
		_audioRenderClient = nullptr;
	}
}
