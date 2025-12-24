using AIMusicCreator.Entity;
using Melanchall.DryWetMidi.MusicTheory;
using Chord = AIMusicCreator.Entity.Chord;
using ChordProgression = AIMusicCreator.Entity.ChordProgression;
using System;
using System.Collections.Generic;
using AIMusicCreator.Utils;
using AIMusicCreator.ApiService.Interfaces;
using Microsoft.Extensions.Logging;

namespace AIMusicCreator.ApiService.Services.DryWetMidiGerenteMidi
{
    /// <summary>
    /// 伴奏生成器实现类
    /// 根据和弦进行生成多样化的伴奏音符序列，支持多种音乐风格
    /// </summary>
    /// <remarks>
    /// 能够根据不同的音乐风格、情感和和弦进行生成相应的伴奏模式
    /// 使用DryWetMidi库处理MIDI音符事件
    /// </remarks>
    public partial class AccompanimentGenerator : IAccompanimentGenerator
    {
        /// <summary>
        /// 随机数生成器，用于创建变化的伴奏模式
        /// </summary>
        private static readonly Random _random = new();
        
        /// <summary>
        /// 日志记录器
        /// </summary>
        private readonly ILogger<AccompanimentGenerator> _logger;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器实例</param>
        public AccompanimentGenerator(ILogger<AccompanimentGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("AccompanimentGenerator initialized");
        }

        /// <summary>
        /// 生成伴奏音符序列
        /// </summary>
        /// <param name="progression">和弦进行，包含一系列和弦及其持续时间</param>
        /// <param name="parameters">旋律参数，包含风格、情感、速度等设置</param>
        /// <returns>生成的伴奏音符列表</returns>
        /// <exception cref="ArgumentNullException">当参数为空时抛出</exception>
        /// <exception cref="ArgumentException">当参数无效时抛出</exception>
        public List<NoteEvent> GenerateAccompaniment(ChordProgression progression,
                                                   MelodyParameters parameters)
        {
            if (progression == null)
            {
                _logger.LogError("ChordProgression is null");
                throw new ArgumentNullException(nameof(progression), "和弦进行不能为空");
            }
            
            if (parameters == null)
            {
                _logger.LogError("MelodyParameters is null");
                throw new ArgumentNullException(nameof(parameters), "旋律参数不能为空");
            }
            
            if (progression.Chords == null || !progression.Chords.Any())
            {
                _logger.LogError("ChordProgression contains no chords");
                throw new ArgumentException("和弦进行必须包含至少一个和弦", nameof(progression));
            }
            
            if (progression.Durations == null || !progression.Durations.Any())
            {
                _logger.LogError("ChordProgression contains no durations");
                throw new ArgumentException("和弦进行必须包含至少一个持续时间", nameof(progression));
            }
            
            _logger.LogInformation("开始生成伴奏，风格: {Style}, 情感: {Emotion}, 和弦数量: {ChordCount}", 
                parameters.Style, parameters.Emotion, progression.Chords.Count);
            
            var accompaniment = new List<NoteEvent>();
            long currentTime = 0;  // 当前时间位置
            int chordIndex = 0;

            // 遍历所有和弦
            foreach (var (chord, duration) in progression.Chords.Zip(progression.Durations, (c, d) => (c, d)))
            {
                if (chord == null)
                {
                    _logger.LogWarning("和弦 #{Index} 为空，跳过", chordIndex);
                    chordIndex++;
                    continue;
                }
                
                _logger.LogDebug("处理和弦 #{Index}: {Root}, 持续时间: {Duration}", 
                    chordIndex, chord.Root, duration);
                
                try
                {
                    // 为每个和弦生成伴奏模式
                    var chordNotes = GenerateChordPattern(chord, parameters, duration, currentTime);
                    accompaniment.AddRange(chordNotes);
                    
                    // 为当前和弦添加风格特定元素
                    AddStyleElements(accompaniment, chord, parameters, currentTime, duration);
                    
                    _logger.LogDebug("和弦 #{Index} 处理完成，生成音符数: {NoteCount}", 
                        chordIndex, chordNotes.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "处理和弦 #{Index} 时发生错误", chordIndex);
                    // 继续处理下一个和弦，不中断整个生成过程
                }
                
                // 更新时间位置
                currentTime += duration * 480; // 将四分音符转换为ticks（假设分辨率480）
                chordIndex++;
            }
            
            _logger.LogInformation("伴奏生成完成，共生成音符数: {TotalNotes}, 总时长: {TotalDuration} ticks", 
                accompaniment.Count, currentTime);

            return accompaniment;
        }

