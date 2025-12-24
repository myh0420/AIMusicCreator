using Microsoft.AspNetCore.Components.Server.Circuits;

﻿namespace AIMusicCreator.Web.Services
{
    /// <summary>
    /// 自定义电路处理程序服务
    /// 用于处理Blazor Server端的电路生命周期事件
    /// </summary>
    public class CircuitHandlerService(ILogger<CircuitHandlerService> logger) : CircuitHandler
    {
        /// <summary>
        /// 记录器
        /// </summary>
        private readonly ILogger<CircuitHandlerService> _logger = logger;
        /// <summary>
        /// 当电路打开时调用
        /// </summary>
        /// <param name="circuit">打开的电路</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>任务</returns>
        /// <remarks>
        /// 记录电路打开事件
        /// </remarks>
        public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Circuit opened: {CircuitId}", circuit.Id);
            return base.OnCircuitOpenedAsync(circuit, cancellationToken);
        }
        /// <summary>
        /// 当电路关闭时调用
        /// </summary>
        /// <param name="circuit">关闭的电路</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>任务</returns>
        /// <remarks>
        /// 记录电路关闭事件
        /// </remarks>
        public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Circuit closed: {CircuitId}", circuit.Id);
            return base.OnCircuitClosedAsync(circuit, cancellationToken);
        }
        /// <summary>
        /// 当连接断开时调用
        /// </summary>
        /// <param name="circuit">断开连接的电路</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>任务</returns>
        /// <remarks>
        /// 记录连接断开事件
        /// </remarks>
        public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Connection down: {CircuitId}", circuit.Id);
            return base.OnConnectionDownAsync(circuit, cancellationToken);
        }
        /// <summary>
        /// 当连接恢复时调用
        /// </summary>
        /// <param name="circuit">恢复连接的电路</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>任务</returns>
        /// <remarks>
        /// 记录连接恢复事件
        /// </remarks>
        public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Connection up: {CircuitId}", circuit.Id);
            return base.OnConnectionUpAsync(circuit, cancellationToken);
        }
    }
}
