using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIMusicCreator.Entity
{
    /// <summary>
    /// 音频效果处理请求模型
    /// </summary>
    /// <remarks>
    /// 此类用于定义音频效果处理的参数集，支持混响、均衡器、压缩器和创意效果等多种音频处理选项。
    /// 该模型与前端界面保持一致，用于前后端数据交互。
    /// </remarks>
    public class AudioEffectRequest
    {
        /// <summary>
        /// 获取或设置Base64编码的音频数据字符串
        /// </summary>
        public string AudioData { get; set; } = "";
        
        /// <summary>
        /// 获取或设置是否应用混响效果
        /// </summary>
        public bool ApplyReverb { get; set; }
        
        /// <summary>
        /// 获取或设置混响房间大小参数（0-1之间）
        /// </summary>
        public double ReverbRoomSize { get; set; }
        
        /// <summary>
        /// 获取或设置混响干湿比例（0-1之间）
        /// </summary>
        public double ReverbWetDry { get; set; }
        
        /// <summary>
        /// 获取或设置混响衰减时间（秒）
        /// </summary>
        public double ReverbDecay { get; set; }
        
        /// <summary>
        /// 获取或设置是否应用均衡器效果
        /// </summary>
        public bool ApplyEQ { get; set; }
        
        /// <summary>
        /// 获取或设置均衡器低频增益（-12到+12分贝）
        /// </summary>
        public double EqBass { get; set; }
        
        /// <summary>
        /// 获取或设置均衡器中频增益（-12到+12分贝）
        /// </summary>
        public double EqMid { get; set; }
        
        /// <summary>
        /// 获取或设置均衡器高频增益（-12到+12分贝）
        /// </summary>
        public double EqTreble { get; set; }
        
        /// <summary>
        /// 获取或设置是否应用压缩器效果
        /// </summary>
        public bool ApplyCompressor { get; set; }
        
        /// <summary>
        /// 获取或设置压缩器阈值（-60到0分贝）
        /// </summary>
        public int CompThreshold { get; set; }
        
        /// <summary>
        /// 获取或设置压缩器压缩比（1:1到20:1）
        /// </summary>
        public double CompRatio { get; set; }
        
        /// <summary>
        /// 获取或设置压缩器增益补偿（0到24分贝）
        /// </summary>
        public double CompGain { get; set; }
        
        /// <summary>
        /// 获取或设置是否应用创意效果
        /// </summary>
        public bool ApplyCreative { get; set; }
        
        /// <summary>
        /// 获取或设置失真效果的强度（0-1之间）
        /// </summary>
        public double DistortionAmount { get; set; }
        
        /// <summary>
        /// 获取或设置立体声宽度参数（0-1之间）
        /// </summary>
        public double StereoWidth { get; set; }
    }
    
    /// <summary>
    /// 人声分离请求模型
    /// </summary>
    /// <remarks>
    /// 此类用于定义音频中人声分离的参数，支持将混合音频分离为人声和伴奏等不同音轨。
    /// </remarks>
    public class VocalSeparationRequest
    {
        /// <summary>
        /// 获取或设置Base64编码的音频数据字符串
        /// </summary>
        public string AudioData { get; set; } = "";
        
        /// <summary>
        /// 获取或设置分离模式
        /// </summary>
        /// <remarks>
        /// 可选值：
        /// - "2stems": 分离为人声和伴奏两轨
        /// - "4stems": 分离为人声、鼓、贝斯和其他四轨
        /// - "5stems": 分离为人声、鼓、贝斯、钢琴和其他五轨
        /// </remarks>
        public string SeparationMode { get; set; } = "2stems";
    }
    
    /// <summary>
    /// 分离后的音轨数据模型
    /// </summary>
    /// <remarks>
    /// 此类用于存储和传输分离后的单个音轨信息，包括音轨名称和原始音频数据。
    /// </remarks>
    public class SeparatedTrackData
    {
        /// <summary>
        /// 获取或设置分离后的音轨名称（如"人声"、"伴奏"等）
        /// </summary>
        public string TrackName { get; set; } = "";
        
        /// <summary>
        /// 获取或设置分离后的原始音频数据字节数组
        /// </summary>
        public byte[] AudioData { get; set; } = Array.Empty<byte>();
    }
}
