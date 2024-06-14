#if defined(__GNUC__)

#include "Client.hpp"

Client::Client(uint16_t format, uint16_t channels, uint16_t bitsPerSample, uint16_t frameSize, uint32_t framesPerSec)
{
	_frameSize = frameSize;
	snd_pcm_open(&_pcm, "default", SND_PCM_STREAM_PLAYBACK, 0);
	snd_pcm_hw_params_t *hwParams;
	snd_pcm_hw_params_alloca(&hwParams);

	auto rate = framesPerSec;

	const auto sndFormat = format == 1 ? SND_PCM_FORMAT_S16_LE : SND_PCM_FORMAT_FLOAT_LE;

	_bufferSize = (size_t)(bitsPerSample / 8 * channels * framesPerSec) / 200;
	snd_pcm_hw_params_any(_pcm, hwParams);
	snd_pcm_hw_params_set_access(_pcm, hwParams, SND_PCM_ACCESS_RW_INTERLEAVED);
	snd_pcm_hw_params_set_format(_pcm, hwParams, sndFormat);
	snd_pcm_hw_params_set_channels(_pcm, hwParams, channels);
	snd_pcm_hw_params_set_rate_near(_pcm, hwParams, &rate, nullptr);
	snd_pcm_hw_params_set_periods(_pcm, hwParams, 2, 0);
	snd_pcm_hw_params_set_buffer_size(_pcm, hwParams, (ulong)_bufferSize);
	snd_pcm_hw_params(_pcm, hwParams);
	snd_pcm_uframes_t periodSize = 0;
	snd_pcm_hw_params_get_period_size(hwParams, &periodSize, nullptr);
	_buffer = new uint8_t[_bufferSize];
}

Client::~Client()
{
	Stop();
	snd_pcm_close(_pcm);
	delete[] _buffer;
}

void Client::Start()
{
}

void Client::Stop()
{
	snd_pcm_drain(_pcm);
}

uint32_t Client::GetBufferFrames()
{
	return _bufferSize / _frameSize;
}

uint32_t Client::GetPaddingFrames()
{
	return 0;
}

uint8_t *Client::GetBuffer(uint32_t requestFrameCount)
{
	return _buffer;
}

void Client::ReleaseBuffer(uint32_t writtenFrameCount)
{
	while (snd_pcm_writei(_pcm, _buffer, writtenFrameCount) == -EPIPE)
		snd_pcm_prepare(_pcm);
}

#endif