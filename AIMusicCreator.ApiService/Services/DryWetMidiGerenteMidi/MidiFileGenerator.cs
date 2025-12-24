using AIMusicCreator.Entity;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Note = Melanchall.DryWetMidi.Interaction.Note;
using NoteEvent = AIMusicCreator.Entity.NoteEvent;
using NoteName = Melanchall.DryWetMidi.MusicTheory.NoteName;
using SevenBitNumber = Melanchall.DryWetMidi.Common.SevenBitNumber;
using System;
using System.Collections.Generic;
using System.Linq;
using AIMusicCreator.ApiService.Interfaces;

namespace AIMusicCreator.ApiService.Services.DryWetMidiGerenteMidi
{
    /// <summary>
    /// MIDI文件生成器类，负责将音符事件列表转换为标准MIDI文件
    /// </summary>
    /// <remarks>
    /// 该类使用DryWetMidi库处理MIDI事件和文件操作，支持各种音符名称格式和MIDI事件处理
    /// 主要功能包括：
    /// - 音符名称解析和转换
    /// - MIDI音符编号计算
    /// - 音符事件创建和管理
    /// - MIDI文件生成和保存
    /// </remarks>
    public class MidiFileGenerator : IMidiFileGenerator
    {
        /// <summary>
        /// 伴奏生成器实例
        /// </summary>
        private readonly AccompanimentGenerator _accompanimentGenerator;
        /// <summary>
        /// 日志记录器实例
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// MIDI配置实例
        /// </summary>
        private readonly MidiConfig _config;

