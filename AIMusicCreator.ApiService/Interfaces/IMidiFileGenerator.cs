using AIMusicCreator.Entity;
using System.Collections.Generic;

namespace AIMusicCreator.ApiService.Interfaces
{
    /// <summary>
    /// MIDI文件生成器接口
    /// 负责将音符事件列表转换为标准MIDI文件
    /// </summary>
    public interface IMidiFileGenerator
    {
        /// <summary>
        /// 生成MIDI文件
        /// </summary>
        /// <param name="notes">音符列表，包含要转换为MIDI的音符信息</param>
        /// <param name="bpm">速度，默认为配置文件中的BPM值</param>
        /// <returns>生成的MIDI文件的字节数组</returns>
        byte[] GenerateMidiFile(List<NoteEvent> notes, int bpm = -1);
    }
}