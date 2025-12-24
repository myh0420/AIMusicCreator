using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIMusicCreator.Entity
{
    /// <summary>
    /// 自定义MIDI声音生成器
    /// </summary>
    /// <remarks>
    /// 此类实现了一个高级音符合成器，使用正弦波形、ADSR包络控制和颤音效果来生成MIDI音符的声音。
    /// 支持音符的触发和释放，并通过包络控制音量的动态变化，模拟真实乐器的音色特性。
    /// 新增了自然颤音效果，根据音符特性动态调整颤音参数。
    /// </remarks>
    public class CustomMidiVoice
    {
        /// <summary>
        /// 音符频率（Hz）
        /// </summary>
        private readonly float _frequency;
        
        /// <summary>
        /// 音符振幅（音量级别）
        /// </summary>
        private readonly float _amplitude;
        
        /// <summary>
        /// 音频采样率
        /// </summary>
        private readonly int _sampleRate;
        
        /// <summary>
        /// 当前波形相位
        /// </summary>
        private double _phase;
        
        /// <summary>
        /// 当前声音生成的时间点（秒）
        /// </summary>
        private double _time;
        
        /// <summary>
        /// 指示声音是否正在释放阶段
        /// </summary>
        private bool _isReleasing;
        
        /// <summary>
        /// 释放阶段开始的时间点
        /// </summary>
        private double _releaseStartTime;
        
        // 颤音相关参数
        /// <summary>
        /// 颤音频率（Hz）- 典型值为4-7Hz
        /// </summary>
        private readonly double _vibratoFrequency;
        
        /// <summary>
        /// 颤音深度（音高变化的百分比）
        /// </summary>
        private readonly double _vibratoDepth;
        
        /// <summary>
        /// 颤音LFO相位
        /// </summary>
        private double _vibratoPhase;
        
        /// <summary>
        /// 颤音启动时间（秒）- 颤音从弱到强的过渡时间
        /// </summary>
        private const double VibratoAttackTime = 0.3; // 颤音渐入时间
        
        /// <summary>
        /// 随机颤音参数变异因子
        /// </summary>
        private readonly Random _random;
        
        /// <summary>
        /// 颤音频率变化范围（百分比）
        /// </summary>
        private readonly double _vibratoFrequencyVariation;

        // ADSR 包络参数
        /// <summary>
        /// 攻击时间（秒）- 从0到最大音量的时间
        /// </summary>
        private const double AttackTime = 0.01;   // 攻击时间
        
        /// <summary>
        /// 衰减时间（秒）- 从最大音量到持续音量的时间
        /// </summary>
        private const double DecayTime = 0.05;    // 衰减时间  
        
        /// <summary>
        /// 持续电平 - 衰减后保持的音量级别（0-1范围）
        /// </summary>
        private const double SustainLevel = 0.7f; // 持续电平
        
        /// <summary>
        /// 释放时间（秒）- 从持续电平到完全静音的时间
        /// </summary>
        private const double ReleaseTime = 0.2;   // 释放时间

        /// <summary>
        /// 获取音符的频率
        /// </summary>
        /// <value>音符频率（Hz）</value>
        public float Frequency => _frequency;
        
        /// <summary>
        /// 获取声音是否已完成播放
        /// </summary>
        /// <value>如果声音已完成释放且完全静音，则返回true；否则返回false</value>
        public bool IsFinished => _isReleasing && (_time - _releaseStartTime) > ReleaseTime;

        /// <summary>
        /// 初始化自定义MIDI声音的新实例
        /// </summary>
        /// <param name="frequency">音符频率（Hz）</param>
        /// <param name="amplitude">音符振幅（音量级别）</param>
        /// <param name="sampleRate">音频采样率</param>
        /// <param name="vibratoDepth">颤音深度（可选，默认0.015）</param>
        /// <param name="vibratoFrequency">颤音频率（可选，默认5.5Hz）</param>
        public CustomMidiVoice(float frequency, float amplitude, int sampleRate, 
                              double vibratoDepth = 0.015, double vibratoFrequency = 5.5)
        {
            _frequency = frequency;
            _amplitude = amplitude;
            _sampleRate = sampleRate;
            _phase = 0;
            _time = 0;
            _isReleasing = false;
            
            // 根据音符频率动态调整颤音参数
            // 高音通常需要更快更浅的颤音，低音需要更慢更深的颤音
            _vibratoFrequency = CalculateDynamicVibratoFrequency(frequency, vibratoFrequency);
            _vibratoDepth = CalculateDynamicVibratoDepth(frequency, amplitude, vibratoDepth);
            
            _vibratoPhase = 0;
            _random = new Random();
            
            // 添加一些随机性来模拟人类演奏的自然变化
            _vibratoFrequencyVariation = 0.05; // 5%的频率变化
        }
        
        /// <summary>
        /// 根据音符频率动态计算颤音频率
        /// 高音通常有更快的颤音
        /// </summary>
        /// <param name="baseFrequency">音符基础频率</param>
        /// <param name="defaultVibratoFreq">默认颤音频率</param>
        /// <returns>调整后的颤音频率</returns>
        private double CalculateDynamicVibratoFrequency(float baseFrequency, double defaultVibratoFreq)
        {
            // 频率映射到MIDI音符
            int midiNote = (int)(Math.Log(baseFrequency / 440.0) / Math.Log(2) * 12 + 69);
            
            // 高音（MIDI音符>72）颤音更快，低音更慢
            if (midiNote > 72)
            {
                return defaultVibratoFreq * 1.2; // 高音快20%
            }
            else if (midiNote < 48)
            {
                return defaultVibratoFreq * 0.8; // 低音慢20%
            }
            
            return defaultVibratoFreq;
        }
        
        /// <summary>
        /// 根据音符频率和力度动态计算颤音深度
        /// 强音和特定音域需要更深的颤音
        /// </summary>
        /// <param name="baseFrequency">音符基础频率</param>
        /// <param name="amplitude">音符振幅</param>
        /// <param name="defaultDepth">默认颤音深度</param>
        /// <returns>调整后的颤音深度</returns>
        private double CalculateDynamicVibratoDepth(float baseFrequency, float amplitude, double defaultDepth)
        {
            double depth = defaultDepth;
            
            // 强音（振幅大）通常有更深的颤音
            depth *= 1.0 + (amplitude * 0.5);
            
            // 中低音域颤音更明显
            if (baseFrequency > 110 && baseFrequency < 440)
            {
                depth *= 1.2; // 中低音域增强20%
            }
            
            // 限制最大深度
            return Math.Min(depth, 0.03);
        }

        /// <summary>
        /// 开始音符的释放阶段
        /// </summary>
        /// <remarks>
        /// 调用此方法后，音符将开始按照ADSR包络的释放阶段淡出。
        /// 如果音符已经在释放阶段，则此方法无效。
        /// </remarks>
        public void StartRelease()
        {
            if (!_isReleasing)
            {
                _isReleasing = true;
                _releaseStartTime = _time;
            }
        }

        // 添加计数器来减少随机数生成频率
        private int _vibratoRandomCountdown = 1000; // 每1000个采样点更新一次随机值
        private double _cachedFrequencyVariation = 1.0;
        
        /// <summary>
        /// 应用颤音效果到当前频率（优化版）
        /// </summary>
        /// <returns>应用颤音后的频率</returns>
        private double ApplyVibrato()
        {
            // 颤音渐入效果
            double vibratoIntensity = 1.0;
            if (_time < VibratoAttackTime)
            {
                // 优化：预先计算除法
                double attackRatio = _time / VibratoAttackTime;
                vibratoIntensity = attackRatio * attackRatio * attackRatio; // 直接使用三次方，避免函数调用
            }
            
            // 优化：减少随机数生成频率，只在计数器归零时更新
            if (--_vibratoRandomCountdown <= 0)
            {
                _vibratoRandomCountdown = 1000; // 重置计数器
                _cachedFrequencyVariation = 1.0 + (_random.NextDouble() - 0.5) * _vibratoFrequencyVariation;
            }
            
            // 预计算常量值以减少重复计算
            double adjustedVibratoFreq = _vibratoFrequency * _cachedFrequencyVariation;
            
            // 更新颤音LFO相位
            double vibratoPhaseIncrement = 2 * Math.PI * adjustedVibratoFreq / _sampleRate;
            _vibratoPhase += vibratoPhaseIncrement;
            
            // 优化：只计算一次正弦值，使用Taylor展开近似二次谐波
            double sinPhase = Math.Sin(_vibratoPhase);
            // 二次谐波近似：sin(2x) ≈ 2sin(x)cos(x)，但为了性能我们使用简化版本
            double lfo = sinPhase * 0.9 + sinPhase * (1 - sinPhase * sinPhase) * 0.2; // 近似二次谐波
            
            // 应用颤音效果到频率
            double vibratoAmount = _vibratoDepth * vibratoIntensity * lfo;
            
            return _frequency * (1.0 + vibratoAmount);
        }
        
        /// <summary>
        /// 缓入三次方函数，用于颤音渐入效果
        /// </summary>
        /// <param name="t">时间进度（0-1）</param>
        /// <returns>缓动后的值</returns>
        private double EaseInCubic(double t)
        {
            return t * t * t;
        }
        
        /// <summary>
        /// 将声音渲染到音频缓冲区
        /// </summary>
        /// <param name="buffer">目标音频缓冲区</param>
        /// <param name="offset">缓冲区中开始写入的偏移量</param>
        /// <param name="count">要渲染的采样总数</param>
        /// <param name="channels">音频通道数</param>
        public void Render(float[] buffer, int offset, int count, int channels)
        {
            var timeIncrement = 1.0 / _sampleRate;

            for (int i = 0; i < count; i += channels)
            {
                // 计算包络
                var envelope = CalculateEnvelope();

                if (envelope > 0)
                {
                    // 应用颤音效果到频率
                    double frequencyWithVibrato = ApplyVibrato();
                    
                    // 计算当前采样的相位增量
                    var phaseIncrement = 2 * Math.PI * frequencyWithVibrato / _sampleRate;
                    
                    // 更新并限制相位
                    _phase += phaseIncrement;
                    if (_phase > 2 * Math.PI) _phase -= 2 * Math.PI;
                    
                    // 生成带颤音的正弦波
                    var sampleValue = (float)(Math.Sin(_phase) * _amplitude * envelope * 0.2f);

                    // 写入所有声道
                    for (int ch = 0; ch < channels; ch++)
                    {
                        buffer[offset + i + ch] += sampleValue;
                    }
                }

                // 更新时间
                _time += timeIncrement;
            }
        }

        /// <summary>
        /// 计算当前时刻的ADSR包络值
        /// </summary>
        /// <returns>当前包络值（0-1范围）</returns>
        private float CalculateEnvelope()
        {
            if (_isReleasing)
            {
                // 释放阶段
                var releaseProgress = (_time - _releaseStartTime) / ReleaseTime;
                if (releaseProgress >= 1.0) return 0.0f;

                // 从当前电平线性衰减到0
                var sustainEnvelope = CalculateSustainEnvelope();
                return sustainEnvelope * (float)(1.0 - releaseProgress);
            }
            else
            {
                // 攻击、衰减、持续阶段
                return CalculateSustainEnvelope();
            }
        }

        /// <summary>
        /// 计算非释放阶段（攻击、衰减、持续）的包络值
        /// </summary>
        /// <returns>当前包络值（0-1范围）</returns>
        private float CalculateSustainEnvelope()
        {
            if (_time < AttackTime)
            {
                // 攻击阶段 - 线性上升
                return (float)(_time / AttackTime);
            }
            else if (_time < AttackTime + DecayTime)
            {
                // 衰减阶段 - 线性下降到持续电平
                var decayProgress = (_time - AttackTime) / DecayTime;
                return (float)(1.0 - (1.0 - SustainLevel) * decayProgress);
            }
            else
            {
                // 持续阶段 - 保持电平
                return (float)SustainLevel;
            }
        }
    }
}
