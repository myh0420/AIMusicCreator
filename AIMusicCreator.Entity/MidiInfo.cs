using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIMusicCreator.Entity
{
    /// <summary>
    /// MIDI文件信息
    /// </summary>
    /// <remarks>
    /// 此类包含MIDI文件的基本元数据信息，用于描述和表示MIDI文件的整体结构和属性。
    /// 包含文件格式、速度、时长、音轨数量等关键参数，以及对各个音轨的引用。
    /// </remarks>
    public class MidiInfo
    {
        /// <summary>
        /// 每分钟节拍数（BPM）
        /// </summary>
        /// <value>MIDI文件的基本速度，通常在50-200范围内</value>
        public int Bpm { get; set; }
        
        /// <summary>
        /// MIDI文件中的音轨数量
        /// </summary>
        public int TrackCount { get; set; }
        
        /// <summary>
        /// MIDI文件的持续时间（秒）
        /// </summary>
        public double DurationSeconds { get; set; }
        
        /// <summary>
        /// 所有音轨中使用的乐器列表
        /// </summary>
        /// <value>乐器编号的列表，使用MIDI标准乐器编号（0-127）</value>
        public List<int> Instruments { get; set; } = [];
        
        /// <summary>
        /// 每四分音符的增量拍数（Ticks）
        /// </summary>
        /// <value>用于衡量MIDI事件时间的基本单位，通常为96或480</value>
        public int DeltaTicksPerQuarterNote { get; set; }
        
        /// <summary>
        /// MIDI文件格式类型
        /// </summary>
        /// <value>0：单音轨，1：多音轨同步，2：多音轨异步</value>
        public int FileFormat { get; set; }
        
        /// <summary>
        /// MIDI文件中的所有音轨信息
        /// </summary>
        public List<MidiTrackInfo> Tracks { get; set; } = [];
    }

    /// <summary>
    /// 音轨信息
    /// </summary>
    /// <remarks>
    /// 此类表示MIDI文件中的单个音轨及其相关属性，包含音轨的索引、事件数量、使用的乐器和通道等信息。
    /// 还包含该音轨中的所有音符信息，用于后续分析和处理。
    /// </remarks>
    public class MidiTrackInfo
    {
        /// <summary>
        /// 音轨索引
        /// </summary>
        /// <value>音轨在MIDI文件中的位置索引，从0开始</value>
        public int TrackIndex { get; set; }
        
        /// <summary>
        /// 音轨中的MIDI事件数量
        /// </summary>
        public int EventCount { get; set; }
        
        /// <summary>
        /// 音轨中使用的乐器列表
        /// </summary>
        /// <value>音轨中使用的MIDI标准乐器编号列表（0-127）</value>
        public List<int> Instruments { get; set; } = [];
        
        /// <summary>
        /// 音轨中使用的MIDI通道列表
        /// </summary>
        /// <value>使用的MIDI通道编号列表（0-15）</value>
        public List<int> Channels { get; set; } = [];
        
        /// <summary>
        /// 音轨中的所有音符信息
        /// </summary>
        public List<MidiNoteInfo> Notes { get; set; } = [];
    }

    /// <summary>
    /// 音符信息
    /// </summary>
    /// <remarks>
    /// 此类表示MIDI文件中的单个音符事件，包含音符的音高、时值、力度等关键参数。
    /// 用于描述MIDI音符的完整属性，是构建MIDI音乐的基本单位。
    /// </remarks>
    public class MidiNoteInfo
    {
        /// <summary>
        /// 音符编号
        /// </summary>
        /// <value>MIDI音符编号（0-127），其中60表示中央C（C4）</value>
        public int NoteNumber { get; set; }
        
        /// <summary>
        /// 音符名称
        /// </summary>
        /// <value>音符的文本表示，如"C4"、"D#5"等</value>
        public string NoteName { get; set; } = string.Empty;
        
        /// <summary>
        /// 音符开始的拍数位置
        /// </summary>
        /// <value>相对于MIDI文件起始位置的拍数偏移量</value>
        public long StartTick { get; set; }
        
        /// <summary>
        /// 音符持续的拍数
        /// </summary>
        public long DurationTicks { get; set; }
        
        /// <summary>
        /// 音符力度
        /// </summary>
        /// <value>音符的强度级别（0-127），值越大表示音量越大</value>
        public int Velocity { get; set; }
        
        /// <summary>
        /// 音符所在的MIDI通道
        /// </summary>
        /// <value>MIDI通道编号（0-15）</value>
        public int Channel { get; set; }
    }
    
    /// <summary>
    /// MIDI分析报告
    /// </summary>
    /// <remarks>
    /// 此类包含MIDI文件的详细分析结果，用于提供对MIDI文件结构和内容的全面描述。
    /// 包含文件格式、速度设置、音轨数量等基本信息，以及每个音轨的详细分析数据。
    /// </remarks>
    public class MidiAnalysisReport
    {
        /// <summary>
        /// MIDI文件格式类型
        /// </summary>
        /// <value>0：单音轨，1：多音轨同步，2：多音轨异步</value>
        public int FileFormat { get; set; }
        
        /// <summary>
        /// 每四分音符的增量拍数（Ticks）
        /// </summary>
        public int DeltaTicksPerQuarterNote { get; set; }
        
        /// <summary>
        /// 音轨总数
        /// </summary>
        public int TotalTracks { get; set; }
        
        /// <summary>
        /// 每个音轨的分析结果
        /// </summary>
        public List<TrackAnalysis> Tracks { get; set; } = [];
    }

    /// <summary>
    /// 音轨分析
    /// </summary>
    /// <remarks>
    /// 此类包含对MIDI音轨的深度分析结果，包括事件统计、乐器使用情况、音符数量等信息。
    /// 还包含节奏和拍号信息，用于理解音轨的音乐结构和特性。
    /// </remarks>
    public class TrackAnalysis
    {
        /// <summary>
        /// 音轨索引
        /// </summary>
        public int TrackIndex { get; set; }
        
        /// <summary>
        /// 音轨中的事件总数
        /// </summary>
        public int TotalEvents { get; set; }
        
        /// <summary>
        /// 音轨中的音符事件数量
        /// </summary>
        public int NoteCount { get; set; }
        
        /// <summary>
        /// 事件类型统计
        /// </summary>
        /// <value>键为事件类型名称，值为该类型事件的数量</value>
        public Dictionary<string, int> EventTypes { get; set; } = [];
        
        /// <summary>
        /// 音轨中使用的乐器集合
        /// </summary>
        /// <value>MIDI标准乐器编号的集合（0-127）</value>
        public HashSet<int> Instruments { get; set; } = [];
        
        /// <summary>
        /// 音轨中使用的MIDI通道集合
        /// </summary>
        /// <value>MIDI通道编号的集合（0-15）</value>
        public HashSet<int> ChannelsUsed { get; set; } = [];
        
        /// <summary>
        /// 音轨中的速度事件
        /// </summary>
        public List<TempoEvent> TempoEvents { get; set; } = [];
        
        /// <summary>
        /// 音轨中的拍号事件
        /// </summary>
        public List<TimeSignatureEvent> TimeSignatures { get; set; } = [];
    }
}
