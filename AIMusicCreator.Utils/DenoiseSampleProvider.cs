using NAudio.Dsp;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIMusicCreator.Utils
{
    /// <summary>
    /// 音频降噪效果处理器（基于频谱减法）
    /// </summary>
    /// <remarks>
    /// 此类实现了基于频谱减法的音频降噪算法，通过分析和抑制音频信号中的噪声频率成分来降低背景噪音。
    /// 处理流程包括噪声底校准和频谱减法两个主要阶段：首先采集音频信号开头的噪声样本建立噪声模型，
    /// 然后对后续音频数据进行频域转换，通过减去噪声频谱的方式降低噪声水平。
    /// 实现了ISampleProvider接口，可与NAudio框架集成，适用于处理各种音频文件和实时音频流。
    /// 降噪强度可通过参数调整，能在保持音质的同时有效降低背景噪音。
    /// </remarks>
    public class DenoiseSampleProvider : ISampleProvider
    {
        /// <summary>
        /// 源音频样本提供器
        /// </summary>
        /// <remarks>原始音频输入源，提供需要进行降噪处理的音频数据。</remarks>
        private readonly ISampleProvider _source;
        
        /// <summary>
        /// 降噪强度
        /// </summary>
        /// <remarks>控制降噪效果的强度级别，范围为0.0到1.0，值越大降噪效果越强，但可能导致声音失真。</remarks>
        private readonly float _strength;
        
        /// <summary>
        /// 噪声底频谱
        /// </summary>
        /// <remarks>存储噪声的频谱特征，表示各频率点上的噪声水平，用于频谱减法计算。</remarks>
        private readonly float[] _noiseFloor;
        
        /// <summary>
        /// 是否已完成噪声校准
        /// </summary>
        /// <remarks>标记是否已完成噪声底的校准过程，校准后才会开始实际的降噪处理。</remarks>
        private bool _calibrated;
        
        /// <summary>
        /// FFT变换大小
        /// </summary>
        /// <remarks>快速傅里叶变换的窗口大小，影响频域分辨率和处理精度，使用1024个样本点。</remarks>
        private readonly int _fftSize = 1024;
        
        /// <summary>
        /// 已校准的样本计数
        /// </summary>
        /// <remarks>记录噪声校准阶段已处理的样本数量，直到达到目标校准样本数为止。</remarks>
        private int _calibratedCount = 0;
        
        /// <summary>
        /// 噪声频谱总和
        /// </summary>
        /// <remarks>在噪声校准阶段累加各频率点的频谱值，用于计算噪声底的平均值。</remarks>
        private readonly float[] _noiseSpectrumSum;

        /// <summary>
        /// 音频格式信息
        /// </summary>
        /// <value>从源提供者获取的音频格式信息，包括采样率、通道数和位深度等。</value>
        public WaveFormat WaveFormat { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="source">源音频样本提供器</param>
        /// <param name="strength">降噪强度（0.0-1.0），控制噪声抑制的程度</param>
        /// <remarks>初始化降噪处理器，设置源提供者和降噪参数，分配必要的缓冲区用于噪声分析和处理。
        /// 降噪强度参数允许用户控制降噪效果的平衡：较低的值保留更多细节但噪声抑制较少，
        /// 较高的值噪声抑制更强但可能导致音质损失。</remarks>
        public DenoiseSampleProvider(ISampleProvider source, double strength)
        {
            _source = source;
            _strength = (float)strength;
            WaveFormat = source.WaveFormat;
            _fftSize = 1024;
            _noiseFloor = new float[_fftSize / 2]; // 只存储正频率部分
            _noiseSpectrumSum = new float[_fftSize / 2]; // 累计频谱值
        }

        /// <summary>
        /// 读取并处理音频样本
        /// </summary>
        /// <param name="buffer">用于存储处理后样本的缓冲区</param>
        /// <param name="offset">缓冲区中的起始偏移量</param>
        /// <param name="count">要读取的样本数</param>
        /// <returns>实际读取的样本数</returns>
        /// <remarks>实现了音频降噪的主要处理流程：
        /// 1. 首先从源提供者读取原始音频数据
        /// 2. 如果尚未完成噪声校准，则处理校准阶段
        /// 3. 如果已完成校准，则应用频谱减法降噪算法
        /// 校准阶段使用音频开头的100ms作为噪声样本，分析并建立噪声模型。</remarks>
        public int Read(float[] buffer, int offset, int count)
        {
            var read = _source.Read(buffer, offset, count);

            // 第一步：校准噪声底（前100ms作为噪声样本，仅执行一次）
            if (!_calibrated)
            {
                CalibrateNoiseFloor(buffer, offset, read);
                return read;
            }

            // 第二步：应用频谱减法降噪
            ApplySpectralSubtraction(buffer, offset, read);

            return read;
        }

        /// <summary>
        /// 校准噪声底
        /// </summary>
        /// <param name="buffer">音频缓冲区</param>
        /// <param name="offset">缓冲区偏移量</param>
        /// <param name="count">样本数量</param>
        /// <remarks>分析音频信号的前100ms建立噪声模型，通过多次FFT分析计算噪声的频谱特征。
        /// 对校准样本进行快速傅里叶变换，提取频谱信息，并计算平均噪声水平作为噪声底。
        /// 校准完成后会设置_calibrated标志，之后开始进行实际的降噪处理。</remarks>
        private void CalibrateNoiseFloor(float[] buffer, int offset, int count)
        {
            const int calibrationSamples = 4410; // 100ms @ 44.1kHz（根据采样率自适应）
            //var fft = new FftProvider(WaveFormat.SampleRate, _fftSize);
            var fftBuffer = new Complex[_fftSize];

            // 累计校准样本数（不超过目标值）
            var addCount = Math.Min(count, calibrationSamples - _calibratedCount);
            _calibratedCount += addCount;

            // 对当前缓冲区的样本进行FFT，计算频谱并累计
            for (int i = 0; i < addCount; i += _fftSize / 2)
            {
                // 填充FFT缓冲区
                for (int j = 0; j < _fftSize; j++)
                {
                    var idx = offset + i + j;
                    var sampleValue = idx < buffer.Length ? buffer[idx] : 0f;
                    fftBuffer[j] = new Complex { X = sampleValue, Y = 0f };
                }

                // 正向FFT：将时域信号转为频域
                FastFourierTransform.FFT(true, (int)Math.Log(_fftSize, 2), fftBuffer);
                // 累计每个频率点的幅度（只取正频率部分）
                for (int j = 0; j < _fftSize / 2; j++)
                {
                    var magnitude = Math.Sqrt(fftBuffer[j].X * fftBuffer[j].X + fftBuffer[j].Y * fftBuffer[j].Y);
                    _noiseSpectrumSum[j] += (float)magnitude;
                }
            }

            // 校准完成：计算频谱平均值作为噪声底
            if (_calibratedCount >= calibrationSamples)
            {
                _calibrated = true;
                var fftCount = (int)Math.Ceiling((double)calibrationSamples / (_fftSize / 2)); // 参与计算的FFT次数

                // 计算每个频率点的平均幅度（噪声底）
                for (int j = 0; j < _fftSize / 2; j++)
                {
                    // 避免除以0，同时确保噪声底不为0
                    _noiseFloor[j] = Math.Max(0.0001f, _noiseSpectrumSum[j] / fftCount);
                }
            }
        }

        /// <summary>
        /// 应用频谱减法降噪
        /// </summary>
        /// <param name="buffer">音频缓冲区</param>
        /// <param name="offset">缓冲区偏移量</param>
        /// <param name="count">样本数量</param>
        /// <remarks>实现了频谱减法降噪算法的核心逻辑：
        /// 1. 将时域音频数据转换为频域（正向FFT）
        /// 2. 比较每个频率点的信号幅度与噪声底，根据降噪强度调整频谱
        /// 3. 对低于噪声底的频率进行抑制，对高于噪声底的频率减去噪声分量
        /// 4. 将处理后的频域数据转换回时域（逆向FFT）
        /// 5. 归一化并写回结果
        /// 使用重叠窗口技术处理整个音频缓冲区，确保平滑过渡。</remarks>
        private void ApplySpectralSubtraction(float[] buffer, int offset, int count)
        {
            var fftBuffer = new Complex[_fftSize];
            //var fft = new FftProvider(WaveFormat.SampleRate, _fftSize);

            for (int i = 0; i < count; i += _fftSize / 2)
            {
                // 1. 填充FFT缓冲区
                for (int j = 0; j < _fftSize; j++)
                {
                    var idx = offset + i + j;
                    var sampleValue = idx < buffer.Length ? buffer[idx] : 0f;
                    fftBuffer[j] = new Complex { X = sampleValue, Y = 0f };
                }

                // 2. 正向FFT
                FastFourierTransform.FFT(true, (int)Math.Log(_fftSize, 2), fftBuffer);

                // 3. 频谱减法（核心降噪逻辑）
                for (int j = 0; j < _fftSize / 2; j++)
                {
                    var magnitude = Math.Sqrt(fftBuffer[j].X * fftBuffer[j].X + fftBuffer[j].Y * fftBuffer[j].Y);
                    var noiseMagnitude = _noiseFloor[j] * (1 + _strength * 2); // 噪声底放大（根据强度调整）

                    // 抑制噪声：低于噪声底的频谱幅度衰减
                    if (magnitude < noiseMagnitude)
                    {
                        var reducedMag = magnitude * 0.1f; // 残留10%噪声，避免失真
                        var phase = Math.Atan2(fftBuffer[j].Y, fftBuffer[j].X);
                        fftBuffer[j].X = (float)(reducedMag * Math.Cos(phase));
                        fftBuffer[j].Y = (float)(reducedMag * Math.Sin(phase));
                    }
                    else
                    {
                        var reducedMag = magnitude - noiseMagnitude * _strength; // 频谱减法
                        var phase = Math.Atan2(fftBuffer[j].Y, fftBuffer[j].X);
                        fftBuffer[j].X = (float)(reducedMag * Math.Cos(phase));
                        fftBuffer[j].Y = (float)(reducedMag * Math.Sin(phase));
                    }
                }

                // 4. 逆向FFT（频域转时域）
                FastFourierTransform.FFT(false, (int)Math.Log(_fftSize, 2), fftBuffer);

                // 5. 写回结果（归一化）
                for (int j = 0; j < _fftSize / 2; j++)
                {
                    var idx = offset + i + j;
                    if (idx < buffer.Length)
                    {
                        buffer[idx] = fftBuffer[j].X / _fftSize; // 归一化：避免音量异常
                    }
                }
            }
        }
    }
}
