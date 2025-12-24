using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIMusicCreator.Entity
{
    /// <summary>
    /// 音频上下文类，用于管理和跟踪音频处理过程中的状态信息
    /// </summary>
    /// <remarks>
    /// 此类提供了音频处理过程中的关键时间信息，作为音频效果、合成器和其他音频处理组件的共享上下文。
    /// 目前主要维护当前时间状态，未来可扩展以支持更多音频上下文参数。
    /// </remarks>
    public class AudioContext
    {
        /// <summary>
        /// 获取或设置当前音频处理的时间位置（秒）
        /// </summary>
        /// <value>当前时间，以秒为单位</value>
        public double CurrentTime { get; set; }

        /// <summary>
        /// 初始化音频上下文的新实例
        /// </summary>
        /// <remarks>
        /// 创建音频上下文时，将当前时间初始化为0，表示音频处理的起始位置。
        /// </remarks>
        public AudioContext()
        {
            CurrentTime = 0;
        }
    }
}
