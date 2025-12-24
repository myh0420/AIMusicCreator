using Melanchall.DryWetMidi.MusicTheory;
using Microsoft.AspNetCore.Mvc;

namespace AIMusicCreator.Entity
{
    /// <summary>
    /// 音乐风格枚举
    /// 定义不同类型的音乐风格，影响旋律和伴奏的生成方式
    /// </summary>
    public enum MusicStyle
    {
        /// <summary>
        /// 流行音乐：简单的旋律和和弦进行
        /// </summary>
        Pop,        // 
        /// <summary>
        /// 摇滚音乐：强烈的节奏感
        /// </summary>
        Rock,       // 
        /// <summary>
        /// 爵士音乐：复杂的和弦和即兴
        /// </summary>
        Jazz,       // 
        /// <summary>
        /// 古典音乐：优雅的旋律线
        /// </summary>
        Classical,  // 
        /// <summary>
        /// 电子音乐：重复的节奏模式
        /// </summary>
        Electronic, // 
        /// <summary>
        /// 布鲁斯音乐：特定的音阶和进行
        /// </summary>
        Blues,      // 
        /// <summary>
        /// 民间音乐：简单的旋律和和声结构
        /// </summary>
        Folk,        // 
        Standard,
        Romantic,
        Mysterious
    }

    /// <summary>
    /// 情绪枚举
    /// 定义音乐表达的情感，影响音符的力度、时值和音高范围
    /// </summary>
    public enum Emotion
    {
        /// <summary>
        /// 快乐：明亮的音色，较快的节奏
        /// </summary>
        Happy,      // 
        /// <summary>
        /// 悲伤：柔和的音色，较慢的节奏
        /// </summary>
        Sad,        // 
        /// <summary>
        /// 活力：强烈的力度，复杂的节奏
        /// </summary>
        Energetic,  // 
        /// <summary>
        /// 平静：平稳的进行，简单的结构
        /// </summary>
        Calm,       // 
        /// <summary>
        /// 神秘：不寻常的音程，变化的力度
        /// </summary>
        Mysterious, // 
        /// <summary>
        /// 浪漫：流畅的旋律，丰富的和声
        /// </summary>
        Romantic,    // 
        /// <summary>
        /// 标准：符合传统音乐理论的进行
        /// </summary>
        Standard,


        Angry,
    }
    

    ///// <summary>
    ///// 旋律参数类
    ///// 包含生成旋律所需的所有配置参数
    ///// </summary>
    //public class MelodyParameters
    //{
    //    /// <summary>
    //    /// 音乐风格
    //    /// </summary>
    //    public MusicStyle Style { get; set; }   // 
    //    /// <summary>
    //    /// 情绪表达
    //    /// </summary>
    //    public Emotion Emotion { get; set; }    // 
    //    /// <summary>
    //    /// 速度（每分钟节拍数）
    //    /// </summary>
    //    public int BPM { get; set; }            // 
    //    /// <summary>
    //    /// 小节数量
    //    /// </summary>
    //    public int Bars { get; set; }           // 
    //    /// <summary>
    //    /// 使用的音阶
    //    /// </summary>
    //    public required Scale Scale { get; set; }      // 
    //    /// <summary>
    //    /// 基准八度
    //    /// </summary>
    //    public int Octave { get; set; }         // 
    //}

    /// <summary>
    /// 和弦进行类
    /// 存储分析得到的和弦序列及其持续时间
    /// </summary>
    public class ChordProgression
    {
        /// <summary>
        /// 和弦列表
        /// </summary>
        public List<Chord> Chords { get; set; } = [];    // 
        /// <summary>
        /// 每个和弦的持续时间（以四分音符为单位）
        /// </summary>
        public List<int> Durations { get; set; } = [];    // 
        /// <summary>
        /// 拍号
        /// </summary>
        public int TimeSignature { get; set; } = 4;

        /// <summary>
        /// 调号
        /// </summary>
        public NoteName Key { get; set; } = NoteName.C;

        /// <summary>
        /// 调式（大调/小调）
        /// </summary>
        public string Mode { get; set; } = "Major";

        /// <summary>
        /// 添加和弦到进行中
        /// </summary>
        /// <param name="chord">要添加的和弦</param>
        public void AddChord(Chord chord)
        {
            Chords.Add(chord);
        }

        /// <summary>
        /// 移除指定索引的和弦
        /// </summary>
        /// <param name="index">和弦索引</param>
        public void RemoveChord(int index)
        {
            if (index >= 0 && index < Chords.Count)
            {
                Chords.RemoveAt(index);
            }
        }
    }

    /// <summary>
    /// 音符事件类
    /// 表示单个音符的完整信息
    /// </summary>
    public class NoteEvent
    {
        /// <summary>
        /// 音符名称
        /// </summary>
        public NoteName Note { get; set; }      // 
        /// <summary>
        /// 八度
        /// </summary>
        public int Octave { get; set; }         // 
        /// <summary>
        /// 开始时间（ticks）
        /// </summary>
        public long StartTime { get; set; }     // 
        /// <summary>
        /// 持续时间（ticks）
        /// </summary>
        public long Duration { get; set; }      // 
        /// <summary>
        /// 力度（0-127）
        /// </summary>
        public int Velocity { get; set; }       // 
    }
    // 
    /// <summary>
    /// 修正Chord类定义
    /// </summary>
    /// <param name="root"></param>
    /// <param name="third"></param>
    /// <param name="fifth"></param>
    public class Chord(NoteName root, NoteName third, NoteName fifth)
    {
        public NoteName Root { get; set; } = root;
        public NoteName Third { get; set; } = third;
        public NoteName Fifth { get; set; } = fifth;
        /// <summary>
        /// 和弦类型
        /// </summary>
        public string ChordType { get; set; } = "Major";

        /// <summary>
        /// 和弦持续的拍数
        /// </summary>
        public int Duration { get; set; } = 4;
        public List<NoteName> GetNotes()
        {
            return [Root, Third, Fifth];
        }
    }
}
