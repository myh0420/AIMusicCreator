using AIMusicCreator.Entity;
using Melanchall.DryWetMidi.MusicTheory;
using Chord = AIMusicCreator.Entity.Chord;

namespace AIMusicCreator.ApiService.Interfaces;

/// <summary>
/// 风格特定元素生成器接口
/// 负责生成不同音乐风格的特有元素
/// </summary>
public interface IStyleElementGenerator
{
    /// <summary>
    /// 添加风格特定的元素到音符列表
    /// </summary>
    /// <param name="notes">音符事件列表</param>
    /// <param name="chord">和弦</param>
    /// <param name="parameters">旋律参数</param>
    /// <param name="startTime">开始时间（tick）</param>
    /// <param name="duration">持续时间（拍数）</param>
    void AddStyleElements(List<NoteEvent> notes, Chord chord, MelodyParameters parameters, long startTime, int duration);

    /// <summary>
    /// 获取特定风格的贝斯模式
    /// </summary>
    /// <param name="style">音乐风格</param>
    /// <returns>贝斯模式列表，每项包含持续时间和音符</returns>
    List<(long duration, NoteName note)> GetBassPattern(MusicStyle style);

    /// <summary>
    /// 获取特定风格和情绪的节奏模式
    /// </summary>
    /// <param name="style">音乐风格</param>
    /// <param name="emotion">情绪</param>
    /// <returns>节奏模式列表</returns>
    List<(long duration, int velocity, bool playChord, bool playBass)> GetRhythmPattern(MusicStyle style, Emotion emotion);
}