using AIMusicCreator.Entity;

namespace AIMusicCreator.ApiService.Services
{
    /// <summary>
    /// 复合波合成器
    /// </summary>
    public class CompositeWaveSynthesizer
    {
        /// <summary>
        /// 复合波合成器，用于生成多个声音的复合波
        /// </summary>
        private readonly List<Voice> _activeVoices = [];
        /// <summary>
        /// 用于同步访问_activeVoices列表的锁对象
        /// </summary>
        private readonly Lock _lockObject = new();

        /// <summary>
        /// 生成复合波样本
        /// </summary>
        /// <param name="time">归一化时间（0-1）</param>
        /// <returns>复合波样本值（-1到1之间）</returns>
        /// <remarks>
        /// 确保：
        /// - 每个活跃声音的样本值被正确混合
        /// - 已完成的声音被从_activeVoices列表中移除
        /// </remarks>
        public double GenerateSample(double time)
        {
            double mixedSample = 0.0;

            lock (_lockObject)
            {
                for (int i = _activeVoices.Count - 1; i >= 0; i--)
                {
                    var voice = _activeVoices[i];
                    var sample = GenerateVoiceSample(voice, time);
                    mixedSample += sample;

                    // 移除已完成的声音
                    if (voice.IsReleased && (time - voice.ReleaseTime) > voice.Settings.ReleaseTime)
                    {
                        _activeVoices.RemoveAt(i);
                    }
                }
            }

            // 限制输出范围避免削波
            return Math.Max(-1.0, Math.Min(1.0, mixedSample));
        }

        /// <summary>
        /// 生成单个声音样本
        /// </summary>
        /// <param name="voice">要生成样本的声音对象</param>
        /// <param name="time">归一化时间（0-1）</param>
        /// <returns>单个声音样本值（-1到1之间）</returns>
        /// <remarks>
        /// 确保：
        /// - 考虑声音的频率、谐波、相位偏移和振幅
        /// - 应用ADSR包络
        /// </remarks>
        private static double GenerateVoiceSample(Voice voice, double time)
        {
            if (voice.Settings?.Harmonics == null || voice.Settings.Harmonics.Count == 0)
                return 0.0;

            double voiceTime = time - voice.StartTime;
            double compositeSample = 0.0;
            double maxAmplitude = 0.0;

            // 生成复合波形
            foreach (var harmonic in voice.Settings.Harmonics)
            {
                double harmonicFrequency = voice.Frequency * harmonic.FrequencyRatio;
                double phase = 2 * Math.PI * harmonicFrequency * voiceTime + harmonic.PhaseOffset;
                double harmonicValue = harmonic.Amplitude * Math.Sin(phase);

                compositeSample += harmonicValue;
                maxAmplitude += Math.Abs(harmonic.Amplitude);
            }

            // 归一化
            if (maxAmplitude > 0)
            {
                compositeSample /= maxAmplitude;
            }

            // 应用包络
            double envelope = CalculateEnvelope(voice, time);
            return compositeSample * envelope;
        }

        /// <summary>
        /// 计算 ADSR 包络
        /// </summary>
        /// <param name="voice">要计算包络的声音对象</param>
        /// <param name="time">归一化时间（0-1）</param>
        /// <returns>ADSR包络值（0到1之间）</returns>
        /// <remarks>
        /// 确保：
        /// - 考虑声音的攻击、衰减、持续和释放时间
        /// - 应用线性插值计算包络值
        /// </remarks>
        private static double CalculateEnvelope(Voice voice, double time)
        {
            double voiceTime = time - voice.StartTime;
            var settings = voice.Settings;

            if (voice.IsReleased)
            {
                double releaseTime = time - voice.ReleaseTime;
                if (releaseTime >= settings.ReleaseTime)
                    return 0.0;

                // 释放阶段：从当前电平线性衰减到0
                double sustainLevel = GetSustainLevel(settings);
                double releaseProgress = releaseTime / settings.ReleaseTime;
                return sustainLevel * (1.0 - releaseProgress);
            }
            else
            {
                // 攻击、衰减、持续阶段
                if (voiceTime < settings.AttackTime)
                {
                    // 攻击阶段
                    return voiceTime / settings.AttackTime;
                }
                else if (voiceTime < settings.AttackTime + settings.DecayTime)
                {
                    // 衰减阶段
                    double decayProgress = (voiceTime - settings.AttackTime) / settings.DecayTime;
                    double sustainLevel = GetSustainLevel(settings);
                    return 1.0 - (1.0 - sustainLevel) * decayProgress;
                }
                else
                {
                    // 持续阶段
                    return GetSustainLevel(settings);
                }
            }
        }
        /// <summary>
        /// safe 获取持续电平
        /// </summary>
        /// <param name="settings"></param>
        /// <returns>持续电平（0到1之间）</returns>
        /// <remarks>
        /// 确保：
        /// - 持续电平在0到1之间
        /// </remarks>
        private static double GetSustainLevel(InstrumentSettings settings)
        {
            return Math.Max(0.0, Math.Min(1.0, settings.SustainLevel));
        }

        /// <summary>
        /// 开始播放声音
        /// </summary>
        /// <param name="frequency">声音频率（赫兹）</param>
        /// <param name="settings">声音设置</param>
        /// <param name="time">归一化时间（0-1）</param>
        /// <remarks>
        /// 确保：
        /// - 新声音对象被添加到_activeVoices列表中
        /// - 声音的起始时间被设置为当前时间
        /// </remarks>
        public void NoteOn(double frequency, InstrumentSettings settings, double time)
        {
            lock (_lockObject)
            {
                _activeVoices.Add(new Voice
                {
                    Frequency = frequency,
                    Settings = settings,
                    StartTime = time
                });
            }
        }

        /// <summary>
        /// 停止播放声音
        /// </summary>
        /// <param name="frequency">要停止播放的声音频率（赫兹）</param>
        /// <param name="time">归一化时间（0-1）</param>
        /// <remarks>
        /// 确保：
        /// - 与给定频率匹配的声音对象被标记为已释放
        /// - 释放时间被设置为当前时间
        /// </remarks>
        public void NoteOff(double frequency, double time)
        {
            lock (_lockObject)
            {
                var voice = _activeVoices.FirstOrDefault(v =>
                    Math.Abs(v.Frequency - frequency) < 0.1 && !v.IsReleased);

                if (voice != null)
                {
                    voice.ReleaseTime = time;
                }
            }
        }
    }
}
