using AR.Drone.Infrastructure;
using AR.Drone.Video.Exceptions;
using FFmpeg.AutoGen;

namespace AR.Drone.Video
{
    public unsafe class VideoConverter : DisposableBase
    {
        private readonly AVPixelFormat _pixelFormat;
        private bool _initialized;

        private byte[] _outputData;

        private SwsContext* _pContext;
        private AVFrame* _pCurrentFrame;


        public VideoConverter(AVPixelFormat pixelFormat)
        {
            _pixelFormat = pixelFormat;
        }

        private void Initialize(int width, int height, AVPixelFormat inFormat)
        {
            _initialized = true;

            _pContext = ffmpeg.sws_getContext(width, height, inFormat,
                                                    width, height, _pixelFormat,
                                                    ffmpeg.SWS_FAST_BILINEAR, null, null, null);
            if (_pContext == null)
                throw new VideoConverterException("Could not initialize the conversion context.");

            _pCurrentFrame = ffmpeg.av_frame_alloc();

            int outputDataSize = ffmpeg.avpicture_get_size(_pixelFormat, width, height);
            _outputData = new byte[outputDataSize];

            fixed (byte* pOutputData = &_outputData[0])
            {
                ffmpeg.avpicture_fill((AVPicture*) _pCurrentFrame, pOutputData, _pixelFormat, width, height);
            }
        }

        public byte[] ConvertFrame(AVFrame* pFrame)
        {
            if (_initialized == false)
                Initialize(pFrame->width, pFrame->height, (AVPixelFormat)pFrame->format);

            fixed (byte* pOutputData = &_outputData[0])
            {
                //byte** pSrcData = &(pFrame)->data_0;
                //byte** pDstData = &(_pCurrentFrame)->data_0;

                //_pCurrentFrame->data_0 = pOutputData;

                var _pOutputData = new byte_ptrArray8 { [0] = pOutputData };

                ffmpeg.sws_scale(_pContext, pFrame->data, pFrame->linesize, 0, pFrame->height, _pCurrentFrame->data, _pCurrentFrame->linesize);
            }
            return _outputData;
        }

        protected override void DisposeOverride()
        {
            if (_initialized == false) return;

            ffmpeg.sws_freeContext(_pContext);
            ffmpeg.av_free(_pCurrentFrame);
        }
    }
}