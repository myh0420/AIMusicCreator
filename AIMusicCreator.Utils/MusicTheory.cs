namespace AIMusicCreator.Utils;

/// <summary>
/// 音乐理论工具类
/// </summary>
public static class MusicTheory
{
    // 音名字符串到MIDI音符号的映射
    private static readonly Dictionary<string, int> _noteToMidi = new()
    {
        { "C", 60 },  { "C#", 61 }, { "Db", 61 }, { "D", 62 },  { "D#", 63 }, { "Eb", 63 },
        { "E", 64 },  { "F", 65 },  { "F#", 66 }, { "Gb", 66 }, { "G", 67 },  { "G#", 68 },
        { "Ab", 68 }, { "A", 69 },  { "A#", 70 }, { "Bb", 70 }, { "B", 71 }
    };
    
    // 音符频率映射（以A4=440Hz为基准）
    private static readonly Dictionary<int, double> _midiToFrequency = new();
    
    static MusicTheory()
    {
        // 预计算MIDI音符到频率的映射
        for (int i = 0; i < 128; i++) // MIDI音符范围是0-127
        {
            _midiToFrequency[i] = 440.0 * Math.Pow(2, (i - 69) / 12.0);
        }
    }
    
    /// <summary>
    /// 获取音符的频率
    /// </summary>
    public static double GetFrequency(string noteName, int octave = 4)
    {
        if (_noteToMidi.TryGetValue(noteName, out int baseMidi))
        {
            // 调整到指定八度
            int midiNote = baseMidi + (octave - 4) * 12;
            
            if (_midiToFrequency.TryGetValue(midiNote, out double frequency))
            {
                return frequency;
            }
        }
        
        // 默认返回440Hz (A4)
        return 440.0;
    }
    
    /// <summary>
    /// 解析和弦符号
    /// </summary>
    /// <param name="chordSymbol">和弦符号，如C、Am、G7等</param>
    /// <param name="key">当前调号</param>
    /// <returns>解析后的和弦对象</returns>
    public static MusicalChord ParseChordSymbol(string chordSymbol, string key = "C")
    {
        // 简化的和弦符号解析
        // 实际实现可能需要更复杂的解析逻辑
        
        string rootNote = chordSymbol[0].ToString();
        string chordType = "major"; // 默认大三和弦
        
        // 检查是否有升降号
        if (chordSymbol.Length > 1 && (chordSymbol[1] == '#' || chordSymbol[1] == 'b'))
        {
            rootNote += chordSymbol[1];
        }
        
        // 检查和弦类型
        if (chordSymbol.Contains("m"))
        {
            chordType = "minor";
        }
        else if (chordSymbol.Contains("7"))
        {
            chordType = "seventh";
        }
        
        // 返回创建的和弦
        return new MusicalChord(rootNote, chordType);
    }
    
    /// <summary>
    /// 获取调号内的和弦进行
    /// </summary>
    public static List<string> GetDiatonicChords(string key, bool useRomanNumerals = true)
    {
        // 简单实现，返回大调调号内的七个基本和弦
        if (useRomanNumerals)
        {
            // 返回罗马数字表示的和弦进行
            return new List<string> { "I", "ii", "iii", "IV", "V", "vi", "vii°" };
        }
        else
        {
            // 根据不同的调号返回具体的和弦
            // 这里简化处理，实际需要根据不同调号计算
            return new List<string> { "C", "Dm", "Em", "F", "G", "Am", "Bdim" };
        }
    }
    
    /// <summary>
    /// 计算两个音符之间的音程
    /// </summary>
    public static int GetInterval(string note1, string note2)
    {
        if (_noteToMidi.TryGetValue(note1, out int midi1) && _noteToMidi.TryGetValue(note2, out int midi2))
        {
            return Math.Abs(midi2 - midi1);
        }
        return 0;
    }
}

/// <summary>
/// 音乐和弦类（用于MusicTheory类的辅助）
/// </summary>
public class MusicalChord
{
    /// <summary>
    /// 根音
    /// </summary>
    public string RootNote { get; }
    
    /// <summary>
    /// 和弦类型
    /// </summary>
    public string ChordType { get; }
    
    /// <summary>
    /// 组成音符
    /// </summary>
    public List<string> Notes { get; }
    
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
        
        // 半音阶音符
        var allNotes = new[] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        
        // 找到根音在半音阶中的索引
        int rootIndex = Array.IndexOf(allNotes, rootNote);
        if (rootIndex < 0)
        {
            // 如果找不到，默认使用C（索引0）
            rootIndex = 0;
            rootNote = "C";
        }
        
        // 根据和弦类型添加音符
        switch (chordType.ToLower())
        {
            case "major":
                // 大三和弦: 根音, 大三度(+4半音), 纯五度(+7半音)
                notes.Add(allNotes[(rootIndex + 4) % 12]);
                notes.Add(allNotes[(rootIndex + 7) % 12]);
                break;
            case "minor":
            case "m":
                // 小三和弦: 根音, 小三度(+3半音), 纯五度(+7半音)
                notes.Add(allNotes[(rootIndex + 3) % 12]);
                notes.Add(allNotes[(rootIndex + 7) % 12]);
                break;
            case "seventh":
            case "7":
                // 属七和弦: 根音, 大三度(+4), 纯五度(+7), 小七度(+10)
                notes.Add(allNotes[(rootIndex + 4) % 12]);
                notes.Add(allNotes[(rootIndex + 7) % 12]);
                notes.Add(allNotes[(rootIndex + 10) % 12]);
                break;
            case "maj7":
                // 大七和弦: 根音, 大三度(+4), 纯五度(+7), 大七度(+11)
                notes.Add(allNotes[(rootIndex + 4) % 12]);
                notes.Add(allNotes[(rootIndex + 7) % 12]);
                notes.Add(allNotes[(rootIndex + 11) % 12]);
                break;
            default:
                // 默认大三和弦
                notes.Add(allNotes[(rootIndex + 4) % 12]);
                notes.Add(allNotes[(rootIndex + 7) % 12]);
                break;
        }
        
        return notes;
    }
}