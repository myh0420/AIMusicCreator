using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIMusicCreator.Entity
{
    /// <summary>
    /// 音色预设
    /// </summary>
    public static class VoicePresets
    {
        /// <summary>
        /// 正弦波（纯音）
        /// </summary>
        public static InstrumentSettings SineWave => new InstrumentSettings
        {
            Name = "Sine Wave",
            Harmonics =
            [
                new() { FrequencyRatio = 1.0, Amplitude = 1.0 }
            ]
        };

        /// <summary>
        /// 方波（奇次谐波）
        /// </summary>
        public static InstrumentSettings SquareWave => new InstrumentSettings
        {
            Name = "Square Wave",
            Harmonics =
            [
                new() { FrequencyRatio = 1.0, Amplitude = 1.0 },
                new() { FrequencyRatio = 3.0, Amplitude = 1.0/3 },
                new() { FrequencyRatio = 5.0, Amplitude = 1.0/5 },
                new() { FrequencyRatio = 7.0, Amplitude = 1.0/7 },
                new() { FrequencyRatio = 9.0, Amplitude = 1.0/9 }
            ]
        };

        /// <summary>
        /// 锯齿波（所有谐波）
        /// </summary>
        public static InstrumentSettings SawtoothWave => new InstrumentSettings
        {
            Name = "Sawtooth Wave",
            Harmonics =
            [
                new() { FrequencyRatio = 1.0, Amplitude = 1.0 },
                new() { FrequencyRatio = 2.0, Amplitude = 1.0/2 },
                new() { FrequencyRatio = 3.0, Amplitude = 1.0/3 },
                new() { FrequencyRatio = 4.0, Amplitude = 1.0/4 },
                new() { FrequencyRatio = 5.0, Amplitude = 1.0/5 },
                new() { FrequencyRatio = 6.0, Amplitude = 1.0/6 }
            ]
        };

        /// <summary>
        /// 三角波（奇次谐波，幅度平方反比）
        /// </summary>
        public static InstrumentSettings TriangleWave => new InstrumentSettings
        {
            Name = "Triangle Wave",
            Harmonics =
            [
                new() { FrequencyRatio = 1.0, Amplitude = 1.0 },
                new() { FrequencyRatio = 3.0, Amplitude = 1.0/9 },
                new() { FrequencyRatio = 5.0, Amplitude = 1.0/25 },
                new() { FrequencyRatio = 7.0, Amplitude = 1.0/49 },
                new() { FrequencyRatio = 9.0, Amplitude = 1.0/81 }
            ]
        };
    }
}
