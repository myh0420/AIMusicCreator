using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIMusicCreator.Utils
{
    /// <summary>
    /// 简单音频延迟效果处理器
    /// </summary>
    /// <remarks>
    /// SimpleDelayProvider类实现了一个简洁的音频延迟效果，通过混合原始信号和延迟后的信号来创建空间感和回声效果。
    /// 作为NAudio框架的扩展，该类解决了NAudio核心库中没有内置延迟效果处理器的问题，提供了一个轻量级的延迟实现。
    /// 
    /// 该延迟效果通过固定的混合比例（原始信号0.8，延迟信号0.5）创建一个简单但有效的回声效果，
    /// 适合快速添加空间感或简单回声，无需复杂参数调整。
    /// 
    /// 实现采用环形缓冲区技术，能够高效地处理音频流，同时确保延迟时间准确，
    /// 可以轻松集成到各种音频处理场景中，为音乐制作、音频演示和声音设计提供基础的延迟效果。</remarks>
    public class SimpleDelayProvider : ISampleProvider
    {
        /// <summary>
        /// 源音频样本提供器
        /// </summary>
        /// <remarks>原始音频输入源，提供需要添加延迟效果的音频数据。</remarks>
        private readonly ISampleProvider _source;
        
        /// <summary>
        /// 延迟样本数
        /// </summary>
        /// <remarks>根据采样率、延迟时间和声道数计算得出的总延迟样本数，决定了延迟的精确长度。</remarks>
        private readonly int _delaySamples;
        
        /// <summary>
        /// 延迟缓冲区
        /// </summary>
        /// <remarks>用于存储历史音频样本的环形缓冲区，长度由延迟样本数决定，确保能够存储完整的延迟信号。</remarks>
        private readonly float[] _delayBuffer;
        
        /// <summary>
        /// 缓冲区索引位置
        /// </summary>
        /// <remarks>指向延迟缓冲区中下一个要读取或写入的位置，使用模运算实现环形缓冲区的循环访问。</remarks>
        private int _bufferIndex;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="source">源音频样本提供器</param>
        /// <param name="sampleRate">音频采样率（Hz）</param>
        /// <param name="delaySeconds">延迟时间（秒）</param>
        /// <remarks>
        /// 初始化简单延迟效果处理器，设置延迟参数并创建延迟缓冲区。初始化过程包括：
        /// 
        /// 1. 保存源音频提供器引用
        /// 2. 计算总延迟样本数：
        ///    延迟样本数 = 采样率(Hz) * 延迟时间(秒) * 声道数
        ///    此计算确保了延迟时间在所有声道上的一致性，并且考虑了音频的采样精度
        /// 3. 创建延迟缓冲区，处理边缘情况（确保缓冲区至少有一个样本，避免空数组）
        /// 4. 从源提供器继承音频格式信息
        /// 5. 初始化缓冲区索引位置为0
        /// 
        /// 延迟时间参数决定了原始信号和延迟信号之间的时间间隔，
        /// 较短的延迟时间(0.1-0.3秒)可创建紧凑的空间效果，
        /// 中等延迟时间(0.4-1.0秒)适合明显的回声效果，
        /// 较长的延迟时间(1.0秒以上)可创建悠长的空间氛围。</remarks>
        public SimpleDelayProvider(ISampleProvider source, int sampleRate, double delaySeconds)
        {
            _source = source;
            _delaySamples = (int)(sampleRate * delaySeconds * source.WaveFormat.Channels);
            _delayBuffer = new float[_delaySamples > 0 ? _delaySamples : 1]; // 避免空数组
            WaveFormat = source.WaveFormat;
        }

        /// <summary>
        /// 音频格式信息
        /// </summary>
        /// <value>从源提供者获取的音频格式信息，包括采样率、声道数和位深度等</value>
        /// <remarks>实现ISampleProvider接口所需的属性，提供当前音频流的格式信息，确保与音频处理管道兼容。
        /// 此属性从源音频提供者继承而来，保证延迟处理不会改变原始音频的格式特性。</remarks>
        public WaveFormat WaveFormat { get; }

        /// <summary>
        /// 读取并处理音频样本
        /// </summary>
        /// <param name="buffer">用于存储处理后样本的缓冲区</param>
        /// <param name="offset">缓冲区中的起始偏移量</param>
        /// <param name="count">要读取的样本数</param>
        /// <returns>实际读取的样本数</returns>
        /// <remarks>
        /// 实现简单延迟效果的核心处理逻辑，采用固定混合比例的环形缓冲区延迟算法。处理流程如下：
        /// 
        /// 1. 从源音频提供器读取原始音频样本到输出缓冲区
        /// 2. 对每个样本执行以下处理：
        ///    a. 获取当前位置的延迟样本值（从延迟缓冲区读取）
        ///    b. 计算混合输出样本：原始信号与延迟信号按固定比例混合
        ///       - 原始信号权重：0.8（保留大部分原始信号特性）
        ///       - 延迟信号权重：0.5（提供明显但不过强的延迟效果）
        ///       - 输出样本 = 原始样本 * 0.8 + 延迟样本 * 0.5
        ///    c. 更新延迟缓冲区：将当前混合后的样本写入延迟缓冲区
        ///    d. 更新缓冲区索引，实现环形缓冲区的循环访问
        /// 3. 返回实际处理的样本数量
        /// 
        /// 这种固定比例混合的简单延迟算法虽然参数不可调整，但提供了一个平衡的默认配置，
        /// 能够在大多数情况下创建自然的延迟效果，同时保持了代码的简洁性和执行效率。
        /// 通过环形缓冲区技术，该实现能够高效地处理音频流，无论延迟时间设置多长，
        /// 内存占用和处理复杂度都保持在较低水平。</remarks>
        public int Read(float[] buffer, int offset, int count)
        {
            var read = _source.Read(buffer, offset, count);

            // 应用延迟：混合原始信号和延迟信号
            for (int i = 0; i < read; i++)
            {
                var currentIndex = offset + i;
                var delayedSample = _delayBuffer[_bufferIndex];

                // 原始信号 + 延迟信号（0.5 为延迟信号音量，可调整）
                buffer[currentIndex] = buffer[currentIndex] * 0.8f + delayedSample * 0.5f;

                // 更新延迟缓冲区
                _delayBuffer[_bufferIndex] = buffer[currentIndex];
                _bufferIndex = (_bufferIndex + 1) % _delaySamples;
            }

            return read;
        }
    }
}
