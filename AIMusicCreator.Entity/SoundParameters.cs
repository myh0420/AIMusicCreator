using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIMusicCreator.Entity
{
    /// <summary>
    /// 音色参数类
    /// </summary>
    public class SoundParameters
    {
        public double AttackTime { get; set; } = 0.05;
        public double DecayTime { get; set; } = 0.2;
        public double SustainLevel { get; set; } = 0.5;
        public double ReleaseTime { get; set; } = 0.25;

        // 谐波含量
        public double Harmonic1 { get; set; } = 1.0;  // 基波
        public double Harmonic2 { get; set; } = 0.5;  // 二次谐波
        public double Harmonic3 { get; set; } = 0.25; // 三次谐波
        public double Harmonic4 { get; set; } = 0.125;
        public double Harmonic5 { get; set; } = 0.0625;
        public double Harmonic6 { get; set; } = 0.03125;
        public double Harmonic7 { get; set; } = 0.015625;

        // 调制效果
        public double VibratoDepth { get; set; } = 0.0;
        public double VibratoSpeed { get; set; } = 5.0;
        public double VibratoDelay { get; set; } = 0.0;
        public double TremoloDepth { get; set; } = 0.0;
        public double TremoloSpeed { get; set; } = 0.0;

        // 滤波器
        public double FilterCutoff { get; set; } = 1.0;
        public double FilterResonance { get; set; } = 0.0;
        public double FilterEnvelopeAmount { get; set; } = 0.0;

        // 其他效果
        public double DetuneAmount { get; set; } = 0.0;
        public double ChorusAmount { get; set; } = 0.0;
        public double PortamentoTime { get; set; } = 0.0;
        public double SlideTime { get; set; } = 0.0;
        public double BreathNoise { get; set; } = 0.0;
        public double SubOscillator { get; set; } = 0.0;
        public double Brightness { get; set; } = 0.5;
    }
}
