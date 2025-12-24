let audioContext;
let analyser;
let animationId;
let sourceNode;

// 启动可视化（修复参数传递编码）
function startVisualization(canvasId, audioElement, type, sensitivity, color) {
    const canvas = document.getElementById(canvasId);
    const ctx = canvas.getContext('2d');

    // 初始化音频上下文（兼容浏览器）
    try {
        audioContext = new (window.AudioContext || window.webkitAudioContext)();
        analyser = audioContext.createAnalyser();
        analyser.fftSize = 1024;
        analyser.smoothingTimeConstant = 0.8;

        // 连接音频源
        sourceNode = audioContext.createMediaElementSource(audioElement);
        sourceNode.connect(analyser);
        analyser.connect(audioContext.destination);

        // 开始可视化
        visualize(ctx, type, sensitivity, color);
    } catch (e) {
        console.error("可视化初始化失败:", e);
        alert("浏览器不支持音频可视化功能");
    }
}

// 可视化核心逻辑
function visualize(ctx, type, sensitivity, color) {
    const canvas = ctx.canvas;
    const bufferLength = analyser.frequencyBinCount;
    const dataArray = new Uint8Array(bufferLength);

    // 清除画布
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    function draw() {
        animationId = requestAnimationFrame(draw);
        analyser.getByteFrequencyData(dataArray);

        // 根据类型绘制
        if (type === "波形") {
            drawWaveform(ctx, dataArray, canvas, sensitivity, color);
        } else if (type === "频谱") {
            drawSpectrum(ctx, dataArray, canvas, sensitivity, color);
        } else if (type === "粒子") {
            drawParticles(ctx, dataArray, canvas, sensitivity, color);
        }
    }

    draw();
}

// 波形绘制
function drawWaveform(ctx, data, canvas, sensitivity, color) {
    const sliceWidth = canvas.width / data.length * 2.5;
    let x = 0;

    ctx.lineWidth = 2;
    ctx.strokeStyle = getColor(0, color);
    ctx.beginPath();

    for (let i = 0; i < data.length; i++) {
        const v = data[i] / 128.0;
        const y = v * canvas.height / 2 * (sensitivity / 5);

        i === 0 ? ctx.moveTo(x, y) : ctx.lineTo(x, y);
        x += sliceWidth;
    }

    ctx.lineTo(canvas.width, canvas.height / 2);
    ctx.stroke();
}

// 频谱绘制
function drawSpectrum(ctx, data, canvas, sensitivity, color) {
    const barWidth = (canvas.width / data.length) * 2.5;
    let x = 0;

    for (let i = 0; i < data.length; i++) {
        const barHeight = (data[i] / 255) * canvas.height * (sensitivity / 5);
        ctx.fillStyle = getColor(i, color);
        ctx.fillRect(x, canvas.height - barHeight, barWidth, barHeight);
        x += barWidth + 1;
    }
}

// 粒子绘制
function drawParticles(ctx, data, canvas, sensitivity, color) {
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    const particleCount = 50;
    const step = Math.floor(data.length / particleCount);

    for (let i = 0; i < particleCount; i++) {
        const value = data[i * step] / 255;
        const size = value * 15 * (sensitivity / 5) + 2;
        const x = (i / particleCount) * canvas.width;
        const y = canvas.height / 2 + (value * 100 * (sensitivity / 5) * Math.sin(i * 0.5));

        ctx.fillStyle = getColor(i, color);
        ctx.beginPath();
        ctx.arc(x, y, size, 0, Math.PI * 2);
        ctx.fill();
    }
}

// 获取颜色（修复中文颜色方案传递）
function getColor(index, scheme) {
    switch (scheme) {
        case "rainbow":
            return `hsl(${(index * 3) % 360}, 70%, 50%)`;
        case "blue":
            return `hsl(200, 70%, ${30 + (index % 50)}%)`;
        case "red":
            return `hsl(0, 70%, ${30 + (index % 50)}%)`;
        case "green":
            return `hsl(120, 70%, ${30 + (index % 50)}%)`;
        default:
            return "white";
    }
}

// 更新可视化参数
function updateVisualizationType(type, color, sensitivity) {
    const canvas = document.getElementById('visualizer');
    const ctx = canvas.getContext('2d');
    cancelAnimationFrame(animationId);
    visualize(ctx, type, sensitivity, color);
}

// 停止可视化
function stopVisualization() {
    if (animationId) cancelAnimationFrame(animationId);
    if (audioContext) audioContext.close();
}