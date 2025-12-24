using AIMusicCreator.Entity; // 假设AudioData和WaveType在此命名空间
using System;

namespace AIMusicCreator.ApiService.Interfaces;

/// <summary>
/// 波形生成服务接口
/// 负责生成各种音频波形
/// </summary>
public interface IWaveGeneratorService
{
    /// <summary>
    /// 生成正弦波
    /// </summary>
    /// <param name="frequency">频率</param>
    /// <param name="duration">持续时间</param>
    /// <param name="amplitude">振幅</param>
    /// <returns>生成的音频数据</returns>
    AudioData GenerateSineWave(double frequency, double duration, double amplitude = 0.5);
    
    /// <summary>
    /// 生成方波
    /// </summary>
    /// <param name="frequency">频率</param>
    /// <param name="duration">持续时间</param>
    /// <param name="dutyCycle">占空比</param>
    /// <returns>生成的音频数据</returns>
    AudioData GenerateSquareWave(double frequency, double duration, double dutyCycle = 0.5);
    
    /// <summary>
    /// 生成锯齿波
    /// </summary>
    /// <param name="frequency">频率</param>
    /// <param name="duration">持续时间</param>
    /// <returns>生成的音频数据</returns>
    AudioData GenerateSawtoothWave(double frequency, double duration);
    
    /// <summary>
    /// 生成噪声
    /// </summary>
    /// <param name="duration">持续时间</param>
    /// <param name="amplitude">振幅</param>
    /// <returns>生成的音频数据</returns>
    AudioData GenerateNoise(double duration, double amplitude = 0.5);
}