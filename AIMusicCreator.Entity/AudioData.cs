namespace AIMusicCreator.Entity;

/// <summary>
/// 音频数据类
/// </summary>
public class AudioData
{
    private readonly double[,] _samples;
    
    /// <summary>
    /// 采样率
    /// </summary>
    public int SampleRate { get; }
    
    /// <summary>
    /// 通道数
    /// </summary>
    public int Channels { get; }
    
    /// <summary>
    /// 总采样数
    /// </summary>
    public int TotalSamples { get; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public AudioData(int sampleRate, int totalSamples, int channels = 2)
    {
        SampleRate = sampleRate;
        Channels = channels;
        TotalSamples = totalSamples;
        _samples = new double[totalSamples, channels];
    }
    
    /// <summary>
    /// 获取指定位置和通道的音频样本
    /// </summary>
    public double GetSample(int sampleIndex, int channel)
    {
        if (sampleIndex < 0 || sampleIndex >= TotalSamples || channel < 0 || channel >= Channels)
        {
            return 0.0;
        }
        return _samples[sampleIndex, channel];
    }
    
    /// <summary>
    /// 设置指定位置和通道的音频样本
    /// </summary>
    public void SetSample(int sampleIndex, int channel, double value)
    {
        if (sampleIndex >= 0 && sampleIndex < TotalSamples && channel >= 0 && channel < Channels)
        {
            _samples[sampleIndex, channel] = value;
        }
    }
    
    /// <summary>
    /// 向指定位置和通道添加音频样本
    /// </summary>
    public void AddSample(int sampleIndex, int channel, double value)
    {
        if (sampleIndex >= 0 && sampleIndex < TotalSamples && channel >= 0 && channel < Channels)
        {
            _samples[sampleIndex, channel] += value;
        }
    }
    
    /// <summary>
    /// 将两个音频数据混合
    /// </summary>
    public void MixWith(AudioData other, double volume = 1.0)
    {
        int samplesToMix = Math.Min(this.TotalSamples, other.TotalSamples);
        int channelsToMix = Math.Min(this.Channels, other.Channels);
        
        for (int i = 0; i < samplesToMix; i++)
        {
            for (int channel = 0; channel < channelsToMix; channel++)
            {
                double mixedValue = this._samples[i, channel] + (other.GetSample(i, channel) * volume);
                
                // 防止过载
                if (mixedValue > 1.0) mixedValue = 1.0;
                if (mixedValue < -1.0) mixedValue = -1.0;
                
                this._samples[i, channel] = mixedValue;
            }
        }
    }
    
    /// <summary>
    /// 应用增益
    /// </summary>
    public void ApplyGain(double gain)
    {
        for (int i = 0; i < TotalSamples; i++)
        {
            for (int channel = 0; channel < Channels; channel++)
            {
                double newSample = _samples[i, channel] * gain;
                
                // 防止过载
                if (newSample > 1.0) newSample = 1.0;
                if (newSample < -1.0) newSample = -1.0;
                
                _samples[i, channel] = newSample;
            }
        }
    }
    
    /// <summary>
    /// 创建音频数据的副本
    /// </summary>
    public AudioData Clone()
    {
        var clone = new AudioData(this.SampleRate, this.TotalSamples, this.Channels);
        
        for (int i = 0; i < this.TotalSamples; i++)
        {
            for (int channel = 0; channel < this.Channels; channel++)
            {
                clone.SetSample(i, channel, this._samples[i, channel]);
            }
        }
        
        return clone;
    }
}