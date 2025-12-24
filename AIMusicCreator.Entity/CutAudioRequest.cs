using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIMusicCreator.Entity
{
    /// <summary>
    /// 音频剪辑请求
    /// </summary>
    /// <remarks>
    /// 用于请求服务器对音频文件进行剪辑操作，指定起始时间和结束时间。
    /// 音频数据通常使用Base64编码的字符串形式传输。
    /// </remarks>
    public class CutAudioRequest
    {
        /// <summary>
        /// 音频数据（Base64编码）
        /// </summary>
        public string AudioData { get; set; } = string.Empty;
        
        /// <summary>
        /// 剪辑起始时间（秒）
        /// </summary>
        /// <value>相对于音频文件开始的时间偏移量</value>
        public double StartSeconds { get; set; }
        
        /// <summary>
        /// 剪辑结束时间（秒）
        /// </summary>
        /// <value>相对于音频文件开始的时间偏移量，必须大于StartSeconds</value>
        public double EndSeconds { get; set; }
    }

    /// <summary>
    /// 音频合并请求
    /// </summary>
    /// <remarks>
    /// 用于请求服务器将多个音频片段按顺序合并为一个完整的音频文件。
    /// 所有音频片段应具有相同的采样率和格式以确保无缝合并。
    /// </remarks>
    public class JoinAudioRequest
    {
        /// <summary>
        /// 音频数据列表（Base64编码）
        /// </summary>
        /// <value>按顺序排列的音频片段数据，将按此顺序合并</value>
        public List<string> AudioDatas { get; set; } = new();
    }

    /// <summary>
    /// AI歌词生成请求
    /// </summary>
    /// <remarks>
    /// 用于请求AI生成歌词内容，可指定主题、风格和段落数量。
    /// AI将根据这些参数生成符合要求的歌词文本。
    /// </remarks>
    public class AiLyricRequest
    {
        /// <summary>
        /// 歌词主题
        /// </summary>
        /// <value>歌词的核心主题或情感方向</value>
        public string Theme { get; set; } = string.Empty;
        
        /// <summary>
        /// 歌词风格
        /// </summary>
        /// <value>歌词的风格类型，如流行、摇滚、民谣等</value>
        public string Style { get; set; } = string.Empty;
        
        /// <summary>
        /// 段落数量
        /// </summary>
        /// <value>希望生成的歌词段落数量，通常为2-4个段落</value>
        public int ParagraphCount { get; set; }
    }

    /// <summary>
    /// AI旋律生成请求
    /// </summary>
    /// <remarks>
    /// 用于请求AI生成旋律内容，可指定情绪和风格。
    /// AI将基于这些参数生成相应的旋律数据。
    /// </remarks>
    public class AiMelodyRequest
    {
        /// <summary>
        /// 旋律情绪
        /// </summary>
        /// <value>旋律的情感色彩，如欢快、悲伤、平静等</value>
        public string Mood { get; set; } = string.Empty;
        
        /// <summary>
        /// 旋律风格
        /// </summary>
        /// <value>旋律的音乐风格，如古典、爵士、电子等</value>
        public string Style { get; set; } = string.Empty;
    }

    /// <summary>
    /// AI和弦进行生成请求
    /// </summary>
    /// <remarks>
    /// 用于请求AI生成和弦进行，可指定调式、风格和音乐段落类型。
    /// AI将根据这些参数生成符合音乐理论和风格特征的和弦序列。
    /// </remarks>
    public class AiChordRequest
    {
        /// <summary>
        /// 音乐调式
        /// </summary>
        /// <value>和弦进行的调式，如"C大调"、"A小调"等</value>
        public string Key { get; set; } = string.Empty;
        
        /// <summary>
        /// 音乐风格
        /// </summary>
        /// <value>和弦进行的风格类型，如流行、爵士、古典等</value>
        public string Style { get; set; } = string.Empty;
        
        /// <summary>
        /// 音乐段落
        /// </summary>
        /// <value>和弦进行所属的音乐段落，如前奏、主歌、副歌等</value>
        public string Section { get; set; } = string.Empty;
    }

    /// <summary>
    /// 和弦进行结果
    /// </summary>
    /// <remarks>
    /// 包含AI生成的和弦进行结果及其解释说明。
    /// 用于返回给前端展示和进一步处理。
    /// </remarks>
    public class ChordProgressionResult
    {
        /// <summary>
        /// 和弦进行表达式
        /// </summary>
        /// <value>和弦进行的文本表示，如"C-Am-F-G"</value>
        public string Progression { get; set; } = string.Empty;
        
        /// <summary>
        /// 和弦进行解释
        /// </summary>
        /// <value>对生成的和弦进行的音乐理论解释和风格说明</value>
        public string Explanation { get; set; } = string.Empty;
    }

}
