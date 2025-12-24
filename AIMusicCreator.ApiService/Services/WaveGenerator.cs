using AIMusicCreator.Entity;
using Microsoft.Extensions.Logging;

namespace AIMusicCreator.ApiService.Services
{
    /// <summary>
    /// 高级波形生成器，用于生成各种音频波形并应用专业音效处理
    /// </summary>
    /// <remarks>
    /// 实现了多种波形生成算法（正弦波、方波、三角波等）和音频效果处理（颤音、混响、包络等）。
    /// 支持精确的音频合成和专业级混响效果处理，可用于音乐创作、音效设计等场景。
    /// </remarks>
    public partial class WaveGenerator
    {
        /// <summary>
        /// 振荡器字典 - 存储不同频率的振荡器实例
        /// </summary>
        /// <remarks>
        /// 键为频率（赫兹），值为对应的振荡器实例。
        /// 每个频率对应一个独立的振荡器，用于生成不同频率的音频波形。
        /// </remarks>
        private readonly Dictionary<int, Oscillator> _oscillators;
        /// <summary>
        /// 效果处理器列表 - 存储已添加的音频效果处理器
        /// </summary>
        /// <remarks>
        /// 用于按顺序应用音频效果，如颤音、混响、包络等。
        /// 每个处理器都实现了 <see cref="IEffectProcessor"/> 接口，允许灵活扩展和定制。
        /// </remarks>
        private readonly List<EffectProcessor> _effects;
        /// <summary>
        /// 随机数生成器 - 用于生成随机值，如颤音效果中的随机相位偏移
        /// </summary>
        private readonly Random _random;
        
        /// <summary>
        /// 日志记录器
        /// </summary>
        private readonly ILogger<WaveGenerator>? _logger;
        
        /// <summary>
        /// 混响效果配置类 - 提供专业级混响效果的参数控制
        /// </summary>
        /// <remarks>
        /// 用于配置混响效果的各项参数，包括延迟时间、衰减因子、混响强度等。
        /// 可以创建不同的配置来模拟不同大小和特性的空间声学效果。
        /// </remarks>
        public class ReverbConfig
        {
            /// <summary>
            /// 获取或设置延迟时间数组（秒）
            /// </summary>
            /// <remarks>
            /// 定义混响效果中的多个延迟线时间，通常使用质数或互质的时间值以避免频率抵消。
            /// 推荐值范围：0.02-0.1秒。
            /// </remarks>
            public double[] DelayTimes { get; set; } = [0.03, 0.05, 0.07];
            
            /// <summary>
            /// 获取或设置衰减因子数组
            /// </summary>
            /// <remarks>
            /// 对应每个延迟线的衰减强度，值越小衰减越快。
            /// 推荐值范围：0.2-0.8，各值应有差异以创造丰富的混响尾部。
            /// </remarks>
            public double[] DecayFactors { get; set; } = [0.6, 0.4, 0.2];
            
            /// <summary>
            /// 是否启用预延迟
            /// </summary>
            public bool EnablePredelay { get; set; } = false;
            
            /// <summary>
            /// 预延迟时间（秒）
            /// </summary>
            /// <remarks>
            /// 控制预延迟的时长，推荐值范围：0.005-0.03秒。
            /// 较大的值创造更明显的空间感，较小的值使混响更紧密。
            /// </remarks>
            public double PredelayTime { get; set; } = 0.025;
            
            /// <summary>
            /// 获取或设置混响强度
            /// </summary>
            /// <value>范围0.0-1.0，0表示关闭混响，1表示最大混响</value>
            /// <remarks>
            /// 控制混响效果的干湿比例，影响混响的明显程度。
            /// 较小的值(0.1-0.3)适合保持清晰度，较大的值(0.4-0.8)创造空间感。
            /// </remarks>
            public double ReverbAmount { get; set; } = 0.3;
            
            /// <summary>
            /// 创建适合小型房间的混响配置
            /// </summary>
            /// <returns>预配置的ReverbConfig实例</returns>
            public static ReverbConfig CreateSmallRoom()
            {
                return new ReverbConfig
                {
                    DelayTimes = [0.02, 0.03, 0.04],
                    DecayFactors = [0.5, 0.3, 0.1],
                    ReverbAmount = 0.2,
                    EnablePredelay = true,
                    PredelayTime = 0.01
                };
            }
            
            /// <summary>
            /// 创建适合大型厅堂的混响配置
            /// </summary>
            /// <returns>预配置的ReverbConfig实例</returns>
            public static ReverbConfig CreateLargeHall()
            {
                return new ReverbConfig
                {
                    DelayTimes = [0.04, 0.06, 0.08],
                    DecayFactors = [0.7, 0.5, 0.3],
                    ReverbAmount = 0.5,
                    EnablePredelay = true,
                    PredelayTime = 0.02
                };
            }
        }
        

        
        /// <summary>
        /// 最大延迟样本数 - 用于混响效果
        /// </summary>
        /// <remarks>设置为5秒的音频长度（基于44.1kHz采样率）</remarks>
        private const int MAX_DELAY_SAMPLES = 44100 * 5; // 5秒延迟
        
        /// <summary>
        /// 延迟线环形缓冲区 - 实现高效的混响算法
        /// </summary>
        /// <remarks>使用固定大小数组而非动态集合，提高性能并减少内存碎片</remarks>
        private double[] _delayLineArray = new double[MAX_DELAY_SAMPLES];
        
        /// <summary>
        /// 写入索引 - 指向环形缓冲区中下一个写入位置
        /// </summary>
        private int _delayLineWriteIndex = 0;
        
        /// <summary>
        /// 读取索引 - 指向环形缓冲区中最旧样本的位置
        /// </summary>
        private int _delayLineReadIndex = 0;
        
        /// <summary>
        /// 当前缓冲区内样本数量 - 跟踪实际使用的样本数量
        /// </summary>
        private int _delayLineSize = 0;

        /// <summary>
        /// 初始化WaveGenerator实例
        /// </summary>
        /// <param name="logger">日志记录器（可选）</param>
        /// <remarks>
        /// 提供可选的日志记录器以支持详细的操作日志，便于调试和监控。
        /// </remarks>
        public WaveGenerator(ILogger<WaveGenerator>? logger = null)
        {
            _oscillators = new Dictionary<int, Oscillator>();
            _effects = new List<EffectProcessor>();
            _random = new Random();
            _logger = logger;
            
            _logger?.LogInformation("WaveGenerator初始化完成，最大延迟: {MaxDelayMs}ms, 支持采样率: 44100Hz", 
                MAX_DELAY_SAMPLES * 1000 / 44100);
        }

