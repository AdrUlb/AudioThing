#pragma once

#if defined(_MSC_VER)
#include <Audioclient.h>
#include <mmdeviceapi.h>
#include <Windows.h>
#define SET_ERROR_AND_RETURN_IF_FAILED(x) if (FAILED(_error = (x))) { return; }
#elif defined(__GNUC__)
#include <alsa/asoundlib.h>
#else
#pragma error Platform not supported.
#endif

#include <cstdint>

class Client
{
public:
	Client(uint16_t format, uint16_t channels, uint16_t bitsPerSample, uint16_t frameSize, uint32_t framesPerSec);
	~Client();

	void Start();
	void Stop();

	uint32_t GetBufferFrames();
	uint32_t GetPaddingFrames();

	uint8_t *GetBuffer(uint32_t requestFrameCount);
	void ReleaseBuffer(uint32_t writtenFrameCount);

#if defined(_MSC_VER)
	HRESULT GetError() const;

private:
	HRESULT _error;

	WAVEFORMATEX *_format;
	IMMDeviceEnumerator *_deviceEnumerator;
	IMMDevice *_device;
	IAudioClient *_audioClient;
	IAudioRenderClient *_audioRenderClient;
#elif defined(__GNUC__)
private:
	snd_pcm_t* _pcm;
	uint8_t* _buffer;
	size_t _bufferSize;
	size_t _frameSize;
#else
#pragma error Platform not supported.
#endif
};
