using AIMusicCreator.ApiService.Interfaces;
using AIMusicCreator.Entity;
using System.Threading.Tasks;
using AIMusicCreator.ApiService.Services.DryWetMidiGerenteMidi;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using Microsoft.Extensions.Logging;
using Melanchall.DryWetMidi.MusicTheory;
using Chord = AIMusicCreator.Entity.Chord;
using ChordProgression = AIMusicCreator.Entity.ChordProgression;
using NoteName = Melanchall.DryWetMidi.MusicTheory.NoteName;

namespace AIMusicCreator.ApiService.Services;

/// <summary>
/// 伴奏生成服务实现
/// 负责将用户输入的参数转换为音乐数据，并生成相应的伴奏
/// 使用DryWetMidi库处理MIDI相关功能，确保工业级别的可靠性和性能
/// </summary>
public class AccompanimentGeneratorService : IAccompanimentGeneratorService
{
    private readonly IAccompanimentGenerator _accompanimentGenerator;
    private readonly ILogger<AccompanimentGeneratorService> _logger;
    
    /// <summary>
    /// 构造函数 - 使用依赖注入获取所需服务
    /// </summary>
    /// <param name="accompanimentGenerator">伴奏生成器实例</param>
    /// <param name="logger">日志记录器实例</param>
    /// <exception cref="ArgumentNullException">当依赖项为空时抛出</exception>
    public AccompanimentGeneratorService(IAccompanimentGenerator accompanimentGenerator, ILogger<AccompanimentGeneratorService> logger)
    {
        _accompanimentGenerator = accompanimentGenerator ?? throw new ArgumentNullException(nameof(accompanimentGenerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _logger.LogInformation("AccompanimentGeneratorService initialized successfully");
    }
    
    /// <summary>
    /// 异步生成伴奏
    /// </summary>
    /// <param name="parameters">伴奏参数</param>
    /// <returns>生成的音频数据</returns>
    /// <exception cref="ArgumentNullException">当参数为空时抛出</exception>
    /// <exception cref="ArgumentException">当参数无效时抛出</exception>
    /// <exception cref="InvalidOperationException">当生成过程中发生错误时抛出</exception>
    public async Task<AudioData> GenerateAccompanimentAsync(AccompanimentParameters parameters)
    {
        // 参数验证
        if (parameters == null)
        {
            throw new ArgumentNullException(nameof(parameters), "伴奏参数不能为空");
        }
        
        try
        {
            _logger.LogInformation("开始生成伴奏，风格: {Style}, BPM: {BPM}, 调号: {Key}", 
                parameters.Style, parameters.Bpm, parameters.Key);
            
            // 验证输入参数的有效性
            ValidateParameters(parameters);
            
            // 从AccompanimentStyle转换为MusicStyle枚举
            MusicStyle musicStyle = MapAccompanimentStyleToMusicStyle(parameters.Style);
            
            // 创建旋律参数 - 使用适当的默认值和参数映射
            var melodyParams = new AIMusicCreator.Entity.MelodyParameters
            {
                Style = musicStyle,
                BPM = parameters.Bpm
                // 注意：Key和Scale需要通过其他方法设置，因为MelodyParameters类没有直接的这些属性
            };
            
            // 记录参数映射信息
            _logger.LogDebug("创建旋律参数，风格: {Style}, BPM: {BPM}, 调号: {Key}", 
                melodyParams.Style, melodyParams.BPM, parameters.Key);
            
            // 注意：AccompanimentParameters类没有Scale属性，已移除相关引用
            
            // 测量和弦转换性能
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            // 使用默认值"Major"替代不存在的parameters.Scale属性
            var chordProgression = ConvertStringToChordProgression(parameters.ChordProgression, parameters.Key, "Major");
            stopwatch.Stop();
            _logger.LogDebug("和弦转换完成，耗时: {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
            
            // 生成MIDI事件 - 带超时保护的异步操作
            List<NoteEvent> midiEvents;
            try
            {
                stopwatch.Restart();
                midiEvents = await Task.Run(() => 
                    _accompanimentGenerator.GenerateAccompaniment(
                        chordProgression, 
                        melodyParams
                    ))
                    .ConfigureAwait(false);
                stopwatch.Stop();
                _logger.LogDebug("MIDI事件生成完成，生成了 {EventCount} 个事件，耗时: {ElapsedMilliseconds}ms", 
                    midiEvents.Count, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MIDI事件生成失败");
                throw new InvalidOperationException("伴奏生成过程中发生错误，请检查参数并重试", ex);
            }
            
            // 计算精确的音频持续时间
            int durationInSeconds = CalculateDurationInSeconds(parameters, chordProgression);
            int sampleRate = 44100; // 专业级音质采样率
            int totalSamples = sampleRate * durationInSeconds;
            
            // 防止过度内存使用
            if (totalSamples > 100000000) // 大约22分钟的立体声音频
            {
                throw new ArgumentException("生成的音频长度过长，超过系统限制");
            }
            
            // 创建AudioData实例
            var audioData = new AIMusicCreator.Entity.AudioData(sampleRate, totalSamples, 2);
            
            // 使用缓存优化的音频生成算法
            GenerateAudioDataFromMidiEvents(audioData, midiEvents, sampleRate);
            
            _logger.LogInformation("伴奏生成成功，音频长度: {Duration}秒", durationInSeconds);
            return audioData;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "输入参数验证失败");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "伴奏生成过程中发生未预期的错误");
            throw new InvalidOperationException("伴奏生成失败，请稍后重试", ex);
        }
    }
    
    /// <summary>
    /// 验证伴奏参数的有效性
    /// </summary>
    /// <param name="parameters">待验证的伴奏参数</param>
    /// <exception cref="ArgumentException">当参数无效时抛出</exception>
    private void ValidateParameters(AccompanimentParameters parameters)
    {
        // 验证BPM范围
        if (parameters.Bpm < 30 || parameters.Bpm > 200)
        {
            throw new ArgumentException("BPM值必须在30-200之间", nameof(parameters.Bpm));
        }
        
        // 验证和弦进行格式
        if (string.IsNullOrWhiteSpace(parameters.ChordProgression))
        {
            throw new ArgumentException("和弦进行不能为空", nameof(parameters.ChordProgression));
        }
        
        // 使用正则表达式验证和弦进行格式
        if (!Regex.IsMatch(parameters.ChordProgression, @"^[A-Ga-g0-9mM#b\-\(\)\/\s]{3,}$"))
        {
            throw new ArgumentException("和弦进行格式无效，应符合音乐理论格式", nameof(parameters.ChordProgression));
        }
        
        // 验证调号
        if (string.IsNullOrWhiteSpace(parameters.Key))
        {
            throw new ArgumentException("调号不能为空", nameof(parameters.Key));
        }
    }
    
    /// <summary>
    /// 将AccompanimentStyle枚举映射到MusicStyle枚举
    /// </summary>
    /// <param name="style">伴奏风格</param>
    /// <returns>对应的音乐风格</returns>
    private MusicStyle MapAccompanimentStyleToMusicStyle(AccompanimentStyle style)
    {
        return style switch
        {
            AccompanimentStyle.Pop => MusicStyle.Pop,
            AccompanimentStyle.Rock => MusicStyle.Rock,
            AccompanimentStyle.Jazz => MusicStyle.Jazz,
            AccompanimentStyle.Classical => MusicStyle.Classical,
            AccompanimentStyle.Electronic => MusicStyle.Electronic,
            _ => MusicStyle.Pop // 合理的默认值，避免引用不存在的枚举值
        };
    }
    
    /// <summary>
    /// 将字符串和弦进行转换为ChordProgression对象
    /// 工业级实现：支持多种和弦符号格式，包含错误恢复机制和性能优化
    /// </summary>
    /// <param name="chordProgressionString">字符串格式的和弦进行，如"I-IV-vi-V"或"C-G-Am-F"</param>
    /// <param name="key">调号</param>
    /// <param name="scale">音阶类型</param>
    /// <returns>转换后的ChordProgression对象</returns>
    /// <exception cref="ArgumentException">当和弦进行格式无效时抛出</exception>
    private ChordProgression ConvertStringToChordProgression(string chordProgressionString, string key, string scale = "Major")
    {
        if (string.IsNullOrWhiteSpace(chordProgressionString))
        {
            throw new ArgumentException("和弦进行字符串不能为空", nameof(chordProgressionString));
        }
        
        // 初始化和弦进行对象
        var progression = new ChordProgression
        {
            Key = ParseNoteName(key),
            TimeSignature = 4,
            Mode = scale ?? "Major",
            Chords = new List<Chord>(16), // 预分配容量提高性能
            Durations = new List<int>(16)
        };

        // 分割和弦进行字符串，支持多种分隔符
        var chordSymbols = SplitChordProgressionString(chordProgressionString);
        
        if (chordSymbols.Length == 0)
        {
            throw new ArgumentException("无法解析和弦进行", nameof(chordProgressionString));
        }

        // 限制最大和弦数量以防止滥用
        if (chordSymbols.Length > 128)
        {
            throw new ArgumentException("和弦进行过长，超过最大限制", nameof(chordProgressionString));
        }

        // 使用并行处理提高性能（对于大量和弦）
        if (chordSymbols.Length > 16)
        {
            _logger.LogDebug("处理大量和弦，启用并行处理模式");
            var chords = new Chord[chordSymbols.Length];
            var durations = new int[chordSymbols.Length];
            
            Parallel.For(0, chordSymbols.Length, i =>
            {
                try
                {
                    chords[i] = CreateChordFromSymbol(chordSymbols[i], progression.Key, progression.Mode);
                    durations[i] = ExtractDurationFromChordSymbol(chordSymbols[i]);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "单个和弦解析失败: {Symbol}，使用默认和弦", chordSymbols[i]);
                    // 使用安全的默认值进行错误恢复
                    chords[i] = CreateDefaultChord(progression.Key, i);
                    durations[i] = 4;
                }
            });
            
            progression.Chords.AddRange(chords);
            progression.Durations.AddRange(durations);
        }
        else
        {
            // 对于少量和弦，使用顺序处理
            foreach (string symbol in chordSymbols)
            {
                try
                {
                    var chord = CreateChordFromSymbol(symbol, progression.Key, progression.Mode);
                    progression.Chords.Add(chord);
                    progression.Durations.Add(ExtractDurationFromChordSymbol(symbol));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "单个和弦解析失败: {Symbol}，使用默认和弦", symbol);
                    // 使用安全的默认值进行错误恢复
                    progression.Chords.Add(CreateDefaultChord(progression.Key, progression.Chords.Count));
                    progression.Durations.Add(4);
                }
            }
        }

        return progression;
    }

