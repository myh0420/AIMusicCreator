
using AIMusicCreator.Entity;
using AIMusicCreator.ApiService.Interfaces;
using NAudio.Midi;
using AIMusicCreator.Utils;
using NoteEvent = NAudio.Midi.NoteEvent;
using ControlChangeEvent = NAudio.Midi.ControlChangeEvent;
using PatchChangeEvent = NAudio.Midi.PatchChangeEvent;
using MidiFile = NAudio.Midi.MidiFile;

// 添加ChannelEvent的定义或使用更通用的类型检查
namespace AIMusicCreator.ApiService.Services
{

    /// <summary>
    /// MIDI编辑服务
    /// </summary>
    public class MidiEditorService : IMidiEditorService
    {
        private readonly ILogger<MidiEditorService> _logger;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public MidiEditorService(ILogger<MidiEditorService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// 解析MIDI文件信息
        /// </summary>
        /// <param name="midiBytes">MIDI文件的字节数组</param>
        /// /// <exception cref="ArgumentException">当MIDI字节数组为空时抛出</exception>
        /// <exception cref="Exception">当解析MIDI文件信息失败时抛出</exception>
        /// /// <returns>包含MIDI文件信息的对象，包含以下属性：
        /// Format: MIDI文件格式（0, 1, 2）
        /// Tracks: 轨道数量
        /// DeltaTicksPerQuarterNote: 每个四分音符的时间刻度
        /// Events: 每个轨道的事件信息，包含以下属性：
        /// TrackIndex: 轨道索引
        /// EventCount: 事件总数
        /// ChannelEvents: 通道事件总数（音符、控制变更、程序变更）
        /// NoteEvents: 音符事件总数
        /// ControlChangeEvents: 控制变更事件总数
        /// ProgramChangeEvents: 程序变更事件总数
        /// </returns>
        /// <remarks>
        /// 该方法会解析MIDI文件的基本信息，包括文件格式、轨道数量、每个四分音符的时间刻度，以及每个轨道的事件总数和不同类型的事件数量。
        /// 它会忽略MIDI文件中的错误事件，仅统计有效事件。
        /// </remarks>
        public object ParseMidiInfo(byte[] midiBytes)
        {
            if (midiBytes == null || midiBytes.Length == 0)
            {
                _logger.LogError("MIDI字节数组为空");
                throw new ArgumentException("MIDI字节数组不能为空", nameof(midiBytes));
            }
            
            try
            {
                _logger.LogInformation("开始解析MIDI文件信息");
                
                using var ms = new MemoryStream(midiBytes);
                var midiFile = new MidiFile(ms, strictChecking: false);
                
                var info = new
                {
                    Format = midiFile.FileFormat,
                    Tracks = midiFile.Events.Tracks,
                    DeltaTicksPerQuarterNote = midiFile.DeltaTicksPerQuarterNote,
                    Events = midiFile.Events.Select((track, index) => new
                    {
                        TrackIndex = index,
                        EventCount = track.Count,
                        ChannelEvents = track.Count(e => e is NoteEvent || e is PatchChangeEvent || e is ControlChangeEvent),
                        NoteEvents = track.Count(e => e is NoteEvent),
                        ControlChangeEvents = track.Count(e => e is ControlChangeEvent),
                        ProgramChangeEvents = track.Count(e => e is PatchChangeEvent)
                    }).ToList()
                };
                
                _logger.LogInformation("MIDI文件信息解析成功");
                return info;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析MIDI文件信息失败");
                throw new Exception("解析MIDI文件信息失败", ex);
            }
        }
        
        /// <summary>
        /// 解析MIDI文件基本信息（简化版）
        /// </summary>
        /// <param name="midiBytes">MIDI文件的字节数组</param>
        /// /// <exception cref="ArgumentException">当MIDI字节数组为空时抛出</exception>
        /// <exception cref="Exception">当解析MIDI文件基本信息失败时抛出</exception>
        /// /// <returns>包含MIDI文件基本信息的对象，包含以下属性：
        /// Format: MIDI文件格式（0, 1, 2）
        /// Tracks: 轨道数量
        /// DeltaTicksPerQuarterNote: 每个四分音符的时间刻度
        /// TotalEventCount: 所有轨道的总事件数
        /// </returns>
        /// <remarks>
        /// 该方法会解析MIDI文件的基本信息，包括文件格式、轨道数量、每个四分音符的时间刻度，以及所有轨道的总事件数。
        /// 它会忽略MIDI文件中的错误事件，仅统计有效事件。
        /// </remarks>
        public object ParseMidiInfoSimple(byte[] midiBytes)
        {
            if (midiBytes == null || midiBytes.Length == 0)
            {
                _logger.LogError("MIDI字节数组为空");
                throw new ArgumentException("MIDI字节数组不能为空", nameof(midiBytes));
            }
            
            try
            {
                _logger.LogInformation("开始简单解析MIDI文件信息");
                
                using var ms = new MemoryStream(midiBytes);
                var midiFile = new MidiFile(ms, strictChecking: false);
                
                var simpleInfo = new
                {
                    Format = midiFile.FileFormat,
                    Tracks = midiFile.Events.Tracks,
                    DeltaTicksPerQuarterNote = midiFile.DeltaTicksPerQuarterNote,
                    TotalEventCount = midiFile.Events.Sum(track => track.Count)
                };
                
                _logger.LogInformation("MIDI文件简单信息解析成功");
                return simpleInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "简单解析MIDI文件信息失败");
                throw new Exception("简单解析MIDI文件信息失败", ex);
            }
        }
        