        /// <summary>
        /// 为指定音符生成波形
        /// </summary>
        /// <param name="voice">要生成波形的声音参数</param>
        /// /// <exception cref="ArgumentNullException">当voice为null时抛出</exception>
        /// /// <exception cref="ArgumentException">当voice.Settings.Harmonics为null时抛出</exception>
        /// <remarks>
        /// 此方法根据声音参数中的音色类型和谐波设置，生成对应的波形。
        /// 支持的音色类型包括正弦波、方波、锯齿波、三角波和复合波。
        /// /// <code>
        /// // 示例用法
        /// var voice = new Voice() { ... };
        /// waveGenerator.GenerateNote(voice);
        /// </code>
        /// </remarks>
        public void GenerateNote(Voice voice)
        {
            ArgumentNullException.ThrowIfNull(voice);

            // 根据音色设置生成相应的波形
            switch (voice.Settings.WaveType)
            {
                /// <summary>
                /// 生成正弦波
                /// </summary>
                case WaveType.Sine:
                    GenerateSineWave(voice);
                    break;
                /// <summary>
                /// 生成方波
                /// </summary>
                case WaveType.Square:
                    GenerateSquareWave(voice);
                    break;
                case WaveType.Sawtooth:
                /// <summary>
                /// 生成锯齿波
                /// </summary>
                    GenerateSawtoothWave(voice);
                    break;
                /// <summary>
                /// 生成三角波
                /// </summary>
                case WaveType.Triangle:
                    GenerateTriangleWave(voice);
                    break;
                /// <summary>
                /// 生成复合波
                /// </summary>
                case WaveType.Composite:
                    GenerateCompositeWave(voice);
                    break;
                    /// <summary>
                    /// 生成默认波形（正弦波）
                    /// </summary>
                default:
                    GenerateSineWave(voice);
                    break;
            }
        }

        /// <summary>
        /// 生成正弦波
        /// </summary>
        /// <param name="voice">要生成波形的声音参数</param>
        /// /// <exception cref="ArgumentNullException">当voice为null时抛出</exception>
        /// /// <exception cref="ArgumentException">当voice.Settings.Harmonics为null时抛出</exception>
        /// <remarks>
        /// 正弦波是一种周期性的波形，由一系列谐波组成，每个谐波的振幅按比例递增。
        /// 正弦波的频率是基频的倍数，相位交替。
        /// /// <code>
        /// // 示例用法
        /// var voice = new Voice() { ... };
        /// waveGenerator.GenerateSineWave(voice);
        /// </code>
        /// </remarks>
        private void GenerateSineWave(Voice voice)
        {
            // 正弦波生成逻辑
            //double phase = 0;
            //double phaseIncrement = 2 * Math.PI * voice.Frequency / 44100; // 假设采样率为44100

            // 应用ADSR包络
            ApplyADSREnvelope(voice);

            // 添加颤音效果
            if (voice.Settings.VibratoDepth > 0)
            {
                ApplyVibrato(voice);
            }
        }

        /// <summary>
        /// 生成方波
        /// </summary>
        /// <param name="voice">要生成波形的声音参数</param>
        /// /// <exception cref="ArgumentNullException">当voice为null时抛出</exception>
        /// /// <exception cref="ArgumentException">当voice.Settings.Harmonics为null时抛出</exception>
        /// <remarks>
        /// 方波是一种周期性的波形，由一系列谐波组成，每个谐波的振幅按比例递增。
        /// 方波的频率是基频的倍数，相位交替。
        /// /// <code>
        /// // 示例用法
        /// var voice = new Voice() { ... };
        /// waveGenerator.GenerateSquareWave(voice);
        /// </code>
        /// </remarks>
        private static void GenerateSquareWave(Voice voice)
        {
            // 方波生成逻辑 - 奇次谐波
            var harmonics = new List<Harmonic>();

            for (int i = 1; i <= 9; i += 2)
            {
                harmonics.Add(new Harmonic
                {
                    FrequencyRatio = i,
                    Amplitude = 1.0 / i
                });
            }

            voice.Settings.Harmonics = harmonics;
        }

        
        /// <summary>
        /// 生成锯齿波
        /// </summary>
        /// <param name="voice">要生成波形的声音参数</param>
        /// /// <exception cref="ArgumentNullException">当voice为null时抛出</exception>
        /// /// <exception cref="ArgumentException">当voice.Settings.Harmonics为null时抛出</exception>
        /// <remarks>
        /// 锯齿波是一种周期性的波形，由一系列谐波组成，每个谐波的振幅按比例递增。
        /// 锯齿波的频率是基频的倍数，相位交替。
        /// /// <code>
        /// // 示例用法
        /// var voice = new Voice() { ... };
        /// waveGenerator.GenerateSawtoothWave(voice);
        /// </code>
        /// </remarks>
        private static void GenerateSawtoothWave(Voice voice)
        {
            // 锯齿波生成逻辑 - 所有谐波
            var harmonics = new List<Harmonic>();

            for (int i = 1; i <= 10; i++)
            {
                harmonics.Add(new Harmonic
                {
                    FrequencyRatio = i,
                    Amplitude = 1.0 / i
                });
            }

            voice.Settings.Harmonics = harmonics;
        }

