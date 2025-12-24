// 设置拖拽数据（解决Blazor DataTransfer.SetData不支持问题）
function setDragData(dataTransfer, value) {
    dataTransfer.setData("text/plain", value);
}

// 获取拖拽数据
function getDragData(dataTransfer) {
    return dataTransfer.getData("text/plain");
}
