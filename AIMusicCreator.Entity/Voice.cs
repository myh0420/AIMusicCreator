using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace AIMusicCreator.Entity
{
    /// <summary>
    /// 单个音符声音实例
    /// </summary>
    public partial class Voice
    {
        public int Note { get; set; }
        public double Frequency { get; set; }
        public double Velocity { get; set; }
        public double StartTime { get; set; }
        public double StopTime { get; set; }
        public bool IsActive { get; set; }
        public InstrumentSettings Settings { get; set; } = new InstrumentSettings();
        public double ReleaseTime { get; set; } = -1;
        public bool IsReleased => ReleaseTime >= 0;

        /// <summary>
        /// 生成指定时间的采样值
        /// </summary>
        public float GenerateSample(double time)
        {
            if (time < StartTime) return 0;

            double noteTime = time - StartTime;
            double amplitude = CalculateAmplitude(noteTime);

            if (amplitude <= 0) return 0;

            // 根据音色设置生成波形
            double sample = 0;
            double phase = 2 * Math.PI * Frequency * time;

            switch (Settings.WaveType)
            {
                case WaveType.Sine:
                    sample = Math.Sin(phase);
                    break;
                case WaveType.Square:
                    sample = Math.Sign(Math.Sin(phase));
                    break;
                case WaveType.Sawtooth:
                    sample = 2 * (time * Frequency - Math.Floor(time * Frequency + 0.5)) - 1;
                    break;
                case WaveType.Triangle:
                    sample = 2 * Math.Abs(2 * (time * Frequency - Math.Floor(time * Frequency + 0.5)) - 1);
                    break;
                case WaveType.Composite:
                    sample = GenerateCompositeWave(phase);
                    break;
            }

            return (float)(sample * amplitude * Velocity);
        }

        /// <summary>
        /// 计算ADSR包络
        /// </summary>
        private double CalculateAmplitude(double time)
        {
            double attack = Settings.AttackTime;
            double decay = Settings.DecayTime;
            double sustain = Settings.SustainLevel;
            double release = Settings.ReleaseTime;

            if (StopTime > 0 && time > (StopTime - StartTime))
            {
                double releaseStart = (StopTime - StartTime);
                double releaseTime = time - releaseStart;

                if (releaseTime < release)
                {
                    return sustain * (1.0 - releaseTime / release);
                }
                return 0;
            }

            // 音符播放阶段
            if (time < attack)
            {
                return time / attack;
            }
            else if (time < attack + decay)
            {
                double decayTime = time - attack;
                return 1.0 - (decayTime / decay) * (1.0 - sustain);
            }
            else
            {
                return sustain;
            }
        }

        /// <summary>
        /// 生成复合波形
        /// </summary>
        private double GenerateCompositeWave(double phase)
        {
            double sample = 0;

            foreach (var harmonic in Settings.Harmonics)
            {
                sample += harmonic.Amplitude * Math.Sin(phase * harmonic.FrequencyRatio);
            }

            return sample;
        }

        /// <summary>
        /// 生成指定时间的采样值（带采样率优化）
        /// </summary>
        public float GenerateSample(double time, int sampleRate)
        {
            if (time < StartTime) return 0;

            double noteTime = time - StartTime;
            double amplitude = CalculateAmplitude(noteTime);

            if (amplitude <= 0) return 0;

            // 根据音色设置生成波形
            double sample = 0;
            double phase = 2 * Math.PI * Frequency * time;

            switch (Settings.WaveType)
            {
                case WaveType.Sine:
                    sample = Math.Sin(phase);
                    break;
                case WaveType.Square:
                    sample = Math.Sign(Math.Sin(phase));
                    break;
                case WaveType.Sawtooth:
                    // 使用采样率优化的锯齿波生成
                    sample = GenerateSawtoothOptimized(time, sampleRate);
                    break;
                case WaveType.Triangle:
                    // 使用采样率优化的三角波生成
                    sample = GenerateTriangleOptimized(time, sampleRate);
                    break;
                case WaveType.Composite:
                    sample = GenerateCompositeWave(phase);
                    break;
            }

            return (float)(sample * amplitude * Velocity);
        }

        /// <summary>
        /// 优化的锯齿波生成（避免锯齿状失真）
        /// </summary>
        private double GenerateSawtoothOptimized(double time, int sampleRate)
        {
            // 基于采样率的抗锯齿锯齿波
            double phase = time * Frequency;
            double fractionalPart = phase - Math.Floor(phase);

            // 简单的抗锯齿处理
            double sample = 2.0 * fractionalPart - 1.0;

            // 根据采样率应用低通滤波减少高频噪声
            double cutoff = 0.9 * sampleRate / 2.0;
            if (Frequency < cutoff)
            {
                // 简单的滤波效果
                sample *= Math.Min(1.0, cutoff / (Frequency * 2));
            }

            return sample;
        }

        /// <summary>
        /// 优化的三角波生成
        /// </summary>
        private double GenerateTriangleOptimized(double time)
        {
            double phase = time * Frequency;
            double fractionalPart = phase - Math.Floor(phase);

            double sample;
            if (fractionalPart < 0.5)
            {
                sample = 4.0 * fractionalPart - 1.0;
            }
            else
            {
                sample = 3.0 - 4.0 * fractionalPart;
            }

            return sample;
        }
        /// <summary>
        /// 优化的三角波生成（带抗锯齿）
        /// </summary>
        private double GenerateTriangleOptimized(double time, int sampleRate)
        {
            double phase = time * Frequency;
            double fractionalPart = phase - Math.Floor(phase);

            // 生成基础三角波
            double sample;
            if (fractionalPart < 0.5)
            {
                sample = 4.0 * fractionalPart - 1.0;
            }
            else
            {
                sample = 3.0 - 4.0 * fractionalPart;
            }

            // 基于采样率的抗锯齿处理
            if (sampleRate > 0)
            {
                // 计算奈奎斯特频率
                double nyquist = sampleRate / 2.0;

                // 如果频率接近奈奎斯特频率，应用低通滤波
                if (Frequency > nyquist * 0.4)
                {
                    // 简单的低通滤波效果
                    double filterFactor = 1.0 - (Frequency / nyquist);
                    sample *= Math.Max(0.1, filterFactor);
                }
            }

            return sample;
        }
    }
}
