using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIMusicCreator.Entity
{
    /// <summary>
    /// 振荡器类 - 负责生成基础波形
    /// </summary>
    public class Oscillator(WaveType waveType, double frequency, double amplitude)
    {
        public WaveType WaveType { get; set; } = waveType;
        public double Frequency { get; set; } = frequency;
        public double Amplitude { get; set; } = amplitude;
        public double Phase { get; set; }

        public float GenerateSample(double time)
        {
            double phase = 2 * Math.PI * Frequency * time + Phase;

            switch (WaveType)
            {
                case WaveType.Sine:
                    return (float)Math.Sin(phase);
                case WaveType.Square:
                    return (float)Math.Sign(Math.Sin(phase));
                case WaveType.Sawtooth:
                    return (float)(2 * (time * Frequency - Math.Floor(time * Frequency + 0.5)) - 1);
                case WaveType.Triangle:
                    return (float)(2 * Math.Abs(2 * (time * Frequency - Math.Floor(time * Frequency + 0.5)) - 1));
                default:
                    return (float)Math.Sin(phase);
            }
        }
    }
}