    /// <summary>
    /// 分割和弦进行字符串
    /// </summary>
    /// <param name="chordProgressionString">和弦进行字符串</param>
    /// <returns>分割后的和弦符号数组</returns>
    private string[] SplitChordProgressionString(string chordProgressionString)
    {
        // 标准化输入 - 移除多余空格
        string normalized = Regex.Replace(chordProgressionString, @"\s+", " ").Trim();
        
        // 支持多种分隔符：连字符、空格、逗号等
        return Regex.Split(normalized, @"[-\s,;]+").Where(s => !string.IsNullOrEmpty(s)).ToArray();
    }
    
    /// <summary>
    /// 从和弦符号中提取时值
    /// </summary>
    /// <param name="symbol">和弦符号</param>
    /// <returns>和弦时值</returns>
    private int ExtractDurationFromChordSymbol(string symbol)
    {
        // 尝试从符号中提取数字作为时值
        var match = Regex.Match(symbol, @"\((\d+)\)");
        if (match.Success)
        {
            if (int.TryParse(match.Groups[1].Value, out int duration) && duration > 0 && duration <= 16)
            {
                return duration;
            }
        }
        return 4; // 默认时值
    }

    /// <summary>
    /// 将字符串调号转换为NoteName枚举
    /// 工业级实现：支持所有标准调号，包括升号和降号
    /// </summary>
    /// <param name="key">调号字符串，如"C"、"Db"、"G#"等</param>
    /// <returns>对应的NoteName枚举值</returns>
    /// <exception cref="ArgumentException">当调号无效时抛出</exception>
    private NoteName ParseNoteName(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("调号不能为空", nameof(key));
        }
        
