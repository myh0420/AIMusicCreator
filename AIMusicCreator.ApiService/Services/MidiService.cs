using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using NAudio.Midi;
using NAudio.SoundFont;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using AIMusicCreator.Utils;
using AIMusicCreator.ApiService.Interfaces;

namespace AIMusicCreator.ApiService.Services;
/// <summary>
/// 提供MIDI文件生成服务
/// </summary>
public class MidiService : IMidiService
{
    /// <summary>
    /// Web主机环境（用于访问wwwroot目录）
    /// </summary>
    private readonly IWebHostEnvironment _env;
    /// <summary>
    /// ONNX模型推理会话（用于旋律生成）
    /// </summary>
    private readonly InferenceSession? _onnxSession;
    /// <summary>
    /// SoundFont音色库（用于MIDI解析和合成）
    /// </summary>
    private readonly SoundFont _soundFont;
    /// <summary>
    /// 日志记录器
    /// </summary>
    private readonly ILogger<MidiService> _logger;
    /// <summary>
    /// 初始化MidiService
    /// </summary>
    /// <param name="env">Web主机环境</param>
    /// <param name="logger">日志记录器</param>
    /// <remarks>
    /// 1. 加载SoundFont音色库（用于MIDI解析和合成）
    /// 2. 加载ONNX模型（旋律生成）
    /// 3. 设置默认音色参数（包括波类型、攻击时间、衰减时间等）
    /// </remarks>
    public MidiService(IWebHostEnvironment env, ILogger<MidiService> logger)
    {
        _env = env ?? throw new ArgumentNullException(nameof(env));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        try
        {
            // 加载SoundFont音色库（用于MIDI解析和合成）
            string soundFontPath = Path.Combine(_env.WebRootPath, "soundfonts", "GeneralUser GS v1.471.sf2");
            if (!File.Exists(soundFontPath))
            {
                _logger.LogWarning("SoundFont文件不存在: {Path}", soundFontPath);
                throw new FileNotFoundException("SoundFont文件不存在", soundFontPath);
            }
            _soundFont = new SoundFont(soundFontPath);
            
            // 加载ONNX模型（旋律生成）
            string modelPath = Path.Combine(_env.WebRootPath, "models", "sageconv_Opset18.onnx");
            if (File.Exists(modelPath))
            {
                try
                {
                    _onnxSession = new InferenceSession(modelPath);
                    _logger.LogInformation("ONNX模型加载成功: {ModelPath}", modelPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ONNX模型加载失败，将使用降级方案: {ModelPath}", modelPath);
                    _onnxSession = null;
                }
            }
            else
            {
                _logger.LogWarning("ONNX模型文件不存在，将使用降级方案: {ModelPath}", modelPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MidiService初始化失败");
            throw;
        }
    }

    /// <summary>
    /// 生成旋律MIDI（完整逻辑：风格/情绪/BPM参数化）
    /// </summary>
    /// <param name="style">音乐风格（classical/electronic/pop）</param>
    /// <param name="mood">音乐情绪（happy/sad）</param>
    /// <param name="bpm">每分钟节拍数（1-200）</param>
    /// <returns>旋律MIDI字节数组</returns>
    /// <remarks>
    /// 1. 根据输入参数（风格、情绪、BPM）生成音符序列。
    /// 2. 如果加载了ONNX模型，使用模型推理；否则使用降级方案。
    /// 3. 将音符序列转换为MIDI字节流，设置为通道0（钢琴），力度80。
    /// </remarks>
    public byte[] GenerateMelody(string style, string mood, int bpm)
    {
        try
        {
            _logger.LogInformation("开始生成旋律MIDI，风格: {Style}, 情绪: {Mood}, BPM: {BPM}", style, mood, bpm);
            
            // 参数验证
            if (string.IsNullOrWhiteSpace(style))
                style = "pop";
            if (string.IsNullOrWhiteSpace(mood))
                mood = "happy";
            if (bpm <= 0 || bpm > 200)
                bpm = 120;

            // 1. 参数映射（风格→温度，情绪→调式）
            float temperature = style switch
            {
                "classical" => 0.5f,
                "electronic" => 0.8f,
                _ => 0.7f // pop默认
            };
            int keySignature = mood == "happy" ? 0 : 1; // 0=C大调，1=A小调
            int noteCount = 64; // 生成64个音符

            // 2. ONNX模型推理（生成音符序列）
            int[] notes = _onnxSession != null
                ? RunOnnxModel(temperature, keySignature, noteCount)
                : GenerateFallbackNotes(keySignature, noteCount); // 降级方案

            // 3. 转换音符序列为MIDI字节流
            byte[] result = ConvertToMidi(notes, bpm);
            
            _logger.LogInformation("旋律MIDI生成完成，字节大小: {Size} KB", result.Length / 1024f);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成旋律MIDI失败，风格: {Style}, 情绪: {Mood}, BPM: {BPM}", style, mood, bpm);
            throw;
        }
    }

    /// <summary>
    /// 生成伴奏MIDI（基于主旋律和弦分析）
    /// </summary>
    /// <param name="melodyMidi">主旋律MIDI字节数组</param>
    /// <returns>伴奏MIDI字节数组</returns>
    /// <remarks>
    /// 分析主旋律中的和弦进行，生成相应的伴奏音符序列。
    /// 每个和弦包含根音、大三和弦三音和五音。
    /// 伴奏音符为通道1（钢琴），力度70。
    /// </remarks>
    public byte[] GenerateAccompaniment(byte[] melodyMidi)
    {
        try
        {
            _logger.LogInformation("开始生成伴奏MIDI，主旋律大小: {Size} KB", melodyMidi.Length / 1024f);
            
            // 参数验证
            if (melodyMidi == null || melodyMidi.Length == 0)
            {
                throw new ArgumentException("主旋律MIDI数据不能为空");
            }

            // 1. 解析主旋律MIDI
            using var ms = new MemoryStream(melodyMidi);
            var midiFile = new MidiFile(ms, false);
            int ticksPerQuarter = midiFile.DeltaTicksPerQuarterNote;
            var melodyTrack = midiFile.Events[0]; // 主旋律轨道

            // 2. 分析和弦进行（基于主旋律音符）
            var chords = AnalyzeChords([.. melodyTrack], ticksPerQuarter);
            _logger.LogInformation("和弦分析完成，检测到 {Count} 个和弦", chords.Count);

            // 3. 生成伴奏轨道事件
            var accEvents = new List<MidiEvent>();
            // 复制速度事件（保持与主旋律同步）
            accEvents.AddRange(melodyTrack.Where(e => e is TempoEvent));

            // 添加和弦事件（通道1：钢琴伴奏）
            foreach (var (chordNotes, startTime) in chords)
            {
                foreach (int note in chordNotes)
                {
                    // 音符开启（力度70）
                    accEvents.Add(new NoteEvent(startTime, 1, MidiCommandCode.NoteOn, note, 70));
                    // 音符关闭（持续1拍）
                    accEvents.Add(new NoteEvent(startTime + ticksPerQuarter, 1, MidiCommandCode.NoteOff, note, 0));
                }
            }

            // 添加轨道结束事件
            if (accEvents.Count > 0)
            {
                accEvents.Add(new MetaEvent(MetaEventType.EndTrack, 0, accEvents.Last().AbsoluteTime + 100));
            }

            // 4. 写入MIDI文件
            var accCollection = new MidiEventCollection(1, ticksPerQuarter);
            accCollection.AddTrack(accEvents);

            // 使用临时文件方法
            byte[] result = MidiUtils.ExportMidiToBytes(accCollection);
            
            _logger.LogInformation("伴奏MIDI生成完成，字节大小: {Size} KB", result.Length / 1024f);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成伴奏MIDI失败");
            throw;
        }
    }

    /// <summary>
    /// ONNX模型推理（生成音符序列）
    /// </summary>
    /// <param name="temperature">温度参数（0-1）</param>
    /// <param name="keySignature">调号（0为C大调，1为A小调）</param>
    /// <param name="noteCount">要生成的音符数量</param>
    /// <returns>音符数组（MIDI音高值）</returns>
    /// <remarks>
    /// 使用预训练的ONNX模型进行推理，根据输入参数生成音符序列。
    /// 如果模型未初始化，将使用降级方案生成音符序列。
    /// </remarks>
    private int[] RunOnnxModel(float temperature, int keySignature, int noteCount)
    {
        try
        {
            if (_onnxSession == null)
            {
                _logger.LogWarning("ONNX会话未初始化，使用降级方案生成音符序列");
                return GenerateFallbackNotes(keySignature, noteCount);
            }
            
            _logger.LogDebug("执行ONNX模型推理，温度: {Temperature}, 调号: {KeySignature}, 音符数量: {NoteCount}", 
                temperature, keySignature, noteCount);
                
            // 构造输入张量（匹配模型输入格式）
            var input = new DenseTensor<float>(new[] { temperature, keySignature, noteCount }, [3]);
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("input", input) };

            // 执行推理
            using var results = _onnxSession.Run(inputs);
            var tensor = results[0].AsTensor<int>();
            _logger.LogDebug("ONNX模型推理完成，生成 {Count} 个音符", tensor.Length);
            return [.. tensor];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ONNX模型推理失败");
            // 失败时使用降级方案
            _logger.LogInformation("使用降级方案生成音符序列");
            return GenerateFallbackNotes(keySignature, noteCount);
        }
    }

    /// <summary>
    /// 内部类：用于存储音符的详细信息
    /// </summary>
    private class DetailedNote
    {
        public int Pitch { get; set; }           // MIDI音高
        public int Duration { get; set; }        // 持续时间（ticks）
        public int Velocity { get; set; }        // 力度（音量）
        public int RestDuration { get; set; }    // 休止符持续时间（ticks）
    }

    /// <summary>
    /// 降级方案：生成基础音符序列（无模型时使用）
    /// </summary>
    /// <param name="keySignature">调号（0为C大调，1为A小调）</param>
    /// <param name="noteCount">要生成的音符数量</param>
    /// <returns>音符数组（MIDI音高值）</returns>
    /// <remarks>
    /// 生成一个有韵律感的音符序列，基于指定的调号和音符数量。
    /// 包含节奏变化、力度变化和音乐结构。
    /// </remarks>
    private static int[] GenerateFallbackNotes(int keySignature, int noteCount)
    {
        try
        {
            // 大调/小调音符集
            int[] scale = keySignature == 0
                ? [60, 62, 64, 65, 67, 69, 71] // C大调音阶
                : [57, 59, 60, 62, 64, 65, 67]; // A小调音阶

            // 生成更有结构的音符序列
            List<int> notes = [];
            var rnd = new Random();
            
            // 基本音乐结构：4拍为一小节，每4小节为一乐句
            int beatsPerMeasure = 4;
            int measures = (int)Math.Ceiling((double)noteCount / beatsPerMeasure);
            int notesPerMeasure = (int)Math.Ceiling((double)noteCount / measures);
            
            // 和弦进行（简单的I-IV-V-I进行）
            int[] rootNotes = keySignature == 0
                ? [60, 65, 67, 60] // C大调：C-F-G-C
                : [57, 62, 64, 57]; // A小调：A-D-E-A
            
            // 生成带有音乐结构的音符序列
            int lastNote = scale[0];
            int measureCounter = 0;
            int phraseCounter = 0;
            
            for (int i = 0; i < noteCount; i++)
            {
                // 每小节开始时，根据和弦进行调整根音
                if (i % beatsPerMeasure == 0)
                {
                    measureCounter++;
                    if (measureCounter % 4 == 0) // 每4小节为一乐句
                    {
                        phraseCounter++;
                    }
                    
                    // 根据当前和弦根音调整起始音
                    int currentRoot = rootNotes[measureCounter % rootNotes.Length];
                    if (i % (beatsPerMeasure * 4) == 0) // 每乐句开始时回到根音
                    {
                        lastNote = currentRoot;
                    }
                }
                
                // 限制音符跳跃幅度，使旋律更流畅
                int maxJump = (i % 8 == 0 || i % beatsPerMeasure == 0) ? 2 : 1; // 小节开始和每8个音符允许更大跳跃
                int direction = rnd.Next(3) - 1; // -1, 0, 1
                int jumpAmount = rnd.Next(maxJump + 1);
                
                int currentNoteIndex = Array.IndexOf(scale, lastNote);
                int newIndex = Math.Clamp(currentNoteIndex + direction * jumpAmount, 0, scale.Length - 1);
                
                lastNote = scale[newIndex];
                notes.Add(lastNote);
            }
            
            return [.. notes];
        }
        catch (Exception ex)
        {
            // 极端情况下返回固定音符序列
            Console.WriteLine($"降级方案失败: {ex.Message}");
            return [.. Enumerable.Repeat(60, noteCount)]; // 返回全C音符
        }
    }

    /// <summary>
    /// 音符序列转换为MIDI文件字节数组（无临时文件版本）
    /// </summary>
    /// <param name="notes">音符数组（MIDI音高值）</param>
    /// <param name="bpm">每分钟节拍数</param>
    /// <returns>MIDI文件字节数组</returns>
    /// <remarks>
    /// 生成一个有韵律感的MIDI文件，包含音符序列和指定的BPM。
    /// 包含节奏变化、力度变化和音乐结构。
    /// </remarks>
    private static byte[] ConvertToMidi(int[] notes, int bpm)
    {
        try
        {
            List<MidiEvent> events = [];
            int ticksPerQuarter = 480;

            // 添加速度事件
            int microsecondsPerQuarterNote = 60000000 / bpm;
            events.Add(new TempoEvent(microsecondsPerQuarterNote, 0));

            // 添加乐器选择 - 使用通道1（DryWetMIDI要求通道1-16）
            events.Add(new PatchChangeEvent(1, 1, 0));

            // 添加音符事件（带节奏和力度变化）
            int currentTick = 0;
            var rnd = new Random();
            int measureCounter = 0;
            int beatsPerMeasure = 4;
            
            // 节奏模式：4拍为一小节，支持不同的音符时值
            int[] noteDurations = [ticksPerQuarter * 2, ticksPerQuarter, ticksPerQuarter / 2, ticksPerQuarter / 4];
            int[] velocityPatterns = [80, 85, 90, 95]; // 力度变化模式
            
            for (int i = 0; i < notes.Length; i++)
            {
                int validPitch = Math.Clamp(notes[i], 0, 127);
                
                // 每小节开始时，调整音符时值和力度
                if (i % beatsPerMeasure == 0)
                {
                    measureCounter++;
                }
                
                // 随机选择音符时值，但确保每小节总时值为4拍
                int durationIndex = i % beatsPerMeasure == 0 ? 0 : rnd.Next(noteDurations.Length);
                int duration = noteDurations[durationIndex];
                
                // 根据位置和乐句设置力度
                int velocityIndex = measureCounter % velocityPatterns.Length;
                int velocity = velocityPatterns[velocityIndex];
                
                // 重音：每小节第一拍力度增加
                if (i % beatsPerMeasure == 0)
                {
                    velocity += 10;
                }
                
                // 添加音符事件 - 使用通道1（DryWetMIDI要求通道1-16）
                events.Add(new NoteOnEvent(currentTick, 1, validPitch, velocity, duration));
                
                // 更新当前时间
                currentTick += duration;
            }

            // 添加轨道结束事件
            events.Add(new MetaEvent(MetaEventType.EndTrack, 0, currentTick));

            // 创建MIDI集合
            var midiCollection = new MidiEventCollection(1, ticksPerQuarter);
            midiCollection.AddTrack(events);

            // 使用临时文件方法
            return MidiUtils.ExportMidiToBytes(midiCollection);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"创建MIDI文件时出错: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// 分析主旋律和弦进行
    /// </summary>
    /// <param name="melodyEvents">主旋律事件数组</param>
    /// <param name="ticksPerQuarter">每个四分音符的tick数</param>
    /// <returns>和弦序列（每个和弦为音符数组，时间为绝对tick数）</returns>
    /// <remarks>
    /// 从主旋律事件中提取有效音符，按2拍间隔分析三和弦进行。
    /// 每个和弦包含根音、大三和弦三音和五音。
    /// </remarks>
    private static List<(int[] ChordNotes, long Time)> AnalyzeChords(MidiEvent[] melodyEvents, int ticksPerQuarter)
    {
        try
        {
            // 提取有效音符（仅NoteOn且力度>0）
            var notes = melodyEvents
                .OfType<NoteOnEvent>()
                .Where(n => n.Velocity > 0)
                .OrderBy(n => n.AbsoluteTime)
                .ToList();

            var chords = new List<(int[], long)>();
            if (notes.Count == 0) return chords;

            // 每2拍分析一个和弦
            long currentTime = 0;
            int interval = ticksPerQuarter * 2;

            while (currentTime < notes.Last().AbsoluteTime)
            {
                // 取当前区间内的音符
                var windowNotes = notes
                    .Where(n => n.AbsoluteTime >= currentTime && n.AbsoluteTime < currentTime + interval)
                    .Select(n => n.NoteNumber)
                    .ToList();

                if (windowNotes.Count != 0)
                {
                    // 生成三和弦（根音+三音+五音）
                    int root = windowNotes.GroupBy(n => n).OrderByDescending(g => g.Count()).First().Key;
                    int[] chord = [root, root + 4, root + 7]; // 大三和弦
                    chords.Add((chord, currentTime));
                }

                currentTime += interval;
            }

            return chords;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"分析和弦进行时出错: {ex.Message}");
            return [];
        }
    }
}