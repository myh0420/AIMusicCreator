// 极简音频帮助器
export function setupSimpleAudioEvents(audioElement, dotNetCallbacks) {
    console.log('设置简单音频事件监听');

    if (!audioElement) {
        console.error('音频元素为空');
        return;
    }

    // 清理旧的事件监听器
    const newAudio = audioElement.cloneNode(true);
    audioElement.parentNode.replaceChild(newAudio, audioElement);
    const audio = newAudio;

    // 加载开始
    audio.addEventListener('loadstart', () => {
        console.log('音频开始加载');
        if (dotNetCallbacks && dotNetCallbacks.invokeMethodAsync) {
            dotNetCallbacks.invokeMethodAsync('OnLoading');
        }
    });

    // 可以播放时
    audio.addEventListener('canplay', () => {
        console.log('音频可以播放');
        if (dotNetCallbacks && dotNetCallbacks.invokeMethodAsync) {
            dotNetCallbacks.invokeMethodAsync('OnLoaded');
        }
    });

    // 错误处理
    audio.addEventListener('error', (e) => {
        console.error('音频错误:', e);

        let errorMsg = '加载失败';
        if (audio.error) {
            switch (audio.error.code) {
                case 1: errorMsg = '加载被中止'; break;
                case 2: errorMsg = '网络错误'; break;
                case 3: errorMsg = '解码错误'; break;
                case 4: errorMsg = '格式不支持'; break;
            }
        }

        console.error('错误信息:', errorMsg);
        if (dotNetCallbacks && dotNetCallbacks.invokeMethodAsync) {
            dotNetCallbacks.invokeMethodAsync('OnError', errorMsg);
        }
    });

    // 返回新的音频元素
    return audio;
}

// 诊断函数：检查音频状态
export function diagnoseAudio(audioElement) {
    if (!audioElement) return '音频元素为空';

    const result = {
        src: audioElement.src ? audioElement.src.substring(0, 100) : '空',
        readyState: audioElement.readyState,
        networkState: audioElement.networkState,
        duration: audioElement.duration,
        error: audioElement.error ? {
            code: audioElement.error.code,
            message: audioElement.error.message
        } : '无错误',
        currentTime: audioElement.currentTime
    };

    console.log('音频诊断信息:', result);
    return result;
}