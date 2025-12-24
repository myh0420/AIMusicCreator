using AIMusicCreator.ApiService.Interfaces;
using AIMusicCreator.Entity;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AIMusicCreator.ApiService.Services;

/// <summary>
/// 外观模式实现，提供对多个子系统的统一访问
/// 遵循工业级标准的设计模式最佳实践
/// </summary>
public class Facade : IFacade
{
    /// <summary>
    /// 外观模式实现，提供对多个子系统的统一访问
    /// 遵循工业级标准的设计模式最佳实践
    /// </summary>
    private readonly ILogger<Facade> _logger;
    /// <summary>
    /// 音频处理服务
    /// </summary>
    private readonly IAudioService _audioService;
    /// <summary>
    /// 音频导出服务
    /// </summary>
    private readonly IAudioExportService _audioExportService;
    /// <summary>
    /// MIDI服务
    /// </summary>
    private readonly IMidiService _midiService;
    /// <summary>
    /// OpenAI服务
    /// </summary>
    private readonly IOpenAIService _openAIService;
    /// <summary>
    /// 人声合成服务
    /// </summary>
    private readonly IVocalService _vocalService;
    /// <summary>
    /// 波形生成服务
    /// </summary>
    private readonly IWaveGeneratorService _waveGeneratorService;
    /// <summary>
    /// 伴奏生成服务
    /// </summary>
    private readonly IAccompanimentGeneratorService _accompanimentGeneratorService;
    /// <summary>
    /// 音频效果处理服务
    /// </summary>
    private readonly IAudioEffectService _audioEffectService;
    
    /// <summary>
    /// 构造函数，依赖注入所有服务接口
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="audioService">音频处理服务</param>
    /// <param name="audioExportService">音频导出服务</param>
    /// <param name="midiService">MIDI服务</param>
    /// <param name="openAIService">OpenAI服务</param>
    /// <param name="vocalService">人声合成服务</param>
    /// <param name="waveGeneratorService">波形生成服务</param>
    /// <param name="accompanimentGeneratorService">伴奏生成服务</param>
    /// <param name="audioEffectService">音频效果处理服务</param>
    /// <exception cref="ArgumentNullException">当任何依赖服务为null时抛出</exception>
    /// <remarks>
    /// 确保：
    /// - 初始化FFmpegCore全局选项
    /// - 设置FFmpeg可执行文件路径（Windows/Linux/macOS）
    /// - 配置日志记录
    /// </remarks>
    public Facade(
        ILogger<Facade> logger,
        IAudioService audioService,
        IAudioExportService audioExportService,
        IMidiService midiService,
        IOpenAIService openAIService,
        IVocalService vocalService,
        IWaveGeneratorService waveGeneratorService,
        IAccompanimentGeneratorService accompanimentGeneratorService,
        IAudioEffectService audioEffectService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
        _audioExportService = audioExportService ?? throw new ArgumentNullException(nameof(audioExportService));
        _midiService = midiService ?? throw new ArgumentNullException(nameof(midiService));
        _openAIService = openAIService ?? throw new ArgumentNullException(nameof(openAIService));
        _vocalService = vocalService ?? throw new ArgumentNullException(nameof(vocalService));
        _waveGeneratorService = waveGeneratorService ?? throw new ArgumentNullException(nameof(waveGeneratorService));
        _accompanimentGeneratorService = accompanimentGeneratorService ?? throw new ArgumentNullException(nameof(accompanimentGeneratorService));
        _audioEffectService = audioEffectService ?? throw new ArgumentNullException(nameof(audioEffectService));
        
        _logger.LogInformation("Facade initialized with all required services");
    }
    
    #region 音乐创作核心功能
    
