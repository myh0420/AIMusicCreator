using AIMusicCreator.Entity;
using System.Collections.Generic;

namespace AIMusicCreator.ApiService.Interfaces;

/// <summary>
/// MIDI编辑服务接口
/// 负责编辑和处理MIDI文件
/// </summary>
public interface IMidiEditorService
{
    /// <summary>
    /// 解析MIDI文件信息
    /// </summary>
    /// <param name="midiBytes">MIDI文件字节数组</param>
    /// <returns>包含MIDI信息的对象</returns>
    object ParseMidiInfo(byte[] midiBytes);
    
    /// <summary>
    /// 简化版解析MIDI文件信息
    /// </summary>
    /// <param name="midiBytes">MIDI文件字节数组</param>
    /// <returns>包含简化MIDI信息的对象</returns>
    object ParseMidiInfoSimple(byte[] midiBytes);
    
    /// <summary>
    /// 修改MIDI速度
    /// </summary>
    /// <param name="midiBytes">MIDI文件字节数组</param>
    /// <param name="newBpm">新的BPM值</param>
    /// <returns>修改后的MIDI字节数组</returns>
    byte[] ChangeMidiTempo(byte[] midiBytes, int newBpm);
    
    /// <summary>
    /// 修改MIDI乐器（支持指定通道）
    /// </summary>
    /// <param name="midiBytes">MIDI文件字节数组</param>
    /// <param name="trackIndex">轨道索引</param>
    /// <param name="channel">MIDI通道</param>
    /// <param name="newInstrument">新乐器编号</param>
    /// <returns>修改后的MIDI字节数组</returns>
    byte[] ChangeMidiInstrument(byte[] midiBytes, int trackIndex, int channel, int newInstrument);
    
    /// <summary>
    /// 批量修改多个轨道的乐器
    /// </summary>
    /// <param name="midiBytes">MIDI文件字节数组</param>
    /// <param name="trackInstruments">轨道索引与乐器编号的映射</param>
    /// <returns>修改后的MIDI字节数组</returns>
    byte[] ChangeMultipleInstruments(byte[] midiBytes, Dictionary<int, int> trackInstruments);
}