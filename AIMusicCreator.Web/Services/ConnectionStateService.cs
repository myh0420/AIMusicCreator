using Microsoft.JSInterop;

﻿namespace AIMusicCreator.Web.Services
{
    /// <summary>
    /// 连接状态服务
    /// 用于跟踪和管理应用程序与服务器的连接状态
    /// </summary>
    public class ConnectionStateService(IJSRuntime jsRuntime)
    {
        /// <summary>
        /// JavaScript 运行时
        /// 用于与浏览器的 JavaScript 交互
        /// </summary>
        private readonly IJSRuntime _jsRuntime = jsRuntime;
        /// <summary>
        /// 连接状态
        /// 表示当前是否已连接到服务器
        /// </summary>
        private bool _isConnected = true;
        /// <summary>
        /// 最后断开时间
        /// 记录上次连接断开的时间（如果已断开）
        /// </summary>
        private DateTime? _lastDisconnectedTime;

        /// <summary>
        /// 获取当前连接状态
        /// </summary>
        public bool IsConnected => _isConnected;

        /// <summary>
        /// 获取最后断开时间
        /// </summary>
        public DateTime? LastDisconnectedTime => _lastDisconnectedTime;

        /// <summary>
        /// 设置连接状态为断开
        /// </summary>
        public void SetDisconnected()
        {
            if (_isConnected)
            {
                _isConnected = false;
                _lastDisconnectedTime = DateTime.UtcNow;
                Console.WriteLine($"连接断开于: {_lastDisconnectedTime}");
            }
        }
        /// <summary>
        /// 设置连接状态为已连接
        /// </summary>
        public void SetConnected()
        {
            if (!_isConnected)
            {
                _isConnected = true;
                _lastDisconnectedTime = null;
                Console.WriteLine("连接已恢复");
            }
        }

        // 检查连接状态的通用方法
        /// <summary>
        /// 检查连接状态
        /// 尝试通过简单的 JavaScript 调用来测试连接是否正常
        /// </summary>
        /// <returns>如果连接正常则返回 true，否则返回 false</returns>
        public async Task<bool> CheckConnectionAsync()
        {
            if (!_isConnected)
                return false;

            try
            {
                // 简单的 JS 调用来测试连接
                await _jsRuntime.InvokeVoidAsync("eval", "0");

                // 如果调用成功，确保状态为已连接
                SetConnected();
                return true;
            }
            catch (JSDisconnectedException)
            {
                SetDisconnected();
                return false;
            }
            catch (TaskCanceledException)
            {
                SetDisconnected();
                return false;
            }
            catch (InvalidOperationException) when (!_isConnected)
            {
                // 连接已断开时的无效操作异常
                return false;
            }
        }

        // 强制重新连接检查
        /// <summary>
        /// 强制重新连接检查
        /// 尝试通过简单的 JavaScript 调用来强制重新连接
        /// </summary>
        /// <returns>如果重新连接成功则返回 true，否则返回 false</returns>
        public async Task<bool> ForceReconnectCheckAsync()
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("eval", "0");
                SetConnected();
                return true;
            }
            catch
            {
                SetDisconnected();
                return false;
            }
        }
    }
}