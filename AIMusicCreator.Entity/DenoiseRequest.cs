using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIMusicCreator.Entity
{
    /// <summary>
    /// 音频降噪请求
    /// </summary>
    /// <remarks>
    /// 用于请求服务器对音频文件进行降噪处理，可指定降噪强度。
    /// 通过调整降噪强度参数，可以平衡噪音消除效果和音频细节保留。
    /// </remarks>
    public class DenoiseRequest
    {
        /// <summary>
        /// 音频数据（Base64编码）
        /// </summary>
        /// <value>需要进行降噪处理的音频文件数据</value>
        public string AudioData { get; set; } = "";
        
        /// <summary>
        /// 降噪强度
        /// </summary>
        /// <value>降噪处理的强度级别（0.0-1.0范围），默认为0.5
        /// 0.0表示不进行降噪，1.0表示最强降噪效果</value>
        public double Strength { get; set; } = 0.5;
    }
}
