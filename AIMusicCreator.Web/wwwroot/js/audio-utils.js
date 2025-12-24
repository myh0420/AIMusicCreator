// 预览音频片段
function previewAudioSegment(audioUrl, start, end) {
    const audio = new Audio(audioUrl);

    audio.addEventListener('loadedmetadata', () => {
        audio.currentTime = start;
        audio.play();

        // 到结束时间自动暂停
        const interval = setInterval(() => {
            if (audio.currentTime >= end) {
                audio.pause();
                clearInterval(interval);
            }
        }, 100);
    });

    audio.addEventListener('error', (e) => {
        alert('片段预览失败: ' + e.message);
    });
}

// 拖拽排序（用于音频拼接列表）
document.addEventListener('DOMContentLoaded', () => {
    const joinList = document.getElementById('joinList');
    if (joinList) {
        new Sortable(joinList, {
            animation: 150,
            ghostClass: 'bg-light',
            onEnd: (e) => {
                // 前端排序已完成，无需额外处理（后端拼接时按当前顺序）
            }
        });
    }
});

// 多轨混音：设置音频音量
function setAudioVolume(audioElement, volume) {
    audioElement.volume = volume;
}

// 多轨混音：播放音频
function playAudio(audioElement) {
    audioElement.play().catch(err => console.error("播放失败:", err));
}

// 多轨混音：暂停音频
function pauseAudio(audioElement) {
    audioElement.pause();
}

// 格式转换：打包下载ZIP（依赖JSZip库）
async function downloadZip(zipName, files) {
    // 动态加载JSZip库
    if (!window.JSZip) {
        const script = document.createElement('script');
        script.src = 'https://cdn.jsdelivr.net/npm/jszip@3.10.1/dist/jszip.min.js';
        document.head.appendChild(script);
        // 等待库加载完成
        await new Promise(resolve => script.onload = resolve);
    }

    const zip = new JSZip();
    // 添加文件到ZIP
    for (const file of files) {
        zip.file(file.Name, file.Data, { base64: true });
    }

    // 生成ZIP并下载
    const content = await zip.generateAsync({ type: 'blob' });
    const url = URL.createObjectURL(content);
    const a = document.createElement('a');
    a.href = url;
    a.download = zipName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
}
