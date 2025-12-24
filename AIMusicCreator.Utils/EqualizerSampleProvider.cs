using NAudio.Dsp;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIMusicCreator.Utils
{
    /// <summary>
    /// 三段音频均衡器
    /// </summary>
    /// <remarks>
    /// EqualizerSampleProvider类实现了一个三段均衡器，可以独立调节音频的低音、中音和高音频率范围。
    /// 使用双二次滤波器(BiQuadFilter)技术，为每个音频通道提供三个频段的增益控制。
    /// 这种均衡器广泛应用于音频处理系统中，可用于调整音乐的音质、增强特定频率、或进行音调校正。
    /// 该实现支持立体声及多通道音频处理，为每个通道分别应用相同的均衡参数。</remarks>
    public class EqualizerSampleProvider : ISampleProvider
    {
        /// <summary>
        /// 低音滤波器数组
        /// </summary>
        /// <remarks>为每个音频通道提供一个低音均衡滤波器，中心频率为60Hz。</remarks>
        private readonly BiQuadFilter[] _bassFilters;

        /// <summary>
        /// 中音滤波器数组
        /// </summary>
        /// <remarks>为每个音频通道提供一个中音均衡滤波器，中心频率为1000Hz。</remarks>
        private readonly BiQuadFilter[] _midFilters;

        /// <summary>
        /// 高音滤波器数组
        /// </summary>
        /// <remarks>为每个音频通道提供一个高音均衡滤波器，中心频率为8000Hz。</remarks>
        private readonly BiQuadFilter[] _trebleFilters;

        /// <summary>
        /// 源音频样本提供器
        /// </summary>
        /// <remarks>原始音频输入源，提供需要进行均衡处理的音频数据。</remarks>
        private readonly ISampleProvider _source;

        /// <summary>
        /// 低音增益
        /// </summary>
        /// <value>控制低频段(中心60Hz)的增益值，单位为分贝(dB)</value>
        /// <remarks>
        /// 正值增加低音，负值减少低音。建议范围为-12dB到+12dB，
        /// 默认值为0dB，表示不改变原始低音强度。低音控制主要影响声音的低频部分，
        /// 如贝斯、鼓和人声的低音部分。</remarks>
        public double BassGain { get; set; } = 0;

        /// <summary>
        /// 中音增益
        /// </summary>
        /// <value>控制中频段(中心1000Hz)的增益值，单位为分贝(dB)</value>
        /// <remarks>
        /// 正值增加中音，负值减少中音。建议范围为-12dB到+12dB，
        /// 默认值为0dB。中音是大多数音乐和人声的主要频率范围，
        /// 调整这个参数可以显著改变声音的清晰度和表现力。</remarks>
        public double MidGain { get; set; } = 0;

        /// <summary>
        /// 高音增益
        /// </summary>
        /// <value>控制高频段(中心8000Hz)的增益值，单位为分贝(dB)</value>
        /// <remarks>
        /// 正值增加高音，负值减少高音。建议范围为-12dB到+12dB，
        /// 默认值为0dB。高音控制主要影响声音的明亮度和细节，
        /// 如人声的清晰度、吉他的高音部分和钹等乐器的高频泛音。</remarks>
        public double TrebleGain { get; set; } = 0;

        /// <summary>
        /// 音频格式信息
        /// </summary>
        /// <value>从源提供者获取的音频格式信息</value>
        /// <remarks>保持与源音频相同的格式，包括采样率、通道数和位深度等。</remarks>
        public WaveFormat WaveFormat { get; }

        /// <summary>
        /// 初始化均衡器
        /// </summary>
        /// <param name="source">源音频样本提供器</param>
        /// <remarks>
        /// 构造函数初始化三段均衡器的关键步骤：
        /// 1. 保存源音频提供者和音频格式信息
        /// 2. 确定音频的通道数和采样率
        /// 3. 为每个音频通道创建三个频段(Bass/Mid/Treble)的PeakingEQ滤波器
        /// 4. 初始化滤波器参数，包括中心频率、Q值和增益
        /// 
        /// 每个频段使用的参数设置：
        /// - 低音：中心频率60Hz，Q值1.414
        /// - 中音：中心频率1000Hz，Q值1.414
        /// - 高音：中心频率8000Hz，Q值1.414
        /// 
        /// Q值为1.414(√2)表示半功率带宽，这种设置提供了均衡的频率选择性和带宽，
        /// 适合一般的音乐均衡处理。</remarks>
        public EqualizerSampleProvider(ISampleProvider source)
        {
            _source = source;
            WaveFormat = _source.WaveFormat;
            var channels = _source.WaveFormat.Channels;
            var sampleRate = _source.WaveFormat.SampleRate;

            // 为每个声道创建低音滤波器，中心频率60Hz
            _bassFilters = [.. Enumerable.Range(0, channels).Select(_ => BiQuadFilter.PeakingEQ(sampleRate, 60, 1.414f, (float)BassGain))];

            // 为每个声道创建中音滤波器，中心频率1000Hz
            _midFilters = [.. Enumerable.Range(0, channels).Select(_ => BiQuadFilter.PeakingEQ(sampleRate, 1000, 1.414f, (float)MidGain))];

            // 为每个声道创建高音滤波器，中心频率8000Hz
            _trebleFilters = [.. Enumerable.Range(0, channels).Select(_ => BiQuadFilter.PeakingEQ(sampleRate, 8000, 1.414f, (float)TrebleGain))];
        }

        /// <summary>
        /// 读取并处理音频样本
        /// </summary>
        /// <param name="buffer">用于存储处理后样本的缓冲区</param>
        /// <param name="offset">缓冲区中的起始偏移量</param>
        /// <param name="count">要读取的样本数</param>
        /// <returns>实际读取的样本数</returns>
        /// <remarks>
        /// 实现了均衡器的核心处理逻辑：
        /// 1. 从源提供者读取原始音频数据
        /// 2. 对每个样本确定其所属的声道
        /// 3. 依次通过低音、中音和高音滤波器处理每个样本
        /// 4. 返回处理后的样本数量
        /// 
        /// 处理顺序是低音→中音→高音，这种顺序确保了均衡效果的连贯性。
        /// 每个声道使用独立的滤波器实例，保证了多声道音频处理的正确性。</remarks>
        public int Read(float[] buffer, int offset, int count)
        {
            var read = _source.Read(buffer, offset, count);
            var channels = WaveFormat.Channels;

            for (int i = 0; i < read; i++)
            {
                // 计算当前样本所属的声道
                var channel = i % channels;
                var index = offset + i;
                
                // 依次通过三个频段的滤波器处理样本
                buffer[index] = _trebleFilters[channel].Transform(_midFilters[channel].Transform(_bassFilters[channel].Transform(buffer[index])));
            }

            return read;
        }
    }
}
