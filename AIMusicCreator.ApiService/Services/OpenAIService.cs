using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using AIMusicCreator.ApiService.Interfaces;

namespace AIMusicCreator.ApiService.Services;
/// <summary>
/// 提供与OpenAI API交互的服务
/// </summary>
public class OpenAIService : IOpenAIService
{
    /// <summary>
    /// 提供与OpenAI API交互的服务
    /// </summary>
    private readonly HttpClient _httpClient;
    /// <summary>
    /// OpenAI API密钥
    /// </summary>
    private readonly string _apiKey;
    /// <summary>
    /// API端点URL
    /// </summary>
    private readonly string _apiEndpoint;
    /// <summary>
    /// 日志记录器
    /// </summary>
    private readonly ILogger<OpenAIService> _logger;
    /// <summary>
    /// JSON序列化选项
    /// </summary>
    private readonly JsonSerializerOptions _jsonOptions;
    /// <summary>
    /// 默认模型名称
    /// </summary>
    private const string DEFAULT_MODEL = "gpt-3.5-turbo";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="httpClient">HTTP客户端</param>
    /// <param name="apiKey">OpenAI API密钥</param>
    /// <param name="apiEndpoint">API端点URL</param>
    /// <param name="logger">日志记录器</param>
    /// <exception cref="ArgumentNullException">当httpClient、apiKey、apiEndpoint或logger为null时抛出</exception>
    /// <exception cref="ArgumentException">当apiKey或apiEndpoint为空时抛出</exception>
    /// <remarks>
    /// 此构造函数初始化OpenAIService实例，配置HTTP客户端、API密钥、API端点和日志记录器。
    /// 它还设置JSON序列化选项，用于处理API响应。
    /// </remarks>
    public OpenAIService(HttpClient httpClient, string apiKey, string apiEndpoint, ILogger<OpenAIService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _apiEndpoint = apiEndpoint ?? throw new ArgumentNullException(nameof(apiEndpoint));
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        
        // 验证API密钥
        var (IsValid, ErrorMessage) = ValidateApiKey(apiKey);
        if (!IsValid)
        {
            _logger.LogWarning("API密钥验证失败: {ErrorMessage}", ErrorMessage);
        }
    }

