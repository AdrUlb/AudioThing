#if defined(_MSC_VER)

#include "Client.hpp"

constexpr const GUID CLSID_MMDeviceEnumerator = __uuidof(MMDeviceEnumerator);
constexpr const IID IID_IMMDeviceEnumerator = __uuidof(IMMDeviceEnumerator);
constexpr const IID IID_IAudioClient = __uuidof(IAudioClient);
constexpr const IID IID_IAudioRenderClient = __uuidof(IAudioRenderClient);

Client::Client(uint16_t format, uint16_t channels, uint16_t bitsPerSample, uint16_t frameSize, uint32_t framesPerSec) : _deviceEnumerator(nullptr), _device(nullptr), _audioClient(nullptr), _audioRenderClient(nullptr)
{
	_format = new WAVEFORMATEX();
	_format->wFormatTag = format;
	_format->nChannels = channels;
	_format->wBitsPerSample = bitsPerSample;
	_format->nBlockAlign = frameSize;
	_format->nSamplesPerSec = framesPerSec;
	_format->nAvgBytesPerSec = framesPerSec * frameSize;
	constexpr REFERENCE_TIME bufferDuration = (REFERENCE_TIME)10'000 * 1; // 1 = 100ns, 10'000 = 1'000'000ns = 1'000 s = 1ms

	SET_ERROR_AND_RETURN_IF_FAILED(CoCreateInstance(CLSID_MMDeviceEnumerator, nullptr, CLSCTX_ALL, IID_IMMDeviceEnumerator, (void **)&_deviceEnumerator));
	SET_ERROR_AND_RETURN_IF_FAILED(_deviceEnumerator->GetDefaultAudioEndpoint(eRender, eConsole, &_device));
	SET_ERROR_AND_RETURN_IF_FAILED(_device->Activate(IID_IAudioClient, CLSCTX_ALL, nullptr, (void **)&_audioClient));
	SET_ERROR_AND_RETURN_IF_FAILED(_audioClient->Initialize(AUDCLNT_SHAREMODE_SHARED, AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM | AUDCLNT_STREAMFLAGS_SRC_DEFAULT_QUALITY, bufferDuration, 0, _format, nullptr));
	SET_ERROR_AND_RETURN_IF_FAILED(_audioClient->GetService(IID_IAudioRenderClient, (void **)&_audioRenderClient));
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

void Client::Start()
{
	SET_ERROR_AND_RETURN_IF_FAILED(_audioClient->Start());
}

void Client::Stop()
{
	SET_ERROR_AND_RETURN_IF_FAILED(_audioClient->Stop());
}

uint32_t Client::GetBufferFrames()
{
	UINT32 ret;

	if (FAILED(_error = _audioClient->GetBufferSize(&ret)))
		return 0;

	return ret;
}

uint32_t Client::GetPaddingFrames()
{
	UINT32 ret;

	if (FAILED(_error = _audioClient->GetCurrentPadding(&ret)))
		return 0;

	return ret;
}

uint8_t *Client::GetBuffer(uint32_t requestFrameCount)
{
	BYTE *ret;

	if (FAILED(_error = _audioRenderClient->GetBuffer(requestFrameCount, &ret)))
		return nullptr;

	return ret;
}

void Client::ReleaseBuffer(uint32_t writtenFrameCount)
{
	_error = _audioRenderClient->ReleaseBuffer(writtenFrameCount, 0);
}

HRESULT Client::GetError() const
{
	return _error;
}

#endif
