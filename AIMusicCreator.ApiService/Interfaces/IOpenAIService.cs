﻿﻿﻿﻿﻿﻿using System.Threading.Tasks;

namespace AIMusicCreator.ApiService.Interfaces;

/// <summary>
/// OpenAI服务接口，提供AI音乐创作相关功能
/// </summary>
public interface IOpenAIService
{
    /// <summary>
    /// 根据描述生成歌词
    /// </summary>
    /// <param name="description">歌词描述或主题</param>
    /// <param name="language">歌词语言</param>
    /// <returns>生成的歌词内容</returns>
    Task<string> GenerateLyricsAsync(string description, string language);
    
    /// <summary>
    /// 分析歌词情感
    /// </summary>
    /// <param name="lyrics">歌词内容</param>
    /// <returns>情感分析结果</returns>
    Task<string> AnalyzeLyricsEmotionAsync(string lyrics);
    
    /// <summary>
    /// 根据歌词生成音乐建议
    /// </summary>
    /// <param name="lyrics">歌词内容</param>
    /// <returns>音乐风格和节奏建议</returns>
    Task<string> GenerateMusicSuggestionsAsync(string lyrics);
    
    /// <summary>
    /// 根据描述生成AI助手响应
    /// </summary>
    /// <param name="prompt">用户提示内容</param>
    /// <returns>AI助手回复</returns>
    Task<string> GetAssistantResponseAsync(string prompt);
    
    /// <summary>
    /// 验证API密钥
    /// </summary>
    /// <param name="apiKey">API密钥</param>
    /// <returns>验证结果</returns>
    (bool IsValid, string ErrorMessage) ValidateApiKey(string apiKey);
    
    /// <summary>
    /// 执行聊天完成
    /// </summary>
    /// <param name="messages">聊天消息</param>
    /// <returns>聊天完成结果</returns>
    Task<string> ChatCompletionAsync(List<object> messages);
}