using System;
using System.IO;
using System.Text.Json;

namespace AIMusicCreator.ApiService.Services.DryWetMidiGerenteMidi
{
    /// <summary>
    /// MIDI文件生成配置类，用于管理MIDI生成的各种参数
    /// </summary>
    public class MidiConfig
    {
        /// <summary>
        /// 默认MIDI音符编号下限
        /// </summary>
        public int MinNoteNumber { get; set; } = 0;

        /// <summary>
        /// 默认MIDI音符编号上限
        /// </summary>
        public int MaxNoteNumber { get; set; } = 127;

        /// <summary>
        /// 默认音符名称（当无法解析时使用）
        /// </summary>
        public string DefaultNoteName { get; set; } = "C";

        /// <summary>
        /// 默认八度（当无法确定时使用）
        /// </summary>
        public int DefaultOctave { get; set; } = 4;

        /// <summary>
        /// 默认MIDI音符编号（当计算失败时使用）
        /// </summary>
        public int DefaultNoteNumber { get; set; } = 60; // C4

        /// <summary>
        /// 默认音符力度
        /// </summary>
        public int DefaultVelocity { get; set; } = 64;

        /// <summary>
        /// 默认BPM（节拍每分钟）
        /// </summary>
        public int DefaultBPM { get; set; } = 120;

        /// <summary>
        /// 音符持续时间默认值（毫秒）
        /// </summary>
        public long DefaultDuration { get; set; } = 480;

        /// <summary>
        /// 从JSON文件加载配置
        /// </summary>
        /// <param name="configPath">配置文件路径</param>
        /// <returns>加载的配置实例</returns>
        public static MidiConfig LoadFromJson(string configPath)
        {
            if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
            {
                return new MidiConfig(); // 返回默认配置
            }

            try
            {
                var jsonContent = File.ReadAllText(configPath);
                return JsonSerializer.Deserialize<MidiConfig>(jsonContent) ?? new MidiConfig();
            }
            catch (Exception)
            {
                return new MidiConfig(); // 出错时返回默认配置
            }
        }

        /// <summary>
        /// 保存配置到JSON文件
        /// </summary>
        /// <param name="configPath">保存路径</param>
        public void SaveToJson(string configPath)
        {
            try
            {
                // 确保目录存在
                var directory = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var jsonContent = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, jsonContent);
            }
            catch (Exception)
            {
                // 保存失败时忽略异常
            }
        }
    }
}
