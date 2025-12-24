namespace AIMusicCreator.Entity;

/// <summary>
/// 伴奏风格枚举
/// </summary>
public enum AccompanimentStyle
{
    Pop,
    Rock,
    Jazz,
    Classical,
    Electronic
}

/// <summary>
/// 伴奏参数
/// </summary>
public class AccompanimentParameters
{
    /// <summary>
    /// 音乐风格
    /// </summary>
    public AccompanimentStyle Style { get; set; } = AccompanimentStyle.Pop;
    
    /// <summary>
    /// 和弦进行
    /// </summary>
    public string ChordProgression { get; set; } = "I-V-vi-IV";
    
    /// <summary>
    /// 速度（BPM）
    /// </summary>
    public int Bpm { get; set; } = 120;
    
    /// <summary>
    /// 乐器配置
    /// </summary>
    public InstrumentationConfiguration Instrumentation { get; set; } = new InstrumentationConfiguration();
    
    /// <summary>
    /// 是否包含鼓点
    /// </summary>
    public bool IncludeDrums { get; set; } = true;
    
    /// <summary>
    /// 调号
    /// </summary>
    public string Key { get; set; } = "C";
}

/// <summary>
/// 乐器配置
/// </summary>
public class InstrumentationConfiguration
{
    /// <summary>
    /// 是否包含鼓
    /// </summary>
    public bool Drums { get; set; } = true;
    
    /// <summary>
    /// 是否包含贝斯
    /// </summary>
    public bool Bass { get; set; } = true;
    
    /// <summary>
    /// 是否包含吉他
    /// </summary>
    public bool Guitar { get; set; } = true;
    
    /// <summary>
    /// 是否包含键盘
    /// </summary>
    public bool Keyboards { get; set; } = true;
}

/// <summary>
/// 音乐和弦
/// </summary>
public class MusicalChord
{
    /// <summary>
    /// 根音
    /// </summary>
    public string RootNote { get; set; }
    
    /// <summary>
    /// 和弦类型
    /// </summary>
    public string ChordType { get; set; }
    
    /// <summary>
    /// 组成音符
    /// </summary>
    public List<string> Notes { get; set; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public MusicalChord(string rootNote, string chordType = "major")
    {
        RootNote = rootNote;
        ChordType = chordType;
        Notes = GenerateChordNotes(rootNote, chordType);
    }
    
    /// <summary>
    /// 生成和弦音符
    /// </summary>
    private List<string> GenerateChordNotes(string rootNote, string chordType)
    {
        var notes = new List<string> { rootNote };
        
        // 简单的和弦音符生成
        switch (chordType.ToLower())
        {
            case "major":
                // 大三和弦: 根音, 大三度, 纯五度
                notes.Add(GetNoteAtInterval(rootNote, 4)); // 大三度
                notes.Add(GetNoteAtInterval(rootNote, 7)); // 纯五度
                break;
            case "minor":
                // 小三和弦: 根音, 小三度, 纯五度
                notes.Add(GetNoteAtInterval(rootNote, 3)); // 小三度
                notes.Add(GetNoteAtInterval(rootNote, 7)); // 纯五度
                break;
            case "seventh":
            case "7":
                // 属七和弦: 根音, 大三度, 纯五度, 小七度
                notes.Add(GetNoteAtInterval(rootNote, 4)); // 大三度
                notes.Add(GetNoteAtInterval(rootNote, 7)); // 纯五度
                notes.Add(GetNoteAtInterval(rootNote, 10)); // 小七度
                break;
            default:
                // 默认大三和弦
                notes.Add(GetNoteAtInterval(rootNote, 4));
                notes.Add(GetNoteAtInterval(rootNote, 7));
                break;
        }
        
        return notes;
    }
    
    /// <summary>
    /// 获取指定音程的音符
    /// </summary>
    private string GetNoteAtInterval(string startNote, int halfSteps)
    {
        // 半音阶音符
        var allNotes = new[] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        
        // 找到起始音符的索引
        int startIndex = Array.IndexOf(allNotes, startNote);
        if (startIndex == -1)
        {
            // 如果找不到，默认使用C
            startIndex = 0;
        }
        
        // 计算新音符的索引
        int newIndex = (startIndex + halfSteps) % 12;
        if (newIndex < 0) newIndex += 12;
        
        return allNotes[newIndex];
    }
}

/// <summary>
/// 鼓参数
/// </summary>
public class DrumParameters
{
    /// <summary>
    /// 音乐风格
    /// </summary>
    public AccompanimentStyle Style { get; set; } = AccompanimentStyle.Pop;
    
    /// <summary>
    /// 速度（BPM）
    /// </summary>
    public int Bpm { get; set; } = 120;
    
    /// <summary>
    /// 小节数
    /// </summary>
    public int Measures { get; set; } = 8;
    
    /// <summary>
    /// 底鼓音量
    /// </summary>
    public double KickVolume { get; set; } = 0.8;
    
    /// <summary>
    /// 军鼓音量
    /// </summary>
    public double SnareVolume { get; set; } = 0.7;
    
    /// <summary>
    /// 镲片音量
    /// </summary>
    public double HihatVolume { get; set; } = 0.5;
}