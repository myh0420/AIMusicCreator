using AIMusicCreator.Entity;
using System.Security.AccessControl;

namespace AIMusicCreator.ApiService.Services
{
    /// <summary>
    /// MIDI音频合成器核心类
    /// </summary>
    public class MidiSynthesizer
    {
        /// <summary>
        /// 音频上下文
        /// </summary>
        private AudioContext _audioContext;
        /// <summary>
        /// 活跃的音色
        /// </summary>
        private List<Voice> _activeVoices;
        /// <summary>
        /// 当前音色设置
        /// </summary>
        private InstrumentSettings? _currentSettings;
        /// <summary>
        /// 波发生器
        /// </summary>
        private WaveGenerator _waveGenerator;
        /// <summary>
        /// 采样率（Hz）
        /// </summary>
        private readonly int _sampleRate = 44100; // 添加采样率字段
        /// <summary>
        /// 构造函数
        /// </summary>
        public MidiSynthesizer()
        {
            _audioContext = new();
            _activeVoices = [];
            _waveGenerator = new();
            InitializeDefaultSettings();
        }

        /// <summary>
        /// 初始化默认音色设置
        /// </summary>
        /// /// <remarks>
        /// 音色设置包括：
        /// - 音色名称
        /// - MIDI程序号
        /// - 波类型（合成/方波/三角波/Sawtooth）
        /// - 攻击时间（秒）
        /// - 衰减时间（秒）
        /// -  sustai  保持级别（0-1）
        /// - 释放时间（秒）
        /// - 谐波系数（频率比和振幅）
        /// - 颤音深度（0-1）
        /// - 颤音频率（Hz）
        /// </remarks>
        private void InitializeDefaultSettings()
        {
            _currentSettings = new InstrumentSettings
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
                    new () { FrequencyRatio = 1, Amplitude = 0.6 },
                    new () { FrequencyRatio = 2, Amplitude = 0.3 },
                    new () { FrequencyRatio = 3, Amplitude = 0.1 }
                ],
                VibratoDepth = 0.02,
                VibratoFrequency = 5.0
            };
        }

        /// <summary>
        /// 处理MIDI音符开始事件
        /// </summary>
        /// <param name="note">MIDI音符号（0-127）</param>
        /// <param name="velocity">音符力度（0-127）</param>
        /// <remarks>
        /// 当音符力度为0时，视为音符结束事件。
        /// 其他情况下，创建一个新的音色对象，设置音符、频率、力度、开始时间和当前音色设置。
        /// 最后，将音色对象添加到活跃音色列表中，并生成音符的音频数据。
        /// </remarks>
        public void NoteOn(int note, int velocity)
        {
            var voice = new Voice
            {
                Note = note,
                Frequency = MidiNoteToFrequency(note),
                Velocity = velocity / 127.0,
                StartTime = _audioContext.CurrentTime,
                Settings = _currentSettings!
            };

            _activeVoices.Add(voice);
            _waveGenerator.GenerateNote(voice);
        }

        /// <summary>
        /// 处理MIDI音符结束事件
        /// </summary>
        /// <param name="note">MIDI音符号（0-127）</param>
        /// <remarks>
        /// 查找活跃音色列表中与给定音符匹配的第一个音色对象。
        /// 如果找到，设置其停止时间为当前音频上下文时间，并将其标记为非活跃状态。
        /// 最后，应用释放包络到音色对象，生成音符的释放音频数据。
        /// </remarks>
        public void NoteOff(int note)
        {
            var voice = _activeVoices.FirstOrDefault(v => v.Note == note && v.IsActive);
            if (voice != null)
            {
                voice.StopTime = _audioContext.CurrentTime;
                voice.IsActive = false;

                // 应用释音包络
                _waveGenerator.ApplyReleaseEnvelope(voice);
            }
        }

        /// <summary>
        /// 切换音色程序
        /// </summary>
        /// <param name="program">MIDI程序号（0-127）</param>
        /// <remarks>
        /// 查找音色预设中与给定程序号匹配的音色设置。
        /// 如果找到，将当前音色设置更新为该预设，并更新所有活跃音色的设置。
        /// </remarks>
        public void ChangeProgram(int program)
        {
            _currentSettings = InstrumentPreset.GetPreset(program);
            UpdateAllVoicesSettings();
        }

        /// <summary>
        /// MIDI音符转频率
        /// </summary>
        /// <param name="note">MIDI音符号（0-127）</param>
        /// <returns>对应频率（Hz）</returns>
        /// <remarks>
        /// 使用公式：f = 440 * 2^(n-69) / 12
        /// 其中，n 为 MIDI 音符号，440 为 A4 音符的频率（Hz）
        /// </remarks>
        private double MidiNoteToFrequency(int note)
        {
            return 440.0 * Math.Pow(2, (note - 69) / 12.0);
        }

        /// <summary>
        /// 更新所有活跃音符的音色设置
        /// </summary>
        /// <remarks>
        /// 遍历所有活跃音色对象，将其音色设置更新为当前音色设置。
        /// </remarks>
        private void UpdateAllVoicesSettings()
        {
            foreach (var voice in _activeVoices.Where(v => v.IsActive))
            {
                voice.Settings = _currentSettings!;
            }
        }

        /// <summary>
        /// 渲染音频数据
        /// </summary>
        /// <param name="sampleRate">采样率（Hz）</param>
        /// <param name="durationSeconds">音频时长（秒）</param>
        /// <returns>音频样本数组</returns>
        /// <remarks>
        /// 生成指定时长的音频样本数组，每个样本点为 [-1, 1] 范围内的浮点数。
        /// 遍历每个样本点，根据当前时间调用 GenerateSample 方法生成音频样本。
        /// 最后，将所有样本点归一化到 [-1, 1] 范围内。
        /// </remarks>
        public float[] RenderAudio(int sampleRate, int durationSeconds)
        {
            int totalSamples = sampleRate * durationSeconds;
            var buffer = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                double time = i / (double)sampleRate;
                buffer[i] = GenerateSample(time);
            }

            return buffer;
        }

        /// <summary>
        /// 生成单个采样点 - 修复版本
        /// </summary>
        /// <param name="time">当前时间（秒）</param>
        /// <returns>音频样本值（[-1, 1] 范围内的浮点数）</returns>
        /// <remarks>
        /// 遍历所有活跃音色对象，累加每个音色对象生成的样本值。
        /// 最后，将累加值归一化到 [-1, 1] 范围内。
        /// </remarks>
        private float GenerateSample(double time)
        {
            float sample = 0;

            foreach (var voice in _activeVoices)
            {
                sample += voice.GenerateSample(time, _sampleRate);
            }

            return Math.Clamp(sample, -1f, 1f);
        }
    }
}