        /// <summary>
        /// 获取MIDI文件的详细分析报告
        /// </summary>
        /// <param name="midiBytes">MIDI文件的字节数组</param>
        /// /// <exception cref="ArgumentException">当MIDI字节数组为空时抛出</exception>
        /// <exception cref="Exception">当生成MIDI文件详细分析报告失败时抛出</exception>
        /// /// <returns>包含MIDI文件详细分析报告的对象，包含以下属性：
        /// Format: MIDI文件格式（0, 1, 2）
        /// Tracks: 轨道数量
        /// DeltaTicksPerQuarterNote: 每个四分音符的时间刻度
        /// DurationInTicks:  MIDI文件的总时长（以时间刻度为单位）
        /// TracksAnalysis: 每个轨道的详细分析信息，包含以下属性：
        /// TrackIndex: 轨道索引
        /// EventCount: 轨道事件总数
        /// NoteCount: 音符事件总数
        /// NoteOnCount: 音符开启事件总数
        /// NoteOffCount: 音符关闭事件总数
        /// Instruments: 轨道中使用的乐器列表，每个乐器包含以下属性：
        /// ProgramNumber: 乐器程序号
        /// Name: 乐器名称
        /// </returns>
        /// <remarks>
        /// 该方法会解析MIDI文件的详细信息，包括文件格式、轨道数量、每个四分音符的时间刻度，以及所有轨道的事件数、音符数、音符开启数、音符关闭数和使用的乐器列表。
        /// 它会忽略MIDI文件中的错误事件，仅统计有效事件。
        /// </remarks>
        public object GetDetailedMidiAnalysis(byte[] midiBytes)
        {
            if (midiBytes == null || midiBytes.Length == 0)
            {
                _logger.LogError("MIDI字节数组为空");
                throw new ArgumentException("MIDI字节数组不能为空", nameof(midiBytes));
            }
            
