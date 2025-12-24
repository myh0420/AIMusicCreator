using System.Security.Cryptography;

namespace AIMusicCreator.Utils;

/// <summary>
/// 音乐工具类
/// </summary>
public static class MusicUtils
{
    private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();
    private static readonly byte[] _randomBuffer = new byte[4];
    
    /// <summary>
    /// 生成白噪音
    /// </summary>
    /// <returns>-1.0到1.0之间的随机值</returns>
    public static double GenerateWhiteNoise()
    {
        lock (_randomBuffer)
        {
            _rng.GetBytes(_randomBuffer);
            int intValue = BitConverter.ToInt32(_randomBuffer, 0);
            // 将Int32范围映射到-1.0到1.0
            return intValue / (double)int.MaxValue;
        }
    }
    
    /// <summary>
    /// 生成粉红噪音
    /// </summary>
    /// <returns>-1.0到1.0之间的随机值</returns>
    public static double GeneratePinkNoise()
    {
        // 简单的粉红噪音生成
        // 实际实现中可能需要使用更复杂的算法
        double white = GenerateWhiteNoise();
        double pink = 0.9 * white + 0.1 * GenerateWhiteNoise();
        return Math.Max(-1.0, Math.Min(1.0, pink));
    }
    
    /// <summary>
    /// 将分贝转换为线性振幅
    /// </summary>
    public static double DbToLinear(double db)
    {
        return Math.Pow(10, db / 20);
    }
    
    /// <summary>
    /// 将线性振幅转换为分贝
    /// </summary>
    public static double LinearToDb(double amplitude)
    {
        if (amplitude <= 0) return -96.0; // 最小分贝值
        return 20 * Math.Log10(amplitude);
    }
    
    /// <summary>
    /// 应用简单的低通滤波器
    /// </summary>
    public static double ApplyLowPassFilter(double sample, double cutoff, double sampleRate, ref double previousOutput)
    {
        double rc = 1.0 / (2 * Math.PI * cutoff);
        double dt = 1.0 / sampleRate;
        double alpha = dt / (rc + dt);
        
        double filtered = previousOutput + alpha * (sample - previousOutput);
        previousOutput = filtered;
        return filtered;
    }
    
    /// <summary>
    /// 应用简单的高通滤波器
    /// </summary>
    public static double ApplyHighPassFilter(double sample, double cutoff, double sampleRate, ref double previousInput, ref double previousOutput)
    {
        double rc = 1.0 / (2 * Math.PI * cutoff);
        double dt = 1.0 / sampleRate;
        double alpha = rc / (rc + dt);
        
        double filtered = alpha * (previousOutput + sample - previousInput);
        previousInput = sample;
        previousOutput = filtered;
        return filtered;
    }
}