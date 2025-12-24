using System;using System.Collections.Generic;using System.Linq;using System.Text;using System.Threading.Tasks;

namespace AIMusicCreator.ApiService.Interfaces;

/// <summary>
/// MIDI服务接口，提供旋律和伴奏生成功能
/// </summary>
public interface IMidiService
{
    /// <summary>
    /// 生成旋律MIDI（完整逻辑：风格/情绪/BPM参数化）
    /// </summary>
    /// <param name="style">音乐风格（classical, electronic, pop等）</param>
    /// <param name="mood">情绪（happy, sad等）</param>
    /// <param name="bpm">每分钟节拍数</param>
    /// <returns>MIDI文件的字节数组</returns>
    byte[] GenerateMelody(string style, string mood, int bpm);

    /// <summary>
    /// 生成伴奏MIDI（基于主旋律和弦分析）
    /// </summary>
    /// <param name="melodyMidi">旋律MIDI的字节数组</param>
    /// <returns>伴奏MIDI的字节数组</returns>
    byte[] GenerateAccompaniment(byte[] melodyMidi);
}