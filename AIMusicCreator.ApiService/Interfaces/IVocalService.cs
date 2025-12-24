using System.Threading.Tasks;

namespace AIMusicCreator.ApiService.Interfaces;

/// <summary>
/// 人声合成服务接口
/// 负责将歌词转换为合成人声音频
/// </summary>
public interface IVocalService
{
    /// <summary>
    /// 生成合成人声
    /// </summary>
    /// <param name="lyrics">歌词文本</param>
    /// <param name="melodyMidi">旋律MIDI数据</param>
    /// <param name="language">歌词语言（默认为中文）</param>
    /// <returns>生成的WAV音频数据字节数组</returns>
    byte[] GenerateVocal(string lyrics, byte[] melodyMidi, string language = "zh");
    
    /// <summary>
    /// 异步生成合成人声
    /// </summary>
    /// <param name="lyrics">歌词文本</param>
    /// <param name="melodyMidi">旋律MIDI数据</param>
    /// <param name="language">歌词语言（默认为中文）</param>
    /// <returns>生成的WAV音频数据字节数组</returns>
    Task<byte[]> GenerateVocalAsync(string lyrics, byte[] melodyMidi, string language = "zh");
    
    /// <summary>
    /// 验证歌词和旋律数据
    /// </summary>
    /// <param name="lyrics">歌词文本</param>
    /// <param name="melodyMidi">旋律MIDI数据</param>
    /// <returns>验证结果和错误信息</returns>
    (bool IsValid, string ErrorMessage) ValidateVocalData(string lyrics, byte[] melodyMidi);
}