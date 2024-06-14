#include <Windows.h>
#include <mmdeviceapi.h>
#include <Audioclient.h>

#define SET_ERROR_AND_RETURN_IF_FAILED(x) if (FAILED(_error = (x))) { return; }

class Client
{
public:
	Client(WAVEFORMATEX* format);
	~Client();

	inline void Start()
	{
		SET_ERROR_AND_RETURN_IF_FAILED(_audioClient->Start());
	}

	inline void Stop()
	{
		SET_ERROR_AND_RETURN_IF_FAILED(_audioClient->Stop());
	}

	inline UINT32 GetBufferFrames()
	{
		UINT32 ret;

		if (FAILED(_error = _audioClient->GetBufferSize(&ret)))
			return 0;

		return ret;
	}

	inline UINT32 GetPaddingFrames()
	{
		UINT32 ret;

		if (FAILED(_error = _audioClient->GetCurrentPadding(&ret)))
			return 0;

		return ret;
	}

	inline BYTE* GetBuffer(UINT32 requestFrameCount)
	{
		BYTE* ret;

		if (FAILED(_error = _audioRenderClient->GetBuffer(requestFrameCount, &ret)))
			return nullptr;

		return ret;
	}

	inline void ReleaseBuffer(UINT32 writtenFrameCount)
	{
		_error = _audioRenderClient->ReleaseBuffer(writtenFrameCount, 0);
	}

	inline HRESULT GetError() const
	{
		return _error;
	}
private:
	HRESULT _error;

	IMMDeviceEnumerator* _deviceEnumerator;
	IMMDevice* _device;
	IAudioClient* _audioClient;
	IAudioRenderClient* _audioRenderClient;
};
