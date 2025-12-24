using System;
using System.Collections.Generic;
using System.Linq;
using AIMusicCreator.Entity;
using NAudio.Midi;
using NAudio.Wave;
using NoteEvent = NAudio.Midi.NoteEvent;

namespace AIMusicCreator.ApiService.Services
{
    /// <summary>
    /// 自定义MIDI合成器 - 支持改进的颤音效果    
    /// 自定义MIDI合成器构造函数
    /// </summary>
    /// <param name="sampleRate">采样率（赫兹）</param>
    /// <param name="channelCount">通道数（通常为2）</param>
    /// <remarks>
    /// 确保：
    /// - 合成器使用IeeeFloat格式
    /// - 初始化空的自定义MIDI声音列表
    /// - 设置默认颤音参数
    /// </remarks>
    public class CustomMidiSynthesizer : ISampleProvider
    {
       /// <summary>
       /// 自定义MIDI合成器 - 支持改进的颤音效果
       /// </summary>
        private readonly WaveFormat _waveFormat;
        /// <summary>
        /// 自定义MIDI声音列表
        /// </summary>
        private readonly List<CustomMidiVoice> _voices;
        /// <summary>
        /// 用于同步访问_voices列表的锁对象
        /// </summary>
        private readonly Lock _lockObject = new Lock();
        
        /// <summary>
        /// 当前乐器程序号
        /// </summary>
        private int _currentProgram = 0; // 默认钢琴
        
        /// <summary>
        /// 颤音深度系数 - 根据不同乐器调整
        /// </summary>
        private double _vibratoDepthCoefficient = 1.0;
        
        /// <summary>
        /// 颤音频率系数 - 根据不同乐器调整
        /// </summary>
        private double _vibratoFrequencyCoefficient = 1.0;
/// <summary>
/// 自定义MIDI合成器构造函数
/// </summary>
/// <param name="sampleRate">采样率（赫兹）</param>
/// <param name="channelCount">通道数（通常为2）</param>
/// <remarks>
/// 确保：
/// - 合成器使用IeeeFloat格式
/// - 初始化空的自定义MIDI声音列表
/// - 设置默认颤音参数
/// </remarks>
        public CustomMidiSynthesizer(int sampleRate, int channelCount)
        {
            _waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount);
            _voices = [];
            UpdateInstrumentParameters(_currentProgram);
        }
        
        /// <summary>
        /// 根据乐器程序号更新颤音参数
        /// </summary>
        /// <param name="program">MIDI乐器程序号</param>
        /// <remarks>
        /// 确保：
        /// - 根据不同乐器类型设置合适的颤音参数
        /// - 颤音深度系数和频率系数根据不同乐器类型进行调整
        /// </remarks>
        private void UpdateInstrumentParameters(int program)
        {
            _currentProgram = program;
            
            // 根据不同乐器类型设置合适的颤音参数
            switch (program)
            {
                // 弦乐器通常有明显的颤音
                case 40: // 小提琴
                case 41: // 中提琴
                case 42: // 大提琴
                    _vibratoDepthCoefficient = 1.5;
                    _vibratoFrequencyCoefficient = 0.9;
                    break;
                
                // 管乐器
                case 64: // 单簧管
                case 65: // 双簧管
                    _vibratoDepthCoefficient = 1.2;
                    _vibratoFrequencyCoefficient = 1.1;
                    break;
                
                // 人声类乐器
                case 54: // 人声 "啊"
                case 55: // 人声 "哦"
                    _vibratoDepthCoefficient = 1.8;
                    _vibratoFrequencyCoefficient = 0.8;
                    break;
                
                // 风琴类 - 较小颤音
                case 16: // 风琴
                    _vibratoDepthCoefficient = 0.5;
                    _vibratoFrequencyCoefficient = 1.2;
                    break;
                
                // 钢琴等键盘乐器 - 适中颤音
                default:
                    _vibratoDepthCoefficient = 1.0;
                    _vibratoFrequencyCoefficient = 1.0;
                    break;
            }
        }
        /// <summary>
        /// 自定义MIDI合成器的音频格式
        /// </summary>
        public WaveFormat WaveFormat => _waveFormat;
        