        /// <summary>
        /// 生成三角波
        /// </summary>
        /// <param name="voice">要生成波形的声音参数</param>
        /// /// <exception cref="ArgumentNullException">当voice为null时抛出</exception>
        /// /// <exception cref="ArgumentException">当voice.Settings.Harmonics为null时抛出</exception>
        /// <remarks>
        /// 三角波是一种周期性的波形，由一系列谐波组成，每个谐波的振幅按比例递减。
        /// 三角波的频率是基频的倍数，相位交替。
        /// /// <code>
        /// // 示例用法
        /// var voice = new Voice() { ... };
        /// waveGenerator.GenerateTriangleWave(voice);
        /// </code>
        /// </remarks>
        private static void GenerateTriangleWave(Voice voice)
        {
            // 三角波生成逻辑 - 奇次谐波，相位交替
            var harmonics = new List<Harmonic>();

            for (int i = 1; i <= 9; i += 2)
            {
                double amplitude = 1.0 / (i * i);
                harmonics.Add(new Harmonic
                {
                    FrequencyRatio = i,
                    Amplitude = (i % 4 == 1) ? amplitude : -amplitude
                });
            }

            voice.Settings.Harmonics = harmonics;
        }

        
        /// <summary>
        /// 生成复合波
        /// </summary>
        /// <param name="voice">要生成波形的声音参数</param>
        /// <exception cref="ArgumentNullException">当voice为null时抛出</exception>
        /// <exception cref="ArgumentException">当voice.Settings.Harmonics为null时抛出</exception>
        /// <remarks>
        /// 复合波是通过将多个谐波叠加生成的波形。每个谐波都有自己的频率和振幅，
        /// 可以根据需要调整谐波配置以生成不同的音色。
        /// /// <code>
        /// // 示例用法
        /// var voice = new Voice() { ... };
        /// waveGenerator.GenerateCompositeWave(voice);
        /// </code>
        /// </remarks>
        
        private void GenerateCompositeWave(Voice voice)
        {
            if (voice == null || voice.Settings?.Harmonics == null) return;

            // 使用预设的谐波配置生成复合波形
            double compositeSample = 0;
            //double phase = 0;

            foreach (var harmonic in voice.Settings.Harmonics)
            {
                double harmonicPhase = 2 * Math.PI * voice.Frequency * harmonic.FrequencyRatio;
                compositeSample += harmonic.Amplitude * Math.Sin(harmonicPhase);
            }

            // 归一化复合波
            double maxAmplitude = voice.Settings.Harmonics.Sum(h => h.Amplitude);
            if (maxAmplitude > 0)
            {
                compositeSample /= maxAmplitude;
            }

            _logger?.LogDebug("生成复合波形: {SettingsName}, 频率: {Frequency:F2} Hz, 谐波数: {HarmonicsCount}", voice.Settings.Name, voice.Frequency, voice.Settings.Harmonics.Count);
        }
        
        /// <summary>
        /// 应用ADSR包络
        /// </summary>
        /// <param name="voice">要应用ADSR包络的声音参数</param>
        /// <exception cref="ArgumentNullException">当voice为null时抛出</exception>
        /// <exception cref="ArgumentException">
        /// 当voice.Settings.AttackTime、voice.Settings.DecayTime、
        /// voice.Settings.SustainLevel或voice.Settings.ReleaseTime无效时抛出
        /// </exception>
        /// <remarks>
        /// 实现ADSR包络算法，处理音符开始到结束的音量变化。
        /// 攻击阶段线性增长，衰减阶段线性衰减，持续阶段保持在持续水平，
        /// 释放阶段指数衰减。
        /// 
        /// <code>
        /// // 示例用法
        /// var voice = new Voice() { ... };
        /// waveGenerator.ApplyADSREnvelope(voice);
        /// </code>
        /// </remarks>
        private void ApplyADSREnvelope(Voice voice)
        {
            if (voice == null) return;

            // 获取ADSR参数
            double attack = voice.Settings.AttackTime;
            double decay = voice.Settings.DecayTime;
            double sustain = voice.Settings.SustainLevel;
            double release = voice.Settings.ReleaseTime;

            // 计算当前时间相对于音符开始的时间
            double currentTime = voice.StartTime;
            double noteDuration = currentTime - voice.StartTime;

            // ADSR包络计算逻辑
            double envelopeValue;
            if (noteDuration < attack)
            {
                // 起音阶段：线性增长到最大值
                envelopeValue = noteDuration / attack;
            }
            else if (noteDuration < attack + decay)
            {
                // 衰减阶段：线性衰减到持续水平
                double decayProgress = (noteDuration - attack) / decay;
                envelopeValue = 1.0 - decayProgress * (1.0 - sustain);
            }
            else
            {
                // 持续阶段：保持在持续水平
                envelopeValue = sustain;
            }

            // 应用包络值到音符音量
            voice.Velocity = Math.Clamp(envelopeValue, 0.0, 1.0);

            // 包络状态管理
            voice.IsActive = true;

            // 记录包络状态用于调试
            _logger?.LogTrace("ADSR包络应用: 音符{Note}, 时间{NoteDuration:F3}s, 包络值{EnvelopeValue:F3}", voice.Note, noteDuration, envelopeValue);
        }
        
        /// <summary>
        /// 应用释音包络 - 修复版本
        /// </summary>
        /// <param name="voice">要应用释音包络的声音参数</param>
        /// <exception cref="ArgumentNullException">当voice为null时抛出</exception>
        /// <exception cref="ArgumentException">
        /// 当voice.Settings.ReleaseTime无效时抛出
        /// </exception>
        /// <remarks>
        /// 实现指数衰减算法，处理音符结束时的音量衰减。
        /// 衰减速率由decayRate参数控制，默认值为0.99。
        /// 
        /// <code>
        /// // 示例用法
        /// var voice = new Voice() { ... };
        /// waveGenerator.ApplyReleaseEnvelope(voice);
        /// </code>
        /// </remarks>
        public void ApplyReleaseEnvelope(Voice voice)
        {
            if (voice == null) return;

            // 计算释音阶段的包络衰减
            double releaseDuration = voice.Settings.ReleaseTime;
            double releaseProgress = 0;
            double initialAmplitude = voice.Velocity;

            // 实现指数衰减
            while (releaseProgress < releaseDuration && initialAmplitude > 0.001)
            {
                releaseProgress += 1.0 / 44100;
                double decayFactor = Math.Exp(-releaseProgress * 5);
                voice.Velocity = initialAmplitude * decayFactor;
            }

            // 设置音符为非活跃状态
            voice.IsActive = false;

            // 应用释音效果
            ApplyReleaseEffect(voice);
        }
        /// <summary>
        /// 应用颤音效果 - 通过频率调制产生音高波动
        /// </summary>
        /// <param name="voice">要应用颤音效果的声音参数</param>
        /// <exception cref="ArgumentNullException">当voice为null时抛出</exception>
        /// <exception cref="ArgumentException">
        /// 当voice.Settings.VibratoDepth或voice.Settings.VibratoFrequency无效时抛出
        /// </exception>
        /// <remarks>
        /// 实现颤音效果，通过频率调制产生音高波动。
        /// 深度参数控制波动范围，频率参数控制波动速度。
        /// 
        /// <code>
        /// // 示例用法
        /// var voice = new Voice() { ... };
        /// waveGenerator.ApplyVibrato(voice);
        /// </code>
        /// </remarks>
        private void ApplyVibrato(Voice voice)
        {
            if (voice == null) return;

            // 使用低频振荡器(LFO)调制频率
            double vibratoDepth = voice.Settings.VibratoDepth;
            double vibratoFrequency = voice.Settings.VibratoFrequency;

            // 颤音实现逻辑
            double modulation = Math.Sin(2 * Math.PI * vibratoFrequency * voice.StartTime);
            double frequencyModulation = voice.Frequency * (1 + vibratoDepth * modulation);

            // 更新频率值
            voice.Frequency = frequencyModulation;
        }

