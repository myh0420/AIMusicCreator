using Microsoft.JSInterop;

namespace AIMusicCreator.Web.Services
{
    /// <summary>
    /// JavaScript 交互服务
    /// 用于在 Blazor 应用程序中调用 JavaScript 方法
    /// </summary>
    public class JsInteropService(IJSRuntime jsRuntime)
    {
        /// <summary>
        /// 调用 JavaScript 方法
        /// </summary>
        /// <typeparam name="TResult">返回值类型</typeparam>
        /// <param name="identifier">JavaScript 方法标识符</param>
        /// <param name="args">调用参数</param>
        /// <returns>JavaScript 方法返回值</returns>
        private readonly IJSRuntime _jsRuntime = jsRuntime;

        public async Task<string> InvokeJsMethodAsync(string identifier, params object?[]? args)
        {
            return await _jsRuntime.InvokeAsync<string>(identifier, args);
        }
        /// <summary>
        /// 调用 JavaScript 无返回值方法
        /// </summary>
        /// <param name="identifier">JavaScript 方法标识符</param>
        /// <param name="args">调用参数</param>
        public async Task InvokeVoidAsync(string identifier, params object?[]? args)
        {
            await _jsRuntime.InvokeVoidAsync(identifier, args);
        }
    }
}
