using AIMusicCreator.ApiService.Services.DryWetMidiGerenteMidi;
using Microsoft.Extensions.DependencyInjection;
using AIMusicCreator.ApiService.Interfaces;
using Microsoft.Extensions.Logging;

namespace AIMusicCreator.ApiService.Services
{
    /// <summary>
    /// 服务集合扩展类
    /// 用于注册应用程序所需的服务
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 注册音乐生成相关服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合（支持链式调用）</returns>
        public static IServiceCollection AddMusicGenerationServices(this IServiceCollection services)
        {
            // 注册伴奏生成器服务
            // 注册接口和实现
            services.AddSingleton<IAccompanimentGenerator, AccompanimentGenerator>();

            // 注册新增的服务
            services.AddScoped<IAccompanimentGeneratorService, AccompanimentGeneratorService>();
            services.AddScoped<IAudioExportService, AudioExportService>();
            services.AddScoped<IWaveGeneratorService, WaveGeneratorService>();
            // 修改MidiFileGenerator服务注册，使用工厂方法提供正确的ILogger实例
            services.AddScoped<IMidiFileGenerator>(provider => 
            {
                var logger = provider.GetRequiredService<ILogger<MidiFileGenerator>>();
                return new MidiFileGenerator(logger);
            });

            return services;
        }
    }
}