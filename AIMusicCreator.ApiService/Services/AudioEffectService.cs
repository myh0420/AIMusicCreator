using AIMusicCreator.ApiService.Interfaces;
using AIMusicCreator.Utils;
using Microsoft.Extensions.Logging;
using NAudio.Dsp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.IO;
namespace AIMusicCreator.ApiService.Services
{
    /// <summary>
    /// 音频效果处理服务
    /// </summary>
    public class AudioEffectService : IAudioEffectService
    {
        /// <summary>
        /// 音频效果处理服务
        /// </summary>
        private readonly ILogger<AudioEffectService> _logger;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <remarks>
        /// 构造函数，初始化音频效果处理服务。
        /// </remarks>
        public AudioEffectService(ILogger<AudioEffectService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 添加回声效果
        /// </summary>
        /// <param name="audioBytes">音频数据</param>
        /// <param name="delaySeconds">回声延迟时间（秒）</param>
        /// <param name="decay">回声衰减因子</param>
        /// <returns>添加回声效果后的音频数据</returns>
        /// <exception cref="ArgumentException">输入数据无效或参数错误</exception>
        /// <remarks>
        /// 添加回声效果到音频数据，返回新的音频数据。如果输入数据无效或参数错误，将抛出异常。
        /// </remarks>
        public byte[] AddEcho(byte[] audioBytes, float delaySeconds = 0.5f, float decay = 0.5f)
        {
            try
            {
                _logger.LogInformation("开始添加回声效果，延迟: {DelaySeconds}s, 衰减: {Decay}", delaySeconds, decay);
                
                // 验证输入参数
                ValidateInput(audioBytes, delaySeconds, decay);
                
                using var inputStream = new MemoryStream(audioBytes);

                try
                {
                    // 使用改进的读取器创建方法
                    var audioFile = MidiUtils.CreateAudioFileReaderImproved(inputStream);
                    var format = audioFile.WaveFormat;

                    // 计算延迟采样数
                    int delaySamples = (int)(delaySeconds * format.SampleRate);
                    var echoProvider = new EchoSampleProvider(audioFile.ToSampleProvider(), delaySamples, decay);

                    using var outputStream = new MemoryStream();
                    WaveFileWriter.WriteWavFileToStream(outputStream, echoProvider.ToWaveProvider16());

                    _logger.LogInformation("回声效果处理完成，输出大小: {OutputLength} 字节", outputStream.Length);
                    return outputStream.ToArray();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "回声效果处理失败");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加回声效果失败");
                throw;
            }
        }

        /// <summary>
        /// 简单均衡器（增强低音）
        /// </summary>
        /// <param name="audioBytes">音频数据</param>
        /// <param name="gainDb">增益值（dB）</param>
        /// <returns>增强低音后的音频数据</returns>
        /// <exception cref="ArgumentException">输入数据无效或参数错误</exception>
        /// <remarks>
        /// 增强音频数据的低音部分，返回新的音频数据。如果输入数据无效或参数错误，将抛出异常。
        /// </remarks>
        public byte[] BoostBass(byte[] audioBytes, float gainDb = 6.0f)
        {
            try
            {
                _logger.LogInformation("开始增强低音，增益: {GainDb}dB", gainDb);
                
                // 验证输入参数
                ValidateInput(audioBytes);
                
                if (gainDb < 0 || gainDb > 20)
                {
                    throw new ArgumentException("增益值必须在0-20dB之间");
                }
                
                using var inputStream = new MemoryStream(audioBytes);
                
                try
                {
                    var reader = MidiUtils.CreateAudioFileReaderImproved(inputStream);
                    var format = reader.WaveFormat;

                    // 设计低通滤波器（截止频率200Hz）
                    var filter = BiQuadFilter.LowPassFilter(format.SampleRate, 200, 0.707f);
                    var filterProvider = new FilterSampleProvider(reader.ToSampleProvider(), filter)
                    {
                        Gain = (float)Math.Pow(10, gainDb / 20) // dB转增益
                    };

                    using var outputStream = new MemoryStream();
                    WaveFileWriter.WriteWavFileToStream(outputStream, filterProvider.ToWaveProvider16());
                    
                    _logger.LogInformation("低音增强完成，输出大小: {OutputLength} 字节", outputStream.Length);
                    return outputStream.ToArray();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "低音增强处理失败");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "增强低音失败");
                throw;
            }
        }

