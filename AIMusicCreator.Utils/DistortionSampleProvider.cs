using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIMusicCreator.Utils
{
    /// <summary>
    /// 音频失真效果处理器
    /// </summary>
    /// <remarks>
    /// DistortionSampleProvider类实现了基于软削波算法的音频失真效果。
    /// 失真效果是一种常见的音频处理技术，通过压缩和扭曲音频波形来产生温暖、厚重或失真的音色。
    /// 此类使用双曲正切函数(Math.Tanh)实现软削波失真，这种方法产生的失真听起来更加自然，
    /// 不会像硬削波那样产生过多的高频谐波。失真程度可通过DistortionAmount属性进行调整，
    /// 适合用于吉他效果、声音设计和电子音乐制作。</remarks>
    public class DistortionSampleProvider(ISampleProvider source) : ISampleProvider
    {
        /// <summary>
        /// 源音频样本提供器
        /// </summary>
        /// <remarks>原始音频输入源，提供需要进行失真处理的音频数据。</remarks>
        private readonly ISampleProvider _source = source;

        /// <summary>
        /// 失真程度
        /// </summary>
        /// <value>控制失真效果的强度，范围从0.0到1.0</value>
        /// <remarks>
        /// 值为0.0时几乎无失真，值为1.0时产生最强的失真效果。
        /// 默认值为0.5，提供中等程度的失真。此参数直接影响内部驱动值的计算，
        /// 驱动值计算方式为：(1 + DistortionAmount * 10)。
        /// 失真会增加音频的谐波内容，使音色更加丰富或扭曲，常用于吉他和合成器音色处理。</remarks>
        public double DistortionAmount { get; set; } = 0.5;

        /// <summary>
        /// 音频格式信息
        /// </summary>
        /// <value>从源提供者获取的音频格式信息</value>
        /// <remarks>保持与源音频相同的格式，包括采样率、通道数和位深度等。</remarks>
        public WaveFormat WaveFormat { get; } = source.WaveFormat;

        /// <summary>
        /// 读取并处理音频样本
        /// </summary>
        /// <param name="buffer">用于存储处理后样本的缓冲区</param>
        /// <param name="offset">缓冲区中的起始偏移量</param>
        /// <param name="count">要读取的样本数</param>
        /// <returns>实际读取的样本数</returns>
        /// <remarks>
        /// 实现了音频失真处理的主要逻辑：
        /// 1. 首先从源提供者读取原始音频数据
        /// 2. 根据当前的DistortionAmount计算驱动值
        /// 3. 对每个音频样本应用软削波失真算法(Math.Tanh)
        /// 4. 返回处理后的样本数量
        /// 
        /// 软削波失真的优势在于当信号超过阈值时，会平滑地压缩音频波形，而不是突然截断，
        /// 这样可以产生更自然、更温暖的失真音色，特别适合模拟电子管放大器的音色特性。</remarks>
        public int Read(float[] buffer, int offset, int count)
        {
            var read = _source.Read(buffer, offset, count);
            var drive = (float)(1 + DistortionAmount * 10);

            for (int i = 0; i < read; i++)
            {
                var currentIndex = offset + i;
                // 应用软削波失真算法
                buffer[currentIndex] = (float)Math.Tanh(buffer[currentIndex] * drive);
            }

            return read;
        }
    }
}
