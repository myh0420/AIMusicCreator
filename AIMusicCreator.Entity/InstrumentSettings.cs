using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIMusicCreator.Entity
{
    /// <summary>
    /// 乐器音色设置
    /// </summary>
    /// <remarks>
    /// 此类定义了生成特定乐器音色所需的全部参数配置，是音频合成器创建声音的核心数据结构。
    /// 包含基本识别信息、波形类型、ADSR包络参数、谐波结构及颤音效果等关键音色特性。
    /// 可通过组合不同参数值来模拟各种不同乐器的音色特征，实现丰富的声音合成效果。
    /// 作为部分类，可在其他文件中扩展额外功能。
    /// </remarks>
    public partial class InstrumentSettings
    {
        /// <summary>
        /// 乐器名称
        /// </summary>
        /// <value>描述乐器音色的可读名称，如"Acoustic Grand Piano"、"Violin"等</value>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// MIDI程序号
        /// </summary>
        /// <value>标识乐器类型的MIDI程序编号，范围为0-127，遵循GM标准</value>
        public int Program { get; set; }
        
        /// <summary>
        /// 波形类型
        /// </summary>
        /// <value>定义基础波形的类型，如正弦波、方波、锯齿波等</value>
        public WaveType WaveType { get; set; }
        
        /// <summary>
        /// 起音时间
        /// </summary>
        /// <value>ADSR包络中的起音时间（Attack Time），单位为秒。
        /// 描述从音符触发到达到最大振幅所需的时间，影响音色的初始响应特性。</value>
        public double AttackTime { get; set; }
        
        /// <summary>
        /// 衰减时间
        /// </summary>
        /// <value>ADSR包络中的衰减时间（Decay Time），单位为秒。
        /// 描述从最大振幅下降到持续电平所需的时间，影响音色的动态过渡特性。</value>
        public double DecayTime { get; set; }
        
        /// <summary>
        /// 持续电平
        /// </summary>
        /// <value>ADSR包络中的持续电平（Sustain Level），范围为0.0-1.0。
        /// 描述在音符持续按下期间保持的振幅级别，影响音色的持续特性。</value>
        public double SustainLevel { get; set; }
        
        /// <summary>
        /// 释放时间
        /// </summary>
        /// <value>ADSR包络中的释放时间（Release Time），单位为秒。
        /// 描述从音符松开到振幅完全消失所需的时间，影响音色的结束特性。</value>
        public double ReleaseTime { get; set; }
        
        /// <summary>
        /// 谐波集合
        /// </summary>
        /// <value>定义音色的谐波结构，包含一系列谐波分量。
        /// 每个谐波分量由频率比例、振幅和相位偏移组成，共同构成复杂的音色特征。</value>
        public List<Harmonic> Harmonics { get; set; } = [];
        
        /// <summary>
        /// 颤音深度
        /// </summary>
        /// <value>颤音效果的强度，范围为0.0-1.0。
        /// 值越大表示音高变化幅度越大，影响音色的表现力和动感。</value>
        public double VibratoDepth { get; set; }
        
        /// <summary>
        /// 颤音频率
        /// </summary>
        /// <value>颤音效果的振动频率，单位为Hz。
        /// 通常在3-8Hz范围内，影响颤音的速度特性。</value>
        public double VibratoFrequency { get; set; }
    }
    
}
