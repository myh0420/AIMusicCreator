using NAudio.Wave;
using NAudio.Midi;
using JiebaNet.Segmenter;
using System.Threading.Tasks;
using AIMusicCreator.ApiService.Interfaces;
using Microsoft.Extensions.Logging;

namespace AIMusicCreator.ApiService.Services;
/// <summary>
/// 提供人声合成功能
/// </summary>
public class VocalService : IVocalService
{
    /// <summary>
    /// 提供中文分词功能
    /// </summary>
    private readonly JiebaSegmenter _jieba = new();
    /// <summary>
    /// 音素到MIDI音高的映射
    /// </summary>
    private readonly Dictionary<string, int> _phonemeMap = new() // 简化音素映射
    {
        { "a", 69 }, { "i", 72 }, { "u", 67 }, { "e", 65 }, { "o", 64 } // 元音对应MIDI音高
    };
    /// <summary>
    /// 日志记录器
    /// </summary>
    private readonly ILogger<VocalService> _logger;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public VocalService(ILogger<VocalService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 歌词合成人声（匹配旋律音高）
    /// </summary>
    /// <param name="lyrics">歌词文本</param>
    /// <param name="melodyMidi">旋律MIDI数据</param>
    /// <param name="language">语言（默认中文）</param>
    /// <returns>合成的人声字节数组</returns>
    /// <exception cref="ArgumentException">输入数据无效</exception>
    /// <exception cref="InvalidOperationException">旋律长度不足</exception>
    public byte[] GenerateVocal(string lyrics, byte[] melodyMidi, string language = "zh")
    {
        try
        {
            // 验证输入
            var validation = ValidateVocalData(lyrics, melodyMidi);
            if (!validation.IsValid)
            {
                throw new ArgumentException(validation.ErrorMessage);
            }

            _logger.LogInformation("开始生成人声，歌词长度: {LyricsLength}, MIDI数据大小: {MidiSize} 字节", 
                lyrics.Length, melodyMidi.Length);

            // 1. 歌词分词
            var words = language == "zh"
                ? _jieba.Cut(lyrics).Where(w => !string.IsNullOrWhiteSpace(w)).ToList()
                : [.. lyrics.Split([' ', '-'], StringSplitOptions.RemoveEmptyEntries)];

            _logger.LogDebug("分词结果: {WordCount} 个字", words.Count);

            // 2. 解析旋律音高和时长
            var (pitches, durations) = ParseMelodyInfo(melodyMidi);
            if (pitches.Count < words.Count)
            {
                throw new InvalidOperationException("旋律长度不足，无法匹配歌词");
            }

            // 3. 音素映射与合成
            using var mixer = new WaveMixerStream32();
            //mixer.WaveFormat = new WaveFormat(44100, 16, 2);只读属性无法赋值

            for (int i = 0; i < words.Count; i++)
            {
                // 为每个字生成对应音高的音频
                var wordAudio = GenerateWordAudio(words[i], pitches[i], durations[i]);
                mixer.AddInputStream(new WaveFileReader(new MemoryStream(wordAudio)));
            }

            // 4. 输出人声音频
            using var outputStream = new MemoryStream();
            WaveFileWriter.WriteWavFileToStream(outputStream, mixer);
            
            _logger.LogInformation("人声生成完成，输出音频大小: {AudioSize} 字节", outputStream.Length);
            return outputStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成人声时出错");
            throw;
        }
    }
    
    /// <summary>
    /// 异步生成合成人声
    /// </summary>
    /// <param name="lyrics">歌词文本</param>
    /// <param name="melodyMidi">旋律MIDI数据</param>
    /// <param name="language">语言（默认中文）</param>
    /// <returns>合成的人声字节数组</returns>
    /// <exception cref="ArgumentException">输入数据无效</exception>
    /// <exception cref="InvalidOperationException">旋律长度不足</exception>
    public async Task<byte[]> GenerateVocalAsync(string lyrics, byte[] melodyMidi, string language = "zh")
    {
        return await Task.Run(() => GenerateVocal(lyrics, melodyMidi, language));
    }
    
    /// <summary>
    /// 验证歌词和旋律数据
    /// </summary>
    /// <param name="lyrics">歌词文本</param>
    /// <param name="melodyMidi">旋律MIDI数据</param>
    /// <returns>验证结果（是否有效，错误消息）</returns>
    public (bool IsValid, string ErrorMessage) ValidateVocalData(string lyrics, byte[] melodyMidi)
    {
        if (string.IsNullOrWhiteSpace(lyrics))
        {
            return (false, "歌词不能为空");
        }
        
        if (lyrics.Length > 500)
        {
            return (false, "歌词长度不能超过500个字符");
        }
        
        if (melodyMidi == null || melodyMidi.Length == 0)
        {
            return (false, "MIDI数据不能为空");
        }
        
        if (melodyMidi.Length > 10 * 1024 * 1024) // 10MB限制
        {
            return (false, "MIDI数据不能超过10MB");
        }
        
        return (true, string.Empty);
    }

    /// <summary>
    /// 解析MIDI旋律信息
    /// </summary>
    /// <param name="melodyMidi">旋律MIDI数据</param>
    /// <returns>音高列表和时长列表</returns>
    /// <exception cref="InvalidOperationException">MIDI解析失败</exception>
    /// <remarks>
    /// 解析MIDI文件，提取音符信息。如果MIDI文件无效或为空，将返回默认音高和时长。
    /// </remarks>
    private (List<int> pitches, List<double> durations) ParseMelodyInfo(byte[] melodyMidi)
    {
        var pitches = new List<int>();
        var durations = new List<double>();

        try
        {
            using var stream = new MemoryStream(melodyMidi);
            var midiFile = new MidiFile(stream, false);

            // 简化的MIDI解析，提取音符信息
            foreach (var track in midiFile.Events)
            {
                int currentPitch = 60; // 默认中央C
                double currentDuration = 0.5; // 默认半拍

                foreach (var midiEvent in track)
                {
                    if (midiEvent is NoteOnEvent noteOn && noteOn.Velocity > 0)
                    {
                        currentPitch = noteOn.NoteNumber;
                        currentDuration = 0.5; // 假设四分音符
                        pitches.Add(currentPitch);
                        durations.Add(currentDuration);
                    }
                }
            }

            // 如果没有音符，添加默认音符
            if (pitches.Count == 0)
            {
                pitches.AddRange([60, 62, 64, 65]);
                durations.AddRange([0.5, 0.5, 0.5, 0.5]);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MIDI解析失败，使用默认旋律");
            // 使用默认旋律
            pitches.AddRange([60, 62, 64, 65]);
            durations.AddRange([0.5, 0.5, 0.5, 0.5]);
        }

        return (pitches, durations);
    }

    /// <summary>
    /// 生成单个字的音频
    /// </summary>
    /// <param name="word">要生成音频的字</param>
    /// <param name="pitch">音高</param>
    /// <param name="duration">时长</param>
    /// <returns>字的音频字节数组</returns>
    /// <exception cref="ArgumentException">输入数据无效</exception>
    /// <remarks>
    /// 生成单个字的音频，基于音高和时长。使用简单的正弦波模型。
    /// </remarks>
    private byte[] GenerateWordAudio(string word, int pitch, double duration)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new WaveFileWriter(memoryStream, new WaveFormat(44100, 16, 2));

        // 简化的音频生成
        int sampleRate = 44100;
        int samplesPerWord = (int)(sampleRate * duration * 0.5); // 简化时长计算
        short[] buffer = new short[samplesPerWord * 2]; // 立体声

        // 音高转频率
        double frequency = 440 * Math.Pow(2, (pitch - 69) / 12.0);

        // 生成音频数据
        for (int i = 0; i < samplesPerWord; i++)
        {
            // 音量包络
            double envelope = GetEnvelope(i, samplesPerWord);
            double sample = Math.Sin(2 * Math.PI * frequency * i / sampleRate) * 0.5 * envelope;
            short value = (short)(sample * 32767);

            buffer[i * 2] = value;     // 左声道
            buffer[i * 2 + 1] = value; // 右声道
        }

        writer.WriteSamples(buffer, 0, buffer.Length);
        writer.Flush();

        return memoryStream.ToArray();
    }

    /// <summary>
    /// 获取音量包络（ADSR）
    /// </summary>
    /// <param name="position">当前样本位置</param>
    /// <param name="totalLength">总样本长度</param>
    /// <returns>音量包络值</returns>
    /// <exception cref="ArgumentException">输入数据无效</exception>
    /// <remarks>
    /// 计算音量包络值，用于模拟音频中的音量变化。使用ADSR模型（攻击、衰减、 sustain、释放）。
    /// </remarks>
    private double GetEnvelope(int position, int totalLength)
    {
        double normalizedPos = (double)position / totalLength;
        
        if (normalizedPos < 0.05) // 起音
            return normalizedPos / 0.05;
        else if (normalizedPos < 0.2) // 衰减
            return 1.0 - (1.0 - 0.8) * (normalizedPos - 0.05) / 0.15;
        else if (normalizedPos < 0.8) // 延音
            return 0.8;
        else // 释音
            return 0.8 * (1.0 - (normalizedPos - 0.8) / 0.2);
    }
}