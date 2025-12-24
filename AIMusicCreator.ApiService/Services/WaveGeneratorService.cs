using AIMusicCreator.ApiService.Interfaces;
using AIMusicCreator.Entity;
using System;
using System.Collections.Generic;

namespace AIMusicCreator.ApiService.Services;

/// <summary>
/// 波形生成服务实现
/// 负责生成各种音频波形
/// </summary>
public class WaveGeneratorService : IWaveGeneratorService
{
    private readonly Random _random = new Random();
    
    /// <summary>
    /// 生成正弦波
    /// </summary>
    /// <param name="frequency">频率</param>
    /// <param name="duration">持续时间</param>
    /// <param name="amplitude">振幅</param>
    /// <returns>生成的音频数据</returns>
    public AudioData GenerateSineWave(double frequency, double duration, double amplitude = 0.5)
    {
        int sampleRate = 44100;
        int sampleCount = (int)(duration * sampleRate);
        var audioData = new AudioData(sampleRate, sampleCount);
        
        for (int i = 0; i < sampleCount; i++)
        {
            double t = i / (double)sampleRate;
            double sample = amplitude * Math.Sin(2 * Math.PI * frequency * t);
            // 设置双声道
            audioData.SetSample(i, 0, sample);
            audioData.SetSample(i, 1, sample);
        }
        
        return audioData;
    }
    
    /// <summary>
    /// 生成方波
    /// </summary>
    /// <param name="frequency">频率</param>
    /// <param name="duration">持续时间</param>
    /// <param name="dutyCycle">占空比</param>
    /// <returns>生成的音频数据</returns>
    public AudioData GenerateSquareWave(double frequency, double duration, double dutyCycle = 0.5)
    {
        int sampleRate = 44100;
        int sampleCount = (int)(duration * sampleRate);
        var audioData = new AudioData(sampleRate, sampleCount);
        double amplitude = 0.5; // 使用默认振幅值
        
        for (int i = 0; i < sampleCount; i++)
        {
            double t = i / (double)sampleRate;
            double phase = (t * frequency) % 1;
            double sample = phase < dutyCycle ? amplitude : -amplitude;
            // 设置双声道
            audioData.SetSample(i, 0, sample);
            audioData.SetSample(i, 1, sample);
        }
        
        return audioData;
    }
    
    /// <summary>
    /// 生成锯齿波
    /// </summary>
    /// <param name="frequency">频率</param>
    /// <param name="duration">持续时间</param>
    /// <returns>生成的音频数据</returns>
    public AudioData GenerateSawtoothWave(double frequency, double duration)
    {
        int sampleRate = 44100;
        int sampleCount = (int)(duration * sampleRate);
        var audioData = new AudioData(sampleRate, sampleCount);
        double amplitude = 0.5; // 使用默认振幅值
        
        for (int i = 0; i < sampleCount; i++)
        {
            double t = i / (double)sampleRate;
            double phase = (t * frequency) % 1;
            double sample = amplitude * (2 * phase - 1);
            // 设置双声道
            audioData.SetSample(i, 0, sample);
            audioData.SetSample(i, 1, sample);
        }
        
        return audioData;
    }
    
    /// <summary>
    /// 生成噪声
    /// </summary>
    /// <param name="duration">持续时间</param>
    /// <param name="amplitude">振幅</param>
    /// <returns>生成的音频数据</returns>
    public AudioData GenerateNoise(double duration, double amplitude = 0.5)
    {
        int sampleRate = 44100;
        int sampleCount = (int)(duration * sampleRate);
        var audioData = new AudioData(sampleRate, sampleCount);
        
        for (int i = 0; i < sampleCount; i++)
        {
            double sample = amplitude * (2 * _random.NextDouble() - 1);
            // 设置双声道
            audioData.SetSample(i, 0, sample);
            audioData.SetSample(i, 1, sample);
        }
        
        return audioData;
    }
}