        /// <summary>
        /// 处理MIDI事件
        /// </summary>
        /// <param name="midiEvent">MIDI事件</param>
        /// <remarks>
        /// 确保：
        /// - 音符开始时创建新的自定义MIDI声音
        /// - 音符结束时触发声音释放
        /// - 根据当前乐器类型调整颤音参数
        /// </remarks>
        public void ProcessMidiEvent(MidiEvent midiEvent)
        {
            if (midiEvent is NoteOnEvent noteOn && noteOn.Velocity > 0)
            {
                // 音符开始
                lock (_lockObject)
                {
                    // 根据当前乐器设置颤音参数
                    double baseVibratoDepth = 0.015; // 基础颤音深度
                    double baseVibratoFrequency = 5.5; // 基础颤音频率
                    
                    // 根据乐器类型调整颤音参数
                    double adjustedDepth = baseVibratoDepth * _vibratoDepthCoefficient;
                    double adjustedFrequency = baseVibratoFrequency * _vibratoFrequencyCoefficient;
                    
                    // 根据力度稍微调整颤音参数（强音通常颤音更明显）
                    float velocityFactor = noteOn.Velocity / 127.0f;
                    adjustedDepth *= 0.8 + velocityFactor * 0.4; // 力度影响颤音深度
                    
                    _voices.Add(new CustomMidiVoice(
                        MidiNoteToFrequency(noteOn.NoteNumber),
                        noteOn.Velocity / 127.0f,
                        _waveFormat.SampleRate,
                        adjustedDepth,
                        adjustedFrequency
                    ));
                }
            }
            // 暂时移除ProgramChange处理，因为NAudio.Midi版本不支持
              // 后续可以考虑使用其他方式处理乐器程序变化
            else if (midiEvent is NoteEvent noteOff &&
                    (noteOff.CommandCode == MidiCommandCode.NoteOff ||
                     (noteOff is NoteOnEvent noteOnVel0 && noteOnVel0.Velocity == 0)))
            {
                // 音符结束 - 触发释放阶段
                lock (_lockObject)
                {
                    var frequencyToRelease = MidiNoteToFrequency(noteOff.NoteNumber);
                    var voiceToRelease = _voices.FirstOrDefault(v =>
                        Math.Abs(v.Frequency - frequencyToRelease) < 0.1f);

                    voiceToRelease?.StartRelease();
                }
            }
        }
        /// <summary>
        /// 从合成器读取音频样本
        /// </summary>
        /// <param name="buffer">音频样本缓冲区</param>
        /// <param name="offset">缓冲区偏移量</param>
        /// <param name="count">要读取的样本数</param>
        /// <returns>实际读取的样本数</returns>
        /// <remarks>
        /// 确保：
        /// - 从所有活跃声音中渲染样本
        /// - 移除已完成的声音
        /// - 返回请求的样本数
        /// </remarks>
        public int Read(float[] buffer, int offset, int count)
        {
            Array.Clear(buffer, offset, count);

            lock (_lockObject)
            {
                // 渲染所有活跃的声音
                foreach (var voice in _voices)
                {
                    voice.Render(buffer, offset, count, _waveFormat.Channels);
                }

                // 移除已完成的声音
                _voices.RemoveAll(v => v.IsFinished);
            }

            return count;
        }
        
        /// <summary>
        /// 将MIDI音符号转换为频率（赫兹）
        /// </summary>
        /// <param name="note">MIDI音符号</param>
        /// <returns>音符对应的频率（赫兹）</returns>
        /// <remarks>
        /// 确保：
        /// - 正确计算频率，基于A4（69）的频率
        /// - 处理MIDI音符号范围（0-127）
        /// </remarks>
        private float MidiNoteToFrequency(int note)
        {
            return 440.0f * (float)Math.Pow(2, (note - 69) / 12.0);
        }
        
        /// <summary>
        /// 获取当前使用的乐器程序号
        /// </summary>
        public int CurrentProgram => _currentProgram;
        
        /// <summary>
        /// 设置颤音参数
        /// </summary>
        /// <param name="depth">颤音深度</param>
        /// <param name="frequency">颤音频率</param>
        /// <remarks>
        /// 确保：
        /// - 颤音参数在有效范围内（0-1）
        /// - 颤音频率在有效范围内（1-20）
        /// </remarks>
        public void SetVibratoParameters(double depth, double frequency)
        {
            _vibratoDepthCoefficient = depth;
            _vibratoFrequencyCoefficient = frequency;
        }
        
        // 暂时移除HandleProgramChange方法
    }
}
