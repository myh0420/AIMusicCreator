using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIMusicCreator.Entity
{
    /// <summary>
    /// 批处理文件项模型
    /// </summary>
    /// <remarks>
    /// 此类用于表示待处理的单个文件数据，包含文件名称、原始数据、格式和MIME类型信息。
    /// 主要用于多文件批量处理场景。
    /// </remarks>
    public class BatchFileItem
    {
        /// <summary>
        /// 获取或设置文件名（包含扩展名）
        /// </summary>
        public string FileName { get; set; } = "";
        
        /// <summary>
        /// 获取或设置文件的原始二进制数据
        /// </summary>
        public byte[] Data { get; set; } = Array.Empty<byte>();
        
        /// <summary>
        /// 获取或设置文件的原始格式（如mp3、wav、mid等）
        /// </summary>
        public string OriginalFormat { get; set; } = "";
        
        /// <summary>
        /// 获取或设置文件的MIME类型
        /// </summary>
        public string MimeType { get; set; } = "";
    }

    /// <summary>
    /// 已处理文件项模型
    /// </summary>
    /// <remarks>
    /// 此类用于表示处理完成后的文件数据，包含原始文件名、处理后的文件名、处理后的数据和MIME类型。
    /// 用于存储和传输处理结果。
    /// </remarks>
    public class ProcessedFileItem
    {
        /// <summary>
        /// 获取或设置原始文件名（处理前的文件名）
        /// </summary>
        public string OriginalFileName { get; set; } = "";
        
        /// <summary>
        /// 获取或设置处理后的文件名
        /// </summary>
        public string ProcessedFileName { get; set; } = "";
        
        /// <summary>
        /// 获取或设置处理后的文件二进制数据
        /// </summary>
        public byte[] Data { get; set; } = Array.Empty<byte>();
        
        /// <summary>
        /// 获取或设置处理后文件的MIME类型
        /// </summary>
        public string MimeType { get; set; } = "";
    }
}
