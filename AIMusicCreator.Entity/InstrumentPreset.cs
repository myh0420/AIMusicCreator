using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIMusicCreator.Entity
{
    /// <summary>
    /// 乐器预设管理
    /// </summary>
    /// <remarks>
    /// 此类提供预定义的乐器音色设置集合，用于音频合成器生成各种不同的乐器音色。
    /// 使用MIDI程序号作为索引，提供钢琴、吉他、小提琴、长笛等常见乐器的音色参数配置。
    /// 每个预设包含波形类型、包络参数、谐波结构及颤音设置等关键音色特性参数。
    /// 预设音色遵循GM标准的程序号分配，便于与MIDI协议兼容。
    /// </remarks>
    public static class InstrumentPreset
    {
        /// <summary>
        /// 预设音色字典
        /// </summary>
        /// <remarks>使用MIDI程序号作为键，对应的乐器设置作为值，存储所有预定义的音色配置。</remarks>
        private static readonly Dictionary<int, InstrumentSettings> _presets;

        /// <summary>
        /// 静态构造函数
        /// </summary>
        /// <remarks>初始化预设字典并加载所有预定义的乐器音色配置。</remarks>
        static InstrumentPreset()
        {
            _presets = [];
            InitializePresets();
        }

        /// <summary>
        /// 初始化所有预设音色
        /// </summary>
        /// <remarks>创建并配置所有预定义的乐器音色，包括钢琴、钢弦吉他、小提琴和长笛等。
        /// 每种乐器音色都设置了特定的波形类型、ADSR包络参数、谐波结构和颤音特性，
        /// 以模拟真实乐器的音色特征。</remarks>
        private static void InitializePresets()
        {
            // 钢琴音色 (程序号0)
            _presets[0] = new InstrumentSettings
            {
                Name = "Acoustic Grand Piano",
                Program = 0,
                WaveType = WaveType.Composite,
                AttackTime = 0.01,
                DecayTime = 0.1,
                SustainLevel = 0.5,
                ReleaseTime = 0.2,
                Harmonics =
                [
                    new() { FrequencyRatio = 1, Amplitude = 0.6 },
                    new() { FrequencyRatio = 2, Amplitude = 0.3 },
                    new() { FrequencyRatio = 3, Amplitude = 0.1 }
                ],
                VibratoDepth = 0.02,
                VibratoFrequency = 5.0
            };

            // 钢弦吉他音色 (程序号25)
            _presets[25] = new InstrumentSettings
            {
                Name = "Steel String Guitar",
                Program = 25,
                WaveType = WaveType.Composite,
                AttackTime = 0.05,
                DecayTime = 0.2,
                SustainLevel = 0.4,
                ReleaseTime = 0.3,
                Harmonics =
                [
                    new() { FrequencyRatio = 1, Amplitude = 0.5 },
                    new() { FrequencyRatio = 2, Amplitude = 0.25 },
                    new() { FrequencyRatio = 3, Amplitude = 0.15 },
                    new() { FrequencyRatio = 4, Amplitude = 0.1 }
                ],
                VibratoDepth = 0.01,
                VibratoFrequency = 3.0
            };

            // 小提琴音色 (程序号41)
            _presets[41] = new InstrumentSettings
            {
                Name = "Violin",
                Program = 41,
                WaveType = WaveType.Sine,
                AttackTime = 0.1,
                DecayTime = 0.15,
                SustainLevel = 0.7,
                ReleaseTime = 0.25,
                Harmonics =
                [
                    new() { FrequencyRatio = 1, Amplitude = 1.0 }
                ],
                VibratoDepth = 0.03,
                VibratoFrequency = 6.0
            };

            // 长笛音色 (程序号73)
            _presets[73] = new InstrumentSettings
            {
                Name = "Flute",
                Program = 73,
                WaveType = WaveType.Sine,
                AttackTime = 0.08,
                DecayTime = 0.12,
                SustainLevel = 0.8,
                ReleaseTime = 0.15,
                Harmonics =
                    [
                        new () { FrequencyRatio = 1, Amplitude = 1.0 }
                    ],
                VibratoDepth = 0.015,
                VibratoFrequency = 4.0
            };
        }

        /// <summary>
        /// 获取指定程序号的音色预设
        /// </summary>
        /// <param name="program">MIDI程序号，用于标识特定的乐器音色</param>
        /// <returns>对应程序号的乐器设置，如果不存在则返回默认设置</returns>
        /// <remarks>根据MIDI程序号检索预定义的乐器音色配置。
        /// 如果请求的程序号没有对应的预设，则返回一个通用的默认乐器设置，
        /// 确保系统总能返回有效的音色参数，不会因音色不存在而导致合成失败。</remarks>
        public static InstrumentSettings GetPreset(int program)
        {
            if (_presets.TryGetValue(program, out var settings))
            {
                return settings;
            }

            // 默认音色
            return new InstrumentSettings
            {
                Name = "Default Instrument",
                Program = program,
                WaveType = WaveType.Composite,
                AttackTime = 0.05,
                DecayTime = 0.15,
                SustainLevel = 0.6,
                ReleaseTime = 0.2,
                Harmonics =
                [
                    new() { FrequencyRatio = 1, Amplitude = 0.8 },
                    new() { FrequencyRatio = 2, Amplitude = 0.15 },
                    new() { FrequencyRatio = 3, Amplitude = 0.05 }
                ],
                VibratoDepth = 0.02,
                VibratoFrequency = 5.0
            };
        }
    }
}