        /// <summary>
        /// 音量标准化（将峰值调整到目标水平）
        /// </summary>
        /// <param name="audioBytes">音频数据</param>
        /// <param name="targetPeak">目标峰值（0-1.0）</param>
        /// <returns>标准化后的音频数据</returns>
        /// <exception cref="ArgumentException">输入数据无效或参数错误</exception>
        /// <remarks>
        /// 标准化音频数据的音量，将峰值调整到指定的目标水平，返回新的音频数据。如果输入数据无效或参数错误，将抛出异常。
        /// </remarks>
        public byte[] NormalizeVolume(byte[] audioBytes, float targetPeak = 0.9f)
        {
            try
            {
                _logger.LogInformation("开始音量标准化，目标峰值: {TargetPeak}", targetPeak);
                
                // 验证输入参数
                ValidateInput(audioBytes);
                
                if (targetPeak <= 0 || targetPeak > 1.0)
                {
                    throw new ArgumentException("目标峰值必须在0-1.0之间");
                }
                
                using var inputStream = new MemoryStream(audioBytes);
                
                try
                {
                    var reader = MidiUtils.CreateAudioFileReaderImproved(inputStream);
                    var provider = reader.ToSampleProvider();

                    // 计算当前峰值
                    float maxSample = 0;
                    var buffer = new float[4096];
                    int read;
                    while ((read = provider.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        for (int i = 0; i < read; i++)
                        {
                            maxSample = Math.Max(maxSample, Math.Abs(buffer[i]));
                        }
                    }

                    // 计算增益并应用
                    if (maxSample == 0)
                    {
                        _logger.LogWarning("音频数据全为零，无法进行音量标准化");
                        return audioBytes; // 避免除以零
                    }
                    
                    float gain = targetPeak / maxSample;
                    
                    _logger.LogInformation("计算音量增益: {Gain}", gain);

                    inputStream.Position = 0; // 重置流位置
                    var volumeProvider = new VolumeSampleProvider(MidiUtils.CreateAudioFileReaderImproved(inputStream).ToSampleProvider()) 
                    { 
                        Volume = gain 
                    };

                    using var outputStream = new MemoryStream();
                    WaveFileWriter.WriteWavFileToStream(outputStream, volumeProvider.ToWaveProvider16());
                    
                    _logger.LogInformation("音量标准化完成，输出大小: {OutputLength} 字节", outputStream.Length);
                    return outputStream.ToArray();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "音量标准化处理失败");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "音量标准化失败");
                throw;
            }
        }
        
        /// <summary>
        /// 验证输入参数
        /// </summary>
        /// <param name="audioBytes">音频数据</param>
        /// <exception cref="ArgumentNullException">音频数据为null</exception>
        /// <exception cref="ArgumentException">音频数据为空或超过100MB</exception>
        /// <remarks>
        /// 验证音频数据是否有效，包括是否为null、是否为空或是否超过100MB的大小。如果验证失败，将抛出异常。
        /// </remarks>
        private void ValidateInput(byte[] audioBytes)
        {
            if (audioBytes == null || audioBytes.Length == 0)
            {
                throw new ArgumentNullException(nameof(audioBytes), "音频数据不能为空");
            }
            
            if (audioBytes.Length > 100 * 1024 * 1024) // 100MB限制
            {
                throw new ArgumentException("音频数据不能超过100MB");
            }
        }
        
        /// <summary>
        /// 验证回声效果的输入参数
        /// </summary>
        /// <param name="audioBytes">音频数据</param>
        /// <param name="delaySeconds">延迟时间（秒）</param>
        /// <param name="decay">衰减系数</param>
        /// <exception cref="ArgumentNullException">音频数据为null</exception>
        /// <exception cref="ArgumentException">音频数据为空或超过100MB、延迟时间无效或衰减系数无效</exception>
        /// <remarks>
        /// 验证回声效果的输入参数是否有效，包括音频数据、延迟时间和衰减系数。如果验证失败，将抛出异常。
        /// </remarks>
        private void ValidateInput(byte[] audioBytes, float delaySeconds, float decay)
        {
            ValidateInput(audioBytes);
            
            if (delaySeconds < 0.01f || delaySeconds > 5.0f)
            {
                throw new ArgumentException("延迟时间必须在0.01-5.0秒之间");
            }
            
            if (decay < 0 || decay > 1.0f)
            {
                throw new ArgumentException("衰减系数必须在0-1.0之间");
            }
        }
    }
    