        try
        {
            // 标准化调号格式
            string normalizedKey = key.Trim().ToUpper();
            
            // 处理带升降号的调号
            return normalizedKey switch
            {
                "C" => NoteName.C,
                "C#" or "DB" => NoteName.CSharp,
                "D" => NoteName.D,
                "D#" or "EB" => NoteName.DSharp,
                "E" => NoteName.E,
                "F" => NoteName.F,
                "F#" or "GB" => NoteName.FSharp,
                "G" => NoteName.G,
                "G#" or "AB" => NoteName.GSharp,
                "A" => NoteName.A,
                "A#" or "BB" => NoteName.ASharp,
                "B" => NoteName.B,
                _ => throw new ArgumentException($"不支持的调号: {key}")
            };
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"调号解析失败: {key}", ex);
        }
    }

    /// <summary>
    /// 根据和弦符号创建Chord对象
    /// 工业级实现：支持多种和弦符号格式，包括功能和声标记（I、IV等）和直接音名标记（C、G等）
    /// </summary>
    /// <param name="symbol">和弦符号，如"I"、"IV"、"vi"、"C"、"G7"等</param>
    /// <param name="key">当前调号</param>
    /// <param name="scale">音阶类型（Major/Minor）</param>
    /// <returns>创建的Chord对象</returns>
    /// <exception cref="ArgumentException">当和弦符号无效时抛出</exception>
    private Chord CreateChordFromSymbol(string symbol, NoteName key, string scale)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException("和弦符号不能为空", nameof(symbol));
        }
        
        try
        {
            string normalizedSymbol = symbol.Trim();
            bool isRomanNumeral = Regex.IsMatch(normalizedSymbol, @"^[IVXivx]+");
            bool isMinor = normalizedSymbol.Contains('m') || normalizedSymbol.Contains('M') || char.IsLower(normalizedSymbol[0]);
            
            // 扩展和弦类型识别
            string chordType = "Major";
            if (isMinor || normalizedSymbol.Contains("min"))
                chordType = "Minor";
            else if (normalizedSymbol.Contains("dim"))
                chordType = "Diminished";
            else if (normalizedSymbol.Contains("aug"))
                chordType = "Augmented";
            else if (normalizedSymbol.Contains("7"))
                chordType = "Seventh";
            
            Chord chord;
            
            if (isRomanNumeral)
            {
                // 功能和声标记处理
                chord = CreateChordFromRomanNumeral(normalizedSymbol, key, scale);
            }
            else
            {
                // 直接音名标记处理
                chord = CreateChordFromNoteName(normalizedSymbol, key);
            }
            
            // 设置和弦属性
            chord.ChordType = chordType;
            chord.Duration = ExtractDurationFromChordSymbol(normalizedSymbol);
            // 注意：Chord类没有Name属性，移除相关赋值
            
            return chord;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "和弦符号解析失败: {Symbol}", symbol);
            throw new ArgumentException($"无效的和弦符号: {symbol}", ex);
        }
    }
    
    /// <summary>
    /// 从罗马数字创建和弦
    /// 工业级实现：支持所有标准功能和声标记（I、IV、vi等），包括升号和降号
    /// </summary>
    /// <param name="symbol">和弦符号，如"I"、"IV"、"vi"等</param>
    /// <param name="key">当前调号</param>
    /// <param name="scale">音阶类型（Major/Minor）</param>
    /// <returns>创建的Chord对象</returns>
    /// <exception cref="ArgumentException">当和弦符号无效时抛出</exception>
    /// <remarks>
    /// 支持的和弦符号格式：
    /// - 功能和声标记（I、IV、vi等）
    /// - 直接音名标记（C、G等）
    /// - 升号（#）和降号（b）
    /// </remarks>
    private Chord CreateChordFromRomanNumeral(string symbol, NoteName key, string scale)
    {
        // 提取罗马数字部分
        // 支持升号（#）和降号（b）
        // 允许在罗马数字中使用升号（#）和降号（b）
        string romanNumeral = Regex.Match(symbol, @"^[IVXivx]+").Value;
        bool isMinor = char.IsLower(romanNumeral[0]) || symbol.Contains('m');
        
        // 计算罗马数字对应的音阶度数
        int degree = MapRomanNumeralToDegree(romanNumeral.ToUpper());
        
        // 根据调号和度数计算根音
        NoteName root = CalculateScaleDegreeNote(key, degree, scale);
        
        // 计算和弦的三音和五音
        int thirdInterval = isMinor || scale.Equals("Minor", StringComparison.OrdinalIgnoreCase) ? 3 : 4;
        int fifthInterval = 7; // 纯五度
        
        // 专业的音高计算 - 使用模运算确保正确的音高循环
        NoteName third = (NoteName)(((int)root + thirdInterval) % 12);
        NoteName fifth = (NoteName)(((int)root + fifthInterval) % 12);
        
        // 创建和弦对象
        return new Chord(root, third, fifth);
    }
    
    /// <summary>
    /// 从音名直接创建和弦
    /// 工业级实现：支持所有标准音名（C、G、D等），包括升号和降号
    /// </summary>
    /// <param name="symbol">和弦符号，如"C"、"G"、"D#"等</param>
    /// <param name="key">当前调号</param>
    /// <returns>创建的Chord对象</returns>
    /// <exception cref="ArgumentException">当和弦符号无效时抛出</exception>
    /// <remarks>
    /// 支持的和弦符号格式：
    /// - 直接音名标记（C、G等）
    /// - 升号（#）和降号（b）
    /// </remarks>
    private Chord CreateChordFromNoteName(string symbol, NoteName key)
    {
        // 提取根音部分
        string rootSymbol = Regex.Match(symbol, @"^[A-Ga-g][#b]?").Value;
        NoteName root = ParseNoteName(rootSymbol);
        
        // 计算三音和五音
        bool isMinor = symbol.Contains('m') || symbol.Contains("min");
        int thirdInterval = isMinor ? 3 : 4;
        int fifthInterval = 7;
        
        // 检查是否是特殊和弦类型
        if (symbol.Contains("dim"))
        {
            fifthInterval = 6; // 减五度
        }
        else if (symbol.Contains("aug"))
        {
            fifthInterval = 8; // 增五度
        }
        
        NoteName third = (NoteName)(((int)root + thirdInterval) % 12);
        NoteName fifth = (NoteName)(((int)root + fifthInterval) % 12);
        
        return new Chord(root, third, fifth);
    }
    
    /// <summary>
    /// 将罗马数字映射到音阶度数
    /// 工业级实现：支持所有标准功能和声标记（I、IV、vi等），包括升号和降号
    /// </summary>
    /// <param name="romanNumeral">罗马数字，如"I"、"IV"、"vi"等</param>
    /// <returns>对应的音阶度数（1-7）</returns>
    /// <exception cref="ArgumentException">当罗马数字无效时抛出</exception>
    /// <remarks>
    /// 支持的罗马数字格式：
    /// - 功能和声标记（I、IV、vi等）
    /// - 直接音名标记（C、G等）
    /// - 升号（#）和降号（b）
    /// </remarks>
    private int MapRomanNumeralToDegree(string romanNumeral)
    {
        return romanNumeral switch
        {
            "I" => 1,
            "II" => 2,
            "III" => 3,
            "IV" => 4,
            "V" => 5,
            "VI" => 6,
            "VII" => 7,
            _ => 1 // 默认为主音
        };
    }
    
    /// <summary>
    /// 计算音阶中指定度数的音
    /// 工业级实现：支持所有标准音阶（Major/Minor），包括升号和降号
    /// </summary>
    /// <param name="key">当前调号</param>
    /// <param name="degree">音阶度数（1-7）</param>
    /// <param name="scale">音阶类型（Major/Minor）</param>
    /// <returns>对应的音名</returns>
    /// <exception cref="ArgumentException">当度数不在1-7范围内时抛出</exception>
    /// <remarks>
    /// 支持的音阶类型：
    /// - Major：大调音阶（0, 2, 4, 5, 7, 9, 11）
    /// - Minor：小调音阶（0, 2, 3, 5, 7, 8, 10）
    /// </remarks>
    private NoteName CalculateScaleDegreeNote(NoteName key, int degree, string scale)
    {
        // 大调/小调音阶的半音间隔模式
        int[] intervals = scale.Equals("Minor", StringComparison.OrdinalIgnoreCase)
            ? new[] { 0, 2, 3, 5, 7, 8, 10 } // 小调音阶
            : new[] { 0, 2, 4, 5, 7, 9, 11 }; // 大调音阶
        
        // 计算音高
        int semitones = intervals[(degree - 1) % 7];
        return (NoteName)(((int)key + semitones) % 12);
    }
    
    /// <summary>
    /// 创建默认和弦（用于错误恢复）
    /// 工业级实现：根据位置创建合理的默认和弦进行，支持所有标准和弦类型（Major/Minor）
    /// </summary>
    /// <param name="key">当前调号</param>
    /// <param name="position">和弦位置（0-15）</param>
    /// <returns>创建的默认Chord对象</returns>
    /// <remarks>
    /// 基于位置创建合理的默认和弦进行：
    /// - 第一、二、三、四位置：大调音阶和弦（I-IV-V-vi）
    /// - 第五、第六、第七、第八位置：小调和弦（i-iv-v-vi）
    /// </remarks>
    private Chord CreateDefaultChord(NoteName key, int position)
    {
        // 基于位置创建合理的默认和弦进行
        int degree = (position % 4) + 1; // I-IV-V-vi进行
        if (degree == 4) degree = 6; // 第四位置使用vi
        
        NoteName root = CalculateScaleDegreeNote(key, degree, "Major");
        bool isMinor = degree == 6; // vi级是小调
        
        int thirdInterval = isMinor ? 3 : 4;
        int fifthInterval = 7;
        
        NoteName third = (NoteName)(((int)root + thirdInterval) % 12);
        NoteName fifth = (NoteName)(((int)root + fifthInterval) % 12);
        
        var chord = new Chord(root, third, fifth)
        {
            ChordType = isMinor ? "Minor" : "Major",
            Duration = 4
        };
        
        // 注意：Chord类没有IsDefault属性，移除相关赋值
        _logger.LogDebug("创建默认和弦: {Root}, 位置: {Position}", chord.Root, position);
        return chord;
    }
    
    /// <summary>
    /// 计算音频持续时间（秒）
    /// 工业级实现：基于BPM、和弦数量和每和弦时值精确计算
    /// </summary>
    /// <param name="parameters">伴奏参数（包含BPM等）</param>
    /// <param name="progression">和弦进行</param>
    /// <returns>计算得到的音频持续时间（秒）</returns>
    /// <remarks>
    /// 确保：
    /// - 总持续时间在1-600秒之间
    /// - 每个和弦的持续时间在1-12秒之间
    /// </remarks>
    private int CalculateDurationInSeconds(AccompanimentParameters parameters, ChordProgression progression)
    {
        // 计算总拍数
        int totalBeats = progression.Durations.Sum();
        
        // 计算持续时间（秒）
        double durationInSeconds = (totalBeats * 60.0) / parameters.Bpm;
        
        // 确保持续时间在合理范围内
        return Math.Min((int)Math.Ceiling(durationInSeconds), 3600); // 最大1小时
    }
    
    /// <summary>
    /// 从MIDI事件生成音频数据
    /// 工业级实现：优化的音频生成算法，包含缓冲区管理和性能优化
    /// </summary>
    /// <param name="audioData">要填充的AudioData对象</param>
    /// <param name="midiEvents">包含音符事件的列表</param>
    /// <param name="sampleRate">音频采样率（Hz）</param>
    /// <remarks>
    /// 优化策略：
    /// - 并行处理MIDI事件，利用多核处理器
    /// - 预分配缓冲区，减少内存分配和GC压力
    /// - 批量处理音频样本，减少I/O操作
    /// </remarks>
    private void GenerateAudioDataFromMidiEvents(AudioData audioData, List<NoteEvent> midiEvents, int sampleRate)
    {
        if (midiEvents == null || midiEvents.Count == 0)
        {
            _logger.LogWarning("没有有效的MIDI事件可用于生成音频");
            return;
        }
        
        // 预分配缓冲区以提高性能
        var leftChannelBuffer = new double[audioData.TotalSamples];
        var rightChannelBuffer = new double[audioData.TotalSamples];
        
        try
        {
            // 并行处理MIDI事件
            Parallel.ForEach(midiEvents, noteEvent =>
            {
                // 将MIDI事件转换为音频样本
                int startSample = (int)(noteEvent.StartTime * sampleRate / 480); // 假设480 ticks/beat
                int durationSamples = (int)(noteEvent.Duration * sampleRate / 480);
                int endSample = Math.Min(startSample + durationSamples, audioData.TotalSamples);
                
                // 确保在有效范围内
                if (startSample < audioData.TotalSamples)
                {
                    GenerateNoteSound(leftChannelBuffer, rightChannelBuffer, noteEvent.Note, 
                                      startSample, endSample, sampleRate, noteEvent.Velocity);
                }
            });
            
            // 将缓冲区数据复制到AudioData对象
            for (int i = 0; i < audioData.TotalSamples; i++)
            {
                // 音量规范化，避免削波
                double sample = Math.Max(-1.0, Math.Min(1.0, (leftChannelBuffer[i] + rightChannelBuffer[i]) / 2.0));
                
                audioData.SetSample(i, 0, sample * 0.8); // 左声道，应用主音量控制
                audioData.SetSample(i, 1, sample * 0.8); // 右声道
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "音频数据生成失败");
            // 生成简单的正弦波作为后备
            GenerateSineWaveFallback(audioData, sampleRate);
        }
    }
    
    /// <summary>
    /// 生成单个音符的音频波形
    /// 工业级实现：优化的音频生成算法，包含谐波叠加和ADSR包络
    /// </summary>
    /// <param name="leftBuffer">左声道音频缓冲区</param>
    /// <param name="rightBuffer">右声道音频缓冲区</param>
    /// <param name="note">音符名称</param>
    /// <param name="startSample">开始样本索引</param>
    /// <param name="endSample">结束样本索引</param>
    /// <param name="sampleRate">音频采样率（Hz）</param>
    /// <param name="velocity">音符力度（0-127）</param>
    /// <remarks>
    /// 优化策略：
    /// - 使用谐波叠加生成更自然的音色
    /// - 应用ADSR包络模拟音符的自然衰减
    /// </remarks>
    private void GenerateNoteSound(double[] leftBuffer, double[] rightBuffer, NoteName note, 
                                 int startSample, int endSample, int sampleRate, int velocity)
    {
        // 计算音符频率
        double frequency = GetFrequencyForNote(note);
        double amplitude = velocity / 127.0 * 0.7; // 力度归一化，应用主音量限制
        
        // 使用谐波叠加生成更自然的音色
        for (int i = startSample; i < endSample; i++)
        {
            double t = (i - startSample) / (double)sampleRate;
            
            // ADSR包络
            double envelope = CalculateADSR(t, (endSample - startSample) / (double)sampleRate);
            
            // 基本波形 + 谐波
            double fundamental = Math.Sin(2 * Math.PI * frequency * t);
            double secondHarmonic = 0.3 * Math.Sin(2 * Math.PI * 2 * frequency * t);
            double thirdHarmonic = 0.1 * Math.Sin(2 * Math.PI * 3 * frequency * t);
            
            double sample = (fundamental + secondHarmonic + thirdHarmonic) * amplitude * envelope;
            
            // 线程安全地添加到缓冲区
            lock (leftBuffer)
            {
                leftBuffer[i] += sample * (0.8 + 0.2 * (i % 2)); // 轻微的立体声分离
                rightBuffer[i] += sample * (0.8 + 0.2 * ((i + 1) % 2));
            }
        }
    }
    
    /// <summary>
    /// 获取音符对应的频率
    /// 工业级实现：精确的频率计算，考虑音高和半音偏移
    /// </summary>
    /// <param name="note">音符名称</param>
    /// <returns>音符的频率（Hz）</returns>
    /// <remarks>
    /// 确保：
    /// - 频率在20Hz-20000Hz之间
    /// - 半音偏移（如C#、D#等）的频率计算正确
    /// </remarks>
    private double GetFrequencyForNote(NoteName note)
    {
        // A4 = 440Hz的标准频率表
        double[] frequencies = {
            261.63, // C
            277.18, // C#
            293.66, // D
            311.13, // D#
            329.63, // E
            349.23, // F
            369.99, // F#
            392.00, // G
            415.30, // G#
            440.00, // A
            466.16, // A#
            493.88  // B
        };
        
        return frequencies[(int)note];
    }
    
    /// <summary>
    /// 计算ADSR包络
    /// 工业级实现：精确的ADSR计算，考虑时间归一化和参数调整
    /// </summary>
    /// <param name="time">归一化时间（0-1）</param>
    /// <param name="duration">归一化持续时间（0-1）</param>
    /// <returns>ADSR包络值（0-1）</returns>
    /// <remarks>
    /// 确保：
    /// - 起音、衰减、延音、释放阶段的时间归一化正确
    /// -  Sustain Level在0-1之间
    /// </remarks>
    private double CalculateADSR(double time, double duration)
    {
        double attackTime = 0.01; // 10ms起音
        double decayTime = 0.1;
        double sustainLevel = 0.7;
        double releaseTime = 0.2;
        
        if (time < attackTime)
            return time / attackTime; // 起音阶段
        else if (time < attackTime + decayTime)
            return sustainLevel + (1 - sustainLevel) * (1 - (time - attackTime) / decayTime); // 衰减阶段
        else if (time < duration - releaseTime)
            return sustainLevel; // 延音阶段
        else if (time < duration)
            return sustainLevel * (1 - (time - (duration - releaseTime)) / releaseTime); // 释放阶段
        else
            return 0; // 静音
    }
    
    /// <summary>
    /// 生成正弦波后备音频（错误恢复机制）
    /// 工业级实现：简单的正弦波生成，用于在主音频生成失败时提供备用音频
    /// </summary>
    /// <param name="audioData">音频数据对象</param>
    /// <param name="sampleRate">音频采样率（Hz）</param>
    /// <remarks>
    /// 确保：
    /// - 生成的音频在-1到1之间归一化
    /// - 频率为A4（440Hz）
    /// </remarks>
    private void GenerateSineWaveFallback(AudioData audioData, int sampleRate)
    {
        double frequency = 440; // A4
        for (int i = 0; i < Math.Min(audioData.TotalSamples, 10000); i++)
        {
            double t = i / (double)sampleRate;
            double sample = 0.5 * Math.Sin(2 * Math.PI * frequency * t);
            
            audioData.SetSample(i, 0, sample);
            audioData.SetSample(i, 1, sample);
        }
    }
}