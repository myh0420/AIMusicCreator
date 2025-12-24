﻿﻿﻿using System.Threading.Tasks;

namespace AIMusicCreator.ApiService.Interfaces;

/// <summary>
/// 音频处理服务接口
/// 提供音频数据处理、格式转换、音量调整等功能
/// </summary>
public interface IAudioService
{
    /// <summary>
    /// 调整音频音量
    /// </summary>
    /// <param name="audioData">原始音频数据</param>
    /// <param name="volumeLevel">音量级别，范围0-100</param>
    /// <returns>调整音量后的音频数据</returns>
    Task<byte[]> AdjustVolumeAsync(byte[] audioData, int volumeLevel);
    
    /// <summary>
    /// 合并多个音频文件
    /// </summary>
    /// <param name="audioDataList">音频数据列表</param>
    /// <returns>合并后的音频数据</returns>
    Task<byte[]> MergeAudiosAsync(List<byte[]> audioDataList);
    
    /// <summary>
    /// 裁剪音频
    /// </summary>
    /// <param name="audioData">原始音频数据</param>
    /// <param name="startSeconds">开始时间（秒）</param>
    /// <param name="durationSeconds">持续时间（秒）</param>
    /// <returns>裁剪后的音频数据</returns>
    Task<byte[]> TrimAudioAsync(byte[] audioData, double startSeconds, double durationSeconds);
    
    /// <summary>
    /// 获取音频时长
    /// </summary>
    /// <param name="audioData">音频数据</param>
    /// <returns>音频时长（秒）</returns>
    Task<double> GetAudioDurationAsync(byte[] audioData);
    
    /// <summary>
    /// 验证音频数据
    /// </summary>
    /// <param name="audioData">音频数据</param>
    /// <returns>验证结果（是否有效，错误信息）</returns>
    (bool IsValid, string ErrorMessage) ValidateAudioData(byte[] audioData);
    
    /// <summary>
    /// 将MIDI转换为WAV
    /// </summary>
    /// <param name="midiData">MIDI数据</param>
    /// <returns>WAV音频数据</returns>
    byte[] MidiToWav(byte[] midiData);
    
    /// <summary>
    /// 调整音频时长
    /// </summary>
    /// <param name="audioData">音频数据</param>
    /// <param name="targetDurationSeconds">目标时长（秒）</param>
    /// <returns>调整后的音频数据</returns>
    byte[] AdaptDuration(byte[] audioData, double targetDurationSeconds);
}