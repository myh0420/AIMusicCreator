using AIMusicCreator.ApiService.Services;
using AIMusicCreator.ApiService.Interfaces;
using AIMusicCreator.Entity;
using Microsoft.AspNetCore.Mvc;
using AIMusicCreator.Utils;
using System.Text.Json;

namespace AIMusicCreator.ApiService.Controllers
{
    /// <summary>
    /// 提供AI服务的控制器
    /// </summary>
    [ApiController]
    [Route("api/ai")]
    public class AiController(IOpenAIService openAIService) : ControllerBase
    {
        /// <summary>
        /// 提供AI服务的控制器
        /// </summary>
        private readonly IOpenAIService _openAIService = openAIService ?? throw new ArgumentNullException(nameof(openAIService));

        /// <summary>
        /// AI生成歌词（基于ChatGPT-3.5-turbo）
        /// </summary>
        /// <param name="request">歌词请求参数</param>
        /// <returns>生成的歌词文本</returns>
        /// <exception cref="ArgumentException">输入数据无效</exception>
        /// <remarks>
        /// 生成专业的歌词，考虑音乐理论知识。如果输入参数无效，将返回默认值。
        /// </remarks>
        [HttpPost("generate-lyrics")]
        public async Task<IActionResult> GenerateLyrics([FromBody] AiLyricRequest request)
        {
            // 构造精准提示词（Prompt Engineering）
            var prompt = $@"请生成一首{request.Style}风格的歌词，主题是「{request.Theme}」，共{request.ParagraphCount}个段落。
                            要求：
                            1. 每段4-6句，每句字数相近（符合中文歌词韵律）
                            2. 语言简洁优美，有画面感，避免空洞
                            3. 不添加任何额外解释，只返回歌词文本
                            4. 格式：段落1、段落2... 开头，无需标题";

            // 将string转换为List<object>格式的消息列表
            var messages = new List<object>
            {
                new { role = "system", content = "你是一个音乐创作助手。请以专业、简洁的方式回应。" },
                new { role = "user", content = prompt }
            };
            
            var lyrics = await _openAIService.ChatCompletionAsync(messages);
            return Content(lyrics.Trim(), "text/plain; charset=utf-8"); // 指定UTF-8避免中文乱码
        }

        /// <summary>
        /// AI生成和弦进行（基于ChatGPT-3.5-turbo，结合音乐理论）
        /// </summary>
        /// <param name="request">和弦请求参数</param>
        /// <returns>和弦进行结果（和弦进行字符串和说明）</returns>
        /// <exception cref="ArgumentException">输入数据无效</exception>
        /// <remarks>
        /// 生成专业的和弦进行，考虑音乐理论知识。如果输入参数无效，将返回默认值。
        /// </remarks>
        [HttpPost("generate-chord-progression")]
        public async Task<IActionResult> GenerateChordProgression([FromBody] AiChordRequest request)
        {
            var prompt = $@"请为{request.Key}调、{request.Style}风格、{request.Section}段落，生成一个专业的和弦进行。
                            要求：
                            1. 和弦用简谱表示（如C、Am、F、G），用「-」连接，长度适合对应段落（主歌4-8小节，副歌8-16小节）
                            2. 附带1-2句简短说明：说明和弦进行的特点、情感表达、为何适合该风格/段落
                            3. 格式：先输出和弦进行，再换行输出说明，不要多余内容";

            // 将string转换为List<object>格式的消息列表
            var messages = new List<object>
            {
                new { role = "system", content = "你是一个音乐创作助手。请以专业、简洁的方式回应。" },
                new { role = "user", content = prompt }
            };
            
            var result = await _openAIService.ChatCompletionAsync(messages);
            var lines = result.Trim().Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

            // 解析结果（容错处理）
            var progression = lines.FirstOrDefault() ?? $"{request.Key} - Am - F - G";
            var explanation = lines.Length > 1 ? string.Join(" ", lines.Skip(1)) : "该和弦进行符合风格特点，情感表达贴切";

            return new JsonResult(new ChordProgressionResult
            {
                Progression = progression,
                Explanation = explanation
            });
        }

        // 辅助模型类（与前端ApiService一致）
        //public class AiLyricRequest
        //{
        //    public string Theme { get; set; } = "";
        //    public string Style { get; set; } = "pop";
        //    public int ParagraphCount { get; set; } = 2;
        //}

        //public class AiChordRequest
        //{
        //    public string Key { get; set; } = "C";
        //    public string Style { get; set; } = "pop";
        //    public string Section { get; set; } = "verse";
        //}

        //public class ChordProgressionResult
        //{
        //    public string Progression { get; set; } = "";
        //    public string Explanation { get; set; } = "";
        //}
    }
}