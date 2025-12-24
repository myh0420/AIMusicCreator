// 流式音频播放帮助器
export function setupStreamingAudio(audioElement, audioUrl, dotNetHelper) {
    if (!audioElement || !audioUrl) {
        console.error('音频元素或URL为空');
        return;
    }

    console.log('开始设置流式音频');

    // 清理之前的音频源
    cleanup(audioElement);

    if (audioUrl.startsWith('data:')) {
        // 对于 Base64 Data URL
        setupDataUrlAudio(audioElement, audioUrl, dotNetHelper);
    } else if (audioUrl.startsWith('blob:')) {
        // 对于 Blob URL
        audioElement.src = audioUrl;
        audioElement.load();
        dotNetHelper.invokeMethodAsync('UpdateLoadProgress', 100);
    } else if (audioUrl.startsWith('http')) {
        // 对于远程 URL，使用流式加载
        loadAudioStreaming(audioElement, audioUrl, dotNetHelper);
    }
}

function setupDataUrlAudio(audioElement, audioUrl, dotNetHelper) {
    console.log('使用 Base64 Data URL');

    let lastReportedProgress = 0;
    const reportProgress = (progress) => {
        // 只有当进度变化超过 5% 或者达到 100% 时才报告
        if (Math.abs(progress - lastReportedProgress) >= 5 || progress === 100) {
            dotNetHelper.invokeMethodAsync('UpdateLoadProgress', progress);
            lastReportedProgress = progress;
        }
    };

    // 设置事件监听器
    audioElement.addEventListener('loadstart', () => {
        reportProgress(10);
    });

    audioElement.addEventListener('loadeddata', () => {
        reportProgress(50);
    });

    audioElement.addEventListener('canplay', () => {
        reportProgress(80);
    });

    audioElement.addEventListener('canplaythrough', () => {
        reportProgress(100);
    });

    audioElement.addEventListener('progress', (e) => {
        if (audioElement.buffered.length > 0) {
            const bufferedEnd = audioElement.buffered.end(audioElement.buffered.length - 1);
            const duration = audioElement.duration;
            if (duration > 0) {
                const progress = Math.round((bufferedEnd / duration) * 100);
                reportProgress(progress);
            }
        }
    });

    // 设置音频源
    audioElement.src = audioUrl;
    audioElement.load();
}

async function loadAudioStreaming(audioElement, audioUrl, dotNetHelper) {
    try {
        console.log('开始流式加载音频');
        let lastReportedProgress = 0;

        const reportProgress = (progress) => {
            // 减少回调频率：只有变化超过 2% 或关键节点才报告
            if (Math.abs(progress - lastReportedProgress) >= 2 ||
                progress === 0 || progress === 20 || progress === 100) {
                dotNetHelper.invokeMethodAsync('UpdateLoadProgress', progress);
                lastReportedProgress = progress;
                console.log('进度更新:', progress + '%');
            }
        };

        reportProgress(5);

        const response = await fetch(audioUrl);

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const contentLength = response.headers.get('content-length');
        const total = parseInt(contentLength, 10);

        console.log('音频文件大小:', total, 'bytes');

        if (!response.body) {
            throw new Error('ReadableStream not supported in this browser');
        }

        const reader = response.body.getReader();
        const chunks = [];
        let loaded = 0;

        // 开始读取流
        while (true) {
            const { done, value } = await reader.read();

            if (done) break;

            chunks.push(value);
            loaded += value.length;

            // 计算进度
            const progress = total ? Math.round((loaded / total) * 100) : 0;
            reportProgress(progress);

            // 当加载到 20% 时开始播放
            if (progress >= 20 && audioElement.readyState === 0) {
                console.log('加载达到 20%，开始播放');
                const blob = new Blob(chunks, { type: 'audio/mpeg' });
                const blobUrl = URL.createObjectURL(blob);
                audioElement.src = blobUrl;

                // 尝试自动播放
                try {
                    await audioElement.play();
                    console.log('自动播放成功');
                } catch (playError) {
                    console.log('自动播放被阻止，需要用户交互');
                }
            }
        }

        // 加载完成
        reportProgress(100);
        console.log('音频加载完成');

    } catch (error) {
        console.error('流式加载音频失败:', error);
        dotNetHelper.invokeMethodAsync('UpdateLoadProgress', 0);
    }
}

// 播放控制方法
export function playAudio(audioElement) {
    if (audioElement) {
        return audioElement.play().catch(error => {
            console.error('播放失败:', error);
        });
    }
}

export function pauseAudio(audioElement) {
    if (audioElement) {
        audioElement.pause();
    }
}

// 清理资源
export function cleanup(audioElement) {
    if (audioElement) {
        // 暂停播放
        audioElement.pause();

        // 移除所有事件监听器
        audioElement.replaceWith(audioElement.cloneNode(true));

        // 清理 Blob URL
        if (audioElement.src && audioElement.src.startsWith('blob:')) {
            URL.revokeObjectURL(audioElement.src);
        }

        // 重置音频元素
        audioElement.src = '';
        audioElement.load();
    }
}