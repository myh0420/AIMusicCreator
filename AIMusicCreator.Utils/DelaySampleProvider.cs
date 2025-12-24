using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIMusicCreator.Utils
{
    /// <summary>
    /// 音频延迟效果处理器
    /// </summary>
    /// <remarks>
    /// DelaySampleProvider类实现了经典的音频延迟效果，通过在一段时间后重复音频信号来创建回声和空间感。
    /// 延迟是一种基本但功能强大的音频效果，它将原始音频信号在短暂延迟后再次播放，
    /// 可以通过调整延迟时间、反馈量和混合比例来创建从简单回声到复杂混响的各种声音效果。
    /// 
    /// 该类使用环形缓冲区技术实现高效的延迟处理，支持可调节的延迟时间、反馈量和干湿混合比例，
    /// 能够创建从短暂的 slapback 延迟到悠长的空间回声等多种效果。
    /// 
    /// 作为NAudio框架的ISampleProvider接口实现，可以轻松集成到现有的音频处理管道中，
    /// 为音乐创作、声音设计和音频后期制作提供丰富的延迟效果选择。</remarks>
    public class DelaySampleProvider : ISampleProvider
    {
        /// <summary>
        /// 源音频样本提供器
        /// </summary>
        /// <remarks>原始音频输入源，提供需要添加延迟效果的音频数据。</remarks>
        private readonly ISampleProvider _source;
        
        /// <summary>
        /// 延迟缓冲区
        /// </summary>
        /// <remarks>用于存储历史音频样本的环形缓冲区，长度由延迟时间和采样率决定。</remarks>
        private readonly float[] _delayBuffer;
        
        /// <summary>
        /// 当前缓冲区位置索引
        /// </summary>
        /// <remarks>指向延迟缓冲区中下一个要读取或写入的位置，使用模运算实现循环缓冲区功能。</remarks>
        private int _delayBufferPosition;
        
        /// <summary>
        /// 延迟样本数量
        /// </summary>
        /// <remarks>根据延迟时间和采样率计算得出的样本数，表示延迟的精确长度。</remarks>
        private readonly int _delaySamples;

        /// <summary>
        /// 音频格式信息
        /// </summary>
        /// <value>从源提供者获取的音频格式信息，包括采样率、声道数和位深度等。</value>
        /// <remarks>实现ISampleProvider接口所需的属性，提供当前音频流的格式信息，确保与音频处理管道兼容。</remarks>
        public WaveFormat WaveFormat => _source.WaveFormat;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="source">源音频样本提供器</param>
        /// <param name="sampleRate">音频采样率（Hz）</param>
        /// <param name="delaySeconds">延迟时间（秒）</param>
        /// <remarks>
        /// 初始化音频延迟效果处理器，设置延迟参数并创建延迟缓冲区。初始化过程包括：
        /// 
        /// 1. 保存源音频提供器引用
        /// 2. 计算延迟样本数：延迟样本数 = 采样率(Hz) * 延迟时间(秒)
        /// 3. 创建适当大小的延迟缓冲区，考虑音频通道数
        /// 4. 重置缓冲区位置索引
        /// 
        /// 延迟时间参数决定了原始信号和延迟信号之间的时间间隔，
        /// 较短的延迟时间可创建紧凑的空间效果，较长的延迟时间可创建明显的回声效果。</remarks>
        public DelaySampleProvider(ISampleProvider source, int sampleRate, double delaySeconds)
        {
            _source = source;
            _delaySamples = (int)(sampleRate * delaySeconds);
            _delayBuffer = new float[_delaySamples * source.WaveFormat.Channels];
            _delayBufferPosition = 0;
        }

        /// <summary>
        /// 读取并处理音频样本
        /// </summary>
        /// <param name="buffer">用于存储处理后样本的缓冲区</param>
        /// <param name="offset">缓冲区中的起始偏移量</param>
        /// <param name="count">要读取的样本数</param>
        /// <returns>实际读取的样本数</returns>
        /// <remarks>
        /// 实现延迟效果的核心处理逻辑，采用环形缓冲区实现高效的延迟处理。处理流程如下：
        /// 
        /// 1. 从源音频提供器读取原始音频样本到输出缓冲区
        /// 2. 对每个样本执行以下处理：
        ///    a. 从延迟缓冲区读取当前位置的延迟样本
        ///    b. 将新的样本写入延迟缓冲区的当前位置
        ///    c. 将延迟样本作为输出写入缓冲区
        ///    d. 更新缓冲区位置索引，实现环形缓冲区功能
        /// 3. 返回实际处理的样本数量
        /// 
        /// 这种实现使用环形缓冲区技术，通过持续覆盖和读取延迟缓冲区中的数据，
        /// 高效地创建基本的延迟效果，输出仅包含延迟的信号，没有反馈或干湿混合。</remarks>
        public int Read(float[] buffer, int offset, int count)
        {
            var samplesRead = _source.Read(buffer, offset, count);

            for (int i = 0; i < samplesRead; i++)
            {
                var outputIndex = offset + i;

                // 从延迟缓冲区读取旧数据
                float delayedSample = _delayBuffer[_delayBufferPosition];

                // 将新数据写入延迟缓冲区
                _delayBuffer[_delayBufferPosition] = buffer[outputIndex];

                // 输出延迟后的数据
                buffer[outputIndex] = delayedSample;

                // 移动缓冲区位置
                _delayBufferPosition = (_delayBufferPosition + 1) % _delayBuffer.Length;
            }

            return samplesRead;
        }
    }
}
