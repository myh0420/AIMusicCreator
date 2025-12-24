// 合并的音频模块 - 包含操作和事件处理
class AudioModule {
    constructor() {
        this.audioElements = new Map();
        this.timeout = 30000; // 30秒超时
    }

    // 带超时的操作
    async withTimeout(promise, operationName) {
        const timeoutPromise = new Promise((_, reject) => {
            setTimeout(() => reject(new Error(`${operationName} 超时`)), this.timeout);
        });

        return Promise.race([promise, timeoutPromise]);
    }

    // 音频操作方法
    playAudio(audioElement) {
        //return audioElement.play().catch(error => {
        //    console.error('播放失败:', error);
        //    throw error;
        //});
        return this.withTimeout(
            audioElement.play().catch(error => {
                console.error('播放失败:', error);
                throw error;
            }),
            '播放音频'
        );
    }

    pauseAudio(audioElement) {
        this.withTimeout(audioElement.pause(),"暂停播放音频");
    }

    setVolume(audioElement, volume) {
        this.withTimeout(audioElement.volume = Math.max(0, Math.min(1, volume)),"设置音量");
    }

    setCurrentTime(audioElement, time) {
        this.withTimeout(audioElement.currentTime = time, "设置当前时间");
    }

    getCurrentTime(audioElement) {
        return this.withTimeout(audioElement.currentTime || 0, "获取当前时间");
    }

    getDuration(audioElement) {
        return this.withTimeout(audioElement.duration || 0, "获取总时长");
    }

    // 事件处理方法
    setupAudioEvents(audioElement, dotNetHelper) {
        if (!audioElement) return;
        this.withTimeout(new Promise(() => {
            const handlers = {
                play: () => dotNetHelper.invokeMethodAsync('HandlePlayEvent'),
                pause: () => dotNetHelper.invokeMethodAsync('HandlePauseEvent'),
                ended: () => dotNetHelper.invokeMethodAsync('HandleEndedEvent')
            };

            // 保存处理函数引用以便后续移除
            this.audioElements.set(audioElement, handlers);

            // 添加事件监听器
            audioElement.addEventListener('play', handlers.play);
            audioElement.addEventListener('pause', handlers.pause);
            audioElement.addEventListener('ended', handlers.ended);
        }), "设置监听事件")
    }

    removeAudioEvents(audioElement) {
        if (!this.audioElements.has(audioElement)) return;
        this.withTimeout(new Promise(() => {
            const handlers = this.audioElements.get(audioElement);

            audioElement.removeEventListener('play', handlers.play);
            audioElement.removeEventListener('pause', handlers.pause);
            audioElement.removeEventListener('ended', handlers.ended);

            this.audioElements.delete(audioElement);
        }), "移除监听事件");
    }
    ////
    isAudioReady(audioElement) {
        return this.withTimeout(audioElement.readyState > 0,"是否已准备好");
    }

    // 检查音频是否准备好
    async waitForAudioLoad(audioElement, timeoutMs = 30000) {
        return this.withTimeout(
            new Promise((resolve, reject) => {
                if (audioElement.readyState > 0) {
                    resolve(true);
                    return;
                }

                const onLoaded = () => {
                    cleanup();
                    resolve(true);
                };

                const onError = (error) => {
                    cleanup();
                    reject(error);
                };

                const cleanup = () => {
                    audioElement.removeEventListener('loadeddata', onLoaded);
                    audioElement.removeEventListener('error', onError);
                };

                audioElement.addEventListener('loadeddata', onLoaded);
                audioElement.addEventListener('error', onError);
            }),
            '等待音频加载'
        );
    }
}

// 创建单例实例
const audioModule = new AudioModule();

// 导出方法
export function playAudio(audioElement) {
    return audioModule.playAudio(audioElement);
}

export function pauseAudio(audioElement) {
    audioModule.pauseAudio(audioElement);
}

export function setVolume(audioElement, volume) {
    audioModule.setVolume(audioElement, volume);
}

export function setCurrentTime(audioElement, time) {
    audioModule.setCurrentTime(audioElement, time);
}

export function getCurrentTime(audioElement) {
    return audioModule.getCurrentTime(audioElement);
}

export function getDuration(audioElement) {
    return audioModule.getDuration(audioElement);
}

export function setupAudioEvents(audioElement, dotNetHelper) {
    audioModule.setupAudioEvents(audioElement, dotNetHelper);
}

export function removeAudioEvents(audioElement) {
    audioModule.removeAudioEvents(audioElement);
}

export function isAudioReady(audioElement) {
    return audioModule.isAudioReady(audioElement);
}

export function waitForAudioLoad(audioElement) {
    return audioModule.waitForAudioLoad(audioElement);
}