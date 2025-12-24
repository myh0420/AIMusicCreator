﻿using Microsoft.AspNetCore.Http;
using AIMusicCreator.ApiService.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using System.Net.Http.Headers;
using System;
using System.Text;
using AIMusicCreator.ApiService.Services;
using AIMusicCreator.Entity;
using System.Diagnostics;

namespace AIMusicCreator.ApiService.Controllers
{
    /// <summary>
    /// 语音控制器
    /// 负责处理与语音相关的HTTP请求
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class VocalController : ControllerBase
    {
        /// <summary>
        /// 语音服务实例
        /// </summary>
        private readonly IVocalService _vocalService;
        /// <summary>
        /// 日志服务实例
        /// </summary>
        private readonly ILogger<VocalController> _logger;
        /// <summary>
        /// 音频效果服务实例
        /// </summary>
        private readonly IAudioEffectService _audioEffectService;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="vocalService">语音服务实例</param>
        /// <param name="audioEffectService">音频效果服务实例</param>
        /// <param name="logger">日志服务实例</param>
        /// <exception cref="ArgumentNullException">当任何服务为null时抛出</exception>
        public VocalController(IVocalService vocalService, IAudioEffectService audioEffectService, ILogger<VocalController> logger)
        {
            _vocalService = vocalService ?? throw new ArgumentNullException(nameof(vocalService));
            _audioEffectService = audioEffectService ?? throw new ArgumentNullException(nameof(audioEffectService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        /// <summary>
        /// 语音请求模型
        /// 包含生成语音所需的参数
        /// </summary>
        public class VocalRequest
        {
            /// <summary>
            /// 歌词内容
            /// </summary>
            public required string Lyrics { get; set; }
            /// <summary>
            /// 旋律数据（Base64编码）
            /// </summary>
            public required string MelodyData { get; set; }
            /// <summary>
            /// 音频数据（Base64编码）
            /// </summary>
            public required string AudioData { get; set; }
            /// <summary>
            /// 语音类型
            /// 默认值为"default"
            /// </summary>
            public string VoiceType { get; set; } = "default";
            /// <summary>
            ///  tempo（BPM）
            /// 默认值为120
            /// </summary>
            public int Tempo { get; set; } = 120;
        }
        /// <summary>
        /// 验证Base64字符串是否有效
        /// </summary>
        /// <param name="base64String">要验证的Base64字符串</param>
        /// <returns>如果字符串有效，返回true；否则返回false</returns>
        /// <remarks>
        /// 此方法检查输入的字符串是否是有效的Base64编码。
        /// </remarks>
        private static bool IsValidBase64String(string base64String)
        {
            if (string.IsNullOrEmpty(base64String) || base64String.Length % 4 != 0)
            {
                return false;
            }
            try
            {
                Convert.FromBase64String(base64String);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 生成语音
        /// </summary>
        /// <param name="request">语音请求模型</param>
        /// <returns>包含生成语音的Base64编码字符串的响应模型</returns>
        [HttpPost("generate-vocal")]
        public IActionResult GenerateVocal([FromBody] VocalRequest request)
        {
            try
            {
                // 参数验证
                if (request == null)
                {
                    return BadRequest(new { title = "无效请求", detail = "请求体不能为空", status = 400 });
                }

                if (string.IsNullOrEmpty(request.Lyrics))
                {
                    return BadRequest(new { title = "参数错误", detail = "歌词内容不能为空", status = 400 });
                }

                if (string.IsNullOrEmpty(request.AudioData))
                {
                    return BadRequest(new { title = "参数错误", detail = "音频数据不能为空", status = 400 });
                }

                // 验证Base64格式
                if (!IsValidBase64String(request.AudioData))
                {
                    return BadRequest(new { title = "格式错误", detail = "音频数据不是有效的Base64编码", status = 400 });
                }

                // 解码Base64数据
                byte[] audioData;
                try
                {
                    audioData = Convert.FromBase64String(request.AudioData);
                }
                catch (FormatException ex)
                {
                    _logger.LogError(ex, "解码Base64音频数据失败");
                    return BadRequest(new { title = "解码错误", detail = "无法解码Base64音频数据", status = 400 });
                }

                // 解码旋律MIDI数据
                byte[] melodyMidi;
                try
                {
                    melodyMidi = Convert.FromBase64String(request.MelodyData);
                }
                catch (FormatException ex)
                {
                    _logger.LogError(ex, "解码Base64旋律数据失败");
                    return BadRequest(new { title = "解码错误", detail = "无法解码Base64旋律数据", status = 400 });
                }

                try
                {
                    // 调用VocalService生成人声
                    var vocalData = _vocalService.GenerateVocal(request.Lyrics, melodyMidi);
                    _logger.LogInformation("人声生成成功");

                    // 设置下载文件名和头部信息
                    string fileName = $"vocal_{DateTime.Now:yyyyMMdd_HHmmss}.wav";
                    Response.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                    {
                        FileName = fileName,
                        FileNameStar = fileName
                    }.ToString();

                    return File(vocalData, "audio/wav");
                }
                catch (ArgumentException ex)
                {
                    _logger.LogWarning(ex, "人声生成参数错误: {Message}", ex.Message);
                    return BadRequest(new { title = "参数错误", detail = ex.Message, status = 400 });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "人声生成过程中发生错误");
                    return StatusCode(500, new { title = "服务器错误", detail = "人声生成失败: " + ex.Message, status = 500 });
                }
            }
            catch (ArgumentException)
            {
                return BadRequest(new { title = "参数错误", detail = "无效的请求参数", status = 400 });
            }
            catch (Exception)
            {
                return StatusCode(500, new { title = "服务器错误", detail = "生成人声时发生错误", status = 500 });
            }
        }

        /// <summary>
        /// 处理人声音频效果
        /// </summary>
        /// <param name="request">音频效果请求对象</param>
        /// <returns>处理后的音频数据</returns>
        /// <response code="200">成功处理音频效果</response>
        /// <response code="400">无效的请求参数</response>
        /// <response code="500">服务器内部错误</response>
        [HttpPost("process-vocal")]
        [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<byte[]> ProcessVocal([FromBody] AIMusicCreator.Entity.AudioEffectRequest request)
        {
            // 参数验证
            if (request == null)
            {
                _logger.LogWarning("ProcessVocal - 接收到空请求对象");
                return BadRequest("请求对象不能为空");
            }

            if (string.IsNullOrEmpty(request.AudioData))
            {
                _logger.LogWarning("ProcessVocal - 接收到空音频数据");
                return BadRequest("音频数据不能为空");
            }

            // 验证Base64字符串格式
            if (!IsValidBase64String(request.AudioData))
            {
                _logger.LogWarning("ProcessVocal - 接收到无效的Base64音频数据");
                return BadRequest("音频数据不是有效的Base64格式");
            }

            try
            {
                _logger.LogInformation("ProcessVocal - 开始处理音频效果");
                var stopwatch = Stopwatch.StartNew();

                // 解码Base64音频数据
                byte[] audioBytes = Convert.FromBase64String(request.AudioData);
                _logger.LogInformation("ProcessVocal - 成功解码Base64音频数据，大小: {AudioSize} 字节", audioBytes.Length);

                // 应用音频效果
                byte[] processedAudio = audioBytes;

                // 根据接口可用方法应用基础音频效果
                // 1. 应用音量标准化（如果启用）
                if (request.ApplyCreative || request.ApplyReverb || request.ApplyEQ || request.ApplyCompressor)
                {
                    _logger.LogInformation("ProcessVocal - 应用音量标准化");
                    processedAudio = _audioEffectService.NormalizeVolume(processedAudio, 0.9f);
                }

                // 2. 应用低音增强（如果启用EQ且有低音增益）
                if (request.ApplyEQ && request.EqBass > 0)
                {
                    _logger.LogInformation("ProcessVocal - 应用低音增强，增益: {BassGain}dB", request.EqBass);
                    processedAudio = _audioEffectService.BoostBass(processedAudio, (float)request.EqBass);
                }

                // 3. 应用回声效果（模拟混响的简单版本）
                if (request.ApplyReverb)
                {
                    float delaySeconds = (float)(request.ReverbDecay * 0.5); // 使用混响衰减时间来模拟延迟
                    float decay = (float)request.ReverbWetDry; // 使用混响干湿比例作为衰减参数
                    _logger.LogInformation("ProcessVocal - 应用回声效果模拟混响，延迟: {DelaySeconds}s, 衰减: {Decay}", delaySeconds, decay);
                    processedAudio = _audioEffectService.AddEcho(processedAudio, delaySeconds, decay);
                }

                stopwatch.Stop();
                _logger.LogInformation("ProcessVocal - 音频效果处理完成，耗时: {ElapsedMilliseconds} 毫秒", stopwatch.ElapsedMilliseconds);

                // 返回处理后的音频数据
                return Ok(processedAudio);
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "ProcessVocal - Base64解码失败");
                return BadRequest("无效的Base64格式音频数据");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProcessVocal - 音频处理过程中发生错误");
                return StatusCode(StatusCodes.Status500InternalServerError, "音频处理失败: " + ex.Message);
            }
        }
    }
}