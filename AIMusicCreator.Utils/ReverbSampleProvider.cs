using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIMusicCreator.Utils
{
    /// <summary>
    /// 音频混响效果处理器
    /// </summary>
    /// <remarks>
    /// ReverbSampleProvider类实现了基于物理空间模拟的音频混响效果，为音频信号添加空间感和深度。
    /// 混响是一种通过模拟声音在封闭空间中的多次反射来创建自然空间感的音频效果。
    /// 该实现使用简单的反馈延迟线算法，通过调节房间大小、干湿比例和衰减时间等参数，可以模拟各种不同空间的声学特性。
    /// 
    /// 该类实现了NAudio框架的ISampleProvider接口，可以轻松集成到现有的音频处理管道中，
    /// 为音乐创作、音频后期处理和声音设计提供空间环境模拟功能。</remarks>
    public class ReverbSampleProvider : ISampleProvider
    {
        /// <summary>
        /// 源音频样本提供器
        /// </summary>
        /// <remarks>原始音频输入源，提供需要添加混响效果的音频数据。</remarks>
        private readonly ISampleProvider _source;
        
        /// <summary>
        /// 音频采样率
        /// </summary>
        /// <remarks>单位为Hz，用于计算混响延迟时间和衰减特性。</remarks>
        private readonly int _sampleRate;
        
        /// <summary>
        /// 混响缓冲区
        /// </summary>
        /// <remarks>用于存储历史音频样本并模拟声音反射的缓冲区，长度基于采样率和衰减时间计算。</remarks>
        private readonly float[] _reverbBuffer;
        
        /// <summary>
        /// 当前缓冲区索引位置
        /// </summary>
        /// <remarks>指向混响缓冲区中下一个要读取或写入的位置，使用模运算实现循环缓冲区功能。</remarks>
        private int _bufferIndex;

        /// <summary>
        /// 房间大小参数
        /// </summary>
        /// <value>控制混响空间的大小，范围从0.0到1.0，默认值为0.5</value>
        /// <remarks>
        /// RoomSize参数影响混响的密度和整体音色：
        /// - 较小的值(0.1-0.3)：模拟小房间或封闭空间，混响效果紧凑
        /// - 中等的值(0.4-0.6)：模拟中等大小的房间或工作室
        /// - 较大的值(0.7-1.0)：模拟大空间如音乐厅或教堂，混响效果宽广
        /// 
        /// 此参数主要影响混响的初始反射密度和整体丰满度，是塑造混响空间特性的关键参数。</remarks>
        public double RoomSize { get; set; } = 0.5;
        
        /// <summary>
        /// 干湿比例参数
        /// </summary>
        /// <value>控制混响效果的强度，范围从0.0（完全干声）到1.0（完全湿声），默认值为0.3</value>
        /// <remarks>
        /// WetDryMix参数决定了原始声音(干声)与处理后声音(湿声)的混合比例：
        /// - 较小的值(0.1-0.3)：添加微妙的空间感，保留原始音色
        /// - 中等的值(0.4-0.6)：平衡的混响效果，提供明显的空间感
        /// - 较大的值(0.7-1.0)：强烈的空间感，原始声音占比较小
        /// 
        /// 此参数直接影响混响效果的感知强度，调整时应考虑音频内容类型和所需空间感。</remarks>
        public double WetDryMix { get; set; } = 0.3;
        
        /// <summary>
        /// 混响衰减时间
        /// </summary>
        /// <value>控制混响的持续时间，单位为秒，默认值为1.5秒</value>
        /// <remarks>
        /// DecayTime参数定义了混响声衰减到初始振幅的1/1000所需的时间：
        /// - 较短的值(0.3-0.8秒)：混响效果短暂，适合清晰的内容如语音或打击乐
        /// - 中等的值(1.0-2.0秒)：平衡的混响持续时间，适合大多数音乐内容
        /// - 较长的值(2.5-5.0秒)：混响效果持久，适合营造宏大空间感，如管弦乐或氛围音乐
        /// 
        /// 此参数对混响的空间特性和持续感有决定性影响，较长的衰减时间会产生更深远的空间感。</remarks>
        public double DecayTime { get; set; } = 1.5;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="source">源音频样本提供器</param>
        /// <param name="sampleRate">音频采样率（Hz）</param>
        /// <remarks>
        /// 初始化混响效果处理器，创建用于模拟声音反射的混响缓冲区。
        /// 缓冲区大小根据采样率和衰减时间计算，确保有足够的空间存储混响尾音。
        /// 
        /// 初始化过程包括：
        /// 1. 保存源音频提供器和采样率信息
        /// 2. 计算并创建混响缓冲区
        /// 3. 初始化音频格式信息
        /// 4. 重置缓冲区索引位置
        /// 
        /// 建议选择与源音频相同的采样率，以确保处理的准确性和避免采样率转换带来的音质损失。</remarks>
        public ReverbSampleProvider(ISampleProvider source, int sampleRate)
        {
            _source = source;
            _sampleRate = sampleRate;
            var bufferLength = (int)(sampleRate * DecayTime * 2);
            _reverbBuffer = new float[bufferLength];
            WaveFormat = source.WaveFormat;
        }

        /// <summary>
        /// 音频格式信息
        /// </summary>
        /// <value>从源提供者获取的音频格式信息，包括采样率、声道数和位深度等</value>
        /// <remarks>实现ISampleProvider接口所需的属性，提供当前音频流的格式信息，确保与音频处理管道兼容。</remarks>
        public WaveFormat WaveFormat { get; }

        /// <summary>
        /// 读取并处理音频样本
        /// </summary>
        /// <param name="buffer">用于存储处理后样本的缓冲区</param>
        /// <param name="offset">缓冲区中的起始偏移量</param>
        /// <param name="count">要读取的样本数</param>
        /// <returns>实际读取的样本数</returns>
        /// <remarks>
        /// 实现混响效果的核心处理逻辑，采用反馈延迟线算法模拟声音在空间中的反射。处理流程如下：
        /// 
        /// 1. 从源音频提供器读取原始音频样本
        /// 2. 计算干湿信号比例和衰减因子
        /// 3. 对每个样本执行以下处理：
        ///    a. 计算当前干信号（原始信号乘干湿比例的干部分）
        ///    b. 从混响缓冲区读取湿信号（历史反射信号乘干湿比例的湿部分）
        ///    c. 将干湿信号混合并写入输出缓冲区
        ///    d. 更新混响缓冲区：将当前混合信号按房间大小比例加入，并应用衰减因子保留部分历史值
        ///    e. 更新缓冲区索引位置，实现循环缓冲区功能
        /// 4. 返回实际处理的样本数量
        /// 
        /// 这种算法通过简单而有效的方式模拟了声音在封闭空间中的多次反射和衰减过程，
        /// 能够创建从紧凑到宽广的各种混响效果，为音频增加自然的空间感和深度。</remarks>
        public int Read(float[] buffer, int offset, int count)
        {
            var read = _source.Read(buffer, offset, count);
            var wet = (float)WetDryMix;
            var dry = 1 - wet;
            var decayFactor = (float)Math.Exp(-3 / (DecayTime * _sampleRate));

            for (int i = 0; i < read; i++)
            {
                var currentIndex = offset + i;
                var drySample = buffer[currentIndex] * dry;
                var wetSample = _reverbBuffer[_bufferIndex] * wet;

                buffer[currentIndex] = drySample + wetSample;
                _reverbBuffer[_bufferIndex] = (float)(buffer[currentIndex] * RoomSize + _reverbBuffer[_bufferIndex] * decayFactor);
                _bufferIndex = (_bufferIndex + 1) % _reverbBuffer.Length;
            }

            return read;
        }
    }
}
