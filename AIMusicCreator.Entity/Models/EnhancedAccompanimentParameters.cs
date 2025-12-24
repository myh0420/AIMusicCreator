using System.Collections.Generic;

namespace AIMusicCreator.Entity.Models;

/// <summary>
/// 增强版伴奏风格枚举
/// </summary>
public enum EnhancedMusicStyle
{
    Pop,
    Rock,
    Jazz,
    Classical,
    Electronic,
    Blues
}

/// <summary>
/// 乐器配置类
/// </summary>
public class Instrumentation
{
    /// <summary>
    /// 主奏乐器
    /// </summary>
    public string LeadInstrument { get; set; } = "Piano";
    
    /// <summary>
    /// 和弦乐器
    /// </summary>
    public string ChordInstrument { get; set; } = "Guitar";
    
    /// <summary>
    /// 贝斯乐器
    /// </summary>
    public string BassInstrument { get; set; } = "Bass";
    
    /// <summary>
    /// 鼓组类型
    /// </summary>
    public string DrumKit { get; set; } = "Standard";
    
    /// <summary>
    /// 额外乐器列表
    /// </summary>
    public List<string> AdditionalInstruments { get; set; } = new List<string>();
}

/// <summary>
    /// 增强版伴奏参数类
    /// 包含生成伴奏所需的所有参数
    /// </summary>
    public class EnhancedAccompanimentParameters
    {
        /// <summary>
        /// 音乐风格
        /// </summary>
        public EnhancedMusicStyle Style { get; set; } = EnhancedMusicStyle.Pop;
    
    /// <summary>
    /// 和弦进行
    /// </summary>
    public string ChordProgression { get; set; } = "I-IV-V";
    
    /// <summary>
    /// 每分钟节拍数(BPM)
    /// </summary>
    public int Bpm { get; set; } = 120;
    
    /// <summary>
    /// 乐器配置
    /// </summary>
    public Instrumentation Instrumentation { get; set; } = new Instrumentation();
    
    /// <summary>
    /// 是否包含鼓
    /// </summary>
    public bool IncludeDrums { get; set; } = true;
    
    /// <summary>
    /// 情绪类型
    /// </summary>
    public string Emotion { get; set; } = "Happy";
    
    /// <summary>
    /// 小节数
    /// </summary>
    public int Bars { get; set; } = 8;
    
    /// <summary>
    /// 力度值(0-100)
    /// </summary>
    public int Velocity { get; set; } = 80;
}