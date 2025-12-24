using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AIMusicCreator.ApiService.Services;
using AIMusicCreator.ApiService.Interfaces;
using AIMusicCreator.Entity;

namespace AIMusicCreator.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccompanimentGeneratorController : ControllerBase
{
    private readonly ILogger<AccompanimentGeneratorController> _logger;
    private readonly IAccompanimentGeneratorService _accompanimentService;
    private readonly IWaveGeneratorService _waveGeneratorService;
    private readonly IAudioExportService _audioExportService;

    public AccompanimentGeneratorController(
        ILogger<AccompanimentGeneratorController> logger,
        IAccompanimentGeneratorService accompanimentService,
        IWaveGeneratorService waveGeneratorService,
        IAudioExportService audioExportService)
    {
        _logger = logger;
        _accompanimentService = accompanimentService;
        _waveGeneratorService = waveGeneratorService;
        _audioExportService = audioExportService;
    }

    /// <summary>
    /// 生成伴奏
    /// </summary>
    /// <param name="request">伴奏生成请求参数</param>
    /// <returns>生成的伴奏音频文件</returns>
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateAccompaniment([FromBody] AccompanimentRequest request)
    {
        try
        {
            _logger.LogInformation("开始生成伴奏，风格: {Style}, 和弦进行: {Chords}, BPM: {Bpm}", 
                request.Style, request.ChordProgression, request.Bpm);

            // 创建伴奏参数
            var parameters = new AIMusicCreator.Entity.AccompanimentParameters
            {
                Style = MapStyleToEnum(request.Style),
                ChordProgression = request.ChordProgression,
                Bpm = request.Bpm,
                Instrumentation = MapInstrumentation(request.Instrumentation),
                IncludeDrums = request.IncludeDrums
            };

            // 生成伴奏
            var audioData = await _accompanimentService.GenerateAccompanimentAsync(parameters);

            // 导出为WAV格式
            using var stream = new MemoryStream();
            _audioExportService.ExportToWav(audioData, stream);
            stream.Position = 0;

            _logger.LogInformation("伴奏生成完成");
            
            return File(stream, "audio/wav", $"accompaniment_{request.Style}_{request.Bpm}.wav");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成伴奏失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取可用的音乐风格列表
    /// </summary>
    [HttpGet("styles")]
    public IActionResult GetAvailableStyles()
    {
        var styles = new[]
        {
            new { value = "pop", label = "流行" },
            new { value = "rock", label = "摇滚" },
            new { value = "jazz", label = "爵士" },
            new { value = "classical", label = "古典" },
            new { value = "electronic", label = "电子" }
        };

        return Ok(styles);
    }

    /// <summary>
    /// 获取可用的和弦进行列表
    /// </summary>
    [HttpGet("chord-progressions")]
    public IActionResult GetChordProgressions()
    {
        var progressions = new[]
        {
            new { value = "I-IV-V", label = "I-IV-V (经典流行)" },
            new { value = "I-V-vi-IV", label = "I-V-vi-IV (流行热曲)" },
            new { value = "vi-IV-I-V", label = "vi-IV-I-V (抒情)" },
            new { value = "I-vi-IV-V", label = "I-vi-IV-V (卡农进行)" },
            new { value = "ii-V-I", label = "ii-V-I (爵士进行)" }
        };

        return Ok(progressions);
    }

    /// <summary>
    /// 将字符串风格映射为枚举
    /// </summary>
    /// <param name="style">风格字符串</param>
    /// <returns>伴奏风格枚举</returns>
    private AccompanimentStyle MapStyleToEnum(string style)
    {
        return style?.ToLower() switch
        {
            "rock" => AccompanimentStyle.Rock,
            "jazz" => AccompanimentStyle.Jazz,
            "classical" => AccompanimentStyle.Classical,
            "electronic" => AccompanimentStyle.Electronic,
            _ => AccompanimentStyle.Pop
        };
    }
    
    /// <summary>
    /// 将字符串配置映射为乐器配置对象
    /// </summary>
    /// <param name="instrumentation">乐器配置字符串</param>
    /// <returns>乐器配置对象</returns>
    private InstrumentationConfiguration MapInstrumentation(string instrumentation)
    {
        return instrumentation?.ToLower() switch
        {
            "rhythm" => new InstrumentationConfiguration { Drums = true, Bass = true, Guitar = false, Keyboards = false },
            "acoustic" => new InstrumentationConfiguration { Drums = true, Bass = false, Guitar = true, Keyboards = false },
            "electronic" => new InstrumentationConfiguration { Drums = true, Bass = true, Guitar = false, Keyboards = true },
            _ => new InstrumentationConfiguration { Drums = true, Bass = true, Guitar = true, Keyboards = true }
        };
    }
}