    /// <summary>
    /// 根据描述生成歌词
    /// </summary>
    /// <param name="description">歌词描述</param>
    /// <param name="language">歌词语言</param>
    /// <returns>生成的歌词内容</returns>
    /// <exception cref="ArgumentException">当描述或语言为空时抛出</exception>
    /// <exception cref="HttpRequestException">当歌词生成API调用失败时抛出</exception>
    /// <exception cref="JsonException">当歌词生成API返回的JSON解析失败时抛出</exception>
    /// <exception cref="InvalidOperationException">当歌词生成API返回的响应格式无效时抛出</exception>
    /// <remarks>
    /// 此方法负责与歌词生成API进行交互，发送描述和语言并接收生成的歌词。
    /// 它处理请求体的创建、API调用、响应解析和错误处理。
    /// </remarks>
    public async Task<string> GenerateLyricsAsync(string description, string language)
    {
        try
        {
            _logger.LogInformation("开始生成歌词，描述: {Description}, 语言: {Language}", 
                description.Length > 50 ? description[..50] + "..." : description, language);
            
            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentException("歌词描述不能为空");
            }
            
            if (string.IsNullOrWhiteSpace(language))
            {
                throw new ArgumentException("歌词语言不能为空");
            }

            string prompt = $"生成一首{language}歌词，主题是'{description}'。请以纯文本形式返回，不要包含其他说明。";
            var response = await CallOpenAIAsync(prompt);
            
            _logger.LogInformation("歌词生成完成，长度: {Length} 字符", response.Length);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成歌词失败");
            throw;
        }
    }

    /// <summary>
    /// 分析歌词情感
    /// </summary>
    /// <param name="lyrics">要分析的歌词内容</param>
    /// <returns>分析结果内容</returns>
    /// <exception cref="ArgumentException">当歌词内容为空时抛出</exception>
    /// <exception cref="HttpRequestException">当情感分析API调用失败时抛出</exception>
    /// <exception cref="JsonException">当情感分析API返回的JSON解析失败时抛出</exception>
    /// <exception cref="InvalidOperationException">当情感分析API返回的响应格式无效时抛出</exception>
    /// <remarks>
    /// 此方法负责与情感分析API进行交互，发送歌词并接收情感分析结果。
    /// 它处理请求体的创建、API调用、响应解析和错误处理。
    /// </remarks>
    public async Task<string> AnalyzeLyricsEmotionAsync(string lyrics)
    {
        try
        {
            _logger.LogInformation("开始分析歌词情感");
            
            if (string.IsNullOrWhiteSpace(lyrics))
            {
                throw new ArgumentException("歌词内容不能为空");
            }

            string prompt = $"分析以下歌词的情感类型：\n\n{lyrics}\n\n请返回主要情感类型（如：快乐、悲伤、激动、平静等）及情感强度（1-10分）。";
            var response = await CallOpenAIAsync(prompt);
            
            _logger.LogInformation("歌词情感分析完成");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分析歌词情感失败");
            throw;
        }
    }

    /// <summary>
    /// 根据歌词生成音乐建议
    /// </summary>
    /// <param name="lyrics">要分析的歌词内容</param>
    /// <returns>音乐建议内容</returns>
    /// <exception cref="ArgumentException">当歌词内容为空时抛出</exception>
    /// <exception cref="HttpRequestException">当音乐建议API调用失败时抛出</exception>
    /// <exception cref="JsonException">当音乐建议API返回的JSON解析失败时抛出</exception>
    /// <exception cref="InvalidOperationException">当音乐建议API返回的响应格式无效时抛出</exception>
    /// <remarks>
    /// 此方法负责与音乐建议API进行交互，发送歌词并接收建议。
    /// 它处理请求体的创建、API调用、响应解析和错误处理。
    /// </remarks>
    public async Task<string> GenerateMusicSuggestionsAsync(string lyrics)
    {
        try
        {
            _logger.LogInformation("开始生成音乐建议");
            
            if (string.IsNullOrWhiteSpace(lyrics))
            {
                throw new ArgumentException("歌词内容不能为空");
            }

            string prompt = $"根据以下歌词内容，推荐适合的音乐风格、速度（BPM）和调性：\n\n{lyrics}\n\n请以简洁的格式返回建议。";
            var response = await CallOpenAIAsync(prompt);
            
            _logger.LogInformation("音乐建议生成完成");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成音乐建议失败");
            throw;
        }
    }

    /// <summary>
    /// 根据描述生成AI助手响应
    /// </summary>
    /// <param name="prompt">要发送给AI助手的提示内容</param>
    /// <returns>AI助手返回的响应内容</returns>
    /// <exception cref="ArgumentException">当提示内容为空时抛出</exception>
    /// <exception cref="HttpRequestException">当AI助手API调用失败时抛出</exception>
    /// <exception cref="JsonException">当AI助手API返回的JSON解析失败时抛出</exception>
    /// <exception cref="InvalidOperationException">当AI助手API返回的响应格式无效时抛出</exception>
    /// <remarks>
    /// 此方法负责与AI助手API进行交互，发送提示并接收响应。
    /// 它处理请求体的创建、API调用、响应解析和错误处理。
    /// </remarks>
    public async Task<string> GetAssistantResponseAsync(string prompt)
    {
        try
        {
            _logger.LogInformation("开始获取AI助手响应，提示词长度: {PromptLength}", prompt.Length);
            
            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException("提示内容不能为空");
            }

            var response = await CallOpenAIAsync(prompt);
            
            _logger.LogInformation("AI助手响应获取完成，响应长度: {ResponseLength}", response.Length);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取AI助手响应失败");
            throw;
        }
    }

    /// <summary>
    /// 验证API密钥
    /// </summary>
    /// <param name="apiKey">要验证的API密钥</param>
    /// <returns>一个元组，包含验证结果（是否有效）和错误消息（如果无效）</returns>
    /// <remarks>
    /// 此方法验证API密钥是否符合OpenAI的要求。
    /// 它检查密钥是否为空、是否以"sk-"开头，以及是否长度超过30个字符。
    /// </remarks>
    public (bool IsValid, string ErrorMessage) ValidateApiKey(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return (false, "API密钥不能为空");
        }
        
        // 简单的格式验证
        if (apiKey.StartsWith("sk-") && apiKey.Length > 30)
        {
            return (true, string.Empty);
        }
        
        return (false, "API密钥格式无效");
    }

    /// <summary>
    /// 调用OpenAI API的核心方法
    /// </summary>
    /// <param name="prompt">要发送给OpenAI的提示内容</param>
    /// <returns>OpenAI API返回的响应内容</returns>
    /// <exception cref="HttpRequestException">当OpenAI API调用失败时抛出</exception>
    /// <exception cref="JsonException">当OpenAI API返回的JSON解析失败时抛出</exception>
    /// <exception cref="InvalidOperationException">当OpenAI API返回的响应格式无效时抛出</exception>
    /// <remarks>
    /// 此方法负责与OpenAI API进行交互，发送提示并接收响应。
    /// 它处理请求体的创建、API调用、响应解析和错误处理。
    /// </remarks>
    private async Task<string> CallOpenAIAsync(string prompt)
    {
        try
        {
            // 准备请求体
            var requestBody = new
            {
                model = DEFAULT_MODEL,
                messages = new[]
                {
                    new { role = "system", content = "你是一个音乐创作助手。请以专业、简洁的方式回应。" },
                    new { role = "user", content = prompt }
                },
                temperature = 0.7,
                max_tokens = 1000
            };
            
            var jsonBody = JsonSerializer.Serialize(requestBody, _jsonOptions);
            
            // 创建请求
            var request = new HttpRequestMessage(HttpMethod.Post, _apiEndpoint)
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };
            
            // 设置认证头
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            // 发送请求
            _logger.LogDebug("发送请求到OpenAI API");
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            
            // 读取响应内容
            var responseContent = await response.Content.ReadAsStringAsync();
            
            // 检查响应状态
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("OpenAI API返回错误: {StatusCode} - {Content}", 
                    response.StatusCode, responseContent);
                throw new HttpRequestException($"OpenAI API调用失败: {response.StatusCode}", null, response.StatusCode);
            }
            
            // 解析响应
            var result = JsonSerializer.Deserialize<OpenAIResponse>(responseContent, _jsonOptions);
            
            if (result?.Choices == null || result.Choices.Length == 0)
            {
                throw new InvalidOperationException("OpenAI API返回无效响应");
            }
            
            var firstChoice = result.Choices[0];
            if (firstChoice.Message == null || firstChoice.Message.Content == null)
            {
                throw new InvalidOperationException("OpenAI API返回无效响应");
            }
            
            string content = firstChoice.Message.Content;
            return content;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "OpenAI API请求异常");
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "解析OpenAI API响应异常");
            throw new InvalidOperationException("解析OpenAI API响应失败", ex);
        }
    }

    #region 内部模型类
    /// <summary>
    /// OpenAI API响应模型
    /// </summary>
    private class OpenAIResponse
    {
        /// <summary>
        /// 唯一标识符
        /// </summary>
        public string? Id { get; set; }
        /// <summary>
        /// 对象类型
        /// </summary>
        public string? Object { get; set; }
        /// <summary>
        /// 创建时间（Unix时间戳）
        /// </summary>
        public long Created { get; set; }
        /// <summary>
        /// 使用的模型
        /// </summary>
        public string? Model { get; set; }
        /// <summary>
        /// 生成的选择
        /// </summary>
        public OpenAIChoice[]? Choices { get; set; }
        /// <summary>
        /// 令牌使用统计
        /// </summary>
        public OpenAIUsage? Usage { get; set; }
    }
    /// <summary>
    /// 生成选择
    /// </summary>
    private class OpenAIChoice
    {
        /// <summary>
        /// 选择索引
        /// </summary>
        public int Index { get; set; }
        /// <summary>
        /// 消息内容
        /// </summary>
        public OpenAIMessage? Message { get; set; }
        /// <summary>
        /// 完成原因
        /// </summary>
        public string? FinishReason { get; set; }
    }
    /// <summary>
    /// 消息内容
    /// </summary>
    private class OpenAIMessage
    {
        /// <summary>
        /// 消息角色
        /// </summary>
        public string? Role { get; set; }
        /// <summary>
        /// 消息内容
        /// </summary>
        public string? Content { get; set; }
    }
    /// <summary>
    /// 令牌使用统计
    /// </summary>
    private class OpenAIUsage
    {
        /// <summary>
        /// 提示令牌数
        /// </summary>
        public int PromptTokens { get; set; }
        /// <summary>
        /// 完成令牌数
        /// </summary>
        public int CompletionTokens { get; set; }
        /// <summary>
        /// 总令牌数
        /// </summary>
        public int TotalTokens { get; set; }
    }
    
    #endregion

    /// <summary>
    /// 执行聊天完成
    /// </summary>
    public async Task<string> ChatCompletionAsync(List<object> messages)
    {
        try
        {
            _logger.LogInformation("开始执行聊天完成操作");
            
            // 验证输入
            if (messages == null || messages.Count == 0)
            {
                throw new ArgumentException("消息列表不能为空");
            }
            
            // 设置请求头
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _apiKey);
            
            // 准备请求体
            var requestBody = new
            {
                model = DEFAULT_MODEL,
                messages
            };
            
            var jsonContent = JsonSerializer.Serialize(requestBody, _jsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            // 发送请求到OpenAI API
            _logger.LogInformation("向OpenAI API发送聊天完成请求");
            
            // 这里将实现调用OpenAI API的逻辑
            // 目前返回一个空字符串作为占位符
            _logger.LogInformation("聊天完成请求处理完成");
            await Task.CompletedTask;
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "聊天完成操作失败");
            throw;
        }
    }
}