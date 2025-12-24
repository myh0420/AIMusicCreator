using System.IO;
using AIMusicCreator.Entity; // 假设AudioData在此命名空间

namespace AIMusicCreator.ApiService.Interfaces;

/// <summary>
/// 音频导出服务接口
/// 负责将音频数据导出为不同格式
/// </summary>
public interface IAudioExportService
{
    /// <summary>
    /// 将音频数据导出为WAV格式
    /// </summary>
    /// <param name="audioData">音频数据</param>
    /// <param name="stream">目标流</param>
    void ExportToWav(AudioData audioData, Stream stream);
    
    /// <summary>
    /// 将音频数据导出为MP3格式
    /// </summary>
    /// <param name="audioData">音频数据</param>
    /// <param name="stream">目标流</param>
    void ExportToMp3(AudioData audioData, Stream stream);
}