        /// <summary>
        /// 应用释放效果到音符 - 默认版本
        /// </summary>
        /// <param name="voice">要应用释放效果的声音参数</param>
        /// <exception cref="ArgumentNullException">当voice参数为null时抛出</exception>
        /// <exception cref="ArgumentException">当voice参数无效时抛出</exception>
        /// <remarks>
        /// 处理音符结束时的自然释放过程，包括音量衰减和混响效果。
        /// 使用默认的释放时间参数。
        /// </remarks>
        private void ApplyReleaseEffect(Voice voice)
        {
            ApplyReleaseEffect(voice, null);
        }
        
        /// <summary>
        /// 应用释放效果到音符 - 可配置版本
        /// </summary>
        /// <param name="voice">要应用释放效果的声音参数</param>
        /// <param name="reverbConfig">混响效果配置（可选）</param>
        /// <exception cref="ArgumentNullException">当voice或voice.Settings为null时抛出</exception>
        /// <exception cref="ArgumentException">
        /// 当voice.Settings.ReleaseTime为负数时抛出
        /// </exception>
        /// <remarks>
        /// 处理音符结束时的自然释放过程，包括音量衰减和混响效果。
        /// 可自定义混响配置参数，控制音符衰减的空间特性。
        /// 
        /// <code>
        /// // 示例用法
        /// var voice = new Voice() { ... };
        /// var reverbConfig = ReverbConfig.CreateLargeHall();
        /// waveGenerator.ApplyReleaseEffect(voice, reverbConfig);
        /// </code>
        /// </remarks>
        private void ApplyReleaseEffect(Voice voice, ReverbConfig? reverbConfig)
        {
            // 严格的参数验证
            ArgumentNullException.ThrowIfNull(voice, nameof(voice));
            ArgumentNullException.ThrowIfNull(voice.Settings, $"{nameof(voice)}.{nameof(voice.Settings)}");

            // 验证参数的有效性
            if (voice.Settings.ReleaseTime < 0)
            {
                throw new ArgumentException("释音时间不能为负数", $"{nameof(voice)}.{nameof(voice.Settings.ReleaseTime)}");
            }

            // 如果没有提供自定义配置，则使用默认配置
            reverbConfig ??= new ReverbConfig();
            
            // 根据释音时间调整混响强度
            reverbConfig.ReverbAmount = Math.Min(1.0, reverbConfig.ReverbAmount * 
                (1.0 + voice.Settings.ReleaseTime * 0.5)); // 释音时间越长，混响越强

            // 释音效果处理
            ApplyReverbEffect(voice, reverbConfig);
            _logger?.LogInformation("应用释音效果到音符: {Note}, 释音时间: {ReleaseTime:F2}s, 混响强度: {ReverbAmount:F2}", 
                voice.Note, voice.Settings.ReleaseTime, reverbConfig.ReverbAmount);
        }
        /// <summary>
        /// 应用混响效果 - 默认版本
        /// </summary>
        /// <param name="voice">要应用混响效果的声音参数</param>
        /// <exception cref="ArgumentNullException">当voice为null时抛出</exception>
        /// <exception cref="ArgumentException">
        /// 当voice.Velocity不在0到1之间时抛出
        /// </exception>
        /// <remarks>
        /// 实现基于延迟和衰减的混响效果，使用默认配置参数。
        /// 为音符添加空间感，增强听感。
        /// 
        /// <code>
        /// // 示例用法
        /// var voice = new Voice() { ... };
        /// waveGenerator.ApplyReverbEffect(voice);
        /// </code>
        /// </remarks>
        private void ApplyReverbEffect(Voice voice)
        {
            // 严格的参数验证
            ArgumentNullException.ThrowIfNull(voice, nameof(voice));
            
            // 验证参数的有效性
            if (voice.Velocity < 0 || voice.Velocity > 1.0)
            {
                throw new ArgumentException("声音速度值必须在0到1之间", $"{nameof(voice)}.{nameof(voice.Velocity)}");
            }
            
            // 创建默认配置并调用优化后的可配置版本
            var defaultConfig = new ReverbConfig
            {
                DelayTimes = [0.03, 0.05, 0.07],
                DecayFactors = [0.6, 0.4, 0.2],
                ReverbAmount = 0.3,
                EnablePredelay = true,
                PredelayTime = 0.01
            };
            
            // 调用优化后的可配置版本方法
            ApplyReverbEffect(voice, defaultConfig);
            
            _logger?.LogDebug("使用默认参数应用混响效果到音符: {Note}", voice.Note);
        }
        
