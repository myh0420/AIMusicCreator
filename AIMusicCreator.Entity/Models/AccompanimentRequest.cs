namespace AIMusicCreator.Entity;

/// <summary>
/// 伴奏生成请求类
/// 用于接收API的伴奏生成请求参数
/// </summary>
public class AccompanimentRequest
{
    /// <summary>
    /// 音乐风格字符串
    /// </summary>
    public string Style { get; set; } = "pop";
    
    /// <summary>
    /// 和弦进行
    /// </summary>
    public string ChordProgression { get; set; } = "I-IV-V";
    
    /// <summary>
    /// 每分钟节拍数(BPM)
    /// </summary>
    public int Bpm { get; set; } = 120;
    
    /// <summary>
    /// 乐器配置字符串
    /// </summary>
    public string Instrumentation { get; set; } = "standard";
    
    /// <summary>
    /// 是否包含鼓
    /// </summary>
    public bool IncludeDrums { get; set; } = true;
    
    /// <summary>
    /// 情绪类型
    /// </summary>
    public string Emotion { get; set; } = "happy";
    
    /// <summary>
    /// 小节数
    /// </summary>
    public int Bars { get; set; } = 8;
    
    /// <summary>
    /// 力度值
    /// </summary>
    public int Velocity { get; set; } = 80;
}