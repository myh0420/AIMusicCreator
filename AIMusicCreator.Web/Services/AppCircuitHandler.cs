using Microsoft.AspNetCore.Components.Server.Circuits;

namespace AIMusicCreator.Web.Services
{
    /// <summary>
    /// 应用程序电路处理程序，用于处理电路打开、关闭、连接上下线事件
    /// </summary>
    public class AppCircuitHandler(ConnectionStateService connectionState, ILogger<AppCircuitHandler> logger) : CircuitHandler
    {
        /// <summary>
        /// 当电路打开时调用
        /// </summary>
        /// <param name="circuit">打开的电路实例</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>任务完成时返回null</returns>
        /// <exception cref="InvalidOperationException">当连接断开时抛出</exception>
        /// <exception cref="Exception">其他异常时抛出</exception>
        /// <remarks>
        /// 当电路打开时，记录电路ID并更新连接状态为已连接
        /// </remarks>
        private readonly ConnectionStateService _connectionState = connectionState;
        /// <summary>
        /// 应用程序电路处理程序的日志记录器
        /// </summary>
        private readonly ILogger<AppCircuitHandler> _logger = logger;

        /// <summary>
        /// 当电路打开时调用，记录电路ID并更新连接状态为已连接
        /// </summary>
        /// <param name="circuit">打开的电路实例</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>任务完成时返回null</returns>
        /// /// <remarks>
        /// 当电路打开时，记录电路ID并更新连接状态为已连接
        /// </remarks>
        public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Circuit opened: {CircuitId}", circuit.Id);
            // 电路打开时设置连接状态为已连接
            _connectionState.SetConnected();
            return base.OnCircuitOpenedAsync(circuit, cancellationToken);
        }
        /// <summary>
        /// 当电路关闭时调用，记录电路ID并更新连接状态为断开
        /// </summary>
        /// <param name="circuit">关闭的电路实例</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>任务完成时返回null</returns>
        /// /// <remarks>
        /// 当电路关闭时，记录电路ID并更新连接状态为断开
        /// </remarks>
        public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Circuit closed: {CircuitId}", circuit.Id);
            // 电路关闭时设置连接状态为断开
            _connectionState.SetDisconnected();
            return base.OnCircuitClosedAsync(circuit, cancellationToken);
        }
        /// <summary>
        /// 当连接断开时调用，记录电路ID并更新连接状态为断开
        /// </summary>
        /// <param name="circuit">断开连接的电路实例</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>任务完成时返回null</returns>
        /// /// <remarks>
        /// 当连接断开时，记录电路ID并更新连接状态为断开
        /// </remarks>
        public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Connection down: {CircuitId}", circuit.Id);
            // 连接断开时设置连接状态
            _connectionState.SetDisconnected();
            return base.OnConnectionDownAsync(circuit, cancellationToken);
        }
        /// <summary>
        /// 当连接恢复时调用，记录电路ID
        /// </summary>
        /// <param name="circuit">恢复连接的电路实例</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>任务完成时返回null</returns>
        /// /// <remarks>
        /// 当连接恢复时，记录电路ID
        /// </remarks>
        public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Connection up: {CircuitId}", circuit.Id);
            // 连接恢复时，ConnectionStateService 会在下次检查时自动更新状态
            return base.OnConnectionUpAsync(circuit, cancellationToken);
        }
    }
}