        /// <summary>
        /// 应用混响效果 - 可配置版本
        /// </summary>
        /// <param name="voice">要应用混响效果的声音参数</param>
        /// <param name="config">混响配置（可选）</param>
        /// <exception cref="ArgumentNullException">当voice或config为null时抛出</exception>
        /// <exception cref="ArgumentException">
        /// 当voice.Velocity不在0到1之间，或config参数无效时抛出
        /// </exception>
        /// <remarks>
        /// 实现基于延迟和衰减的混响效果，可自定义延迟时间、衰减因子和混响强度。
        /// 支持预延迟，为混响添加空间感。
        /// 
        /// <code>
        /// // 示例用法
        /// var voice = new Voice() { ... };
        /// var reverbConfig = ReverbConfig.CreateLargeHall();
        /// waveGenerator.ApplyReverbEffect(voice, reverbConfig);
        /// </code>
        /// </remarks>
        private void ApplyReverbEffect(Voice voice, ReverbConfig config)
        {
            // 严格的参数验证
            ArgumentNullException.ThrowIfNull(voice, nameof(voice));
            
            // 验证参数的有效性
            if (voice.Velocity < 0 || voice.Velocity > 1.0)
            {
                throw new ArgumentException("声音速度值必须在0到1之间", $"{nameof(voice)}.{nameof(voice.Velocity)}");
            }

            // 如果没有提供自定义配置，则使用默认配置
            config ??= new ReverbConfig();
            
            // 验证配置的有效性
            if (config.DelayTimes == null || config.DelayTimes.Length == 0)
            {
                throw new ArgumentException("延迟时间数组不能为空", nameof(config.DelayTimes));
            }
            
            if (config.DecayFactors == null || config.DecayFactors.Length == 0)
            {
                throw new ArgumentException("衰减因子数组不能为空", nameof(config.DecayFactors));
            }
            
            // 确保延迟时间和衰减因子数组长度匹配
            if (config.DelayTimes.Length != config.DecayFactors.Length)
            {
                throw new ArgumentException("延迟时间数组和衰减因子数组长度必须匹配");
            }
            
            // 保存原始音量，用于后续混合
            double originalVelocity = voice.Velocity;
            
            // 应用预延迟（如果启用）
            if (config.EnablePredelay && config.PredelayTime > 0)
            {
                // 实现预延迟，为混响添加空间感
                GetDelayedSample(voice, config.PredelayTime, voice.Velocity, 44100.0);
                _logger?.LogTrace("应用预延迟: {PredelayTime:F3}s", config.PredelayTime);
            }
            
            // 使用配置的参数
            double[] delayTimes = config.DelayTimes;
            double[] decayFactors = config.DecayFactors;
            const double defaultSampleRate = 44100.0; // 默认采样率
            
            // 优化：预先分配数组避免重复创建
            double[] delayedSamples = new double[delayTimes.Length];

            // 实现专业的混响算法，结合梳状滤波器和全通滤波器
            try
            {
                // 1. 应用梳状滤波器部分
                for (int i = 0; i < delayTimes.Length; i++)
                {
                    // 确保延迟时间是有效的
                    if (delayTimes[i] < 0)
                    {
                        throw new ArgumentException($"延迟时间不能为负数: {delayTimes[i]}");
                    }
                    
                    // 使用优化的GetDelayedSample方法
                    delayedSamples[i] = GetDelayedSample(voice, delayTimes[i], voice.Velocity, defaultSampleRate, decayFactors[i]);
                }
                
                // 2. 应用全通滤波器，创建更丰富的混响尾部
                // 全通滤波器参数 - 基于Moog混响算法的参数
                double[] allpassDelays = [0.005, 0.0017, 0.0005];
                double[] allpassGains = [0.7, 0.6, 0.5];
                
                double reverbedSignal = 0;
                foreach (double sample in delayedSamples)
                {
                    reverbedSignal += sample;
                }
                reverbedSignal /= delayedSamples.Length; // 归一化
                
                // 应用全通滤波器链
                for (int i = 0; i < allpassDelays.Length; i++)
                {
                    // 简化的全通滤波器实现
                    double allpassResult = GetDelayedSample(voice, allpassDelays[i], reverbedSignal, defaultSampleRate);
                    reverbedSignal = -allpassGains[i] * reverbedSignal + allpassResult * (1 - allpassGains[i] * allpassGains[i]);
                }
                
                // 3. 混合干信号和湿信号
                double wetAmount = config.ReverbAmount;
                double dryAmount = 1.0 - wetAmount;
                
                // 4. 更新声音速度，应用EQ调整
                // 高频衰减模拟空气吸收
                double highFrequencyAttenuation = Math.Exp(-config.ReverbAmount * 0.5);
                voice.Velocity = originalVelocity * dryAmount + reverbedSignal * wetAmount * highFrequencyAttenuation;
                
                // 防止过载
                voice.Velocity = Math.Clamp(voice.Velocity, -1.0, 1.0);
            }
            catch (Exception ex)
            {
                // 捕获并包装异常，添加更多上下文信息
                _logger?.LogError(ex, "在应用混响效果时发生错误");
                throw new InvalidOperationException($"在应用混响效果时发生错误", ex);
            }

            // 记录混响应用情况
            _logger?.LogDebug("应用混响效果到音符: {Note}, 延迟线数: {DelayLinesCount}, 启用预延迟: {EnablePredelay}, 混响强度: {ReverbAmount:F2}", 
                voice.Note, delayTimes.Length, config.EnablePredelay, config.ReverbAmount);
        }
        /// <summary>
        /// 获取延迟样本，实现混响算法的核心功能
        /// </summary>
        /// <param name="voice">相关的声音参数</param>
        /// <param name="delayTime">延迟时间（秒），控制样本延迟的时长</param>
        /// <param name="currentSample">当前样本值</param>
        /// <param name="sampleRate">采样率（Hz），通常为44100</param>
        /// <param name="decayFactor">衰减因子（0.0-1.0），控制延迟样本的音量衰减</param>
        /// <returns>经过延迟和衰减处理后的样本值</returns>
        /// <remarks>
        /// 实现了高效的延迟线算法，使用环形缓冲区存储历史样本。
        /// 支持小数延迟和线性插值，提供平滑的延迟效果。
        /// 
        /// 算法特点：
        /// - 使用固定大小的环形缓冲区，避免动态内存分配
        /// - 实现线性插值，支持非整数延迟时间
        /// - 自动限制延迟时间和衰减因子在有效范围内
        /// - 采用指数衰减曲线，模拟真实的声学特性
        /// </remarks>
        private double GetDelayedSample(Voice voice, double delayTime, double currentSample, double sampleRate, double decayFactor = 1.0)
        {
            // 限制延迟时间在有效范围内
            delayTime = Math.Max(0, Math.Min(delayTime, MAX_DELAY_SAMPLES / sampleRate));
            
            // 确保衰减因子在有效范围内
            decayFactor = Math.Max(0, Math.Min(1.0, decayFactor));
            
            // 计算延迟的样本数
            int delaySamples = (int)(delayTime * sampleRate);
            
            // 使用环形缓冲区实现，避免ToList()操作的性能开销
            // 1. 写入当前样本
            _delayLineArray[_delayLineWriteIndex] = currentSample;
            _delayLineWriteIndex = (_delayLineWriteIndex + 1) % MAX_DELAY_SAMPLES;
            _delayLineSize = Math.Min(_delayLineSize + 1, MAX_DELAY_SAMPLES);
            
            // 2. 更新读索引（指向最旧的样本）
            if (_delayLineSize > MAX_DELAY_SAMPLES)
            {
                _delayLineReadIndex = (_delayLineReadIndex + 1) % MAX_DELAY_SAMPLES;
                _delayLineSize = MAX_DELAY_SAMPLES;
            }
            
            // 如果延迟线中没有足够的样本，返回当前样本（带衰减）
            if (_delayLineSize <= delaySamples)
            {
                // 使用更自然的衰减曲线
                double dynamicDecay = Math.Exp(-delayTime * 5) * decayFactor; // 减少衰减率，使混响更自然
                return currentSample * dynamicDecay;
            }
            
            // 计算正确的读取位置（考虑环形缓冲区）
            int targetIndex = (_delayLineWriteIndex - 1 - delaySamples + MAX_DELAY_SAMPLES) % MAX_DELAY_SAMPLES;
            
            // 获取延迟样本（支持整数延迟和线性插值）
            double delayedSample;
            
            // 实现简单的线性插值以获得更平滑的延迟效果
            int integerDelay = (int)delaySamples;
            double fractionalDelay = delaySamples - integerDelay;
            
            if (fractionalDelay > 0.001) // 只有当小数部分显著时才进行插值
            {
                int index1 = (_delayLineWriteIndex - 1 - integerDelay + MAX_DELAY_SAMPLES) % MAX_DELAY_SAMPLES;
                int index2 = (_delayLineWriteIndex - 1 - (integerDelay + 1) + MAX_DELAY_SAMPLES) % MAX_DELAY_SAMPLES;
                
                double sample1 = _delayLineArray[index1];
                double sample2 = _delayLineArray[index2];
                delayedSample = sample1 * (1 - fractionalDelay) + sample2 * fractionalDelay;
            }
            else
            {
                delayedSample = _delayLineArray[targetIndex];
            }
            
            // 使用更自然的衰减曲线 - 模拟空气吸收和房间特性
            double amplitudeDecay = Math.Exp(-delayTime * 5) * decayFactor;
            
            // 返回衰减后的延迟样本
            return delayedSample * amplitudeDecay;
        }
        