        /// <summary>
        /// 构造函数，初始化MIDI文件生成器
        /// </summary>
        /// <param name="logger">日志记录器实例，用于记录操作日志和错误信息</param>
        /// <exception cref="ArgumentNullException">当logger参数为null时抛出异常，提示"Logger cannot be null"</exception>
        public MidiFileGenerator(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger), "Logger cannot be null");
            _config = new MidiConfig(); // 使用默认配置
            _accompanimentGenerator = new AccompanimentGenerator(NullLogger<AccompanimentGenerator>.Instance);
        }

        /// <summary>
        /// 构造函数，初始化MIDI文件生成器
        /// </summary>
        /// <param name="logger">日志记录器实例，用于记录操作日志和错误信息</param>
        /// <param name="configPath">配置文件路径</param>
        /// <exception cref="ArgumentNullException">当logger参数为null时抛出异常，提示"Logger cannot be null"</exception>
        public MidiFileGenerator(ILogger logger, string configPath)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger), "Logger cannot be null");
            _config = MidiConfig.LoadFromJson(configPath); // 从配置文件加载
            _accompanimentGenerator = new AccompanimentGenerator(NullLogger<AccompanimentGenerator>.Instance);
        }

        /// <summary>
        /// 构造函数，初始化MIDI文件生成器
        /// </summary>
        /// <param name="logger">日志记录器实例，用于记录操作日志和错误信息</param>
        /// <param name="config">MIDI配置实例</param>
        /// <exception cref="ArgumentNullException">当logger参数为null时抛出异常，提示"Logger cannot be null"</exception>
        /// <exception cref="ArgumentNullException">当config参数为null时抛出异常，提示"配置参数不能为空"</exception>
        public MidiFileGenerator(ILogger logger, MidiConfig config)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger), "Logger cannot be null");
            _config = config ?? throw new ArgumentNullException(nameof(config), "配置参数不能为空");
            _accompanimentGenerator = new AccompanimentGenerator(NullLogger<AccompanimentGenerator>.Instance);
        }

        /// <summary>
        /// 生成MIDI文件
        /// </summary>
        /// <param name="notes">音符列表，包含要转换为MIDI的音符信息</param>
        /// <param name="bpm">速度，默认为配置文件中的BPM值</param>
        /// <returns>生成的MIDI文件的字节数组</returns>
        /// <exception cref="ArgumentNullException">当notes参数为null时抛出</exception>
        /// <exception cref="Exception">当生成MIDI文件过程中发生错误时抛出</exception>
        public byte[] GenerateMidiFile(List<NoteEvent> notes, int bpm = -1)
        {
            try
            {
                _logger.LogInformation("开始生成MIDI文件，音符数量: {NoteCount}, BPM: {BPM}", notes.Count, bpm);

                var midiFile = new MidiFile();
                var actualBpm = bpm > 0 ? bpm : _config.DefaultBPM;
                var tempoMap = TempoMap.Create(Tempo.FromBeatsPerMinute(actualBpm), new TimeSignature(4, 4));
                
                var trackChunk = new TrackChunk();
                
                // 添加音符到轨道，使用正确的MIDI事件创建方式
                foreach (var noteEvent in notes)
                {
                    try
                    {
                        // 直接使用noteEvent.Note（已经是NoteName类型），不需要解析
                        var noteNumber = GetNoteNumber(noteEvent.Note, noteEvent.Octave);
                        
                        // 计算音符持续时间（以tick为单位）
                        long deltaTime = (long)(noteEvent.StartTime * 100); // 简单转换
                        long duration = (long)(noteEvent.Duration * 100);
                        int velocity = noteEvent.Velocity; // 移除??操作符，因为int不是可空类型
                        if (velocity <= 0 || velocity > 127)
                        {
                            velocity = _config.DefaultVelocity; // 使用配置中的默认值
                        }
                        
                        // 创建音符开事件
                        var noteOnEvent = new NoteOnEvent(noteNumber, (SevenBitNumber)velocity) {
                            DeltaTime = deltaTime
                        };
                        
                        // 创建音符关事件
                        var noteOffEvent = new NoteOffEvent(noteNumber, (SevenBitNumber)velocity) {
                            DeltaTime = duration
                        };
                        
                        trackChunk.Events.Add(noteOnEvent);
                        trackChunk.Events.Add(noteOffEvent);
                        
                        _logger.LogTrace("添加音符: {Note}, 八度: {Octave}, 开始时间: {StartTime}, 持续时间: {Duration}", 
                            noteEvent.Note, noteEvent.Octave, noteEvent.StartTime, noteEvent.Duration);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "添加音符失败: {Note}", noteEvent.Note);
                    }
                }
                
                midiFile.Chunks.Add(trackChunk);
                midiFile.ReplaceTempoMap(tempoMap);
                
                using (var stream = new System.IO.MemoryStream())
                {
                    midiFile.Write(stream);
                    _logger.LogInformation("MIDI文件生成成功");
                    return stream.ToArray();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成MIDI文件失败");
                throw;
            }
        }

        /// <summary>
        /// 解析音符名称字符串为NoteName枚举值
        /// </summary>
        /// <param name="noteName">音符名称字符串，例如 "C", "C#", "Db" 等，默认为配置中的默认音符名称</param>
        /// <returns>对应的NoteName枚举值，如果无法解析则返回配置中的默认音符名称或NoteName.C</returns>
        /// <remarks>
        /// 支持的音符格式：
        /// - 自然音：C, D, E, F, G, A, B
        /// - 升音：C#, D#, F#, G#, A#
        /// - 降音：Db, Eb, Gb, Ab, Bb
        /// </remarks>
        private NoteName ParseNoteName(string noteName)
        {
            if (string.IsNullOrWhiteSpace(noteName))
            {
                // 使用配置中的默认音符名称
                return Enum.TryParse(_config.DefaultNoteName, out NoteName defaultNote) ? defaultNote : NoteName.C;
            }
            // 简单的音符名称解析
            noteName = noteName.Trim().ToUpper();
            
            switch (noteName)
            {
                case "C": return NoteName.C;
                case "C#": case "DB": return NoteName.CSharp;
                case "D": return NoteName.D;
                case "D#": case "EB": return NoteName.DSharp;
                case "E": return NoteName.E;
                case "F": return NoteName.F;
                case "F#": case "GB": return NoteName.FSharp;
                case "G": return NoteName.G;
                case "G#": case "AB": return NoteName.GSharp;
                case "A": return NoteName.A;
                case "A#": case "BB": return NoteName.ASharp;
                case "B": return NoteName.B;
                default:
                // 使用配置中的默认音符名称
                return Enum.TryParse(_config.DefaultNoteName, out NoteName defaultNote) ? defaultNote : NoteName.C;
            }
        }

        /// <summary>
        /// 根据音符名称和八度计算MIDI音符编号
        /// </summary>
        /// <param name="note">音符名称枚举值</param>
        /// <param name="octave">八度值，标准MIDI范围为-1到9</param>
        /// <returns>MIDI音符编号（0-127），如果计算结果超出范围则返回配置中的默认音符编号</returns>
        /// <remarks>
        /// MIDI音符编号计算规则：
        /// - 中央C（C4）对应编号60
        /// - 每个八度增加12个半音
        /// - 结果会被限制在配置的MIDI范围内（MinNoteNumber-MaxNoteNumber）
        /// </remarks>
        private SevenBitNumber GetNoteNumber(NoteName note, int octave)
        {
            try
            {
                // 确保八度在有效范围内 (-1-9)
                octave = Math.Clamp(octave, -1, 9);
                // 直接使用硬编码的音符值映射，避免类型转换问题
                int baseNoteNumber = note switch
                {
                    NoteName.C => 0,
                    NoteName.CSharp => 1,
                    NoteName.D => 2,
                    NoteName.DSharp => 3,
                    NoteName.E => 4,
                    NoteName.F => 5,
                    NoteName.FSharp => 6,
                    NoteName.G => 7,
                    NoteName.GSharp => 8,
                    NoteName.A => 9,
                    NoteName.ASharp => 10,
                    NoteName.B => 11,
                    _ => 0
                };
                int midiNumber = baseNoteNumber + (octave + 1) * 12;
                // 确保结果在配置的MIDI范围内
                var result = Math.Max(_config.MinNoteNumber, Math.Min(midiNumber, _config.MaxNoteNumber));
                return (SevenBitNumber)result;
            }
            catch
            {
                // 使用配置中的默认音符编号
                return (SevenBitNumber)_config.DefaultNoteNumber;
            }
        }
        
        /// <summary>
        /// 获取音符在八度内的半音偏移量
        /// </summary>
        /// <param name="note">音符名称枚举值</param>
        /// <returns>从C开始的半音偏移量，范围0-11</returns>
        /// <remarks>
        /// 偏移量计算规则：
        /// C: 0, C#/Db: 1, D: 2, D#/Eb: 3, E: 4, F: 5,
        /// F#/Gb: 6, G: 7, G#/Ab: 8, A: 9, A#/Bb: 10, B: 11
        /// </remarks>
        private static int GetNoteOffset(NoteName note)
        {
            switch (note)
            {
                case NoteName.C: return 0;
                case NoteName.CSharp: return 1;
                case NoteName.D: return 2;
                case NoteName.DSharp: return 3;
                case NoteName.E: return 4;
                case NoteName.F: return 5;
                case NoteName.FSharp: return 6;
                case NoteName.G: return 7;
                case NoteName.GSharp: return 8;
                case NoteName.A: return 9;
                case NoteName.ASharp: return 10;
                case NoteName.B: return 11;
                default: return 0; // 默认返回C
            }
        }
    }
}