            try
            {
                _logger.LogInformation("开始生成MIDI文件详细分析报告");
                
                using var ms = new MemoryStream(midiBytes);
                var midiFile = new MidiFile(ms, strictChecking: false);
                
                var report = new
                {
                    Format = midiFile.FileFormat,
                    Tracks = midiFile.Events.Tracks,
                    DeltaTicksPerQuarterNote = midiFile.DeltaTicksPerQuarterNote,
                    DurationInTicks = midiFile.Events.Max(track => track.LastOrDefault()?.AbsoluteTime ?? 0),
                    TracksAnalysis = midiFile.Events.Select((track, index) =>
                    {
                        var noteEvents = track.Where(e => e is NoteEvent).Cast<NoteEvent>().ToList();
                        var patchChanges = track.Where(e => e is PatchChangeEvent).Cast<PatchChangeEvent>().ToList();
                        var tempoEvents = track.Where(e => e is TempoEvent).Cast<TempoEvent>().ToList();
                        
                        return new
                        {
                            TrackIndex = index,
                            EventCount = track.Count,
                            NoteCount = noteEvents.Count,
                            NoteOnCount = noteEvents.Count(e => e.CommandCode == MidiCommandCode.NoteOn),
                            NoteOffCount = noteEvents.Count(e => e.CommandCode == MidiCommandCode.NoteOff),
                            Instruments = patchChanges.Select(pc => new
                            {
                                Time = pc.AbsoluteTime,
                                Channel = pc.Channel,
                                Instrument = pc.Patch
                            }).ToList(),
                            Tempos = tempoEvents.Select(te => new
                            {
                                Time = te.AbsoluteTime,
                                Bpm = (int)(60000000.0 / te.MicrosecondsPerQuarterNote)
                            }).ToList(),
                            ChannelsUsed = track.Select(e => e.Channel).Distinct().Where(c => c >= 0).ToList(),
                            FirstNoteTime = noteEvents.Min(e => e.AbsoluteTime),
                            LastNoteTime = noteEvents.Max(e => e.AbsoluteTime)
                        };
                    }).ToList()
                };
                
                _logger.LogInformation("MIDI文件详细分析报告生成成功");
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成MIDI文件详细分析报告失败");
                throw new Exception("生成MIDI文件详细分析报告失败", ex);
            }
        }
        