        // 重载版本，兼容旧代码
        private double GetDelayedSample(Voice voice, double delayTime, double currentSample, double sampleRate)
        {
            return GetDelayedSample(voice, delayTime, currentSample, sampleRate, 1.0);
        }
        
        /// <summary>
        /// 生成白噪声
        /// </summary>
        /// <param name="durationSamples">噪声持续时间（样本数）</param>
        /// <returns>生成的白噪声数组</returns>
        /// <remarks>
        /// 此方法用于生成白噪声，
        /// 可以用于实现波型的调制或相位同步。
        /// </remarks>
        public float[] GenerateWhiteNoise(int durationSamples)
        {
            var noise = new float[durationSamples];

            for (int i = 0; i < durationSamples; i++)
            {
                noise[i] = (float)(_random.NextDouble() * 2 - 1);
            }

            return noise;
        }

        /// <summary>
        /// 生成粉红噪声
        /// </summary>
        /// <param name="durationSamples">噪声持续时间（样本数）</param>
        /// <returns>生成的粉红噪声数组</returns>
        /// <remarks>
        /// 此方法用于生成粉红噪声，
        /// 可以用于实现波型的调制或相位同步。
        /// </remarks>
        public float[] GeneratePinkNoise(int durationSamples)
        {
            var noise = new float[durationSamples];
            var b0 = 0.0; var b1 = 0.0; var b2 = 0.0; var b3 = 0.0; var b4 = 0.0; var b5 = 0.0; //var b6 = 0.0;

            for (int i = 0; i < durationSamples; i++)
            {
                var white = _random.NextDouble() * 2 - 1;

                b0 = 0.99886 * b0 + white * 0.0555179;
                b1 = 0.99332 * b1 + white * 0.0750759;
                b2 = 0.96900 * b2 + white * 0.1538520;
                b3 = 0.86650 * b3 + white * 0.3104856;
                b4 = 0.5 * b4 + white * 0.5329522;
                b5 = -0.192 * b5 + white * 0.5362;
                var b6 = white * 0.1156;

                noise[i] = (float)(b0 + b1 + b2 + b3 + b4 + b5 + b6 * 0.11);

                // 归一化
                if (noise[i] > 1.0) noise[i] = 1.0f;
                if (noise[i] < -1.0) noise[i] = -1.0f;
            }

            return noise;
        }

        ///// <summary>
        ///// 生成扫频信号
        ///// </summary>
        //public float[] GenerateSweep(double startFrequency, double endFrequency, int durationSamples)
        //{
        //    var sweep = new float[durationSamples];

        //    for (int i = 0; i < durationSamples; i++)
        //    {
        //        double progress = i / (double)durationSamples;
        //        double currentFrequency = startFrequency + (endFrequency - startFrequency) * progress;

        //        double phase = 0;
        //        double phaseIncrement = 2 * Math.PI * currentFrequency / 44100;

        //        for (int j = 0; j < durationSamples; j++)
        //        {
        //            sweep[j] = (float)Math.Sin(phase);
        //            phase += phaseIncrement;
        //        }
        //        return sweep;
        //    }
            
        //}
        /// <summary>
        /// 生成扫频信号 - 频率随时间线性变化的信号
        /// </summary>
        /// <param name="startFrequency">扫频开始频率（赫兹）</param>
        /// <param name="endFrequency">扫频结束频率（赫兹）</param>
        /// <param name="durationSamples">扫频持续时间（样本数）</param>
        /// <returns>生成的扫频信号数组</returns>
        /// <remarks>
        /// 此方法用于生成扫频信号，
        /// 可以用于实现波型的调制或相位同步。
        /// </remarks>
        public float[] GenerateSweep(double startFrequency, double endFrequency, int durationSamples)
        {
            var sweep = new float[durationSamples];
            double phase = 0;

            for (int i = 0; i < durationSamples; i++)
            {
                double progress = i / (double)durationSamples;
                double currentFrequency = startFrequency + (endFrequency - startFrequency) * progress;

                double phaseIncrement = 2 * Math.PI * currentFrequency / 44100;
                sweep[i] = (float)Math.Sin(phase);
                phase += phaseIncrement;
            }

            return sweep; // 确保所有路径都有返回值
        }

