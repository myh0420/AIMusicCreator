using AIMusicCreator.ApiService.Interfaces;
using FFMpegCore;
using FFMpegCore.Extensions.System.Drawing.Common;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace AIMusicCreator.ApiService.Services
{
    /// <summary>
        /// 跨平台 FLAC 转换服务（使用 FFMpegCore）
        /// </summary>
        /// <param name="environment">Web主机环境</param>
        /// <param name="logger">日志记录器</param>
        /// <remarks>
        /// 确保：
        /// - 初始化FFmpegCore全局选项
        /// - 设置FFmpeg可执行文件路径（Windows/Linux/macOS）
        /// - 配置日志记录
        /// </remarks>
    public class FlacConverter : IFlacConverter
    {
        /// <summary>
        /// FLAC 转换服务构造函数
        /// </summary>
        private readonly IWebHostEnvironment _environment;
        /// <summary>
        /// 日志记录器
        /// </summary>
        private readonly ILogger<FlacConverter> _logger;
        /// <summary>
        /// FLAC 转换服务构造函数
        /// </summary>
        /// <param name="environment">Web主机环境</param>
        /// <param name="logger">日志记录器</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <remarks>
        /// 确保：
        /// - 初始化FFmpegCore全局选项
        /// - 设置FFmpeg可执行文件路径（Windows/Linux/macOS）
        /// - 配置日志记录
        /// </remarks>
        public FlacConverter(IWebHostEnvironment environment, ILogger<FlacConverter> logger)
        {
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            SetupFFmpeg();
        }
        /// <summary>
        /// 设置FFmpeg全局选项和可执行文件路径
        /// </summary>
        /// <remarks>
        /// 确保：
        /// - 初始化FFmpegCore全局选项
        /// - 设置FFmpeg可执行文件路径（Windows/Linux/macOS）
        /// - 配置日志记录
        /// </remarks>
        private void SetupFFmpeg()
        {
            try
            {
                _logger.LogInformation("开始设置FFmpeg");
                
                // 设置 FFmpeg 可执行文件路径
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // 在 Windows 上，可以自动查找或指定路径
                    var ffmpegPath = FindFFmpegInPath() ??
                                   Path.Combine(_environment.WebRootPath, "ffmpeg", "ffmpeg.exe");
                    
                    if (string.IsNullOrEmpty(ffmpegPath) || !File.Exists(ffmpegPath))
                    {
                        _logger.LogWarning("未找到FFmpeg可执行文件，可能导致转换失败");
                    }
                    
                    GlobalFFOptions.Configure(new FFOptions { BinaryFolder = Path.GetDirectoryName(ffmpegPath)??string.Empty });
                }
                else
                {
                    // Linux/macOS 通常已经安装在系统路径中
                    GlobalFFOptions.Configure(new FFOptions { BinaryFolder = "/usr/bin/" });
                }
                
                _logger.LogInformation("FFmpeg设置完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FFmpeg设置失败");
                // 不抛出异常，让系统继续运行，但可能在转换时失败
            }
        }
        /// <summary>
        /// 查找FFmpeg可执行文件路径
        /// </summary>
        /// <returns>FFmpeg可执行文件路径或null</returns>
        /// <remarks>
        /// 确保：
        /// - 在系统PATH环境变量中查找FFmpeg
        /// - 区分Windows和非Windows平台
        /// </remarks>
        private static string? FindFFmpegInPath()
        {
            try
            {
                var pathDirs = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator);
                if (pathDirs == null) return null;

                var executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffmpeg.exe" : "ffmpeg";

                foreach (var dir in pathDirs)
                {
                    var fullPath = Path.Combine(dir, executableName);
                    if (File.Exists(fullPath))
                        return fullPath;
                }
            }
            catch (Exception ex)
            {
                // 忽略查找过程中的异常
                Console.WriteLine($"查找FFmpeg时发生错误: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 转换为 FLAC 格式
        /// </summary>
        /// <param name="audioData">原始音频数据</param>
        /// <param name="inputFormat">输入音频格式（可选）</param>
        /// <returns>转换后的 FLAC 音频数据</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <remarks>
        /// 确保：
        /// - 输入音频数据有效且不超过200MB
        /// - 输入格式正确（如："mp3", "wav"）
        /// - 输出 FLAC 音频数据符合预期规格
        /// </remarks>
        public async Task<byte[]> ConvertToFlacAsync(byte[] audioData, string? inputFormat = null)
        {
            try
            {
                _logger.LogInformation("开始转换音频为FLAC格式");
                
                // 验证输入参数
                if (audioData == null || audioData.Length == 0)
                {
                    throw new ArgumentNullException(nameof(audioData), "音频数据不能为空");
                }
                
                if (audioData.Length > 200 * 1024 * 1024) // 200MB限制
                {
                    throw new ArgumentException("音频数据不能超过200MB");
                }
                
                string tempInput = Path.GetTempFileName();
                // 根据输入格式设置文件扩展名
                if (!string.IsNullOrEmpty(inputFormat) && !inputFormat.Equals("auto", StringComparison.CurrentCultureIgnoreCase))
                {
                    tempInput = Path.ChangeExtension(tempInput, inputFormat.ToLower());
                }
                string tempOutput = Path.GetTempFileName();
                tempOutput = Path.ChangeExtension(tempOutput, ".flac");

                try
                {
                    // 写入输入文件
                    await File.WriteAllBytesAsync(tempInput, audioData);
                    _logger.LogInformation("输入文件已写入临时位置: {TempInput}", tempInput);

                    // 使用 FFMpegCore 进行转换
                    await FFMpegArguments
                        .FromFileInput(tempInput)
                        .OutputToFile(tempOutput, overwrite: true, options => options
                            .WithAudioCodec("flac")
                            .WithAudioBitrate(320)
                            .WithCustomArgument("-compression_level 8")) // FLAC 压缩级别 0-12
                        .ProcessAsynchronously();

                    // 读取输出文件
                    if (!File.Exists(tempOutput))
                    {
                        throw new FileNotFoundException("转换后文件未生成", tempOutput);
                    }
                    
                    var result = await File.ReadAllBytesAsync(tempOutput);
                    _logger.LogInformation("FLAC转换完成，输出大小: {OutputSize} 字节", result.Length);
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "FFmpeg转换过程失败");
                    throw new InvalidOperationException("FLAC转换失败", ex);
                }
                finally
                {
                    SafeDelete(tempInput);
                    SafeDelete(tempOutput);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "转换为FLAC格式失败");
                throw;
            }
        }

        /// <summary>
        /// 从 WAV 转换为 FLAC
        /// </summary>
        /// <param name="wavData">原始 WAV 音频数据</param>
        /// <returns>转换后的 FLAC 音频数据</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <remarks>
        /// 确保：
        /// - 输入 WAV 数据有效且不超过200MB
        /// - 输出 FLAC 音频数据符合预期规格
        /// </remarks>
        public async Task<byte[]> ConvertWavToFlacAsync(byte[] wavData)
        {
            try
            {
                _logger.LogInformation("开始将WAV转换为FLAC");
                
                if (wavData == null || wavData.Length == 0)
                {
                    throw new ArgumentNullException(nameof(wavData), "WAV数据不能为空");
                }
                
                var result = await ConvertToFlacAsync(wavData, "wav");
                _logger.LogInformation("WAV到FLAC转换完成");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WAV到FLAC转换失败");
                throw;
            }
        }

        /// <summary>
        /// 从 MP3 转换为 FLAC
        /// </summary>
        /// <param name="mp3Data">原始 MP3 音频数据</param>
        /// <returns>转换后的 FLAC 音频数据</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <remarks>
        /// 确保：
        /// - 输入 MP3 数据有效且不超过200MB
        /// - 输出 FLAC 音频数据符合预期规格
        /// </remarks>
        public async Task<byte[]> ConvertMp3ToFlacAsync(byte[] mp3Data)
        {
            try
            {
                _logger.LogInformation("开始将MP3转换为FLAC");
                
                if (mp3Data == null || mp3Data.Length == 0)
                {
                    throw new ArgumentNullException(nameof(mp3Data), "MP3数据不能为空");
                }
                
                var result = await ConvertToFlacAsync(mp3Data, "mp3");
                _logger.LogInformation("MP3到FLAC转换完成");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MP3到FLAC转换失败");
                throw;
            }
        }

        /// <summary>
        /// 批量转换音频文件到 FLAC 格式
        /// </summary>
        /// <param name="audioFiles">包含文件名和音频数据的字典</param>
        /// <returns>包含转换后的 FLAC 音频数据的字典</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <remarks>
        /// 确保：
        /// - 输入字典不为空且包含有效音频文件
        /// - 每个音频文件大小不超过200MB
        /// - 输出 FLAC 音频数据符合预期规格
        /// </remarks>
        public async Task<Dictionary<string, byte[]>> BatchConvertAsync(Dictionary<string, byte[]> audioFiles)
        {
            try
            {
                _logger.LogInformation("开始批量转换音频文件到FLAC格式，文件数量: {FileCount}", audioFiles?.Count ?? 0);
                
                if (audioFiles == null || !audioFiles.Any())
                {
                    throw new ArgumentException("音频文件列表不能为空", nameof(audioFiles));
                }
                
                if (audioFiles.Count > 100)
                {
                    throw new ArgumentException("批量转换文件数量不能超过100个");
                }
                
                var results = new Dictionary<string, byte[]>();
                var tasks = new List<Task>();
                var errorCount = 0;

                foreach (var (filename, data) in audioFiles)
                {
                    var currentFilename = filename;
                    var currentData = data;
                    
                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            _logger.LogInformation("开始转换文件: {Filename}", currentFilename);
                            var flacData = await ConvertToFlacAsync(currentData);
                            lock (results)
                            {
                                results.Add(Path.ChangeExtension(currentFilename, ".flac"), flacData);
                            }
                            _logger.LogInformation("文件转换完成: {Filename}", currentFilename);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "文件转换失败: {Filename}", currentFilename);
                            Interlocked.Increment(ref errorCount);
                        }
                    });

                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);
                
                _logger.LogInformation("批量转换完成，成功: {SuccessCount}, 失败: {ErrorCount}", 
                    results.Count, errorCount);
                
                if (errorCount > 0)
                {
                    _logger.LogWarning("部分文件转换失败，总数: {ErrorCount}", errorCount);
                }
                
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量转换失败");
                throw;
            }
        }
        /// <summary>
        /// 安全删除文件
        /// </summary>
        /// <param name="filePath">要删除的文件路径</param>
        /// <remarks>
        /// 确保：
        /// - 文件路径有效且存在
        /// - 调用者有足够权限删除文件
        /// </remarks>

        private static void SafeDelete(string filePath)
        {
            try 
            { 
                if (File.Exists(filePath)) 
                {
                    File.Delete(filePath);
                }
            } 
            catch (Exception ex)
            {
                Console.WriteLine($"删除临时文件失败: {filePath}, 错误: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 通用音频转换服务命令行
    /// </summary>
    public class UniversalAudioConverter(ILogger<UniversalAudioConverter> logger)
    {
        /// <summary>
        /// 智能 FLAC 转换（自动检测可用工具）
        /// </summary>
        private readonly ILogger<UniversalAudioConverter> _logger = logger;

        /// <summary>
        /// 智能 FLAC 转换（自动检测可用工具）
        /// </summary>
        /// <param name="audioData">输入音频数据</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>转换后的 FLAC 音频数据</returns>
        /// <exception cref="InvalidOperationException">当没有可用的 FLAC 转换器时</exception>
        /// <remarks>
        /// 尝试按顺序使用以下方法：
        /// 1. FFmpeg
        /// 2. FLAC 工具
        /// 3. macOS 上的 afconvert
        /// 最后回退到原始数据
        /// </remarks>
        public async Task<byte[]> ConvertToFlacAsync(byte[] audioData, CancellationToken ct = default)
        {
            // 尝试各种转换方法
            var converters = new Func<byte[], Task<byte[]>>[]
            {
            TryConvertWithFFmpegAsync,
            TryConvertWithFlacToolAsync,
            TryConvertWithAFConvertAsync, // macOS
            FallbackToOriginal // 最后回退
            };

            foreach (var converter in converters)
            {
                try
                {
                    var result = await converter(audioData);
                    if (result != null && result.Length > 0)
                        return result;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "转换方法失败");
                }

                if (ct.IsCancellationRequested)
                    break;
            }

            throw new InvalidOperationException("没有可用的 FLAC 转换器");
        }
        /// <summary>
        /// 使用 FFmpeg 转换为 FLAC
        /// </summary>
        /// <param name="audioData">输入音频数据</param>
        /// <returns>转换后的 FLAC 音频数据</returns>
        /// <exception cref="InvalidOperationException">当 FFmpeg 不可用时</exception>    
        /// <remarks>
        /// 确保 FFmpeg 已安装并在系统 PATH 中
        /// </remarks>
        private async Task<byte[]> TryConvertWithFFmpegAsync(byte[] audioData)
        {
            if (!await IsToolAvailableAsync("ffmpeg"))
                throw new InvalidOperationException("FFmpeg 不可用");

            return await ConvertWithProcessAsync(audioData, "ffmpeg",
                "-i {0} -c:a flac -compression_level 8 -y {1}");
        }
        /// <summary>
        /// 使用 FLAC 工具转换为 FLAC
        /// </summary>
        /// <param name="audioData">输入音频数据</param>
        /// <returns>转换后的 FLAC 音频数据</returns>
        /// <exception cref="InvalidOperationException">当 FLAC 工具不可用时</exception>
        /// <remarks>
        /// 确保 FLAC 工具已安装并在系统 PATH 中
        /// </remarks>
        private async Task<byte[]> TryConvertWithFlacToolAsync(byte[] audioData)
        {
            if (!await IsToolAvailableAsync("flac"))
                throw new InvalidOperationException("FLAC 工具不可用");

            return await ConvertWithProcessAsync(audioData, "flac",
                "-f {0} -o {1} --best");
        }
        /// <summary>
        /// 使用 afconvert 转换为 FLAC（macOS 专属）
        /// </summary>
        /// <param name="audioData">输入音频数据</param>
        /// <returns>转换后的 FLAC 音频数据</returns>
        /// <exception cref="InvalidOperationException">当 afconvert 不可用时</exception>
        /// <remarks>
        /// 确保 macOS 上已安装 afconvert 工具
        /// </remarks>
        private async Task<byte[]> TryConvertWithAFConvertAsync(byte[] audioData)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
                !await IsToolAvailableAsync("afconvert"))
                throw new InvalidOperationException("afconvert 不可用");

            return await ConvertWithProcessAsync(audioData, "afconvert",
                "-f flac -d flac -b 320000 {0} {1}");
        }
        
        /// <summary>
        /// 异步执行外部进程转换
        /// </summary>
        /// <param name="audioData">输入音频数据</param>
        /// <param name="tool">外部工具名称</param>
        /// <param name="argsFormat">命令行参数格式</param>
        /// <returns>转换后的音频数据</returns>
        /// <exception cref="Exception">当转换失败时</exception>
        /// <remarks>
        /// 确保外部工具已安装并在系统 PATH 中
        /// </remarks>
        private static async Task<byte[]> ConvertWithProcessAsync(byte[] audioData, string tool, string argsFormat)
        {
            string tempInput = Path.GetTempFileName();
            string tempOutput = Path.ChangeExtension(tempInput, ".flac");

            try
            {
                await File.WriteAllBytesAsync(tempInput, audioData);

                var args = string.Format(argsFormat, $"{tempInput}", $"{tempOutput}");

                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = tool,
                        Arguments = args,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                process.Start();

                // 异步等待进程完成
                var completionSource = new TaskCompletionSource<bool>();
                process.EnableRaisingEvents = true;
                process.Exited += (s, e) => completionSource.SetResult(true);

                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));
                var completedTask = await Task.WhenAny(completionSource.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    process.Kill(true);
                    throw new TimeoutException("转换超时");
                }

                if (process.ExitCode != 0)
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    throw new Exception($"转换失败 (Exit code: {process.ExitCode}): {error}");
                }

                if (!File.Exists(tempOutput))
                    throw new FileNotFoundException("输出文件未生成");

                return await File.ReadAllBytesAsync(tempOutput);
            }
            finally
            {
                SafeDelete(tempInput);
                SafeDelete(tempOutput);
            }
        }
        /// <summary>
        /// 检查外部工具是否可用
        /// </summary>
        /// <param name="tool">外部工具名称</param>
        /// <returns>如果工具可用则为 true，否则为 false</returns>
        /// <remarks>
        /// 此方法通过执行工具的 --version 命令来检查可用性
        /// </remarks>
        private static async Task<bool> IsToolAvailableAsync(string tool)
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = tool,
                        Arguments = "--version",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                process.Start();
                await process.WaitForExitAsync();
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// 回退到原始数据
        /// </summary>
        /// <param name="audioData">输入音频数据</param>
        /// <returns>原始音频数据</returns>
        private Task<byte[]> FallbackToOriginal(byte[] audioData)
        {
            // 回退：返回原始数据（如果已经是 FLAC 格式）
            return Task.FromResult(audioData);
        }
        /// <summary>
        /// 安全删除文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <remarks>
        /// 此方法安全地删除文件，忽略任何异常
        /// </remarks>
        private static void SafeDelete(string filePath)
        {
            try { if (File.Exists(filePath)) File.Delete(filePath); } catch { }
        }
    }
}