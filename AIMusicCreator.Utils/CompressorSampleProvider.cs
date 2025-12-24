using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIMusicCreator.Utils
{
    /// <summary>
    /// 音频压缩器处理器
    /// </summary>
    /// <remarks>
    /// CompressorSampleProvider类实现了动态范围压缩器，用于控制音频信号的动态范围。
    /// 压缩器是一种音频效果处理器，它减小了音频信号中音量变化的范围，使音量更加均匀。
    /// 当音频信号超过设定的阈值时，压缩器会按设定的比率降低信号增益，
    /// 从而使较大的声音变小，较小的声音相对变大，提高整体的音量一致性。
    /// 
    /// 该类实现了NAudio框架的ISampleProvider接口，可以无缝集成到现有的音频处理管道中，
    /// 为音乐制作、语音处理和音频后期制作提供动态范围控制功能。
    /// 适用于平衡混音中的乐器音量、提高音频的感知响度、防止音频过载等场景。</remarks>
    public class CompressorSampleProvider(ISampleProvider source) : ISampleProvider
    {
        /// <summary>
        /// 源音频样本提供器
        /// </summary>
        /// <remarks>原始音频输入源，提供需要进行动态范围压缩处理的音频数据。</remarks>
        private readonly ISampleProvider _source = source;
        
        /// <summary>
        /// 当前峰值样本值
        /// </summary>
        /// <remarks>用于跟踪音频信号的峰值电平，用于压缩计算，使用指数衰减来模拟峰值保持功能。
        /// 峰值检测器通过比较当前样本的绝对值和衰减后的历史峰值来更新峰值估计，
        /// 使用0.999的衰减因子提供平滑的峰值跟随效果，避免峰值检测过于灵敏导致的压缩器频繁触发。</remarks>
        private float _peak = 0;

        /// <summary>
        /// 压缩阈值（dB）
        /// </summary>
        /// <value>触发压缩的电平阈值，单位为分贝，默认值为-20dB。
        /// 值越高表示开始压缩的音量越小，值越低则需要更大的音量才会触发压缩。
        /// 典型范围为-40dB至0dB。</value>
        /// <remarks>
        /// Threshold参数定义了信号超过哪个音量级别时会被压缩：
        /// - 较高的阈值(如-10 dB)：只压缩最响的部分，保留更多动态
        /// - 中等的阈值(如-20 dB到-24 dB)：平衡的压缩，适合大多数音乐应用
        /// - 较低的阈值(如-30 dB以下)：压缩更多信号，产生更紧凑的声音
        /// 
        /// 阈值设置直接影响压缩器的工作频率和压缩效果的明显程度，
        /// 应根据音频内容和所需的压缩强度进行调整。</remarks>
        public int Threshold { get; set; } = -20;
        
        /// <summary>
        /// 压缩比率
        /// </summary>
        /// <value>压缩的强度，定义超过阈值的信号被压缩的程度，默认值为4:1。
        /// 例如：4:1的比率意味着超过阈值4dB的信号只增加1dB。
        /// 典型范围为2:1至20:1，更高的比率（如∞:1）被称为限制器。</value>
        /// <remarks>
        /// Ratio参数决定了当信号超过阈值时，压缩的强度：
        /// - 较低的比率(1.5:1到2:1)：轻度压缩，适合增加密度而不过度改变动态
        /// - 中等的比率(3:1到4:1)：标准压缩，适合大多数应用场景
        /// - 较高的比率(8:1到10:1)：重度压缩，产生紧凑的声音
        /// - 无限比率(∞:1)：限制器效果，防止信号超过特定电平
        /// 
        /// 压缩比率的数学含义是：对于每超过阈值X分贝的输入，输出只会增加X/Ratio分贝。
        /// 例如，4:1的比率意味着输入每超过阈值4分贝，输出只增加1分贝。</remarks>
        public double Ratio { get; set; } = 4;
        
        /// <summary>
        /// 补偿增益（dB）
        /// </summary>
        /// <value>应用于压缩后信号的增益量，单位为分贝，默认值为2dB。
        /// 用于补偿压缩引起的音量降低，使整体音量恢复到更自然的水平。</value>
        /// <remarks>
        /// MakeUpGain参数用于补偿压缩造成的整体音量下降：
        /// - 由于压缩器降低了超过阈值部分的音量，整体感知响度可能会降低
        /// - 增益补偿可以提升整个信号的电平，使压缩后的音频达到理想的响度水平
        /// - 通常设置为刚好能够恢复压缩前的整体感知响度
        /// 
        /// 合理的增益补偿可以充分利用动态范围，提高音频的清晰度和存在感，
        /// 但过度补偿可能导致背景噪音同时被放大，应谨慎调整。</remarks>
        public double MakeUpGain { get; set; } = 2;

        /// <summary>
        /// 音频格式信息
        /// </summary>
        /// <value>从源提供者获取的音频格式信息，包括采样率、声道数和位深度等。</value>
        /// <remarks>实现ISampleProvider接口所需的属性，提供当前音频流的格式信息，确保与音频处理管道兼容。
        /// 此属性从源音频提供者继承而来，确保压缩器处理后的音频保持原始的格式特性，
        /// 使整个音频处理链能够正确处理信号而不会引入格式不匹配的问题。</remarks>
        public WaveFormat WaveFormat { get; } = source.WaveFormat;

        /// <summary>
        /// 读取并处理音频样本
        /// </summary>
        /// <param name="buffer">用于存储读取样本的缓冲区</param>
        /// <param name="offset">缓冲区中的起始偏移量</param>
        /// <param name="count">要读取的样本数</param>
        /// <returns>实际读取的样本数</returns>
        /// <remarks>从源提供者读取样本，检测峰值电平，应用压缩算法根据阈值和压缩比降低超过阈值的信号，
        /// 然后应用补偿增益使整体音量更加一致。该方法实现了动态范围压缩，有效控制音频信号的振幅变化。
        /// 
        /// 处理流程如下：
        /// 1. 从源音频提供器读取原始音频样本到缓冲区
        /// 2. 将阈值从分贝转换为线性振幅值
        /// 3. 对每个样本执行以下处理：
        ///    a. 更新峰值检测器，记录当前峰值电平
        ///    b. 检查峰值是否超过压缩阈值
        ///    c. 如果超过阈值，根据压缩比计算所需的增益减少量
        ///    d. 应用增益减少量，降低样本振幅
        ///    e. 对所有样本应用增益补偿，提高整体音量
        /// 4. 返回实际处理的样本数量
        /// 
        /// 此实现使用基于峰值的压缩算法，能够有效地控制音频信号的动态范围，
        /// 适用于音乐制作、播客处理、语音增强等多种应用场景。</remarks>
        public int Read(float[] buffer, int offset, int count)
        {
            var read = _source.Read(buffer, offset, count);
            var threshold = (float)Math.Pow(10, Threshold / 20.0);
            var ratio = (float)Ratio;
            var makeUp = (float)Math.Pow(10, MakeUpGain / 20.0);

            for (int i = 0; i < read; i++)
            {
                var currentIndex = offset + i;
                var sample = buffer[currentIndex];
                var absSample = Math.Abs(sample);

                // 检测峰值
                _peak = Math.Max(absSample, _peak * 0.999f);

                // 应用压缩
                if (_peak > threshold)
                {
                    var gainReduction = (float)Math.Log10(_peak / threshold) * 20 / ratio;
                    sample *= (float)Math.Pow(10, -gainReduction / 20.0);
                }

                // 应用 makeup gain
                buffer[currentIndex] = sample * makeUp;
            }

            return read;
        }
    }
}
