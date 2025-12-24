using NAudio.Wave;
using System.Threading.Tasks;
using AIMusicCreator.ApiService.Interfaces;
using Microsoft.Extensions.Logging;
using NAudio.Wave.SampleProviders;

namespace AIMusicCreator.ApiService.Services;
/// <summary>
/// 音频服务，提供音频处理功能
/// </summary>
public class AudioService : IAudioService
{
    /// <summary>
    /// 音频服务，提供音频处理功能
    /// </summary>
    private readonly ILogger<AudioService> _logger;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <exception cref="ArgumentNullException">日志记录器为null</exception>
    /// <remarks>
    /// 初始化音频服务，使用提供的日志记录器。
    /// </remarks>
    public AudioService(ILogger<AudioService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 调整音频音量
    /// </summary>
    /// <param name="audioData">原始音频数据</param>
    /// <param name="volumeLevel">音量级别（0-100）</param>
    /// <returns>调整后的音频数据</returns>
    /// <exception cref="ArgumentException">输入数据无效或音量级别无效</exception>
    /// <remarks>
    /// 调整音频数据的音量，返回新的音频数据。如果输入数据无效或音量级别不在0-100之间，将抛出异常。
    /// </remarks>
    public async Task<byte[]> AdjustVolumeAsync(byte[] audioData, int volumeLevel)
    {
        try
        {
            _logger.LogInformation("开始调整音频音量，音量级别: {VolumeLevel}", volumeLevel);
            
            // 验证输入
            var validation = ValidateAudioData(audioData);
            if (!validation.IsValid)
            {
                throw new ArgumentException(validation.ErrorMessage);
            }
            
            if (volumeLevel < 0 || volumeLevel > 100)
            {
                throw new ArgumentException("音量级别必须在0-100之间");
            }

            return await Task.Run(() => 
            {
                // 计算音量增益
                float volumeGain = volumeLevel / 100f;
                
                using var memoryStream = new MemoryStream();
                using (var reader = new WaveFileReader(new MemoryStream(audioData)))
                {
                    var volumeProvider = new VolumeWaveProvider16(reader)
                    {
                        Volume = volumeGain
                    };
                    
                    // 使用WaveFileWriter.Write方法而不是CreateWaveFile16，因为CreateWaveFile16期望文件路径作为字符串
                    using (var writer = new WaveFileWriter(memoryStream, volumeProvider.WaveFormat))
                    {
                        byte[] buffer = new byte[4096];
                        int bytesRead;
                        while ((bytesRead = volumeProvider.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            writer.Write(buffer, 0, bytesRead);
                        }
                    }
                }
                
                _logger.LogInformation("音频音量调整完成");
                return memoryStream.ToArray();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调整音频音量失败");
            throw;
        }
    }

    /// <summary>
    /// 合并多个音频文件
    /// </summary>
    /// <param name="audioDataList">音频数据列表</param>
    /// <returns>合并后的音频数据</returns>
    /// <exception cref="ArgumentException">音频列表为空或包含无效音频数据</exception>
    /// <remarks>
    /// 合并多个音频文件，返回新的音频数据。如果音频列表为空或包含无效音频数据，将抛出异常。
    /// </remarks>
    public async Task<byte[]> MergeAudiosAsync(List<byte[]> audioDataList)
    {
        try
        {
            _logger.LogInformation("开始合并音频文件，文件数量: {AudioCount}", audioDataList?.Count ?? 0);
            
            if (audioDataList == null || !audioDataList.Any())
            {
                throw new ArgumentException("音频列表不能为空");
            }
            
            // 验证所有音频数据
            foreach (var audioData in audioDataList)
            {
                var validation = ValidateAudioData(audioData);
                if (!validation.IsValid)
                {
                    throw new ArgumentException($"音频数据无效: {validation.ErrorMessage}");
                }
            }

            return await Task.Run(() => 
            {
                using var memoryStream = new MemoryStream();
                using var mixer = new WaveMixerStream32();
                mixer.AutoStop = false; // 允许混合器继续运行直到所有流完成
                
                // 打开并添加所有音频流
                var readers = new List<WaveFileReader>();
                try
                {
                    foreach (var audioData in audioDataList)
                    {
                        var stream = new MemoryStream(audioData);
                        var reader = new WaveFileReader(stream);
                        readers.Add(reader);
                        
                        // 将音频转换为32位浮点格式并添加到混合器
                        var sampleProvider = new WaveToSampleProvider(reader);
                        // 使用WaveToSampleProvider直接提供采样数据
                        // 直接将reader添加到混合器，reader已经是WaveStream类型
                        mixer.AddInputStream(reader);
                    }
                    
                    // 写入结果
                    WaveFileWriter.WriteWavFileToStream(memoryStream, mixer);
                    
                    _logger.LogInformation("音频文件合并完成，输出大小: {Size} 字节", memoryStream.Length);
                    return memoryStream.ToArray();
                }
                finally
                {
                    // 释放所有读取器
                    foreach (var reader in readers)
                    {
                        reader.Dispose();
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "合并音频文件失败");
            throw;
        }
    }

    /// <summary>
    /// 裁剪音频
    /// </summary>
    /// <param name="audioData">原始音频数据</param>
    /// <param name="startSeconds">裁剪开始时间（秒）</param>
    /// <param name="durationSeconds">裁剪持续时间（秒）</param>
    /// <returns>裁剪后的音频数据</returns>
    /// <exception cref="ArgumentException">输入数据无效或参数错误</exception>
    /// <remarks>
    /// 裁剪音频数据，返回新的音频数据。如果输入数据无效或参数错误，将抛出异常。
    /// </remarks>
    public async Task<byte[]> TrimAudioAsync(byte[] audioData, double startSeconds, double durationSeconds)
    {
        try
        {
            _logger.LogInformation("开始裁剪音频，起始时间: {StartSeconds}s, 持续时间: {DurationSeconds}s", 
                startSeconds, durationSeconds);
            
            // 验证输入
            var validation = ValidateAudioData(audioData);
            if (!validation.IsValid)
            {
                throw new ArgumentException(validation.ErrorMessage);
            }
            
            if (startSeconds < 0 || durationSeconds <= 0)
            {
                throw new ArgumentException("起始时间必须大于等于0，持续时间必须大于0");
            }

            return await Task.Run(() => 
            {
                using var memoryStream = new MemoryStream();
                using var reader = new WaveFileReader(new MemoryStream(audioData));
                
                // 计算起始位置和持续采样数
                int sampleRate = reader.WaveFormat.SampleRate;
                int channels = reader.WaveFormat.Channels;
                int bitsPerSample = reader.WaveFormat.BitsPerSample;
                int bytesPerSample = bitsPerSample / 8;
                
                long startSample = (long)(startSeconds * sampleRate);
                long durationSamples = (long)(durationSeconds * sampleRate);
                
                // 确保不超出音频范围
                long totalSamples = reader.Length / (bytesPerSample * channels);
                if (startSample >= totalSamples)
                {
                    throw new ArgumentException("起始时间超出音频长度");
                }
                
                if (startSample + durationSamples > totalSamples)
                {
                    durationSamples = totalSamples - startSample;
                    _logger.LogWarning("裁剪持续时间超出音频长度，已调整为剩余长度");
                }
                
                // 定位到起始位置
                reader.Position = startSample * bytesPerSample * channels;
                
                // 创建结果文件
                using var writer = new WaveFileWriter(memoryStream, reader.WaveFormat);
                
                // 读取并写入裁剪的数据
                byte[] buffer = new byte[4096];
                long bytesToRead = durationSamples * bytesPerSample * channels;
                long bytesRead = 0;
                
                while (bytesRead < bytesToRead)
                {
                    int bytesToReadNow = (int)Math.Min(buffer.Length, bytesToRead - bytesRead);
                    int count = reader.Read(buffer, 0, bytesToReadNow);
                    if (count == 0) break;
                    
                    writer.Write(buffer, 0, count);
                    bytesRead += count;
                }
                
                _logger.LogInformation("音频裁剪完成，输出大小: {Size} 字节", memoryStream.Length);
                return memoryStream.ToArray();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "裁剪音频失败");
            throw;
        }
    }

    /// <summary>
    /// 获取音频时长
    /// </summary>
    /// <param name="audioData">音频数据</param>
    /// <returns>音频时长（秒）</returns>
    /// <exception cref="ArgumentException">输入数据无效</exception>
    /// <remarks>
    /// 获取音频文件的时长（秒）。如果输入数据无效，将抛出异常。
    /// </remarks>
    public async Task<double> GetAudioDurationAsync(byte[] audioData)
    {
        try
        {
            _logger.LogInformation("获取音频时长");
            
            // 验证输入
            var validation = ValidateAudioData(audioData);
            if (!validation.IsValid)
            {
                throw new ArgumentException(validation.ErrorMessage);
            }

            return await Task.Run(() => 
            {
                using var reader = new WaveFileReader(new MemoryStream(audioData));
                double duration = reader.TotalTime.TotalSeconds;
                
                _logger.LogInformation("获取音频时长完成: {Duration} 秒", duration);
                return duration;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取音频时长失败");
            throw;
        }
    }

    /// <summary>
    /// 验证音频数据
    /// </summary>
    /// <param name="audioData">音频数据</param>
    /// <returns>验证结果，包含是否有效和错误消息</returns>
    /// <remarks>
    /// 验证音频数据是否有效，包括格式、大小等。如果音频数据无效，将返回错误消息。
    /// </remarks>
    public (bool IsValid, string ErrorMessage) ValidateAudioData(byte[] audioData)
    {
        if (audioData == null || audioData.Length == 0)
        {
            return (false, "音频数据不能为空");
        }
        
        if (audioData.Length > 100 * 1024 * 1024) // 100MB限制
        {
            return (false, "音频数据不能超过100MB");
        }
        
        try
        {
            // 尝试打开音频文件验证格式
            using var reader = new WaveFileReader(new MemoryStream(audioData));
            if (reader.WaveFormat.SampleRate <= 0 || reader.WaveFormat.BitsPerSample <= 0)
            {
                return (false, "无效的音频格式参数");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "音频格式验证失败");
            return (false, $"无效的音频格式: {ex.Message}");
        }
        
        return (true, string.Empty);
    }

    /// <summary>
    /// 将MIDI数据转换为WAV格式
    /// </summary>
    /// <param name="midiData">MIDI数据</param>
    /// <returns>WAV格式的音频数据</returns>
    /// <exception cref="ArgumentException">输入数据无效</exception>
    /// <remarks>
    /// 将MIDI数据转换为WAV格式的音频数据。如果输入数据无效，将抛出异常。
    /// </remarks>
    public byte[] MidiToWav(byte[] midiData)
    {
        try
        {
            _logger.LogInformation("开始将MIDI数据转换为WAV格式");
            
            // 验证输入数据
            if (midiData == null || midiData.Length == 0)
            {
                throw new ArgumentException("MIDI数据不能为空");
            }
            
            // 这里将实现MIDI到WAV的转换逻辑
            // 目前返回一个空的字节数组作为占位符
            _logger.LogInformation("MIDI到WAV转换完成");
            return Array.Empty<byte>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MIDI到WAV转换失败");
            throw;
        }
    }
    
    /// <summary>
    /// 调整音频持续时间
    /// </summary>
    /// <param name="audioData">音频数据</param>
    /// <param name="targetDurationSeconds">目标持续时间（秒）</param>
    /// <returns>调整后的音频数据</returns>
    /// <exception cref="ArgumentException">输入数据无效或参数错误</exception>
    /// <remarks>
    /// 调整音频数据的持续时间，返回新的音频数据。如果输入数据无效或参数错误，将抛出异常。
    /// </remarks>
    public byte[] AdaptDuration(byte[] audioData, double targetDurationSeconds)
    {
        try
        {
            _logger.LogInformation("开始调整音频持续时间为: {TargetDuration}秒", targetDurationSeconds);
            
            // 验证输入数据
            var validation = ValidateAudioData(audioData);
            if (!validation.IsValid)
            {
                throw new ArgumentException(validation.ErrorMessage);
            }
            
            if (targetDurationSeconds <= 0)
            {
                throw new ArgumentException("目标持续时间必须大于0");
            }
            
            // 这里将实现音频持续时间调整逻辑
            // 目前返回原始数据作为占位符
            _logger.LogInformation("音频持续时间调整完成");
            return audioData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "音频持续时间调整失败");
            throw;
        }
    }
}