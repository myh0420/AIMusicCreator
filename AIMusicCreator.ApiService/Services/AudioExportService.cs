using AIMusicCreator.ApiService.Interfaces;
using AIMusicCreator.Entity;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using System;
using System.IO;

namespace AIMusicCreator.ApiService.Services
{
    /// <summary>
    /// 音频导出服务类
    /// 负责将AudioData对象导出为不同格式的音频文件
    /// </summary>
    public class AudioExportService : IAudioExportService
    {
        private readonly ILogger<AudioExportService> _logger;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public AudioExportService(ILogger<AudioExportService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 将音频数据导出为WAV格式
        /// </summary>
        /// <param name="audioData">音频数据</param>
        /// <param name="stream">目标流</param>
        /// <exception cref="ArgumentNullException">音频数据或流为null</exception>
        /// <exception cref="ArgumentException">音频数据无效</exception>
        /// <exception cref="IOException">IO操作失败</exception>
        /// <remarks>
        /// 将AudioData对象中的音频样本数据导出为标准WAV格式，
        /// 支持立体声和单声道，使用16位PCM编码。
        /// </remarks>
        public void ExportToWav(AudioData audioData, Stream stream)
        {
            try
            {
                // 输入验证
                ValidateInputs(audioData, stream);
                
                _logger.LogInformation("开始WAV格式导出，采样率: {SampleRate}Hz, 通道数: {Channels}, 采样数: {TotalSamples}",
                    audioData.SampleRate, audioData.Channels, audioData.TotalSamples);
                
                using var waveStream = new WaveFileWriter(stream, new WaveFormat(audioData.SampleRate, 16, audioData.Channels));
                
                // 预分配缓冲区以提高性能
                int bufferSize = Math.Min(audioData.TotalSamples * audioData.Channels * 2, 1024 * 1024); // 最多1MB缓冲区
                byte[] buffer = new byte[bufferSize];
                
                int samplesProcessed = 0;
                const int samplesPerChunk = 4096;
                
                while (samplesProcessed < audioData.TotalSamples)
                {
                    // 计算当前块的采样数
                    int currentChunkSize = Math.Min(samplesPerChunk, audioData.TotalSamples - samplesProcessed);
                    int bufferIndex = 0;
                    
                    // 处理当前块的所有采样
                    for (int i = 0; i < currentChunkSize; i++)
                    {
                        int sampleIndex = samplesProcessed + i;
                        
                        for (int channel = 0; channel < audioData.Channels; channel++)
                        {
                            // 获取样本并转换为16位PCM格式
                            double sample = audioData.GetSample(sampleIndex, channel);
                            
                            // 限制样本范围以避免削波
                            sample = Math.Max(-1.0, Math.Min(1.0, sample));
                            
                            // 转换为16位有符号整数
                            short pcmSample = (short)(sample * short.MaxValue);
                            
                            // 写入到缓冲区（小端序）
                            buffer[bufferIndex++] = (byte)(pcmSample & 0xFF);
                            buffer[bufferIndex++] = (byte)((pcmSample >> 8) & 0xFF);
                        }
                    }
                    
                    // 写入当前块到流
                    waveStream.Write(buffer, 0, bufferIndex);
                    samplesProcessed += currentChunkSize;
                    
                    // 记录进度（每处理10%记录一次）
                    int progressPercentage = (int)((double)samplesProcessed / audioData.TotalSamples * 100);
                    if (progressPercentage % 10 == 0)
                    {
                        _logger.LogDebug("WAV导出进度: {Progress}%", progressPercentage);
                    }
                }
                
                // 确保所有数据都写入到底层流
                waveStream.Flush();
                stream.Flush();
                
                _logger.LogInformation("WAV格式导出完成，总字节数: {BytesWritten}", waveStream.Length);
            }
            catch (IOException ioEx)
            {
                _logger.LogError(ioEx, "WAV导出过程中发生IO错误");
                throw new IOException("WAV导出失败: IO操作错误", ioEx);
            }
            catch (Exception ex)
            {
                if (ex is ArgumentNullException || ex is ArgumentException)
                {
                    _logger.LogError(ex, "WAV导出失败: 无效参数");
                    throw;
                }
                
                _logger.LogError(ex, "WAV导出过程中发生未预期的错误");
                throw new Exception("WAV导出失败: 内部错误", ex);
            }
        }

        /// <summary>
        /// 将音频数据导出为MP3格式
        /// </summary>
        /// <param name="audioData">音频数据</param>
        /// <param name="stream">目标流</param>
        /// <exception cref="ArgumentNullException">音频数据或流为null</exception>
        /// <exception cref="ArgumentException">音频数据无效</exception>
        /// <exception cref="IOException">IO操作失败</exception>
        /// <remarks>
        /// 使用NAudio.Lame将AudioData对象导出为MP3格式，
        /// 支持高质量音频编码，使用320kbps比特率。
        /// </remarks>
        public void ExportToMp3(AudioData audioData, Stream stream)
        {
            try
            {
                // 输入验证
                ValidateInputs(audioData, stream);
                
                _logger.LogInformation("开始MP3格式导出，采样率: {SampleRate}Hz, 通道数: {Channels}, 采样数: {TotalSamples}",
                    audioData.SampleRate, audioData.Channels, audioData.TotalSamples);
                
                // 创建内存流作为中间缓冲区
                using var wavStream = new MemoryStream();
                
                // 先导出为WAV格式（使用中间流）
                using (var waveStream = new WaveFileWriter(wavStream, new WaveFormat(audioData.SampleRate, 16, audioData.Channels)))
                {
                    // 预分配缓冲区以提高性能
                    byte[] buffer = new byte[audioData.TotalSamples * audioData.Channels * 2];
                    int bufferIndex = 0;
                    
                    // 处理所有采样
                    for (int i = 0; i < audioData.TotalSamples; i++)
                    {
                        for (int channel = 0; channel < audioData.Channels; channel++)
                        {
                            // 获取样本并转换为16位PCM格式
                            double sample = audioData.GetSample(i, channel);
                            
                            // 限制样本范围以避免削波
                            sample = Math.Max(-1.0, Math.Min(1.0, sample));
                            
                            // 转换为16位有符号整数
                            short pcmSample = (short)(sample * short.MaxValue);
                            
                            // 写入到缓冲区（小端序）
                            buffer[bufferIndex++] = (byte)(pcmSample & 0xFF);
                            buffer[bufferIndex++] = (byte)((pcmSample >> 8) & 0xFF);
                        }
                        
                        // 定期刷新缓冲区以避免内存压力
                        if (i % 10000 == 0 && bufferIndex > 0)
                        {
                            waveStream.Write(buffer, 0, bufferIndex);
                            bufferIndex = 0;
                        }
                    }
                    
                    // 写入剩余的缓冲区数据
                    if (bufferIndex > 0)
                    {
                        waveStream.Write(buffer, 0, bufferIndex);
                    }
                }
                
                // 重置WAV流的位置
                wavStream.Position = 0;
                
                // 使用NAudio.Lame将WAV转换为MP3，使用高质量预设
                using (var reader = new WaveFileReader(wavStream))
                {
                    _logger.LogInformation("开始MP3编码，使用高质量预设");
                    
                    // 使用NAudio.Lame的STANDARD预设，这会创建高质量MP3
                    using (var writer = new NAudio.Lame.LameMP3FileWriter(stream, reader.WaveFormat, NAudio.Lame.LAMEPreset.STANDARD))
                    {
                        // 使用缓冲区进行分块复制以提高性能
                        byte[] mp3Buffer = new byte[1024 * 1024]; // 1MB缓冲区
                        int bytesRead;
                        long totalBytesRead = 0;
                        long totalLength = reader.Length;
                        
                        while ((bytesRead = reader.Read(mp3Buffer, 0, mp3Buffer.Length)) > 0)
                        {
                            writer.Write(mp3Buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;
                            
                            // 记录进度（每处理10%记录一次）
                            int progressPercentage = (int)((double)totalBytesRead / totalLength * 100);
                            if (progressPercentage % 10 == 0)
                            {
                                _logger.LogDebug("MP3编码进度: {Progress}%", progressPercentage);
                            }
                        }
                        
                        _logger.LogInformation("MP3编码完成");
                    }
                }
                
                // 确保所有数据都写入到目标流
                stream.Flush();
                
                _logger.LogInformation("MP3格式导出成功完成");
            }
            catch (IOException ioEx)
            {
                _logger.LogError(ioEx, "MP3导出过程中发生IO错误");
                throw new IOException("MP3导出失败: IO操作错误", ioEx);
            }
            catch (Exception ex)
            {
                if (ex is ArgumentNullException || ex is ArgumentException)
                {
                    _logger.LogError(ex, "MP3导出失败: 无效参数");
                    throw;
                }
                
                _logger.LogError(ex, "MP3导出过程中发生未预期的错误");
                throw new Exception("MP3导出失败: 内部错误", ex);
            }
        }
        
        /// <summary>
        /// 验证输入参数
        /// </summary>
        /// <param name="audioData">音频数据</param>
        /// <param name="stream">目标流</param>
        /// <exception cref="ArgumentNullException">音频数据或流为null</exception>
        /// <exception cref="ArgumentException">音频数据无效或流不可写</exception>
        private void ValidateInputs(AudioData audioData, Stream stream)
        {
            // 验证audioData不为null
            if (audioData == null)
            {
                throw new ArgumentNullException(nameof(audioData), "音频数据不能为空");
            }
            
            // 验证stream不为null且可写
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream), "目标流不能为空");
            }
            
            if (!stream.CanWrite)
            {
                throw new ArgumentException("目标流不可写", nameof(stream));
            }
            
            // 验证音频参数有效性
            if (audioData.SampleRate <= 0 || audioData.SampleRate > 192000)
            {
                throw new ArgumentException($"无效的采样率: {audioData.SampleRate}Hz，有效范围: 1-192000Hz", nameof(audioData));
            }
            
            if (audioData.Channels <= 0 || audioData.Channels > 8)
            {
                throw new ArgumentException($"无效的通道数: {audioData.Channels}，有效范围: 1-8", nameof(audioData));
            }
            
            if (audioData.TotalSamples <= 0)
            {
                throw new ArgumentException("音频数据没有采样点", nameof(audioData));
            }
            
            // 检查音频时长是否合理（防止过大的音频文件）
            double durationSeconds = (double)audioData.TotalSamples / audioData.SampleRate;
            if (durationSeconds > 3600) // 限制最大时长为1小时
            {
                throw new ArgumentException($"音频时长过长: {durationSeconds:F2}秒，最大支持1小时", nameof(audioData));
            }
        }
    }
}