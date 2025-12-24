// 连接状态处理
window.connectionHandler = {
    isConnected: true,

    checkConnection: function () {
        // 检查 Blazor 连接状态
        if (typeof Blazor !== 'undefined' && Blazor.defaultReconnectionHandler) {
            return Blazor.defaultReconnectionHandler._state === 'Connected';
        }
        return true;
    },

    onConnectionLost: function () {
        console.log('Blazor connection lost');
        // 可以显示重连界面或通知用户
        const event = new CustomEvent('blazor-connection-lost');
        document.dispatchEvent(event);
    },

    onConnectionRestored: function () {
        console.log('Blazor connection restored');
        const event = new CustomEvent('blazor-connection-restored');
        document.dispatchEvent(event);
    }
};

// 监听连接状态变化
if (typeof Blazor !== 'undefined') {
    Blazor.defaultReconnectionHandler._reconnectionDisplay = {
        show: () => connectionHandler.onConnectionLost(),
        hide: () => connectionHandler.onConnectionRestored(),
        failed: () => {
            console.log('Reconnection failed');
            // 重连失败，建议刷新页面
            setTimeout(() => window.location.reload(), 3000);
        }
    };
}