    // 辅助类：回声效果实现
    /// <summary>
    /// 回声效果实现，用于添加回声到音频数据中
    /// </summary>
    /// <param name="source">音频源</param>
    /// <param name="delaySamples">延迟样本数</param>
    /// <param name="decay">衰减系数</param>
    /// <remarks>
    /// 回声效果实现，用于添加回声到音频数据中。延迟样本数表示回声的延迟时间，衰减系数表示回声的衰减程度。
    /// </remarks>
    public class EchoSampleProvider(ISampleProvider source, int delaySamples, float decay) : ISampleProvider
    {
        /// <summary>
        /// 回声效果实现，用于添加回声到音频数据中
        /// </summary>
        private readonly ISampleProvider _source = source;
        /// <summary>
        /// 回声效果实现，用于添加回声到音频数据中
        /// </summary>
        private readonly float[] _delayBuffer = new float[delaySamples];
        /// <summary>
        /// 回声效果实现，用于添加回声到音频数据中
        /// </summary>
        private readonly float _decay = decay;
        /// <summary>
        /// 回声效果实现，用于添加回声到音频数据中
        /// </summary>
        private int _delayPosition;
        /// <summary>
        /// 回声效果实现，用于添加回声到音频数据中
        /// </summary>
        public WaveFormat WaveFormat => _source.WaveFormat;
        /// <summary>
        /// 回声效果实现，用于添加回声到音频数据中
        /// </summary>
        public int Read(float[] buffer, int offset, int count)
        {
            int sourceRead = _source.Read(buffer, offset, count);

            for (int i = 0; i < sourceRead; i++)
            {
                int index = offset + i;
                float input = buffer[index];

                // 读取延迟缓冲区的值
                float delayed = _delayBuffer[_delayPosition];

                // 混合输入和延迟信号
                buffer[index] = input + delayed * _decay;

                // 更新延迟缓冲区
                _delayBuffer[_delayPosition] = input + delayed * _decay;
                _delayPosition = (_delayPosition + 1) % _delayBuffer.Length;
            }

            return sourceRead;
        }
    }

    // 辅助类：滤波器效果实现
    /// <summary>
    /// 滤波器效果实现，用于对音频数据进行滤波处理
    /// </summary>
    /// <param name="source">音频源</param>
    /// <param name="filter">滤波器</param>
    /// <remarks>
    /// 滤波器效果实现，用于对音频数据进行滤波处理。滤波器可以是低通、高通、带通或带阻滤波器。
    /// </remarks>
    public class FilterSampleProvider(ISampleProvider source, BiQuadFilter filter) : ISampleProvider
    {
        /// <summary>
        /// 滤波器效果实现，用于对音频数据进行滤波处理
        /// </summary>
        private readonly ISampleProvider _source = source;
        /// <summary>
        /// 滤波器效果实现，用于对音频数据进行滤波处理
        /// </summary>
        private readonly BiQuadFilter _filter = filter;
        /// <summary>
        /// 滤波器效果实现，用于对音频数据进行滤波处理
        /// </summary>
        public WaveFormat WaveFormat => _source.WaveFormat;
        /// <summary>
        /// 滤波器效果实现，用于对音频数据进行滤波处理
        /// </summary>
        public float Gain { get; set; } = 1.0f;
        /// <summary>
        /// 滤波器效果实现，用于对音频数据进行滤波处理
        /// </summary>
        public int Read(float[] buffer, int offset, int count)
        {
            int read = _source.Read(buffer, offset, count);

            for (int i = 0; i < read; i++)
            {
                buffer[offset + i] = _filter.Transform(buffer[offset + i]) * Gain;
            }

            return read;
        }
    }
}