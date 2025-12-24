using AIMusicCreator.Entity;

namespace AIMusicCreator.ApiService.Interfaces;

/// <summary>
/// 伴奏音符生成器接口
/// 负责生成具体的音符事件
/// </summary>
public interface IAccompanimentNoteGenerator
{
    /// <summary>
    /// 生成和弦伴奏音符
    /// </summary>
    /// <param name="chord">和弦</param>
    /// <param name="parameters">旋律参数</param>
    /// <param name="startTime">开始时间（tick）</param>
    /// <param name="duration">持续时间（拍数）</param>
    /// <returns>生成的音符事件列表</returns>
    List<NoteEvent> GenerateChordNotes(Chord chord, MelodyParameters parameters, long startTime, int duration);

    /// <summary>
    /// 生成琶音模式的音符
    /// </summary>
    /// <param name="chord">和弦</param>
    /// <param name="parameters">旋律参数</param>
    /// <param name="startTime">开始时间（tick）</param>
    /// <param name="duration">持续时间（拍数）</param>
    /// <returns>生成的音符事件列表</returns>
    List<NoteEvent> GenerateArpeggioPattern(Chord chord, MelodyParameters parameters, long startTime, int duration);

    /// <summary>
    /// 生成块和弦模式的音符
    /// </summary>
    /// <param name="chord">和弦</param>
    /// <param name="parameters">旋律参数</param>
    /// <param name="startTime">开始时间（tick）</param>
    /// <param name="duration">持续时间（拍数）</param>
    /// <returns>生成的音符事件列表</returns>
    List<NoteEvent> GenerateBlockChordPattern(Chord chord, MelodyParameters parameters, long startTime, int duration);

    /// <summary>
    /// 生成节奏模式的音符
    /// </summary>
    /// <param name="chord">和弦</param>
    /// <param name="parameters">旋律参数</param>
    /// <param name="startTime">开始时间（tick）</param>
    /// <param name="duration">持续时间（拍数）</param>
    /// <returns>生成的音符事件列表</returns>
    List<NoteEvent> GenerateRhythmicPattern(Chord chord, MelodyParameters parameters, long startTime, int duration);
}