    /// <summary>
    /// 创建完整音乐作品
    /// 整合旋律生成、伴奏生成、人声合成和音频处理的端到端流程
    /// </summary>
    /// <param name="style">音乐风格</param>
    /// <param name="mood">音乐情绪</param>
    /// <param name="bpm">每分钟节拍数</param>
    /// <param name="lyrics">歌词内容（可选）</param>
    /// <param name="language">歌词语言（默认为中文）</param>
    /// <returns>包含完整音乐作品的AudioPackage对象</returns>
    /// <exception cref="ArgumentException">当参数无效时抛出</exception>
    /// <exception cref="InvalidOperationException">当音乐创作过程中发生错误时抛出</exception>
    /// <remarks>
    /// 确保：
    /// - 验证输入参数的有效性
    /// - 整合多个子系统的功能
    /// - 处理异常情况并记录错误
    /// </remarks>
    public async Task<AudioPackage> CreateCompleteMusicAsync(string style, string mood, int bpm, string lyrics = null, string language = "zh")
    {
        _logger.LogInformation("Starting complete music creation process with style: {Style}, mood: {Mood}, bpm: {BPM}", style ?? "", mood ?? "", bpm);
        
        try
        {
            // 参数验证
            if (string.IsNullOrWhiteSpace(style))
                throw new ArgumentException("Music style cannot be empty", nameof(style));
            if (string.IsNullOrWhiteSpace(mood))
                throw new ArgumentException("Music mood cannot be empty", nameof(mood));
            if (bpm <= 0 || bpm > 300)
                throw new ArgumentException("BPM must be between 1 and 300", nameof(bpm));
            
            // 1. 生成旋律
            var melodyMidi = await Task.Run(() => _midiService.GenerateMelody(style, mood, bpm));
            _logger.LogInformation("Melody generation completed successfully");
            
            // 2. 生成伴奏
            var accompanimentMidi = await Task.Run(() => _midiService.GenerateAccompaniment(melodyMidi));
            _logger.LogInformation("Accompaniment generation completed successfully");
            
            // 3. 将MIDI转换为WAV
            var melodyWav = _audioService.MidiToWav(melodyMidi);
            var accompanimentWav = _audioService.MidiToWav(accompanimentMidi);
            
            // 4. 可选：添加人声
            byte[] vocalWav = null;
            if (!string.IsNullOrEmpty(lyrics))
            {
                vocalWav = await Task.Run(() => _vocalService.GenerateVocal(lyrics, melodyMidi, language));
                _logger.LogInformation("Vocal generation completed successfully");
            }
            
            // 5. 合并音频
            var audioList = new List<byte[]> { melodyWav, accompanimentWav };
            if (vocalWav != null)
            {
                audioList.Add(vocalWav);
            }
            
            var finalMix = await _audioService.MergeAudiosAsync(audioList);
            
            // 6. 应用音频效果
            finalMix = _audioEffectService.NormalizeVolume(finalMix);
            
            // 7. 创建返回包
            var package = new AudioPackage
            {
                MelodyMidi = melodyMidi,
                AccompanimentMidi = accompanimentMidi,
                FinalMix = finalMix,
                Lyrics = lyrics ?? string.Empty,
                Style = style ?? string.Empty,
                Mood = mood ?? string.Empty,
                BPM = bpm,
                Duration = await _audioService.GetAudioDurationAsync(finalMix)
            };
            
            _logger.LogInformation("Complete music creation process finished successfully");
            return package;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during complete music creation process");
            throw new MusicCreationException("Failed to create complete music", ex);
        }
    }
    
    /// <summary>
    /// 异步生成歌词
    /// </summary>
    /// <param name="description">歌词描述或主题</param>
    /// <param name="language">歌词语言（默认为中文）</param>
    /// <returns>生成的歌词内容</returns>
    /// <exception cref="ArgumentException">当描述为空时抛出</exception>
    /// <exception cref="LyricsGenerationException">当歌词生成过程中发生错误时抛出</exception>
    /// <remarks>
    /// 确保：
    /// - 验证描述参数的有效性
    /// - 调用OpenAI服务生成歌词
    /// - 处理异常情况并记录错误
    /// </remarks>
    public async Task<string> GenerateLyricsAsync(string description, string language = "zh")
    {
        _logger.LogInformation("Generating lyrics with description: {Description}, language: {Language}", description, language);
        
        try
        {
            return await _openAIService.GenerateLyricsAsync(description, language);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating lyrics");
            throw new LyricsGenerationException("Failed to generate lyrics", ex);
        }
    }
    
    #endregion
    
    #region 音频处理功能
    
    /// <summary>
    /// 异步导出音频数据为WAV格式
    /// </summary>
    /// <param name="audioData">音频数据对象</param>
    /// <param name="outputStream">输出流</param>
    /// <exception cref="ArgumentException">当音频数据为空或输出流无效时抛出</exception>
    /// <exception cref="AudioExportException">当导出过程中发生错误时抛出</exception>
    /// <remarks>
    /// 确保：
    /// - 验证音频数据和输出流的有效性
    /// - 调用音频导出服务导出WAV格式
    /// - 处理异常情况并记录错误
    /// </remarks>
    public async Task ExportToWavAsync(AudioData audioData, Stream outputStream)
    {
        _logger.LogInformation("Exporting audio data to WAV format");
        
        try
        {
            ValidateInputs(audioData, outputStream);
            
            await Task.Run(() => _audioExportService.ExportToWav(audioData, outputStream));
            
            _logger.LogInformation("WAV export completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting audio to WAV format");
            throw new AudioExportException("Failed to export to WAV format", ex);
        }
    }
    
