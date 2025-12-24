using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIMusicCreator.Entity
{
    /// <summary>
    /// 音色类型枚举
    /// </summary>
    /// <remarks>
    /// 此枚举定义了可用的基本波形类型，用于合成器生成不同音色的声音。
    /// 不同的波形类型具有不同的谐波结构，从而产生不同的音色特性。
    /// </remarks>
    public enum WaveType
    {
        /// <summary>
        /// 正弦波
        /// </summary>
        /// <remarks>最简单的波形，只有基频，没有谐波，产生柔和、纯净的音色</remarks>
        Sine,
        
        /// <summary>
        /// 方波
        /// </summary>
        /// <remarks>包含奇次谐波，音色明亮、尖锐，常用于电子音乐中的合成音色</remarks>
        Square,
        
        /// <summary>
        /// 锯齿波
        /// </summary>
        /// <remarks>包含所有奇次和偶次谐波，音色丰富、明亮，常用于模拟合成器</remarks>
        Sawtooth,
        
        /// <summary>
        /// 三角波
        /// </summary>
        /// <remarks>音色介于正弦波和方波之间，比正弦波丰富，比方波柔和</remarks>
        Triangle,
        
        /// <summary>
        /// 复合波
        /// </summary>
        /// <remarks>多种基本波形的组合，可产生更复杂、更接近真实乐器的音色</remarks>
        Composite
    }
}
