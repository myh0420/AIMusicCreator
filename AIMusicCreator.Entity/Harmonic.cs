using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIMusicCreator.Entity
{
    /// <summary>
    /// 谐波分量
    /// </summary>
    /// <remarks>
    /// 此类表示声音中的谐波分量，用于构建复杂的音色合成。
    /// 真实乐器的音色由基频和谐波共同构成，通过调整谐波的频率比例、幅度和相位，可以创建各种不同的音色。
    /// 在合成器中，多个谐波组合可以生成丰富多样的声音效果。
    /// </remarks>
    public class Harmonic
    {
        /// <summary>
        /// 频率比例
        /// </summary>
        /// <value>相对于基频的倍数，通常为整数或特定分数。
        /// 例如：1.0表示基频，2.0表示第一泛音（八度音），1.5表示五度音，等等。</value>
        public double FrequencyRatio { get; set; } = 1.0;
        
        /// <summary>
        /// 振幅
        /// </summary>
        /// <value>谐波的振幅级别，范围为0.0到1.0。
        /// 值越大表示该谐波在整体音色中所占比例越大，对音色的影响也越明显。</value>
        public double Amplitude { get; set; } = 1.0;
        
        /// <summary>
        /// 相位偏移
        /// </summary>
        /// <value>谐波的起始相位偏移，单位为弧度（0到2π）。
        /// 相位偏移会影响波形的形状，但通常人耳难以察觉相位变化产生的差异。</value>
        public double PhaseOffset { get; set; } = 0.0;
    }
}