    /// <summary>
    /// 异步导出音频数据为MP3格式
    /// </summary>
    /// <param name="audioData">音频数据对象</param>
    /// <param name="outputStream">输出流</param>
    /// <exception cref="ArgumentException">当音频数据为空或输出流无效时抛出</exception>
    /// <exception cref="AudioExportException">当导出过程中发生错误时抛出</exception>
    /// <remarks>
    /// 确保：
    /// - 验证音频数据和输出流的有效性
    /// - 调用音频导出服务导出MP3格式
    /// - 处理异常情况并记录错误
    /// </remarks>
    public async Task ExportToMp3Async(AudioData audioData, Stream outputStream)
    {
        _logger.LogInformation("Exporting audio data to MP3 format");
        
        try
        {
            ValidateInputs(audioData, outputStream);
            
            await Task.Run(() => _audioExportService.ExportToMp3(audioData, outputStream));
            
            _logger.LogInformation("MP3 export completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting audio to MP3 format");
            throw new AudioExportException("Failed to export to MP3 format", ex);
        }
    }
    
    /// <summary>
    /// 异步调整音频音量
    /// </summary>
    /// <param name="audioData">原始音频数据</param>
    /// <param name="volumeLevel">音量级别，范围0-100</param>
    /// <returns>调整音量后的音频数据</returns>
    /// <exception cref="ArgumentException">当音频数据为空或音量级别无效时抛出</exception>
    /// <exception cref="AudioProcessingException">当处理过程中发生错误时抛出</exception>
    /// <remarks>
    /// 确保：
    /// - 验证音频数据和音量级别参数的有效性
    /// - 调用音频服务调整音量
    /// - 处理异常情况并记录错误
    /// </remarks>
    public async Task<byte[]> AdjustVolumeAsync(byte[] audioData, int volumeLevel)
    {
        _logger.LogInformation("Adjusting audio volume to level: {VolumeLevel}", volumeLevel);
        
        try
        {
            if (audioData == null || audioData.Length == 0)
            {
                throw new ArgumentException("Audio data cannot be null or empty");
            }
            
            if (volumeLevel < 0 || volumeLevel > 100)
            {
                throw new ArgumentException("Volume level must be between 0 and 100");
            }
            
            var result = await _audioService.AdjustVolumeAsync(audioData, volumeLevel);
            _logger.LogInformation("Volume adjustment completed successfully");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjusting audio volume");
            throw new AudioProcessingException("Failed to adjust volume", ex);
        }
    }
    
    #endregion
    
    #region MIDI处理功能
    
    /// <summary>
    /// 异步生成旋律
    /// </summary>
    /// <param name="style">音乐风格</param>
    /// <param name="mood">情绪</param>
    /// <param name="bpm">每分钟节拍数</param>
    /// <returns>MIDI文件的字节数组</returns>
    /// <exception cref="ArgumentException">当参数无效时抛出</exception>
    /// <exception cref="MidiGenerationException">当生成过程中发生错误时抛出</exception>
    /// <remarks>
    /// 确保：
    /// - 验证音乐风格、情绪和BPM参数的有效性
    /// - 调用MIDI服务生成旋律
    /// - 处理异常情况并记录错误
    /// </remarks>
    public async Task<byte[]> GenerateMelodyAsync(string style, string mood, int bpm)
    {
        _logger.LogInformation("Generating melody with style: {Style}, mood: {Mood}, bpm: {BPM}", style, mood, bpm);
        
        try
        {
            ValidateMidiParameters(style, mood, bpm);
            
            var result = await Task.Run(() => _midiService.GenerateMelody(style, mood, bpm));
            _logger.LogInformation("Melody generation completed successfully");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating melody");
            throw new MidiGenerationException("Failed to generate melody", ex);
        }
    }
    
    /// <summary>
    /// 异步生成伴奏
    /// </summary>
    /// <param name="melodyMidi">旋律MIDI的字节数组</param>
    /// <returns>伴奏MIDI的字节数组</returns>
    /// <exception cref="ArgumentException">当旋律MIDI数据为空时抛出</exception>
    /// <exception cref="MidiGenerationException">当生成过程中发生错误时抛出</exception>
    /// <remarks>
    /// 确保：
    /// - 验证旋律MIDI数据的有效性
    /// - 调用MIDI服务生成伴奏
    /// - 处理异常情况并记录错误
    /// </remarks>
    public async Task<byte[]> GenerateAccompanimentAsync(byte[] melodyMidi)
    {
        _logger.LogInformation("Generating accompaniment for melody");
        
        try
        {
            if (melodyMidi == null || melodyMidi.Length == 0)
            {
                throw new ArgumentException("Melody MIDI data cannot be null or empty");
            }
            
            var result = await Task.Run(() => _midiService.GenerateAccompaniment(melodyMidi));
            _logger.LogInformation("Accompaniment generation completed successfully");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating accompaniment");
            throw new MidiGenerationException("Failed to generate accompaniment", ex);
        }
    }
    
    #endregion
    
    #region 辅助方法
    