        /// <summary>
        /// 生成脉冲信号
        /// </summary>
        /// <param name="frequency">脉冲频率（赫兹）</param>
        /// <param name="dutyCycle">占空比（0到1之间的小数）</param>
        /// <param name="durationSamples">脉冲持续时间（样本数）</param>
        /// <returns>生成的脉冲信号数组</returns>
        /// <remarks>
        /// 此方法用于生成脉冲信号，
        /// 可以用于实现波型的调制或相位同步。
        /// </remarks>
        public float[] GeneratePulse(double frequency, double dutyCycle, int durationSamples)
        {
            var pulse = new float[durationSamples];

            double period = 44100 / frequency;
            double pulseWidth = period * dutyCycle;

            for (int i = 0; i < durationSamples; i++)
            {
                double positionInPeriod = i % period;
                pulse[i] = (positionInPeriod < pulseWidth) ? 1.0f : -1.0f;
            }

            return pulse;
        }
    }

    /// <summary>
    /// 波形生成工具
    /// </summary>
    ///  <remarks>
    /// 此工具类提供了生成不同波型的方法，
    /// 包括正弦波、方波、锯齿波和三角波。
    /// </remarks>
    public static class WaveformGenerator
    {
        /// <summary>
        /// 生成正弦波
        /// </summary>
        /// <param name="time">时间（秒）</param>
        /// <param name="frequency">频率（赫兹）</param>
        /// <returns>生成的波形值</returns>
        /// <remarks>
        /// 此方法用于生成正弦波，
        /// 可以用于实现波型的调制或相位同步。
        /// </remarks>
        public static double GenerateSine(double time, double frequency)
        {
            return Math.Sin(2 * Math.PI * frequency * time);
        }

        /// <summary>
        /// 生成方波
        /// </summary>
        /// <param name="time">时间（秒）</param>
        /// <param name="frequency">频率（赫兹）</param>
        /// <returns>生成的波形值</returns>
        /// <remarks>
        /// 此方法用于生成方波，
        /// 可以用于实现波型的调制或相位同步。
        /// </remarks>
        public static double GenerateSquare(double time, double frequency)
        {
            return Math.Sign(Math.Sin(2 * Math.PI * frequency * time));
        }

        /// <summary>
        /// 生成锯齿波
        /// </summary>
        /// <param name="time">时间（秒）</param>
        /// <param name="frequency">频率（赫兹）</param>
        /// <returns>生成的波形值</returns>
        /// <remarks>
        /// 此方法用于生成锯齿波，
        /// 可以用于实现波型的调制或相位同步。
        /// </remarks>
        public static double GenerateSawtooth(double time, double frequency)
        {
            double phase = time * frequency;
            return 2.0 * (phase - Math.Floor(phase + 0.5)) - 1.0;
        }

        /// <summary>
        /// 生成三角波
        /// </summary>
        /// <param name="time">时间（秒）</param>
        /// <param name="frequency">频率（赫兹）</param>
        /// <returns>生成的波形值</returns>
        /// <remarks>
        /// 此方法用于生成三角波，
        /// 可以用于实现波型的调制或相位同步。
        /// </remarks>
        public static double GenerateTriangle(double time, double frequency)
        {
            double phase = time * frequency;
            double fractionalPart = phase - Math.Floor(phase);

            if (fractionalPart < 0.5)
            {
                return 4.0 * fractionalPart - 1.0;
            }
            else
            {
                return 3.0 - 4.0 * fractionalPart;
            }
        }

        /// <summary>
        /// 生成带抗锯齿的三角波
        /// </summary>
        /// <param name="time">时间（秒）</param>
        /// <param name="frequency">频率（赫兹）</param>
        /// <param name="sampleRate">采样率（赫兹）</param>
        /// <returns>生成的波形值</returns>
        /// <remarks>
        /// 此方法用于生成带抗锯齿的三角波，
        /// 可以用于避免在数字音频处理中引入高频分量。
        /// </remarks>
        public static double GenerateTriangleAntiAliased(double time, double frequency, int sampleRate)
        {
            double phase = time * frequency;
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

            // 抗锯齿处理
            if (sampleRate > 0)
            {
                double nyquist = sampleRate / 2.0;
                if (frequency > nyquist * 0.3)
                {
                    // 应用简单的低通滤波
                    double cutoff = 0.7 * nyquist;
                    if (frequency > cutoff)
                    {
                        double attenuation = 1.0 - ((frequency - cutoff) / (nyquist - cutoff));
                        sample *= Math.Max(0.0, attenuation);
                    }
                }
            }

            return sample;
        }

