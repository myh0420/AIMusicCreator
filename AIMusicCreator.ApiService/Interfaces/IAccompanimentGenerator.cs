using AIMusicCreator.Entity;

namespace AIMusicCreator.ApiService.Interfaces;

/// <summary>
/// 伴奏生成器接口
/// 定义生成音乐伴奏的核心功能
/// </summary>
public interface IAccompanimentGenerator
{
    /// <summary>
    /// 为给定的和弦进行生成伴奏
    /// </summary>
    /// <param name="chordProgression">和弦进行</param>
    /// <param name="parameters">伴奏参数</param>
    /// <returns>生成的音符事件列表</returns>
    List<NoteEvent> GenerateAccompaniment(ChordProgression chordProgression, MelodyParameters parameters);

    /// <summary>
    /// 根据音乐风格获取伴奏模式
    /// </summary>
    /// <param name="style">音乐风格</param>
    /// <returns>伴奏模式名称</returns>
    string GetAccompanimentPattern(MusicStyle style);
}