        /// <summary>
        /// 为单个和弦生成伴奏模式
        /// </summary>
        /// <param name="chord">当前处理的和弦</param>
        /// <param name="parameters">旋律参数，包含风格、情感等设置</param>
        /// <param name="duration">和弦持续的拍数</param>
        /// <param name="startTime">开始时间（以tick为单位）</param>
        /// <returns>为该和弦生成的伴奏音符列表</returns>
        /// <exception cref="ArgumentNullException">当参数为空时抛出</exception>
        /// <exception cref="ArgumentException">当参数无效时抛出</exception>
        public List<NoteEvent> GenerateChordPattern(Chord chord, MelodyParameters parameters,
                                           int duration, long startTime)
        {
            if (chord == null)
            {
                _logger.LogError("Chord is null");
                throw new ArgumentNullException(nameof(chord), "和弦不能为空");
            }
            
            if (parameters == null)
            {
                _logger.LogError("MelodyParameters is null");
                throw new ArgumentNullException(nameof(parameters), "旋律参数不能为空");
            }
            
            if (duration <= 0)
            {
                _logger.LogError("Invalid duration: {Duration}", duration);
                throw new ArgumentException("持续时间必须大于0", nameof(duration));
            }
            
            if (startTime < 0)
            {
                _logger.LogError("Invalid startTime: {StartTime}", startTime);
                throw new ArgumentException("开始时间不能为负数", nameof(startTime));
            }
            
            _logger.LogDebug("生成和弦模式，风格: {Style}, 持续时间: {Duration}, 开始时间: {StartTime}", 
                parameters.Style, duration, startTime);
            
            try
            {
                // 根据风格获取伴奏模式类型
                var pattern = GetAccompanimentPattern(parameters.Style);
                _logger.LogTrace("选择伴奏模式: {Pattern}", pattern);
                
                List<NoteEvent> notes = pattern switch
                {
                    "arpeggio" => GenerateArpeggio(chord, parameters, duration, startTime),// 琶音模式
                    "block" => GenerateBlockChords(chord, parameters, duration, startTime),// 块和弦模式
                    "rhythmic" => GenerateRhythmicPattern(chord, parameters, duration, startTime),// 节奏模式
                    _ => GenerateBlockChords(chord, parameters, duration, startTime),// 默认块和弦
                };
                
                _logger.LogDebug("和弦模式生成完成，生成音符数: {NoteCount}", notes.Count);
                return notes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成和弦模式失败");
                throw;
            }
        }

        /// <summary>
        /// 生成琶音伴奏模式（依次演奏和弦音符）
        /// </summary>
        /// <param name="chord">当前处理的和弦</param>
        /// <param name="parameters">旋律参数，包含风格、情感等设置</param>
        /// <param name="duration">和弦持续的拍数</param>
        /// <param name="startTime">开始时间（以tick为单位）</param>
        /// <returns>生成的琶音音符列表</returns>
        /// <exception cref="ArgumentNullException">当参数为空时抛出</exception>
        private List<NoteEvent> GenerateArpeggio(Chord chord, MelodyParameters parameters,
                                               int duration, long startTime)
        {
            if (chord == null)
            {
                _logger.LogError("Chord is null in GenerateArpeggio");
                throw new ArgumentNullException(nameof(chord), "和弦不能为空");
            }
            
            if (parameters == null)
            {
                _logger.LogError("MelodyParameters is null in GenerateArpeggio");
                throw new ArgumentNullException(nameof(parameters), "旋律参数不能为空");
            }
            
            _logger.LogDebug("开始生成琶音模式，和弦根音: {ChordRoot}, 持续时间: {Duration}", 
                chord.Root, duration);
            
            try
            {
                var notes = new List<NoteEvent>();
                var chordNotes = chord.GetNotes().ToList();
                
                if (chordNotes.Count == 0)
                {
                    _logger.LogWarning("和弦没有音符，返回空列表");
                    return notes;
                }
                
                long noteDuration = 120; // 8分音符的ticks数

                // 在持续时间内循环生成琶音
                for (long time = 0; time < duration * 480; time += noteDuration)
                {
                    var noteIndex = (int)(time / noteDuration) % chordNotes.Count;
                    var noteName = chordNotes[noteIndex];

                    notes.Add(new NoteEvent
                    {
                        Note = noteName,
                        Octave = parameters.Octave - 1, // 低八度，避免与旋律冲突
                        StartTime = startTime + time,
                        Duration = noteDuration,
                        Velocity = 70  // 中等力度
                    });
                }
                
                _logger.LogDebug("琶音模式生成完成，生成音符数: {NoteCount}", notes.Count);
                return notes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成琶音模式失败");
                throw;
            }
        }

        /// <summary>
        /// 生成块和弦伴奏模式（同时演奏所有和弦音符）
        /// </summary>
        /// <param name="chord">当前处理的和弦</param>
        /// <param name="parameters">旋律参数，包含风格、情感等设置</param>
        /// <param name="duration">和弦持续的拍数</param>
        /// <param name="startTime">开始时间（以tick为单位）</param>
        /// <returns>生成的块和弦音符列表</returns>
        /// <exception cref="ArgumentNullException">当参数为空时抛出</exception>
        private List<NoteEvent> GenerateBlockChords(Chord chord, MelodyParameters parameters,
                                                  int duration, long startTime)
        {
            if (chord == null)
            {
                _logger.LogError("Chord is null in GenerateBlockChords");
                throw new ArgumentNullException(nameof(chord), "和弦不能为空");
            }
            
            if (parameters == null)
            {
                _logger.LogError("MelodyParameters is null in GenerateBlockChords");
                throw new ArgumentNullException(nameof(parameters), "旋律参数不能为空");
            }
            
            _logger.LogDebug("开始生成块和弦模式，和弦根音: {ChordRoot}, 持续时间: {Duration}", 
                chord.Root, duration);
            
            try
            {
                var notes = new List<NoteEvent>();
                var chordNotes = chord.GetNotes();

                if (!chordNotes.Any())
                {
                    _logger.LogWarning("和弦没有音符，返回空列表");
                    return notes;
                }
                
                // 为和弦中的每个音符创建同时开始的事件
                foreach (var noteName in chordNotes)
                {
                    notes.Add(new NoteEvent
                    {
                        Note = noteName,
                        Octave = parameters.Octave - 1, // 低八度
                        StartTime = startTime,
                        Duration = duration * 480,      // 整个持续时间
                        Velocity = 60                   // 较轻的力度
                    });
                }
                
                _logger.LogDebug("块和弦模式生成完成，生成音符数: {NoteCount}", notes.Count);
                return notes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成块和弦模式失败");
                throw;
            }
        }