        /// <summary>
        /// 生成带相位偏移的波形
        /// </summary>
        /// <param name="waveType">波形类型</param>
        /// <param name="time">时间（秒）</param>
        /// <param name="frequency">频率（赫兹）</param>
        /// <param name="phaseOffset">相位偏移（弧度）</param>
        /// <returns>生成的波形值</returns>
        /// <remarks>
        /// 此方法用于生成带相位偏移的波形，
        /// 可以用于实现波型的调制或相位同步。
        /// </remarks>
        public static double GenerateWaveform(WaveType waveType, double time, double frequency, double phaseOffset = 0.0)
        {
            double phase = 2 * Math.PI * frequency * time + phaseOffset;

            return waveType switch
            {
                WaveType.Sine => Math.Sin(phase),
                WaveType.Square => Math.Sign(Math.Sin(phase)),
                WaveType.Sawtooth => GenerateSawtooth(time, frequency),
                WaveType.Triangle => GenerateTriangle(time, frequency),
                _ => Math.Sin(phase) // 默认回退到正弦波
            };
        }
    }
    /// <summary>
    /// 波形生成工具测试类
    /// </summary>
    /// <remarks>
    /// 此测试类用于验证波形生成工具的功能，
    /// 包括生成不同类型的波型和调整参数的效果。
    /// </remarks>
    public class WaveformGeneratorTest
    {
        /// <summary>
        /// 波形类型
        /// </summary>
        /// <remarks>
        /// 此属性用于设置波形的类型，
        /// 可以选择正弦波、方波、锯齿波或三角波。
        /// </remarks>
        public WaveType WaveType { get; set; } = WaveType.Sine;
        /// <summary>
        /// 频率（赫兹）
        /// </summary>
        /// <remarks>
        /// 此属性用于设置波形的频率，
        /// 可以用于调整波型的周期。
        /// </remarks>  
        public double Frequency { get; set; }
        /// <summary>
        /// 开始时间（秒）
        /// </summary>
        /// <remarks>
        /// 此属性用于设置波形的开始时间，
        /// 可以用于调整波型的延迟。
        /// </remarks>
        public double StartTime { get; set; }
        /// <summary>
        /// 释放时间（秒）
        /// </summary>
        /// <remarks>
        /// 此属性用于设置波形的释放时间，
        /// 可以用于实现波型的衰减或结束。
        /// </remarks>
        public double ReleaseTime { get; set; } = -1;
        /// <summary>
        /// 音量（0-1）
        /// </summary>
        /// <remarks>
        /// 此属性用于设置波形的音量，
        /// 可以用于调整波型的强度。
        /// </remarks>
        public double Velocity { get; set; } = 1.0;
        /// <summary>
        /// 乐器设置
        /// </summary>
        /// <remarks>
        /// 此属性用于设置波形的乐器参数，
        /// 可以用于调整波型的音色和效果。
        /// </remarks>
        public InstrumentSettings Settings { get; set; } = new InstrumentSettings();
        /// <summary>
        /// 相位偏移（弧度）
        /// </summary>
        /// <remarks>
        /// 此属性用于设置波形的相位偏移，
        /// 可以用于实现波型的调制或偏移。
        /// </remarks>
        public double PhaseOffset { get; set; } = 0.0;
        /// <summary>
        /// 是否已释放
        /// </summary>
        /// <returns>是否已释放</returns>
        /// <remarks>
        /// 此属性用于判断波形是否已被释放，
        /// 可以根据释放时间是否大于等于0进行判断。
        /// </remarks>
        public bool IsReleased => ReleaseTime >= 0;
        /// <summary>
        /// 是否已完成
        /// </summary>
        /// <param name="currentTime">当前时间（秒）</param>
        /// <returns>是否已完成</returns>
        /// <remarks>
        /// 此方法用于判断波形是否已完成播放，
        /// 可以根据释放时间和释放时间间隔进行判断。
        /// </remarks>
        public bool IsFinished(double currentTime) =>
            IsReleased && (currentTime - ReleaseTime) > Settings.ReleaseTime;

        /// <summary>
        /// 测试生成正弦波
        /// </summary>
        /// <remarks>
        /// 此方法用于测试生成正弦波，
        /// 可以验证生成的采样值是否符合预期。
        /// </remarks>
        public void TestGenerateSine()
        {
            double time = 0.0;
            double frequency = 440.0; // A4音符频率
            double sample = WaveformGenerator.GenerateSine(time, frequency);
            Console.WriteLine($"Sine Wave Sample at time {time}s: {sample}");
        }
        /// <summary>
        /// 生成指定时间的采样值
        /// </summary>
        /// <param name="time">时间（秒）</param>
        /// <returns>采样值</returns>
        /// <remarks>
        /// 此方法用于生成指定时间的采样值，
        /// 可以根据波形类型、频率、相位偏移等参数进行定制。
        /// </remarks>
        public float GenerateSample(double time)
        {
            if (time < StartTime) return 0;

            double noteTime = time - StartTime;
            double amplitude = 0;// new Voice().CalculateAmplitude(noteTime);

            if (amplitude <= 0) return 0;

            // 使用波形生成工具生成波形
            double sample = WaveformGenerator.GenerateWaveform(
                Settings.WaveType, noteTime, Frequency);

            return (float)(sample * amplitude * Velocity);
        }

        /// <summary>
        /// 生成指定时间的采样值（带采样率优化）
        /// </summary>
        /// <param name="time">时间（秒）</param>
        /// <param name="sampleRate">采样率（赫兹）</param>
        /// <returns>采样值</returns>
        /// <remarks>
        /// 此方法用于生成指定时间的采样值，
        /// 可以根据采样率进行优化，
        /// 以避免aliasing（混叠）问题。
        /// </remarks>
        public float GenerateSample(double time, int sampleRate)
        {
            if (time < StartTime) return 0;

            double noteTime = time - StartTime;
            double amplitude = 0;// new Voice().CalculateAmplitude(noteTime);

            if (amplitude <= 0) return 0;

            // 根据波形类型选择生成方法
            double sample = Settings.WaveType switch
            {
                WaveType.Sine => WaveformGenerator.GenerateSine(noteTime, Frequency),
                WaveType.Square => WaveformGenerator.GenerateSquare(noteTime, Frequency),
                WaveType.Sawtooth => WaveformGenerator.GenerateSawtooth(noteTime, Frequency),
                WaveType.Triangle => WaveformGenerator.GenerateTriangleAntiAliased(noteTime, Frequency, sampleRate),
                WaveType.Composite => 0,// new Voice().GenerateCompositeWave(2 * Math.PI * Frequency * noteTime),
                _ => WaveformGenerator.GenerateSine(noteTime, Frequency)
            };

            return (float)(sample * amplitude * Velocity);
        }
    }

    /// <summary>
    /// 效果处理器基类
    /// </summary>
    /// <remarks>
    /// 此抽象类定义了音频效果处理器的基本结构，
    /// 所有具体的效果处理器都必须继承自这个类。
    /// </remarks>
    public abstract class EffectProcessor
    {
        /// <summary>
        /// 处理单个采样值
        /// </summary>
        /// <param name="sample">输入采样值</param>
        /// <param name="time">当前时间（秒）</param>
        /// <returns>处理后的采样值</returns>
        /// <remarks>
        /// 此方法用于处理单个采样值，
        /// 可以在其中实现各种音频效果，
        /// 如音量调整、延迟、混响等。
        /// </remarks>
        public abstract float ProcessSample(float sample, double time);
    }
}
