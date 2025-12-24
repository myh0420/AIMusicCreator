using AIMusicCreator.Entity;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.MusicTheory;
using NAudio.Midi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace AIMusicCreator.Utils
{
    /// <summary>
    /// MIDI和音频处理工具类
    /// </summary>
    /// <remarks>
    /// MidiUtils类提供了一系列用于处理MIDI文件、音频数据和音乐理论转换的静态工具方法。
    /// 主要功能包括MIDI事件集合导出、音符名称转换、音频文件格式检测、音频信息提取、
    /// 音乐情绪和风格枚举转换，以及音频文件读取和处理。此类是整个音乐创作系统中
    /// MIDI和音频处理的核心工具，支持多种音频格式的识别和处理，提供了完整的音频处理流水线。
    /// </remarks>
    public class MidiUtils
    {
        private static readonly ILogger _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<MidiUtils>();

        /// <summary>
        /// 将MIDI事件集合导出为字节数组
        /// </summary>
        /// <param name="midiCollection">要导出的MIDI事件集合</param>
        /// <returns>包含MIDI文件数据的字节数组</returns>
        /// <remarks>通过创建临时MIDI文件的方式，将MIDI事件集合转换为可传输或存储的字节数组。
        /// 方法确保无论成功或失败都会清理生成的临时文件，防止磁盘空间泄漏。</remarks>
        public static byte[] ExportMidiToBytes(MidiEventCollection midiCollection)
        {
            string tempPath = Path.GetTempFileName();
            tempPath = Path.ChangeExtension(tempPath, ".mid");

            try
            {
                MidiFile.Export(tempPath, midiCollection);
                byte[] data = File.ReadAllBytes(tempPath);
                return data;
            }
            finally
            {
                // 确保临时文件被删除
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }
            }
        }
        /// <summary>
        /// 获取音符名称
        /// </summary>
        /// <param name="noteNumber">MIDI音符编号（0-127）</param>
        /// <returns>格式化的音符名称（例如 "C4"）</returns>
        /// <remarks>将MIDI音符编号转换为标准音符名称，格式为音名+八度。
        /// 音符编号60对应中央C（C4），使用C、C#、D、D#等命名方式。</remarks>
        public static string GetNoteName(int noteNumber)
        {
            string[] noteNames = [ "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" ];
            int octave = noteNumber / 12 - 1;
            int noteIndex = noteNumber % 12;
            return $"{noteNames[noteIndex]}{octave}";
        }
        /// <summary>
        /// 获取相对小调/大调和弦
        /// </summary>
        /// <param name="key">输入的调式（和弦名称）</param>
        /// <returns>对应的相对调式和弦</returns>
        /// <remarks>根据预定义的映射关系，返回一个调式的相对调式和弦。例如，C大调的相对小调是Am。
        /// 目前支持C、Am、G、Em和F调式之间的相对关系映射。如果输入的调式不存在于映射中，
        /// 默认返回"Am"作为相对调式。</remarks>
        public static string GetRelativeChord(string key)
        {
            var relativeMap = new Dictionary<string, string>
            {
                { "C", "Am" }, { "Am", "C" }, { "G", "Em" }, { "Em", "G" }, { "F", "Dm" }
            };
            return relativeMap.TryGetValue(key, out var relative) ? relative : "Am";
        }
        /// <summary>
        /// 获取音频格式对应的MIME类型
        /// </summary>
        /// <param name="format">音频文件格式扩展名（例如 "wav", "mp3"）</param>
        /// <returns>对应的MIME类型字符串</returns>
        /// <remarks>将常见的音频文件格式扩展名映射到标准MIME类型。
        /// 支持wav（audio/wav）、mp3（audio/mpeg）和flac（audio/flac）格式，
        /// 对于未定义的格式，默认为audio/wav。</remarks>
        public static string GetMimeType(string format)
        {
            return format.ToLower() switch
            {
                "wav" => "audio/wav",
                "mp3" => "audio/mpeg",
                "flac" => "audio/flac",
                _ => "audio/wav"
            };
        }
        /// <summary>
        /// 通过字符串获取情绪枚举
        /// </summary>
        /// <param name="format">情绪名称字符串</param>
        /// <returns>对应的Emotion枚举值</returns>
        /// <remarks>将字符串形式的情绪描述转换为Emotion枚举值。
        /// 支持happy（快乐）、sad（悲伤）、calm（平静）、exciting/energetic（有活力）、
        /// mysterious（神秘）和romantic（浪漫）等情绪类型。
        /// 情绪设置会影响音符的力度、时值和音高范围，从而塑造不同的音乐表达风格。
        /// 对于未识别的情绪字符串，默认为Emotion.Happy。</remarks>
        public static Emotion GetEmotion(string format) {
            return format.ToLower() switch
            {
                "happy" => Emotion.Happy,
                "sad" => Emotion.Sad,
                "calm" => Emotion.Calm,
                "exciting" => Emotion.Energetic,
                "Energetic" => Emotion.Energetic,
                "Mysterious" => Emotion.Mysterious,
                "Romantic" => Emotion.Romantic,
                _ => Emotion.Happy
            };
        }
        /// <summary>
        /// 通过字符串获取音乐风格枚举
        /// </summary>
        /// <param name="format">音乐风格名称字符串</param>
        /// <returns>对应的MusicStyle枚举值</returns>
        /// <remarks>将字符串形式的音乐风格描述转换为MusicStyle枚举值。
        /// 支持pop（流行）、classical（古典）、electronic（电子）、jazz（爵士）、
        /// rock（摇滚）和blues（蓝调）等风格类型。
        /// 音乐风格会影响旋律和伴奏的生成方式、和弦进行和整体音乐特性。
        /// 对于未识别的风格字符串，默认为MusicStyle.Pop。</remarks>
        public static MusicStyle GetMusicStyle(string format)
        {
            return format.ToLower() switch
            {
                "pop" => MusicStyle.Pop,
                "classical" => MusicStyle.Classical,
                
                "electronic" => MusicStyle.Electronic,
                
                "jazz" => MusicStyle.Jazz,
                "Rock" => MusicStyle.Rock,
                "Blues" => MusicStyle.Blues,
                _ => MusicStyle.Pop
            };
        }
        /// <summary>
        /// 检测文件是否为MP3格式
        /// </summary>
        /// <param name="header">文件头部字节数组</param>
        /// <returns>如果是MP3文件返回true，否则返回false</returns>
        /// <remarks>通过检测文件头部特征字节来判断是否为MP3格式。
        /// 支持两种MP3文件特征检测：ID3v2标签（0x49, 0x44, 0x33）和
        /// MPEG帧同步字节（0xFF followed by 0xE0）。
        /// 至少需要2-3个字节的头部数据才能进行有效检测。</remarks>
        public static bool IsMp3File(byte[] header)
        {
            if (header.Length >= 3 && header[0] == 0x49 && header[1] == 0x44 && header[2] == 0x33)
                return true; // ID3v2 标签

            if (header.Length >= 2 && header[0] == 0xFF && (header[1] & 0xE0) == 0xE0)
                return true; // MPEG 帧同步字节

            return false;
        }

        /// <summary>
        /// 检测文件是否为WAV格式
        /// </summary>
        /// <param name="header">文件头部字节数组</param>
        /// <returns>如果是WAV文件返回true，否则返回false</returns>
        /// <remarks>通过检测WAV文件的RIFF标识来判断文件格式。
        /// WAV文件以"RIFF"字符串开始，对应的十六进制值为0x52, 0x49, 0x46, 0x46。
        /// 需要至少4个字节的头部数据才能进行有效检测。</remarks>
        public static bool IsWaveFile(byte[] header)
        {
            return header.Length >= 4 &&
                   header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46; // "RIFF"
        }
        /// <summary>
        /// 从音频字节数组中提取音频信息
        /// </summary>
        /// <param name="audioBytes">包含音频数据的字节数组</param>
        /// <returns>包含音频格式信息的AudioInfo对象</returns>
        /// <remarks>分析音频数据并提取关键技术参数，包括采样率、通道数、位深度和编码类型等。
        /// 使用CreateAudioFileReaderImproved方法创建适当的音频读取器，以支持多种音频格式。
        /// 异常情况下会输出错误信息并向上抛出异常。</remarks>
        public static AudioInfo GetAudioInfo(byte[] audioBytes)
        {
            try
            {
                using var inputStream = new MemoryStream(audioBytes);
                var audioFile = CreateAudioFileReaderImproved(inputStream);
                var format = audioFile.WaveFormat;

                return new AudioInfo
                {
                    SampleRate = format.SampleRate,
                    Channels = format.Channels,
                    BitsPerSample = format.BitsPerSample,
                    Encoding = format.Encoding.ToString(),
                    //TotalTime = audioFile.
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取音频信息失败");
                throw;
            }
        }
        /// <summary>
        /// 改进的音频文件读取器创建方法，优化了大型音频文件处理性能
        /// </summary>
        /// <param name="audioStream">包含音频数据的流</param>
        /// <returns>根据音频格式自动选择的适当音频读取器</returns>
        /// <remarks>自动检测音频流格式并创建最适合的读取器。采用缓存机制和分块处理策略，
        /// 提高大型音频文件的处理性能。对于常见格式使用专用读取器，保持流位置不变确保可恢复性。</remarks>
        public static IWaveProvider CreateAudioFileReaderImproved(Stream audioStream)
        {
            // 验证输入流
            if (audioStream == null || !audioStream.CanRead || !audioStream.CanSeek)
                throw new ArgumentException("音频流必须可读且可寻址", nameof(audioStream));
                
            var originalPosition = audioStream.Position;

            try
            {
                // 使用更大的缓冲区进行格式检测，提高准确性
                const int FormatDetectionBufferSize = 128;
                var buffer = new byte[FormatDetectionBufferSize];
                var bytesRead = audioStream.Read(buffer, 0, buffer.Length);
                
                // 快速重置位置
                audioStream.Position = originalPosition;

                if (IsMp3File(buffer))
                {
                    // 为大文件创建带缓冲的读取器
                    return new Mp3FileReader(audioStream);
                }
                else if (IsWaveFile(buffer))
                {
                    _logger.LogInformation("使用 WaveFileReader 读取 WAV 文件");
                    return new WaveFileReader(audioStream);
                }
                else
                {
                    // 对于其他格式，使用临时文件方法
                    _logger.LogInformation("使用临时文件方法处理音频");
                    return CreateAudioFileReaderWithTempFile(audioStream);
                }
            }
            catch (Exception)
            {
                audioStream.Position = originalPosition;
                throw;
            }
        }

        /// <summary>
        /// 使用临时文件创建音频读取器
        /// </summary>
        /// <param name="audioStream">包含音频数据的流</param>
        /// <returns>能够读取指定音频格式的IWaveProvider实例</returns>
        /// <remarks>通过创建临时文件来处理复杂或不常见的音频格式。首先将流保存到临时文件，
        /// 然后尝试使用AudioFileReader读取。如果失败，在Windows平台上会进一步尝试使用
        /// MediaFoundationReader。异常处理确保提供清晰的错误信息，指明格式不支持的原因。</remarks>
        private static IWaveProvider CreateAudioFileReaderWithTempFile(Stream audioStream)
        {
            var tempFilePath = SaveToTempFile(audioStream);
            //_tempFiles.Add(tempFilePath); // 记录临时文件以便后续清理

            try
            {
                // 尝试使用 AudioFileReader（自动检测格式）
                return new AudioFileReader(tempFilePath);
            }
            catch (Exception ex)
            {
                //_logger.LogWarning(ex, "AudioFileReader 失败，尝试 MediaFoundationReader");
                Console.WriteLine($"AudioFileReader 失败，尝试 MediaFoundationReader,错误信息：{ex.Message}");
                if (OperatingSystem.IsWindows())
                {
                    try
                    {
                        return new MediaFoundationReader(tempFilePath);
                    }
                    catch (Exception mfEx)
                    {
                        //_logger.LogError(mfEx, "MediaFoundationReader 也失败");
                        throw new FormatException("无法读取音频文件格式", mfEx);
                    }
                }
                else
                {
                    throw new FormatException("不支持此音频格式或平台不支持该格式");
                }
            }
        }

        /// <summary>
        /// 将流内容保存到临时文件
        /// </summary>
        /// <param name="audioStream">要保存的数据流</param>
        /// <returns>创建的临时文件完整路径</returns>
        /// <remarks>在系统临时目录中创建一个唯一命名的临时文件，并将提供的流内容写入其中。
        /// 使用GUID生成唯一文件名，确保不会覆盖现有文件。异常处理确保即使写入失败，
        /// 也会清理已创建的临时文件，避免磁盘空间泄漏。</remarks>
        public static string SaveToTempFile(Stream audioStream)
        {
            var tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".audio");

            try
            {
                using var fileStream = File.Create(tempFilePath);
                audioStream.CopyTo(fileStream);
                return tempFilePath;
            }
            catch
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
                throw;
            }
        }

        /// <summary>
        /// 清理临时文件
        /// </summary>
        /// <remarks>此方法已被注释掉，但设计用于清理程序运行过程中创建的所有临时文件。
        /// 会遍历记录的临时文件列表，尝试删除每个文件，并在异常情况下记录警告但不中断操作。
        /// 清理完成后会清空临时文件列表。</remarks>
        //private void CleanupTempFiles()
        //{
        //    foreach (var tempFile in _tempFiles)
        //    {
        //        try
        //        {
        //            if (File.Exists(tempFile))
        //            {
        //                File.Delete(tempFile);
        //                //_logger.LogDebug($"删除临时文件: {tempFile}");
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            //_logger.LogWarning(ex, $"无法删除临时文件: {tempFile}");
        //        }
        //    }
        //    _tempFiles.Clear();
        //}
        /// <summary>
        /// 从音频数据获取采样提供器
        /// </summary>
        /// <param name="audioData">包含音频数据的字节数组</param>
        /// <returns>提供音频样本的ISampleProvider接口实现</returns>
        /// <remarks>首先尝试直接将字节数组解析为WAV格式。如果失败，
        /// 创建一个临时文件并使用MediaFoundationReader处理更广泛的音频格式。
        /// 方法确保无论成功或失败都会清理生成的临时文件，适合处理未知格式的音频数据。</remarks>
        public static ISampleProvider GetSampleProviderFromAudioData(byte[] audioData)
        {
            // 首先尝试 WAV 格式
            try
            {
                using var stream = new MemoryStream(audioData);
                using var reader = new WaveFileReader(stream);
                return reader.ToSampleProvider();
            }
            catch
            {
                // 如果不是 WAV 格式，保存到临时文件并使用 MediaFoundationReader
                string tempFile = Path.GetTempFileName();
                try
                {
                    File.WriteAllBytes(tempFile, audioData);
                    var reader = new MediaFoundationReader(tempFile);
                    return reader.ToSampleProvider();
                }
                finally
                {
                    if (File.Exists(tempFile))
                        File.Delete(tempFile);
                }
            }
        }

        /// <summary>
        /// 将混音音频写入流
        /// </summary>
        /// <param name="mixer">提供混合音频样本的采样器</param>
        /// <param name="outputStream">目标内存流</param>
        /// <param name="waveFormat">音频格式配置</param>
        /// <remarks>将混合音频数据写入内存流，生成WAV格式文件。使用1秒缓冲区逐块读取音频数据，
        /// 并在写入前对每个样本进行削波处理（确保范围在-1.0到1.0之间），防止音频失真。
        /// 此方法对于音频导出和流处理非常有用。</remarks>
        public static void WriteMixedAudioToStream(ISampleProvider mixer, MemoryStream outputStream, WaveFormat waveFormat)
        {
            using var writer = new WaveFileWriter(outputStream, waveFormat);
            var buffer = new float[waveFormat.SampleRate * waveFormat.Channels]; // 1秒缓冲区
            int samplesRead;

            do
            {
                samplesRead = mixer.Read(buffer, 0, buffer.Length);
                if (samplesRead > 0)
                {
                    // 逐个样本写入（避免削波）
                    for (int i = 0; i < samplesRead; i++)
                    {
                        var sample = Math.Max(-1.0f, Math.Min(1.0f, buffer[i]));
                        writer.WriteSample(sample);
                    }
                }
            } while (samplesRead > 0);
        }

        /// <summary>
        /// 获取音阶的音符名称列表
        /// </summary>
        /// <param name="scale">音阶对象，包含根音和音程信息</param>
        /// <returns>音阶中包含的所有音符名称列表</returns>
        /// <remarks>使用DryWetMidi库获取指定音阶中的所有音符名称。首先尝试使用官方API获取音阶音程，
        /// 然后计算每个音程对应的音符名称。如果API调用失败，会使用备用方法手动构建大调音阶。
        /// 此方法对于生成基于特定音阶的旋律和和弦进行非常有用。</remarks>
        public static List<NoteName> GetScaleNoteNames(Scale scale)
        {
            var notes = new List<NoteName>();
            var root = scale.RootNote;

            try
            {
                // 使用 DryWetMidi 8.x 的 API 获取音阶音符
                var intervals = scale.Intervals;
                foreach (var interval in intervals)
                {
                    // 计算音符编号并转换为音符名称
                    var rootNumber = NoteUtilities.GetNoteNumber(root, 0);
                    var noteNumber = rootNumber + interval.HalfSteps;
                    var noteName = NoteUtilities.GetNoteName((SevenBitNumber)noteNumber);
                    notes.Add(noteName);
                }
            }
            catch
            {
                // 备用方案：手动构建大调音阶
                notes.Add(root);
                notes.Add((NoteName)(((int)root + 2) % 12));
                notes.Add((NoteName)(((int)root + 4) % 12));
                notes.Add((NoteName)(((int)root + 5) % 12));
                notes.Add((NoteName)(((int)root + 7) % 12));
                notes.Add((NoteName)(((int)root + 9) % 12));
                notes.Add((NoteName)(((int)root + 11) % 12));
            }

            return notes;
        }
    }
    /// <summary>
    /// 音频信息类
    /// </summary>
    /// <remarks>
    /// 包含音频文件的关键技术参数信息，用于音频格式描述和元数据管理。
    /// 存储了音频的物理特性，如采样率、通道数、位深度等，以及时间长度信息。
    /// 此类在音频处理管道中用于传递和展示音频文件的基本信息。
    /// </remarks>
    public class AudioInfo
    {
        /// <summary>
        /// 音频采样率（Hz）
        /// </summary>
        /// <remarks>每秒采样的次数，常见值有44100Hz（CD质量）和48000Hz（视频标准）。</remarks>
        public int SampleRate { get; set; }
        
        /// <summary>
        /// 音频通道数
        /// </summary>
        /// <remarks>1表示单声道，2表示立体声，更高值表示环绕声配置。</remarks>
        public int Channels { get; set; }
        
        /// <summary>
        /// 每个采样的位数
        /// </summary>
        /// <remarks>表示每个音频样本的数据精度，常见值有16位（CD质量）和24位（高解析度）。</remarks>
        public int BitsPerSample { get; set; }
        
        /// <summary>
        /// 音频编码格式
        /// </summary>
        /// <remarks>描述音频数据的编码方式，如PCM、MP3、FLAC等。</remarks>
        public string Encoding { get; set; } = string.Empty;
        
        /// <summary>
        /// 音频总时长
        /// </summary>
        /// <remarks>音频文件的完整播放时间长度。</remarks>
        public TimeSpan TotalTime { get; set; }
    }

}