        /// <summary>
        /// 生成节奏模式伴奏
        /// </summary>
        /// <param name="chord">当前处理的和弦</param>
        /// <param name="parameters">旋律参数，包含风格、情感等设置</param>
        /// <param name="duration">和弦持续的拍数</param>
        /// <param name="startTime">开始时间（以tick为单位）</param>
        /// <returns>生成的节奏模式音符列表</returns>
        /// <exception cref="ArgumentNullException">当参数为空时抛出</exception>
        private List<NoteEvent> GenerateRhythmicPattern(Chord chord, MelodyParameters parameters,
                                      int duration, long startTime)
        {
            if (chord == null)
            {
                _logger.LogError("Chord is null in GenerateRhythmicPattern");
                throw new ArgumentNullException(nameof(chord), "和弦不能为空");
            }
            
            if (parameters == null)
            {
                _logger.LogError("MelodyParameters is null in GenerateRhythmicPattern");
                throw new ArgumentNullException(nameof(parameters), "旋律参数不能为空");
            }
            
            _logger.LogDebug("开始生成节奏模式，和弦根音: {ChordRoot}, 风格: {Style}, 情感: {Emotion}", 
                chord.Root, parameters.Style, parameters.Emotion);
            
            try
            {
                var notes = new List<NoteEvent>();
                var chordNotes = chord.GetNotes();
                
                if (!chordNotes.Any())
                {
                    _logger.LogWarning("和弦没有音符，返回空列表");
                    return notes;
                }
                
                var rhythmPattern = GetRhythmPattern(parameters.Style, parameters.Emotion);
                var bassPattern = GetBassPattern(parameters.Style);

                long currentTime = startTime;
                int patternIndex = 0;
                int bassPatternIndex = 0;

            // 生成节奏伴奏
            while (currentTime < startTime + duration * 480)
            {
                var (noteDuration, velocity, playChord, playBass) = rhythmPattern[patternIndex % rhythmPattern.Count];

                if (playBass)
                {
                    // 使用bassPattern中的音符而不是固定的和弦根音
                    var (bassDuration, bassNote) = bassPattern[bassPatternIndex % bassPattern.Count];

                    notes.Add(new NoteEvent
                    {
                        Note = bassNote,
                        Octave = parameters.Octave - 2, // 低两个八度
                        StartTime = currentTime,
                        Duration = Math.Min(noteDuration * 2, bassDuration), // 使用bassPattern中的时长
                        Velocity = (int)(velocity * 0.8) // 贝斯力度稍低
                    });

                    bassPatternIndex++;
                }

                if (playChord)
                {
                    // 添加和弦音符
                    foreach (var noteName in chordNotes)
                    {
                        notes.Add(new NoteEvent
                        {
                            Note = noteName,
                            Octave = parameters.Octave - 1,
                            StartTime = currentTime,
                            Duration = noteDuration,
                            Velocity = velocity
                        });
                    }
                }
                else if (_random.NextDouble() < 0.3) // 30%概率添加单音节奏
                {
                    // 添加单个节奏音符（使用bassPattern中的音符或和弦音）
                    NoteName rhythmNote;
                    if (bassPattern.Count > 0 && _random.NextDouble() < 0.5)
                    {
                        rhythmNote = bassPattern[bassPatternIndex % bassPattern.Count].note;
                    }
                    else
                    {
                        rhythmNote = patternIndex % 2 == 0 ? chord.Root : chord.Fifth;
                    }

                    notes.Add(new NoteEvent
                    {
                        Note = rhythmNote,
                        Octave = parameters.Octave - 1,
                        StartTime = currentTime,
                        Duration = noteDuration,
                        Velocity = (int)(velocity * 0.6)
                    });
                }

                    currentTime += noteDuration;
                    patternIndex++;

                    // 如果超过持续时间，退出循环
                    if (currentTime >= startTime + duration * 480)
                        break;
                }

                // 根据风格添加额外的节奏元素
                AddStyleElements(notes, chord, parameters, startTime, duration);
                
                _logger.LogDebug("节奏模式生成完成，生成音符数: {NoteCount}", notes.Count);
                return notes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成节奏模式失败");
                throw;
            }
        }
        /// <summary>
        /// 添加布鲁斯音乐元素
        /// </summary>
        /// <param name="notes">要添加元素的音符列表</param>
        /// <param name="chord">当前处理的和弦</param>
        /// <param name="parameters">旋律参数，包含风格、情感等设置</param>
        /// <param name="startTime">开始时间（以tick为单位）</param>
        /// <param name="duration">和弦持续的拍数</param>
        private void AddBluesElements(List<NoteEvent> notes, Chord chord,
                                           MelodyParameters parameters, long startTime, int duration)
        {
            if (notes == null)
            {
                _logger.LogError("Notes list is null in AddBluesElements");
                throw new ArgumentNullException(nameof(notes), "音符列表不能为空");
            }
            
            if (chord == null)
            {
                _logger.LogError("Chord is null in AddBluesElements");
                throw new ArgumentNullException(nameof(chord), "和弦不能为空");
            }
            
            if (parameters == null)
            {
                _logger.LogError("MelodyParameters is null in AddBluesElements");
                throw new ArgumentNullException(nameof(parameters), "旋律参数不能为空");
            }
            
            _logger.LogDebug("开始添加蓝调元素，和弦根音: {ChordRoot}, 风格: {Style}", 
                chord.Root, parameters.Style);
            
            try
            {
                // 添加蓝调音符（降三、降七音）
                var blueNotes = GetBlueNotes(chord.Root);

                foreach (var blueNote in blueNotes)
                {
                    if (_random.NextDouble() < 0.5) // 50%概率添加蓝调音符
                    {
                        notes.Add(new NoteEvent
                        {
                            Note = blueNote,
                            Octave = parameters.Octave - 1,
                            StartTime = startTime + 180, // 在弱拍加入
                            Duration = 90,
                            Velocity = 65
                        });
                    }
                }

                // 添加布鲁斯特色的贝斯walking
                AddBluesWalkingBass(notes, chord, parameters, startTime, duration);
                
                _logger.LogDebug("蓝调元素添加完成，添加了{Count}个蓝调音符", blueNotes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加蓝调元素时发生错误，和弦根音: {ChordRoot}", chord.Root);
                throw;
            }
        }

        /// <summary>
        /// 添加布鲁斯walking bass
        /// </summary>
        /// <param name="notes">要添加元素的音符列表</param>
        /// <param name="chord">当前处理的和弦</param>
        /// <param name="parameters">旋律参数，包含风格、情感等设置</param>
        /// <param name="startTime">开始时间（以tick为单位）</param>
        /// <param name="duration">和弦持续的拍数</param>
        private void AddBluesWalkingBass(List<NoteEvent> notes, Chord chord,
                                              MelodyParameters parameters, long startTime, int duration)
        {            
            if (notes == null)
            {
                _logger.LogError("Notes list is null in AddBluesWalkingBass");
                throw new ArgumentNullException(nameof(notes), "音符列表不能为空");
            }
            
            if (chord == null)
            {
                _logger.LogError("Chord is null in AddBluesWalkingBass");
                throw new ArgumentNullException(nameof(chord), "和弦不能为空");
            }
            
            if (parameters == null)
            {
                _logger.LogError("MelodyParameters is null in AddBluesWalkingBass");
                throw new ArgumentNullException(nameof(parameters), "旋律参数不能为空");
            }
            
            _logger.LogDebug("开始添加蓝调walking bass，和弦根音: {ChordRoot}", chord.Root);
            
            try
            {
                // 布鲁斯特色的walking bass模式
                List<NoteName> walkingPattern =
                [
                    chord.Root,
                    (NoteName)(((int)chord.Root + 4) % 12),  // 大三度
                    chord.Fifth,
                    (NoteName)(((int)chord.Root + 7) % 12)   // 小七度（蓝调特色）
                ];

                for (long time = startTime; time < startTime + duration * 480; time += 120)
                {
                    var step = (int)((time - startTime) / 120) % walkingPattern.Count;
                    var bassNote = walkingPattern[step];

                    notes.Add(new NoteEvent
                    {
                        Note = bassNote,
                        Octave = parameters.Octave - 2,
                        StartTime = time,
                        Duration = 120,
                        Velocity = 70
                    });
                }
                
                _logger.LogDebug("蓝调walking bass添加完成，生成了{Count}个音符", 
                    duration * 480 / 120);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加蓝调walking bass时发生错误，和弦根音: {ChordRoot}", chord.Root);
                throw;
            }
        }

        /// <summary>
        /// 添加爵士音乐元素
        /// </summary>
        /// <param name="notes">要添加元素的音符列表</param>
        /// <param name="chord">当前处理的和弦</param>
        /// <param name="parameters">旋律参数，包含风格、情感等设置</param>
        /// <param name="startTime">开始时间（以tick为单位）</param>
        /// <param name="duration">和弦持续的拍数</param>
        private void AddJazzElements(List<NoteEvent> notes, Chord chord,
                                          MelodyParameters parameters, long startTime, int duration)
        {
            if (notes == null)
            {
                _logger.LogError("Notes list is null in AddJazzElements");
                throw new ArgumentNullException(nameof(notes), "音符列表不能为空");
            }
            
            if (chord == null)
            {
                _logger.LogError("Chord is null in AddJazzElements");
                throw new ArgumentNullException(nameof(chord), "和弦不能为空");
            }
            
            if (parameters == null)
            {
                _logger.LogError("MelodyParameters is null in AddJazzElements");
                throw new ArgumentNullException(nameof(parameters), "旋律参数不能为空");
            }
            
            _logger.LogDebug("开始添加爵士元素，和弦根音: {ChordRoot}, 音阶: {Scale}", 
                chord.Root, parameters.Scale);
            
            try
            {
                // 添加爵士七和弦的第七音
                var seventhNote = GetSeventhNote(chord.Root, parameters.Scale);
                if (seventhNote != null)
                {
                    notes.Add(new NoteEvent
                    {
                        Note = seventhNote.Value,
                        Octave = parameters.Octave - 1,
                        StartTime = startTime + 240, // 在第二拍加入
                        Duration = 240,
                        Velocity = 70
                    });
                }

                // 添加walking bass模式
                AddJazzWalkingBass(notes, chord, parameters, startTime, duration);

                // 添加爵士和弦扩展音
                AddJazzChordExtensions(notes, chord, parameters, startTime, duration);
                
                _logger.LogDebug("爵士元素添加完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加爵士元素时发生错误，和弦根音: {ChordRoot}", chord.Root);
                throw;
            }
        }

        /// <summary>
        /// 添加爵士walking bass
        /// </summary>
        /// <param name="notes">要添加元素的音符列表</param>
        /// <param name="chord">当前处理的和弦</param>
        /// <param name="parameters">旋律参数，包含风格、情感等设置</param>
        /// <param name="startTime">开始时间（以tick为单位）</param>
        /// <param name="duration">和弦持续的拍数</param>
        private void AddJazzWalkingBass(List<NoteEvent> notes, Chord chord,
                                             MelodyParameters parameters, long startTime, int duration)
        {
            if (notes == null)
            {
                _logger.LogError("Notes list is null in AddJazzWalkingBass");
                throw new ArgumentNullException(nameof(notes), "音符列表不能为空");
            }
            
            if (chord == null)
            {
                _logger.LogError("Chord is null in AddJazzWalkingBass");
                throw new ArgumentNullException(nameof(chord), "和弦不能为空");
            }
            
            if (parameters == null)
            {
                _logger.LogError("MelodyParameters is null in AddJazzWalkingBass");
                throw new ArgumentNullException(nameof(parameters), "旋律参数不能为空");
            }
            
            _logger.LogDebug("开始添加爵士walking bass，和弦根音: {ChordRoot}", chord.Root);
            
            try
            {
                // 复杂的爵士walking bass模式
                List<NoteName> walkingPattern =
                [
                    chord.Root,
                    (NoteName)(((int)chord.Root + 2) % 12),  // 大二度（经过音）
                    chord.Third,
                    (NoteName)(((int)chord.Root + 5) % 12),  // 纯四度（经过音）
                    chord.Fifth,
                    (NoteName)(((int)chord.Root + 7) % 12),  // 大六度
                    (NoteName)(((int)chord.Root + 10) % 12), // 大七度
                    (NoteName)(((int)chord.Root + 9) % 12)   // 小七度
                ];

                for (long time = startTime; time < startTime + duration * 480; time += 120)
                {
                    var step = (int)((time - startTime) / 120) % walkingPattern.Count;
                    var bassNote = walkingPattern[step];

                    notes.Add(new NoteEvent
                    {
                        Note = bassNote,
                        Octave = parameters.Octave - 2,
                        StartTime = time,
                        Duration = 120,
                        Velocity = 75
                    });
                }
                
                _logger.LogDebug("爵士walking bass添加完成，生成了{Count}个音符", 
                    duration * 480 / 120);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加爵士walking bass时发生错误，和弦根音: {ChordRoot}", chord.Root);
                throw;
            }
        }

        /// <summary>
        /// 添加爵士和弦扩展音
        /// </summary>
        /// <param name="notes">要添加元素的音符列表</param>
        /// <param name="chord">当前处理的和弦</param>
        /// <param name="parameters">旋律参数，包含风格、情感等设置</param>
        /// <param name="startTime">开始时间（以tick为单位）</param>
        /// <param name="duration">和弦持续的拍数</param>
        private void AddJazzChordExtensions(List<NoteEvent> notes, Chord chord,
                                         MelodyParameters parameters, long startTime, int duration)
        {
            if (notes == null)
            {
                _logger.LogError("Notes list is null in AddJazzChordExtensions");
                throw new ArgumentNullException(nameof(notes), "音符列表不能为空");
            }
            
            if (chord == null)
            {
                _logger.LogError("Chord is null in AddJazzChordExtensions");
                throw new ArgumentNullException(nameof(chord), "和弦不能为空");
            }
            
            if (parameters == null)
            {
                _logger.LogError("MelodyParameters is null in AddJazzChordExtensions");
                throw new ArgumentNullException(nameof(parameters), "旋律参数不能为空");
            }
            
            _logger.LogDebug("开始添加爵士和弦扩展音，和弦根音: {ChordRoot}", chord.Root);
            
            try
            {
                // 添加第九音、第十一音等扩展音
                List<NoteName> extensions =
                [
                    (NoteName)(((int)chord.Root + 2) % 12),  // 第九音
                    (NoteName)(((int)chord.Root + 5) % 12),  // 第十一音
                    (NoteName)(((int)chord.Root + 9) % 12)   // 第十三音
                ];

            // 在整个持续时间内分散添加扩展音
            long totalDuration = duration * 480;
            int extensionsAdded = 0;

            foreach (var extension in extensions)
            {
                if (_random.NextDouble() < 0.4) // 40%概率添加扩展音
                {
                    // 根据duration计算合适的时间位置，避免所有扩展音都在同一时间
                    long extensionTime = startTime + (extensionsAdded * totalDuration / (extensions.Count + 1)) + 120;

                    // 确保时间不会超出范围
                    extensionTime = Math.Min(extensionTime, startTime + totalDuration - 180);

                    notes.Add(new NoteEvent
                    {
                        Note = extension,
                        Octave = parameters.Octave,
                        StartTime = extensionTime,
                        Duration = 180,
                        Velocity = 60
                    });

                    extensionsAdded++;
                }
            }

            // 如果duration较长，可以在后半段再添加一些扩展音变化
            if (duration >= 2 && _random.NextDouble() < 0.5)
            {
                long lateExtensionTime = startTime + (totalDuration * 3 / 4);
                var lateExtension = extensions[_random.Next(extensions.Count)];

                notes.Add(new NoteEvent
                {
                    Note = lateExtension,
                    Octave = parameters.Octave,
                    StartTime = lateExtensionTime,
                    Duration = 240, // 后半段的扩展音可以持续更久
                    Velocity = 55
                });
            }
            
            _logger.LogDebug("爵士和弦扩展音添加完成，添加了{Count}个扩展音", extensionsAdded);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加爵士和弦扩展音时发生错误，和弦根音: {ChordRoot}", chord.Root);
            throw;
        }
        
        // 以下是旧实现，已替换为新实现
        }
        //private static void AddJazzChordExtensions(List<NoteEvent> notes, Chord chord,
        //                                         MelodyParameters parameters, long startTime, int duration)
        //{
        //    // 添加第九音、第十一音等扩展音
        //    List<NoteName> extensions =
        //    [
        //        (NoteName)(((int)chord.Root + 2) % 12),  // 第九音
        //        (NoteName)(((int)chord.Root + 5) % 12),  // 第十一音
        //        (NoteName)(((int)chord.Root + 9) % 12)   // 第十三音
        //    ];

        //    foreach (var extension in extensions)
        //    {
        //        if (_random.NextDouble() < 0.4) // 40%概率添加扩展音
        //        {
        //            notes.Add(new NoteEvent
        //            {
        //                Note = extension,
        //                Octave = parameters.Octave,
        //                StartTime = startTime + 180,
        //                Duration = 180,
        //                Velocity = 60
        //            });
        //        }
        //    }
        //}

        /// <summary>
        /// 添加风格特定的节奏元素 - 实现IStyleElementGenerator接口
        /// </summary>
        public void AddStyleElements(List<NoteEvent> notes, Chord chord,
                                     MelodyParameters parameters, long startTime, int duration)
        {
            if (notes == null || chord == null || parameters == null)
            {
                // 参数验证已在调用方进行，这里只做基本检查
                return;
            }
            
            switch (parameters.Style)
            {
                case MusicStyle.Rock:
                    // 摇滚：使用bassPattern增强节奏
                    AddRockPercussion(notes, chord, parameters, startTime, duration);
                    break;

                case MusicStyle.Electronic:
                    // 电子：使用bassPattern创建复杂的电子节奏
                    AddElectronicElements(notes, chord, parameters, startTime, duration);
                    break;

                case MusicStyle.Jazz:
                    // 爵士：使用walking bass模式
                    AddJazzElements(notes, chord, parameters, startTime, duration);
                    break;

                case MusicStyle.Blues:
                    // 布鲁斯：结合bassPattern和蓝调元素
                    AddBluesElements(notes, chord, parameters, startTime, duration);
                    break;

                case MusicStyle.Classical:
                    // 古典：使用bassPattern创建持续低音
                    AddClassicalElements(notes, chord, parameters, startTime, duration);
                    break;

                case MusicStyle.Pop:
                    // 流行：添加简单的节奏填充
                    AddPopElements(notes, chord, parameters, startTime, duration);
                    break;
            }
        }

        /// <summary>
        /// 添加流行音乐元素
        /// </summary>
        private static void AddPopElements(List<NoteEvent> notes, Chord chord,
                                         MelodyParameters parameters, long startTime, int duration)
        {
            // 流行音乐常见的简单节奏填充
            for (long time = startTime + 480; time < startTime + duration * 480; time += 480)
            {
                if (_random.NextDouble() < 0.6) // 60%概率在每小节添加填充
                {
                    notes.Add(new NoteEvent
                    {
                        Note = chord.Root,
                        Octave = parameters.Octave,
                        StartTime = time - 60,
                        Duration = 120,
                        Velocity = 70
                    });
                }
            }
        }

        /// <summary>
        /// 添加摇滚打击乐元素 - 修正版本
        /// </summary>
        private static void AddRockPercussion(List<NoteEvent> notes, Chord chord,
                                            MelodyParameters parameters, long startTime, int duration)
        {
            // 模拟鼓点：底鼓（强拍）和军鼓（弱拍）
            for (long time = startTime; time < startTime + duration * 480; time += 120)
            {
                bool isStrongBeat = (time - startTime) % 480 == 0;

                notes.Add(new NoteEvent
                {
                    Note = isStrongBeat ? chord.Root : chord.Fifth,
                    Octave = parameters.Octave - 3, // 非常低的八度模拟鼓声
                    StartTime = time,
                    Duration = 60, // 很短的持续时间
                    Velocity = isStrongBeat ? 100 : 80
                });
            }
        }

        /// <summary>
        /// 添加电子音乐元素 - 修正版本
        /// </summary>
        private static void AddElectronicElements(List<NoteEvent> notes, Chord chord,
                                                MelodyParameters parameters, long startTime, int duration)
        {
            // 添加高频琶音
            for (long time = startTime + 60; time < startTime + duration * 480; time += 60)
            {
                if (_random.NextDouble() < 0.4) // 40%概率添加高频音符
                {
                    var arpeggioNotes = chord.GetNotes();
                    var noteIndex = (int)((time - startTime) / 60) % arpeggioNotes.Count;

                    notes.Add(new NoteEvent
                    {
                        Note = arpeggioNotes[noteIndex],
                        Octave = parameters.Octave + 1, // 高八度
                        StartTime = time,
                        Duration = 30,
                        Velocity = 60
                    });
                }
            }

            // 添加电子音效（高音）
            for (long time = startTime + 240; time < startTime + duration * 480; time += 480)
            {
                notes.Add(new NoteEvent
                {
                    Note = chord.Root,
                    Octave = parameters.Octave + 2, // 很高八度
                    StartTime = time,
                    Duration = 60,
                    Velocity = 50
                });
            }
        }

        /// <summary>
        /// 添加古典音乐元素 - 修正版本
        /// </summary>
        private static void AddClassicalElements(List<NoteEvent> notes, Chord chord,
                                               MelodyParameters parameters, long startTime, int duration)
        {
            // 古典音乐使用长持续的低音
            notes.Add(new NoteEvent
            {
                Note = chord.Root,
                Octave = parameters.Octave - 2,
                StartTime = startTime,
                Duration = duration * 480, // 整个持续时间的低音
                Velocity = 70
            });

            // 添加古典音乐的琶音装饰
            for (long time = startTime + 120; time < startTime + duration * 480; time += 240)
            {
                if (_random.NextDouble() < 0.3)
                {
                    var arpeggio = new List<NoteName> { chord.Root, chord.Third, chord.Fifth };
                    long arpeggioTime = time;
                    foreach (var note in arpeggio)
                    {
                        notes.Add(new NoteEvent
                        {
                            Note = note,
                            Octave = parameters.Octave,
                            StartTime = arpeggioTime,
                            Duration = 60,
                            Velocity = 65
                        });
                        arpeggioTime += 30;
                    }
                }
            }
        }
        /// <summary>
        /// 获取贝斯模式 - 增强版，返回实际的音符
        /// </summary>
        public static List<(long duration, NoteName note)> GetBassPattern(MusicStyle style)
        {
            return style switch
            {
                MusicStyle.Rock =>
                [
                    (480, NoteName.C), // 根音持续
                    (240, NoteName.C), // 根音
                    (240, NoteName.G)  // 五音
                ],

                MusicStyle.Pop =>
                [
                    (360, NoteName.C), // 根音
                    (360, NoteName.G), // 五音
                    (360, NoteName.C), // 根音
                    (360, NoteName.E)  // 三音
                ],

                MusicStyle.Blues =>
                [
                    (180, NoteName.C), // 根音
                    (180, NoteName.E), // 三音
                    (180, NoteName.G), // 五音
                    (180, NoteName.D), // 经过音
                    (180, NoteName.E), // 三音
                    (180, NoteName.G)  // 五音
                ],

                MusicStyle.Jazz =>
                [
                    (120, NoteName.C), // walking bass
                    (120, NoteName.D),
                    (120, NoteName.E),
                    (120, NoteName.F),
                    (120, NoteName.G),
                    (120, NoteName.A),
                    (120, NoteName.B),
                    (120, NoteName.C)
                ],

                MusicStyle.Electronic =>
                [
                    (240, NoteName.C), // 重复的电子贝斯
                    (240, NoteName.C),
                    (240, NoteName.G),
                    (240, NoteName.G)
                ],

                MusicStyle.Classical =>
                [
                    (960, NoteName.C) // 长持续音
                ],

                _ =>
                [
                    (480, NoteName.C) // 简单的根音
                ]
            };
        }


        /// <summary>
        /// 根据风格和情绪获取节奏模式
        /// </summary>
        public static List<(long duration, int velocity, bool playChord, bool playBass)> GetRhythmPattern(MusicStyle style, Emotion emotion)
        {
            return (style, emotion) switch
            {
                // 摇滚风格 - 强烈的节奏感
                (MusicStyle.Rock, Emotion.Energetic) =>
                [
                    (120, 90, true, true),   // 强拍：和弦+贝斯
                    (120, 70, false, false), // 空拍
                    (120, 80, true, false),  // 弱拍：只有和弦
                    (120, 60, false, false)  // 空拍
                ],

                (MusicStyle.Rock, _) =>
                [
                    (240, 85, true, true),   // 强拍
                    (240, 75, false, false)  // 空拍
                ],

                // 流行风格 - 规律的节奏
                (MusicStyle.Pop, Emotion.Happy) =>
                [
                    (180, 80, true, true),   // 强拍
                    (180, 70, false, false), // 空拍
                    (180, 75, true, false),  // 弱拍
                    (180, 65, false, false)  // 空拍
                ],

                (MusicStyle.Pop, Emotion.Sad) =>
                [
                    (240, 70, true, true),   // 较慢的节奏
                    (240, 60, false, false),
                    (240, 65, true, false),
                    (240, 55, false, false)
                ],

                // 电子风格 - 重复的节奏模式
                (MusicStyle.Electronic, _) =>
                [
                    (120, 95, true, true),   // 强烈的电子节奏
                    (120, 0, false, false),  // 空拍
                    (120, 85, false, true),  // 只有贝斯
                    (120, 0, false, false),  // 空拍
                    (120, 90, true, false),  // 只有和弦
                    (120, 0, false, false)   // 空拍
                ],

                // 布鲁斯风格 - 摇摆节奏
                (MusicStyle.Blues, _) =>
                [
                    (180, 75, true, true),   // 摇摆节奏
                    (180, 65, false, false),
                    (180, 70, true, false),
                    (180, 60, false, false),
                    (180, 68, false, true),  // 贝斯walking
                    (180, 58, false, false)
                ],

                // 爵士风格 - 复杂的节奏
                (MusicStyle.Jazz, _) =>
                [
                    (120, 70, true, true),   // 复杂的爵士节奏
                    (120, 60, false, false),
                    (120, 65, false, true),
                    (120, 55, true, false),
                    (120, 63, false, false),
                    (120, 58, true, true)
                ],

                // 默认模式
                _ =>
                [
                    (240, 80, true, true),
                    (240, 70, false, false)
                ]
            };
        }


        /// <summary>
        /// 获取指定根音和音阶的第七音
        /// </summary>
        /// <param name="root">根音</param>
        /// <param name="scale">音阶</param>
        /// <returns>第七音，如果无法计算则返回null</returns>
        private NoteName? GetSeventhNote(NoteName root, Scale? scale)
        {
            try
            {
                if (scale == null)
                {
                    _logger.LogWarning("获取第七音时音阶为null，根音: {Root}", root);
                    return null;
                }
                
                var scaleNotes = MidiUtils.GetScaleNoteNames(scale);
                if (scaleNotes == null || scaleNotes.Count == 0)
                {
                    _logger.LogWarning("获取第七音时音阶音符列表为空，根音: {Root}", root);
                    return null;
                }
                
                var rootIndex = scaleNotes.IndexOf(root);
                if (rootIndex == -1)
                {
                    _logger.LogWarning("在音阶中未找到根音: {Root}", root);
                    return null;
                }
                
                var seventhIndex = (rootIndex + 6) % scaleNotes.Count;
                var result = scaleNotes[seventhIndex];
                _logger.LogDebug("获取第七音成功，根音: {Root}, 第七音: {Seventh}", root, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取第七音时发生错误，根音: {Root}", root);
                throw; // 重新抛出异常以便上层处理
            }
        }

        /// <summary>
        /// 获取蓝调音符（降三音、降五音、降七音）
        /// </summary>
        /// <param name="root">根音</param>
        /// <returns>蓝调音符列表</returns>
        private List<NoteName> GetBlueNotes(NoteName root)
        {
            try
            {
                _logger.LogDebug("开始获取蓝调音符，根音: {Root}", root);
                
                // 蓝调音阶特有的降三、降五、降七音
                var blueNotes = new List<NoteName>
                {
                    (NoteName)(((int)root + 3) % 12),  // 降三音
                    (NoteName)(((int)root + 6) % 12),  // 降五音
                    (NoteName)(((int)root + 10) % 12)  // 降七音
                };
                
                _logger.LogDebug("蓝调音符获取成功，根音: {Root}, 音符列表: [{BlueNotes}]", 
                    root, string.Join(", ", blueNotes));
                
                return blueNotes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取蓝调音符时发生错误，根音: {Root}", root);
                // 返回默认值，确保即使出错也能继续执行
                return new List<NoteName>();
            }
        }

        /// <summary>
        /// 根据音乐风格获取伴奏模式
        /// 实现IAccompanimentGenerator接口
        /// </summary>
        /// <param name="style">音乐风格</param>
        /// <returns>伴奏模式名称</returns>
        /// <exception cref="ArgumentException">当音乐风格无效时抛出</exception>
        public string GetAccompanimentPattern(MusicStyle style)
        {
            _logger.LogDebug("开始获取伴奏模式，风格: {Style}", style);
            
            // 验证输入参数
            if (!Enum.IsDefined(typeof(MusicStyle), style))
            {
                _logger.LogError("无效的音乐风格: {Style}", style);
                throw new ArgumentException($"无效的音乐风格: {style}", nameof(style));
            }
            
            // 使用字典映射代替switch语句，提高可维护性和扩展性
            var stylePatternMap = new Dictionary<MusicStyle, string>
            {
                { MusicStyle.Pop, "block" },           // 流行音乐常用块和弦
                { MusicStyle.Rock, "rhythmic" },       // 摇滚音乐强调节奏
                { MusicStyle.Jazz, "arpeggio" },       // 爵士音乐常用琶音
                { MusicStyle.Classical, "arpeggio" },  // 古典音乐常用琶音
                { MusicStyle.Electronic, "rhythmic" }, // 电子音乐强调节奏
                { MusicStyle.Blues, "rhythmic" }       // 布鲁斯音乐强调节奏
            };
            
            // 获取对应模式，如果未找到则使用默认值
            if (stylePatternMap.TryGetValue(style, out var pattern))
            {
                _logger.LogDebug("成功获取伴奏模式，风格: {Style}, 模式: {Pattern}", style, pattern);
                return pattern;
            }
            
            // 默认返回块和弦模式
            _logger.LogWarning("未找到指定风格的伴奏模式，使用默认模式，风格: {Style}", style);
            return "block";
        }
    }
}