        /// <summary>
        /// 调整MIDI速度
        /// </summary>
        /// <param name="midiBytes">MIDI文件的字节数组</param>
        /// <param name="newBpm">新的速度（BPM），必须在1-300之间</param>
        /// /// <exception cref="ArgumentException">当MIDI字节数组为空时抛出</exception>
        /// <exception cref="ArgumentOutOfRangeException">当新的BPM值不在1-300之间时抛出</exception>
        /// <exception cref="Exception">当调整MIDI速度失败时抛出</exception>
        /// /// <returns>调整速度后的MIDI文件的字节数组</returns>
        /// <remarks>
        /// 该方法会调整MIDI文件的速度（BPM），并返回调整后的MIDI文件字节数组。
        /// 它会忽略MIDI文件中的错误事件，仅调整有效事件。
        /// </remarks>
        public byte[] ChangeMidiTempo(byte[] midiBytes, int newBpm)
        {
            if (midiBytes == null || midiBytes.Length == 0)
            {
                _logger.LogError("MIDI字节数组为空");
                throw new ArgumentException("MIDI字节数组不能为空", nameof(midiBytes));
            }
            
            if (newBpm <= 0 || newBpm > 300)
            {
                _logger.LogError("无效的BPM值: {newBpm}", newBpm);
                throw new ArgumentOutOfRangeException(nameof(newBpm), "BPM值必须在1-300之间");
            }
            
            try
            {
                _logger.LogInformation("开始调整MIDI速度，新的BPM值: {newBpm}", newBpm);
                
                using var ms = new MemoryStream(midiBytes);
                var midiFile = new MidiFile(ms, strictChecking: false);
                int ticksPerQuarter = midiFile.DeltaTicksPerQuarterNote;

                var eventCollection = new MidiEventCollection(midiFile.FileFormat, ticksPerQuarter);
                bool tempoFound = false;

                for (int trackIndex = 0; trackIndex < midiFile.Events.Tracks; trackIndex++)
                {
                    var newTrack = new List<MidiEvent>();
                    var originalTrack = midiFile.Events[trackIndex];

                    foreach (var midiEvent in originalTrack)
                    {
                        if (midiEvent is TempoEvent tempo)
                        {
                            var newTempo = new TempoEvent(CalculateMicrosecondsPerQuarterNote(newBpm), tempo.AbsoluteTime);
                            newTrack.Add(newTempo);
                            tempoFound = true;
                        }
                        else
                        {
                            newTrack.Add(midiEvent);
                        }
                    }

                    if (!tempoFound && trackIndex == 0)
                    {
                        newTrack.Insert(0, new TempoEvent(CalculateMicrosecondsPerQuarterNote(newBpm), 0));
                    }

                    eventCollection.AddTrack(newTrack);
                }

                var result = MidiUtils.ExportMidiToBytes(eventCollection);
                _logger.LogInformation("MIDI速度调整成功");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "调整MIDI速度失败");
                throw new Exception($"调整MIDI速度失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 计算每四分音符的微秒数
        /// </summary>
        /// <param name="bpm">速度（BPM）</param>
        /// <returns>每四分音符的微秒数</returns>
        /// <exception cref="ArgumentOutOfRangeException">当BPM值不在1-300之间时抛出</exception>
        /// <remarks>
        /// 该方法根据BPM值计算每四分音符的微秒数。
        /// 微秒数用于调整MIDI事件的时间戳，以改变播放速度。
        /// </remarks>
        private int CalculateMicrosecondsPerQuarterNote(int bpm)
        {
            return 60000000 / bpm;
        }

        /// <summary>
        /// 从轨道事件中获取第一个可用的通道
        /// </summary>
        /// <param name="trackEvents">音轨事件列表</param>
        /// <returns>第一个可用的通道编号（0-15），如果没有可用通道则返回0</returns>
        /// <remarks>
        /// 该方法会遍历音轨事件列表，查找第一个非默认通道（通道号大于0）。
        /// 如果没有找到非默认通道，则返回默认通道0。
        /// </remarks>
        private byte GetFirstChannel(List<MidiEvent> trackEvents)
        {
            foreach (var midiEvent in trackEvents)
            {
                if (midiEvent.Channel > 0)
                    return (byte)midiEvent.Channel;
            }
            return 0; // 默认通道0
        }
        /// <summary>
        /// 转换MIDI乐器（支持指定通道）
        /// </summary>
        /// <param name="midiBytes">MIDI文件的字节数组</param>
        /// <param name="trackIndex">要转换乐器的音轨索引</param>
        /// <param name="channel">要转换乐器的MIDI通道（0-15）</param>
        /// <param name="newInstrument">新的乐器编号（0-127）</param>
        /// <returns>转换乐器后的MIDI文件的字节数组</returns>
        /// <exception cref="ArgumentException">当MIDI字节数组为空时抛出</exception>
        /// <exception cref="ArgumentOutOfRangeException">当音轨索引、通道或乐器编号无效时抛出</exception>
        /// <exception cref="Exception">当转换MIDI乐器失败时抛出</exception>
        /// <remarks>
        /// 该方法会将指定音轨中指定通道的乐器编号替换为新的乐器编号。
        /// 如果通道为0，则会转换所有通道的乐器。
        /// </remarks>
        public byte[] ChangeMidiInstrument(byte[] midiBytes, int trackIndex, int channel, int newInstrument)
        {
            if (midiBytes == null || midiBytes.Length == 0)
            {
                _logger.LogError("MIDI字节数组为空");
                throw new ArgumentException("MIDI字节数组不能为空", nameof(midiBytes));
            }
            
            if (trackIndex < 0)
            {
                _logger.LogError("无效的音轨索引: {trackIndex}", trackIndex);
                throw new ArgumentOutOfRangeException(nameof(trackIndex), "音轨索引必须大于等于0");
            }
            
            if (channel < 0 || channel > 15)
            {
                _logger.LogError("无效的MIDI通道: {channel}", channel);
                throw new ArgumentOutOfRangeException(nameof(channel), "MIDI通道必须在0-15之间");
            }
            
            if (newInstrument < 0 || newInstrument > 127)
            {
                _logger.LogError("无效的乐器编号: {newInstrument}", newInstrument);
                throw new ArgumentOutOfRangeException(nameof(newInstrument), "乐器编号必须在0-127之间");
            }
            
            try
            {
                _logger.LogInformation("开始转换MIDI乐器，音轨索引: {trackIndex}, 通道: {channel}, 新乐器编号: {newInstrument}", trackIndex, channel, newInstrument);
                
                using var ms = new MemoryStream(midiBytes);
                var midiFile = new MidiFile(ms, strictChecking: false);
                
                if (trackIndex >= midiFile.Events.Tracks)
                {
                    throw new ArgumentOutOfRangeException(nameof(trackIndex), "音轨索引超出范围");
                }
                
                int ticksPerQuarter = midiFile.DeltaTicksPerQuarterNote;
                var eventCollection = new MidiEventCollection(midiFile.FileFormat, ticksPerQuarter);
                bool hasProgramChange = false;

                for (int i = 0; i < midiFile.Events.Tracks; i++)
                {
                    var originalTrack = midiFile.Events[i];
                    var newTrack = new List<MidiEvent>();

                    if (i == trackIndex)
                    {
                        foreach (var midiEvent in originalTrack)
                        {
                            if (midiEvent is PatchChangeEvent patchChange && patchChange.Channel == channel)
                            {
                                var newPatchChange = new PatchChangeEvent(
                                    patchChange.AbsoluteTime,
                                    patchChange.Channel,
                                    newInstrument
                                );
                                newTrack.Add(newPatchChange);
                                hasProgramChange = true;
                            }
                            else
                            {
                                newTrack.Add(midiEvent);
                            }
                        }

                        if (!hasProgramChange)
                        {
                            newTrack.Insert(0, new PatchChangeEvent(0, (byte)channel, newInstrument));
                        }
                    }
                    else
                    {
                        newTrack.AddRange(originalTrack);
                    }

                    eventCollection.AddTrack(newTrack);
                }

                var result = MidiUtils.ExportMidiToBytes(eventCollection);
                _logger.LogInformation("MIDI乐器转换成功（指定通道）");
                return result;
            }
            catch (ArgumentOutOfRangeException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "转换MIDI乐器（指定通道）失败");
                throw new Exception($"转换MIDI乐器（指定通道）失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 批量修改多个轨道的乐器
        /// </summary>
        /// <param name="midiBytes">MIDI文件的字节数组</param>
        /// <param name="trackInstruments">轨道索引到新乐器编号的映射字典</param>
        /// <returns>批量修改乐器后的MIDI文件的字节数组</returns>
        /// <exception cref="ArgumentException">当MIDI字节数组或轨道乐器映射为空时抛出</exception>
        /// <exception cref="ArgumentOutOfRangeException">当音轨索引或乐器编号无效时抛出</exception>
        /// <exception cref="Exception">当批量修改MIDI乐器失败时抛出</exception>
        /// <remarks>
        /// 该方法会根据提供的映射字典，批量修改多个音轨的乐器编号。
        /// 每个键值对表示一个音轨索引和对应的新乐器编号。
        /// </remarks>
        public byte[] ChangeMultipleInstruments(byte[] midiBytes, Dictionary<int, int> trackInstruments)
        {
            if (midiBytes == null || midiBytes.Length == 0)
            {
                _logger.LogError("MIDI字节数组为空");
                throw new ArgumentException("MIDI字节数组不能为空", nameof(midiBytes));
            }
            
            if (trackInstruments == null || trackInstruments.Count == 0)
            {
                _logger.LogError("轨道乐器映射为空");
                throw new ArgumentException("轨道乐器映射不能为空", nameof(trackInstruments));
            }
            
            try
            {
                _logger.LogInformation("开始批量修改MIDI乐器，轨道数: {count}", trackInstruments.Count);
                
                using var ms = new MemoryStream(midiBytes);
                var midiFile = new MidiFile(ms, strictChecking: false);
                
                // 验证参数
                foreach (var kvp in trackInstruments)
                {
                    if (kvp.Key < 0 || kvp.Key >= midiFile.Events.Tracks)
                    {
                        throw new ArgumentOutOfRangeException(nameof(trackInstruments), $"音轨索引 {kvp.Key} 无效");
                    }

                    if (kvp.Value < 0 || kvp.Value > 127)
                    {
                        throw new ArgumentOutOfRangeException(nameof(trackInstruments), $"乐器编号 {kvp.Value} 必须在0-127之间");
                    }
                }
                
                // 创建新的事件集合
                int ticksPerQuarter = midiFile.DeltaTicksPerQuarterNote;
                var eventCollection = new MidiEventCollection(midiFile.FileFormat, ticksPerQuarter);

                // 处理每个轨道
                for (int trackIndex = 0; trackIndex < midiFile.Events.Tracks; trackIndex++)
                {
                    var originalTrack = midiFile.Events[trackIndex];
                    var newTrack = new List<MidiEvent>();

                    if (trackInstruments.ContainsKey(trackIndex))
                    {
                        int newInstrument = trackInstruments[trackIndex];
                        bool hasProgramChange = false;

                        foreach (var midiEvent in originalTrack)
                        {
                            if (midiEvent is PatchChangeEvent patchChange)
                            {
                                var newPatchChange = new PatchChangeEvent(
                                    patchChange.AbsoluteTime,
                                    patchChange.Channel,
                                    newInstrument
                                );
                                newTrack.Add(newPatchChange);
                                hasProgramChange = true;
                            }
                            else
                            {
                                newTrack.Add(midiEvent);
                            }
                        }

                        if (!hasProgramChange && newTrack.Count > 0)
                        {
                            byte channel = GetFirstChannel(newTrack);
                            newTrack.Insert(0, new PatchChangeEvent(0, channel, newInstrument));
                        }
                    }
                    else
                    {
                        newTrack.AddRange(originalTrack);
                    }

                    eventCollection.AddTrack(newTrack);
                }

                var result = MidiUtils.ExportMidiToBytes(eventCollection);
                _logger.LogInformation("批量修改MIDI乐器成功");
                return result;
            }
            catch (ArgumentOutOfRangeException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量修改MIDI乐器失败");
                throw new Exception($"批量修改MIDI乐器失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 内部方法：转换指定音轨的乐器
        /// </summary>
        /// <param name="midiFile">MIDI文件对象</param>
        /// <param name="trackIndex">要转换乐器的音轨索引</param>
        /// <param name="newInstrument">新的乐器编号（0-127）</param>
        /// <returns>转换乐器后的MIDI文件的字节数组</returns>
        /// <exception cref="ArgumentOutOfRangeException">当音轨索引或乐器编号无效时抛出</exception>
        /// <exception cref="Exception">当转换MIDI乐器失败时抛出</exception>
        /// <remarks>
        /// 该方法会将指定音轨的所有通道的乐器编号替换为新的乐器编号。
        /// 如果轨道中没有Program Change事件，则会在轨道开头添加一个默认通道的Program Change事件。
        /// </remarks>
        private byte[] ChangeMidiInstrumentInternal(MidiFile midiFile, int trackIndex, int newInstrument)
        {
            // 复用现有实现的核心逻辑
            int ticksPerQuarter = midiFile.DeltaTicksPerQuarterNote;
            var eventCollection = new MidiEventCollection(midiFile.FileFormat, ticksPerQuarter);
            
            // 处理每个轨道
            for (int i = 0; i < midiFile.Events.Tracks; i++)
            {
                var originalTrack = midiFile.Events[i];
                var newTrack = new List<MidiEvent>();

                if (i == trackIndex)
                {
                    bool hasProgramChange = false;

                    foreach (var midiEvent in originalTrack)
                    {
                        if (midiEvent is PatchChangeEvent patchChange)
                        {
                            var newPatchChange = new PatchChangeEvent(
                                patchChange.AbsoluteTime,
                                patchChange.Channel,
                                newInstrument
                            );
                            newTrack.Add(newPatchChange);
                            hasProgramChange = true;
                        }
                        else
                        {
                            newTrack.Add(midiEvent);
                        }
                    }

                    if (!hasProgramChange && newTrack.Count > 0)
                    {
                        byte channel = GetFirstChannel(newTrack);
                        newTrack.Insert(0, new PatchChangeEvent(0, channel, newInstrument));
                    }
                }
                else
                {
                    newTrack.AddRange(originalTrack);
                }

                eventCollection.AddTrack(newTrack);
            }

            return MidiUtils.ExportMidiToBytes(eventCollection);
        }
        
        // ... existing code for helper methods ...
    }
}