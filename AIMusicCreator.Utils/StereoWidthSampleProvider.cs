using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIMusicCreator.Utils
{
    /// <summary>
    /// 立体声宽度调整效果处理器
    /// </summary>
    /// <remarks>
    /// 此类实现了立体声宽度调整功能，可以扩大或缩小音频的立体声场宽度。
    /// 通过中间声道(Mid)和侧面声道(Side)分解技术实现立体声宽度控制，
    /// 可以让音频听起来更宽广、更有空间感，或者更集中、更紧凑。
    /// 实现了ISampleProvider接口，与NAudio框架无缝集成，仅支持处理双声道音频信号。
    /// 立体声宽度调整技术在音乐制作、电影音效和音频后期处理中广泛应用，
    /// 可以增强音频的表现力和空间感，提高用户的听觉体验。
    /// </remarks>
    public class StereoWidthSampleProvider : ISampleProvider
    {
        /// <summary>
        /// 源音频样本提供器
        /// </summary>
        /// <remarks>原始音频输入源，提供需要进行立体声宽度调整的双声道音频数据。</remarks>
        private readonly ISampleProvider _source;

        /// <summary>
        /// 立体声宽度
        /// </summary>
        /// <value>控制立体声场的宽度，范围从0.0到1.0</value>
        /// <remarks>
        /// 值为0.0时产生最小宽度的立体声效果，值为1.0时产生最大宽度的立体声效果。
        /// 默认值为0.5，表示适度的立体声宽度。
        /// 立体声宽度调整基于中间声道(Mid)和侧面声道(Side)的分解与重构原理：
        /// - Mid = (Left + Right) / 2：包含两个声道共有的声音，代表前方中央
        /// - Side = (Left - Right) / 2：包含两个声道不同的声音，代表立体声场的宽度
        /// 通过调整Side声道的增益来控制立体声宽度，值越大，立体声场越宽。</remarks>
        public double Width { get; set; } = 0.5;

        /// <summary>
        /// 音频格式信息
        /// </summary>
        /// <value>从源提供者获取的音频格式信息</value>
        /// <remarks>保持与源音频相同的格式，但必须是双声道(立体声)格式。</remarks>
        public WaveFormat WaveFormat { get; }

        /// <summary>
        /// 初始化立体声宽度调整处理器
        /// </summary>
        /// <param name="source">源音频样本提供器</param>
        /// <exception cref="InvalidOperationException">当源音频不是双声道格式时抛出</exception>
        /// <remarks>
        /// 构造函数的主要功能：
        /// 1. 保存源音频提供者的引用
        /// 2. 获取并保存音频格式信息
        /// 3. 验证输入音频必须是双声道格式，否则抛出异常
        /// 
        /// 立体声宽度调整只能应用于双声道音频信号，因为它依赖于左右声道之间的差异来创建立体声场。
        /// </remarks>
        public StereoWidthSampleProvider(ISampleProvider source)
        {
            _source = source;
            WaveFormat = source.WaveFormat;
            if (WaveFormat.Channels != 2)
                throw new InvalidOperationException("立体声扩展仅支持双声道音频");
        }

        /// <summary>
        /// 读取并处理音频样本，应用立体声宽度调整
        /// </summary>
        /// <param name="buffer">用于存储处理后样本的缓冲区</param>
        /// <param name="offset">缓冲区中的起始偏移量</param>
        /// <param name="count">要读取的样本数</param>
        /// <returns>实际读取的样本数</returns>
        /// <remarks>
        /// 实现了立体声宽度调整的核心算法：
        /// 1. 从源提供者读取原始音频数据
        /// 2. 将立体声宽度参数转换为浮点值
        /// 3. 按双声道顺序(左右左右...)循环处理每个立体声对
        /// 4. 计算中间声道(Mid)和侧面声道(Side)
        /// 5. 根据宽度参数调整侧面声道的增益
        /// 6. 重构左右声道
        /// 7. 将处理后的样本写回缓冲区
        /// 
        /// 处理过程保证了音频的整体音量不变，只改变立体声场的宽度特性。
        /// 当Width参数增大时，左右声道的差异被放大，声音听起来更宽广、更有空间感；
        /// 当Width参数减小时，左右声道变得更加相似，声音听起来更集中。
        /// </remarks>
        public int Read(float[] buffer, int offset, int count)
        {
            var read = _source.Read(buffer, offset, count);
            var width = (float)Width;

            for (int i = 0; i < read; i += 2)
            {
                var left = buffer[offset + i];
                var right = buffer[offset + i + 1];

                // 计算中间声道和侧面声道
                var mid = (left + right) / 2;
                var side = (left - right) / 2;

                // 扩展侧面声道
                var extendedSide = side * (1 + width);

                // 重构左右声道
                buffer[offset + i] = mid + extendedSide;
                buffer[offset + i + 1] = mid - extendedSide;
            }

            return read;
        }
    }
}