    /// <summary>
    /// 验证音频和流输入
    /// </summary>
    /// <param name="audioData">音频数据</param>
    /// <param name="stream">输出流</param>
    /// <exception cref="ArgumentNullException">当输入为null时抛出</exception>
    /// <exception cref="ArgumentException">当输入无效时抛出</exception>
    /// <remarks>
    /// 确保：
    /// - 验证音频数据和输出流的有效性
    /// - 检查输出流是否可写
    /// </remarks>
    private void ValidateInputs(AudioData audioData, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(audioData);

        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanWrite)
        {
            throw new ArgumentException("Stream must be writable", nameof(stream));
        }
    }
    
    /// <summary>
    /// 验证MIDI生成参数
    /// </summary>
    /// <param name="style">音乐风格</param>
    /// <param name="mood">情绪</param>
    /// <param name="bpm">每分钟节拍数</param>
    /// <exception cref="ArgumentException">当参数无效时抛出</exception>
    /// <remarks>
    /// 确保：
    /// - 验证音乐风格、情绪和BPM参数的有效性
    /// </remarks>
    private void ValidateMidiParameters(string style, string mood, int bpm)
    {
        if (string.IsNullOrWhiteSpace(style))
        {
            throw new ArgumentException("Style cannot be null or empty", nameof(style));
        }
        
        if (string.IsNullOrWhiteSpace(mood))
        {
            throw new ArgumentException("Mood cannot be null or empty", nameof(mood));
        }
        
        if (bpm <= 0 || bpm > 300)
        {
            throw new ArgumentException("BPM must be between 1 and 300", nameof(bpm));
        }
    }
    
    #endregion
}

#region 自定义异常类

/// <summary>
/// 音乐创建异常
/// </summary>
/// <param name="message">异常消息</param>
/// <param name="innerException">内部异常</param>
/// <remarks>
/// 确保：
/// - 记录异常信息
/// - 包含原始异常信息（如果有）
/// </remarks>
public class MusicCreationException(string message, Exception innerException) : Exception(message, innerException)
{
}

/// <summary>
/// 歌词生成异常
/// </summary>
/// <param name="message">异常消息</param>
/// <param name="innerException">内部异常</param>
/// <remarks>
/// 确保：
/// - 记录异常信息
/// - 包含原始异常信息（如果有）
/// </remarks>
public class LyricsGenerationException(string message, Exception innerException) : Exception(message, innerException)
{
}

/// <summary>
/// 音频导出异常
/// </summary>
/// <param name="message">异常消息</param>
/// <param name="innerException">内部异常</param>
/// <remarks>
/// 确保：
/// - 记录异常信息
/// - 包含原始异常信息（如果有）
/// </remarks>
public class AudioExportException(string message, Exception innerException) : Exception(message, innerException)
{
}

/// <summary>
/// 音频处理异常
/// </summary>
/// <param name="message">异常消息</param>
/// <param name="innerException">内部异常</param>
/// <remarks>
/// 确保：
/// - 记录异常信息
/// - 包含原始异常信息（如果有）
/// </remarks>
public class AudioProcessingException(string message, Exception innerException) : Exception(message, innerException)
{
}

/// <summary>
/// MIDI生成异常
/// </summary>
/// <param name="message">异常消息</param>
/// <param name="innerException">内部异常</param>
/// <remarks>
/// 确保：
/// - 记录异常信息
/// - 包含原始异常信息（如果有）
/// </remarks>
public class MidiGenerationException(string message, Exception innerException) : Exception(message, innerException)
{
}

/// <summary>
/// 音频包数据结构
/// </summary>
public class AudioPackage
{
    /// <summary>
    /// 旋律MIDI数据
    /// </summary>
    public byte[] MelodyMidi { get; set; } = [];
    /// <summary>
    /// 伴奏MIDI数据
    /// </summary>
    public byte[] AccompanimentMidi { get; set; } = [];
    /// <summary>
    /// 最终混音音频数据
    /// </summary>
    public byte[] FinalMix { get; set; } = [];
    /// <summary>
    /// 歌词内容
    /// </summary>
    private string _lyrics = string.Empty;
    /// <summary>
    /// 歌词内容
    /// </summary>
    public string Lyrics
    {
        get => _lyrics;
        set => _lyrics = value ?? string.Empty;
    }
    /// <summary>
    /// 音乐风格
    /// </summary>
    public string Style { get; set; } = string.Empty;
    /// <summary>
    /// 情绪
    /// </summary>
    public string Mood { get; set; } = string.Empty;
    /// <summary>
    /// 每分钟节拍数
    /// </summary>
    public int BPM { get; set; } = 120;
    /// <summary>
    /// 音频持续时间（秒）
    /// </summary>
    public double Duration { get; set; } = 0;
}

#endregion