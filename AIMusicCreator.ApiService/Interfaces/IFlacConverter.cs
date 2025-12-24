using System.Collections.Generic;
using System.Threading.Tasks;

namespace AIMusicCreator.ApiService.Interfaces;

/// <summary>
/// FLAC转换服务接口
/// </summary>
public interface IFlacConverter
{
    /// <summary>
    /// 转换为FLAC格式
    /// </summary>
    /// <param name="audioData">音频数据</param>
    /// <param name="inputFormat">输入格式（可选）</param>
    /// <returns>转换后的FLAC数据</returns>
    Task<byte[]> ConvertToFlacAsync(byte[] audioData, string? inputFormat = null);

    /// <summary>
    /// 从WAV转换为FLAC
    /// </summary>
    /// <param name="wavData">WAV音频数据</param>
    /// <returns>转换后的FLAC数据</returns>
    Task<byte[]> ConvertWavToFlacAsync(byte[] wavData);

    /// <summary>
    /// 从MP3转换为FLAC
    /// </summary>
    /// <param name="mp3Data">MP3音频数据</param>
    /// <returns>转换后的FLAC数据</returns>
    Task<byte[]> ConvertMp3ToFlacAsync(byte[] mp3Data);

    /// <summary>
    /// 批量转换
    /// </summary>
    /// <param name="audioFiles">文件名和音频数据的字典</param>
    /// <returns>转换后的FLAC数据字典</returns>
    Task<Dictionary<string, byte[]>> BatchConvertAsync(Dictionary<string, byte[]> audioFiles);
}