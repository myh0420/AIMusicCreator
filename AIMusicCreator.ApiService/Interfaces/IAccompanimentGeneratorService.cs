using AIMusicCreator.Entity; // 假设AccompanimentParameters和AudioData在此命名空间
using System.Threading.Tasks;

namespace AIMusicCreator.ApiService.Interfaces;

/// <summary>
/// 伴奏生成服务接口
/// 负责异步生成音乐伴奏
/// </summary>
public interface IAccompanimentGeneratorService
{
    /// <summary>
    /// 异步生成伴奏
    /// </summary>
    /// <param name="parameters">伴奏参数</param>
    /// <returns>生成的音频数据</returns>
    Task<AudioData> GenerateAccompanimentAsync(AccompanimentParameters parameters);
}