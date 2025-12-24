
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Polly;
using Polly.Retry;
namespace AIMusicCreator.Web.Services;
/// <summary>
/// s音频播放器服务接口
/// </summary>
public interface IAudioPlayerService
{
    /// <summary>
    /// 音频播放
    /// </summary>
    /// <param name="audioElement"></param>
    /// <returns>任务完成时返回null</returns>
    /// /// <remarks>
    /// 播放音频
    /// </remarks>
    Task PlayAsync(ElementReference audioElement);
    /// <summary>
    /// s音频暂停
    /// </summary>
    /// <param name="audioElement"></param>
    /// <returns>任务完成时返回null</returns>
    /// /// <remarks>
    /// 暂停音频播放
    /// </remarks>
    Task PauseAsync(ElementReference audioElement);
    /// <summary>
    /// s设置音量
    /// </summary>
    /// <param name="audioElement"></param>
    /// <param name="volume"></param>
    /// <returns>任务完成时返回null</returns>
    /// /// <remarks>
    /// 设置音频音量
    /// </remarks>
    Task SetVolumeAsync(ElementReference audioElement, float volume);
    /// <summary>
    /// s设置播放时间
    /// </summary>
    /// <param name="audioElement"></param>
    /// <param name="time"></param>
    /// <returns>任务完成时返回null</returns>
    /// /// <remarks>
    /// 设置音频播放时间
    /// </remarks>
    Task SetCurrentTimeAsync(ElementReference audioElement, double time);
    /// <summary>
    /// s获取当前播放时间
    /// </summary>
    /// <param name="audioElement"></param>
    /// <returns>当前播放时间（秒）</returns>
    /// /// <remarks>
    /// 获取音频当前播放时间
    /// </remarks>
    Task<double> GetCurrentTimeAsync(ElementReference audioElement);
    /// <summary>
    /// 获取音频总时长
    /// </summary>
    /// <param name="audioElement"></param>
    /// <returns>音频总时长（秒）</returns>
    /// /// <remarks>
    /// 获取音频总时长
    /// </remarks>
    Task<double> GetDurationAsync(ElementReference audioElement);
    // 
    /// <summary>
    /// s检查音频是否可播放
    /// </summary>
    /// <param name="audioElement"></param>
    /// <returns>音频是否可播放</returns>
    /// /// <remarks>
    /// 检查音频是否可播放
    /// </remarks>
    Task<bool> IsAudioReadyAsync(ElementReference audioElement);
    /// <summary>
    /// 检查音频是否播放结束
    /// </summary>
    /// <param name="audioElement"></param>
    /// <returns>音频是否播放结束</returns>
    /// /// <remarks>
    /// 检查音频是否播放结束
    /// </remarks>
    Task<bool> IsAudioEndedAsync(ElementReference audioElement);
    /// <summary>
    /// 监听音频加载事件
    /// </summary>
    /// <param name="audioElement"></param>
    /// <returns>任务完成时返回null</returns>
    /// /// <remarks>
    /// 等待音频加载完成
    /// </remarks>
    Task WaitForAudioLoadAsync(ElementReference audioElement);
    /// <summary>
    /// s等待音频准备就绪
    /// </summary>
    /// <param name="audioElement"></param>
    /// <param name="checkIntervalMs"></param>
    /// <param name="timeoutMs"></param>
    /// <returns>任务完成时返回null</returns>
    /// /// <remarks>
    /// 等待音频准备就绪
    /// </remarks>
    Task WaitForAudioReadyAsync(ElementReference audioElement, int checkIntervalMs = 100, int timeoutMs = 5000);
    // 事件处理方法
    /// <summary>
    /// s
    /// </summary>
    /// <param name="audioElement"></param>
    /// <param name="dotNetHelper"></param>
    /// <returns>任务完成时返回null</returns>
    /// /// <remarks>
    /// 为音频元素设置事件监听器
    /// </remarks>
    Task SetupAudioEventsAsync<T>(ElementReference audioElement, DotNetObjectReference<T> dotNetHelper) where T:class, new();
    /// <summary>
    /// s移除音频事件监听器
    /// </summary>
    /// <param name="audioElement"></param>
    /// <returns>任务完成时返回null</returns>
    /// /// <remarks>
    /// 移除音频元素的事件监听器
    /// </remarks>
    Task RemoveAudioEventsAsync(ElementReference audioElement);

}
/// <summary>
/// s音频播放器服务
/// </summary>
/// <param name="jsRuntime"></param>
public class AudioPlayerService(IJSRuntime jsRuntime, ConnectionStateService connectionState) : IAudioPlayerService, IAsyncDisposable
{
    /// <summary>
    /// s音频播放器服务构造函数
    /// </summary>
    /// <param name="jsRuntime"></param>
    private readonly IJSRuntime _jsRuntime = jsRuntime;
    //private readonly NavigationManager _navigationManager = navigationManager;
    /// <summary>
    /// s音频模块引用
    /// </summary>
    private IJSObjectReference? _audioModule;
    /// <summary>
    /// s连接状态服务
    /// </summary>
    private readonly ConnectionStateService _connectionState = connectionState;
    /// <summary>
    /// s是否已释放
    /// </summary>
    private bool _disposed = false;
    /// <summary>
    /// s异步重试策略
    /// </summary>
    private readonly AsyncRetryPolicy _retryPolicy = Policy
                .Handle<JSException>()
                .Or<TimeoutException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(1 * retryAttempt),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        Console.WriteLine($"重试 {retryCount} 次，异常: {exception.Message}");
                    });

    // 检查连接状态
    /// <summary>
    /// s检查是否已连接
    /// </summary>
    /// <returns>是否已连接</returns>
    /// <remarks>
    /// 检查是否已连接到服务器
    /// </remarks>
    private bool IsConnected()
    {
        // 在 Blazor Server 中，我们可以通过检查某些条件来判断连接状态
        // 这里我们主要依赖错误处理
        return !_disposed;
    }

    // 安全的 JS 调用方法
    /// <summary>
    /// s安全调用JS方法
    /// </summary>
    /// <typeparam name="T">返回类型</typeparam>
    /// <param name="methodName">JS方法名</param>
    /// <param name="args">参数</param>
    /// <returns>JS方法返回值</returns>
    /// <remarks>
    /// 安全调用JS方法，处理连接问题和重试
    /// </remarks>
    private async Task<T?> SafeJsInvokeAsync<T>(string methodName, params object[] args)
    {
        if (_disposed || !await _connectionState.CheckConnectionAsync())
            return default;
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            try
            {
                await InitializeAsync();
                return await _audioModule!.InvokeAsync<T>(methodName, args);
            }
            catch (JSException jsEx) when (jsEx.Message.Contains("connection") || jsEx.Message.Contains("disconnected"))
            {
                // 连接问题，返回默认值
                Console.WriteLine($"JS调用失败（连接问题）: {jsEx.Message}");
                _connectionState.SetDisconnected();
                return default;
            }
            catch (TaskCanceledException)
            {
                // 任务被取消（可能是连接断开）
                Console.WriteLine("JS调用被取消（可能连接断开）");
                //_connectionState.SetDisconnected();
                //return default;
                throw; // 重新抛出让重试策略处理
            }
            catch (InvalidOperationException ioEx) when (ioEx.Message.Contains("connection"))
            {
                Console.WriteLine($"无效操作（连接问题）: {ioEx.Message}");
                //_connectionState.SetDisconnected();
                //return default;
                throw; // 重新抛出让重试策略处理
            }
            catch (Exception ex) {
                Console.WriteLine($"异常: {ex.Message}");
                throw; // 重新抛出让重试策略处理
            }
        });
    }
    /// <summary>
    /// s安全调用JS方法（无返回值）
    /// </summary>
    /// <param name="methodName">JS方法名</param>
    /// <param name="args">参数</param>
    /// <returns>任务</returns>
    /// <remarks>
    /// 安全调用JS方法（无返回值），处理连接问题和重试
    /// </remarks>
    private async Task SafeJsInvokeVoidAsync(string methodName, params object[] args)
    {
        if (_disposed || !await _connectionState.CheckConnectionAsync())
            return;
        await _retryPolicy.ExecuteAsync(async () =>
        {
            try
            {
                await InitializeAsync();
                await _audioModule!.InvokeVoidAsync(methodName, args);
            }
            catch (JSException jsEx) when (jsEx.Message.Contains("connection") || jsEx.Message.Contains("disconnected"))
            {

                Console.WriteLine($"JS调用失败（连接问题）: {jsEx.Message}");
                _connectionState.SetDisconnected();
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("JS调用被取消（可能连接断开）");
                _connectionState.SetDisconnected();
            }
            catch (InvalidOperationException ioEx) when (ioEx.Message.Contains("connection"))
            {
                Console.WriteLine($"无效操作（连接问题）: {ioEx.Message}");
                _connectionState.SetDisconnected();
            }
            catch (Exception) { 
                throw;
            }
        });
    }

    // 初始化音频模块
    /// <summary>
    /// s初始化音频模块
    /// </summary>
    /// <returns>任务</returns>
    /// <remarks>
    /// 初始化音频模块，加载JS文件
    /// </remarks>
    public async Task InitializeAsync()
    {
        //ObjectDisposedException.ThrowIf(_disposed, nameof(AudioPlayerService));
        if (_disposed || !await _connectionState.CheckConnectionAsync())
            return;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        if (_audioModule == null)
        {
            try
            {
                _audioModule = await _jsRuntime.InvokeAsync<IJSObjectReference>(
                    "import", "./js/audioModule.js");
                //_audioModule ??= await _jsRuntime.InvokeAsync<IJSObjectReference>(
                //    "import", "./js/audioPlayer.js");
            }
            catch (JSDisconnectedException)
            {
                _connectionState.SetDisconnected();
                throw new TimeoutException("初始化音频模块超时");
            }
            catch (JSException jsEx) when (jsEx.Message.Contains("connection"))
            {
                _connectionState.SetDisconnected();
                Console.WriteLine($"初始化音频模块失败（连接问题）: {jsEx.Message}");
            }
            catch (TaskCanceledException)
            {
                _connectionState.SetDisconnected();
            }
        }
    }

    // 播放音频
    /// <summary>
    /// s播放音频
    /// </summary>
    /// <param name="audioElement">音频元素引用</param>
    /// <returns>任务</returns>
    /// <remarks>
    /// 播放音频，处理连接问题和重试
    /// </remarks>
    public async Task PlayAsync(ElementReference audioElement)
    {
        //await InitializeAsync();
        await SafeJsInvokeVoidAsync("playAudio", audioElement);
    }

    // 暂停音频
    /// <summary>
    /// s暂停音频
    /// </summary>
    /// <param name="audioElement">音频元素引用</param>
    /// <returns>任务</returns>
    /// <remarks>
    /// 暂停音频，处理连接问题和重试
    /// </remarks>
    public async Task PauseAsync(ElementReference audioElement)
    {
        //await InitializeAsync();
        await SafeJsInvokeVoidAsync("pauseAudio", audioElement);
    }

    // 设置音量
    /// <summary>
    /// s设置音量
    /// </summary>
    /// <param name="audioElement">音频元素引用</param>
    /// <param name="volume">音量（0-1之间）</param>
    /// <returns>任务</returns>
    /// <remarks>
    /// 设置音频音量，处理连接问题和重试
    /// </remarks>
    public async Task SetVolumeAsync(ElementReference audioElement, float volume)
    {
        //await InitializeAsync();
        await SafeJsInvokeVoidAsync("setVolume", audioElement, volume);
    }

    // 设置播放时间
    /// <summary>
    /// s设置播放时间
    /// </summary>
    /// <param name="audioElement">音频元素引用</param>
    /// <param name="time">播放时间（秒）</param>
    /// <returns>任务</returns>
    /// <remarks>
    /// 设置音频播放时间，处理连接问题和重试
    /// </remarks>
    public async Task SetCurrentTimeAsync(ElementReference audioElement, double time)
    {
        //await InitializeAsync();
        await SafeJsInvokeVoidAsync("setCurrentTime", audioElement, time);
    }

    // 获取当前播放时间
    /// <summary>
    /// s获取当前播放时间
    /// </summary>
    /// <param name="audioElement">音频元素引用</param>
    /// <returns>当前播放时间（秒）</returns>
    /// <remarks>
    /// 获取音频当前播放时间，处理连接问题和重试
    /// </remarks>
    public async Task<double> GetCurrentTimeAsync(ElementReference audioElement)
    {
        //await InitializeAsync();
        return await SafeJsInvokeAsync<double>("getCurrentTime", audioElement);
    }

    // 获取音频时长
    /// <summary>
    /// s获取音频时长
    /// </summary>
    /// <param name="audioElement">音频元素引用</param>
    /// <returns>音频时长（秒）</returns>
    /// <remarks>
    /// 获取音频时长，处理连接问题和重试
    /// </remarks>
    public async Task<double> GetDurationAsync(ElementReference audioElement)
    {
        //await InitializeAsync();
        return await SafeJsInvokeAsync<double>("getDuration", audioElement);
    }

    // 检查音频是否可播放
    /// <summary>
    /// s检查音频是否可播放
    /// </summary>
    /// <param name="audioElement">音频元素引用</param>
    /// <returns>是否可播放</returns>
    /// <remarks>
    /// 检查音频是否可播放，处理连接问题和重试
    /// </remarks>
    public async Task<bool> IsAudioReadyAsync(ElementReference audioElement)
    {
        //await InitializeAsync();
        return await SafeJsInvokeAsync<bool>("isAudioReady", audioElement);
    }
    /// <summary>
    /// 检查音频是否播放结束
    /// </summary>
    /// <param name="audioElement"></param>
    /// <returns>是否播放结束</returns>
    /// <remarks>
    /// 检查音频是否播放结束，处理连接问题和重试
    /// </remarks>
    public async Task<bool> IsAudioEndedAsync(ElementReference audioElement)
    {
        //await InitializeAsync();
        return await SafeJsInvokeAsync<bool>("isAudioEnded", audioElement);
    }
    /// <summary>
    /// 监听音频加载事件
    /// </summary>
    /// <param name="audioElement"></param>
    /// <returns>任务</returns>
    /// <remarks>
    /// 等待音频加载完成，处理连接问题和重试
    /// </remarks>
    public async Task WaitForAudioLoadAsync(ElementReference audioElement)
    {
        //await InitializeAsync();
        await SafeJsInvokeVoidAsync("waitForAudioLoad", audioElement);
    }
    /// <summary>
    /// s等待音频准备就绪
    /// </summary>
    /// <param name="audioElement"></param>
    /// <param name="checkIntervalMs"></param>
    /// <param name="timeoutMs"></param>
    /// <returns>任务</returns>
    /// <exception cref="TimeoutException"></exception>
    /// <remarks>
    /// 等待音频准备就绪，处理连接问题和重试
    /// </remarks>
    public async Task WaitForAudioReadyAsync(ElementReference audioElement, int checkIntervalMs = 100, int timeoutMs = 5000)
    {
        var totalWaitTime = 0;
        while (!await IsAudioReadyAsync(audioElement))
        {
            if (totalWaitTime >= timeoutMs)
            {
                throw new TimeoutException("等待音频准备就绪超时。");
            }
            await Task.Delay(checkIntervalMs);
            totalWaitTime += checkIntervalMs;
        }
    }

    // 事件处理方法
    /// <summary>
    /// s
    /// </summary>
    /// <param name="audioElement"></param>
    /// <param name="dotNetHelper"></param>
    /// <returns>任务</returns>
    /// <remarks>
    /// 设置音频事件，处理连接问题和重试
    /// </remarks>
    public async Task SetupAudioEventsAsync<T>(ElementReference audioElement, DotNetObjectReference<T> dotNetHelper) where T : class,new()
    {
        //await InitializeAsync();
        //await _audioModule!.InvokeVoidAsync("setupAudioEvents", audioElement, dotNetHelper);
        await SafeJsInvokeVoidAsync("setupAudioEvents", audioElement, dotNetHelper);
    }
    /// <summary>
    /// s
    /// </summary>
    /// <param name="audioElement"></param>
    /// <returns>任务</returns>
    /// <remarks>
    /// 移除音频事件，处理连接问题和重试
    /// </remarks>
    public async Task RemoveAudioEventsAsync(ElementReference audioElement)
    {
        //if (_audioModule != null && !_disposed)
        //{
        //    await _audioModule.InvokeVoidAsync("removeAudioEvents", audioElement);
        //}
        await SafeJsInvokeVoidAsync("removeAudioEvents", audioElement);
    }
    /// <summary>
    /// s释放异步资源
    /// </summary>
    /// <returns>任务</returns>
    /// <remarks>
    /// 释放异步资源，处理连接问题和重试
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            if (_audioModule != null)
            {
                await _audioModule.DisposeAsync();
                _audioModule = null;
            }
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
    //protected virtual async ValueTask DisposeAsyncCore()
    //{
    //    if (!_disposed)
    //    {
    //        if (_audioModule != null)
    //        {
    //            await _audioModule.DisposeAsync();
    //            _audioModule = null;
    //        }
    //        _disposed = true;
    //    }
    //}

    // 析构函数（可选，如果没有非托管资源可以省略）
    //~AudioPlayerService()
    //{
    //    DisposeAsyncCore().AsTask().Wait();
    //}
}


