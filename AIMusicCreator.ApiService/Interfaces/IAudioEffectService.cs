using System;

namespace AIMusicCreator.ApiService.Interfaces;

/// <summary>
/// 音频效果处理服务接口
/// </summary>
public interface IAudioEffectService
{
    /// <summary>
    /// 添加回声效果
    /// </summary>
    /// <param name="audioBytes">音频数据字节数组</param>
    /// <param name="delaySeconds">延迟时间（秒）</param>
    /// <param name="decay">衰减系数</param>
    /// <returns>处理后的音频数据</returns>
    byte[] AddEcho(byte[] audioBytes, float delaySeconds = 0.5f, float decay = 0.5f);

    /// <summary>
    /// 简单均衡器（增强低音）
    /// </summary>
    /// <param name="audioBytes">音频数据字节数组</param>
    /// <param name="gainDb">增益（分贝）</param>
    /// <returns>处理后的音频数据</returns>
    byte[] BoostBass(byte[] audioBytes, float gainDb = 6.0f);

    /// <summary>
    /// 音量标准化（将峰值调整到目标水平）
    /// </summary>
    /// <param name="audioBytes">音频数据字节数组</param>
    /// <param name="targetPeak">目标峰值</param>
    /// <returns>处理后的音频数据</returns>
    byte[] NormalizeVolume(byte[] audioBytes, float targetPeak = 0.9f);
}