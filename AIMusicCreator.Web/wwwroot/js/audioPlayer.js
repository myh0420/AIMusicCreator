

// 音频播放控制函数
export function playAudio(audioElement) {
    if (audioElement && typeof audioElement.play === 'function') {
        return audioElement.play().catch(error => {
            console.error('播放失败:', error);
            throw error;
        });
    }
    return Promise.reject('Audio element not found or play method not available');
}
//export function playAudio(audioElement) {
//    return audioElement.play().catch(error => {
//        console.error('播放失败:', error);
//        throw error;
//    });
//}
/// 暂停音频播放
export function pauseAudio(audioElement) {
    if (audioElement && typeof audioElement.pause === 'function') {
        audioElement.pause();
    }
}
/// 设置音量，范围0.0到1.0
export function setVolume(audioElement, volume) {
    if (audioElement && typeof audioElement.volume !== 'undefined') {
        audioElement.volume = Math.max(0, Math.min(1, volume));
    }
}
/// 设置当前播放时间
export function setCurrentTime(audioElement, time) {
    if (audioElement && typeof audioElement.currentTime !== 'undefined') {
        audioElement.currentTime = time;
    }
}
/// 获取当前播放时间
export function getCurrentTime(audioElement) {
    if (audioElement && typeof audioElement.currentTime !== 'undefined') {
        return audioElement.currentTime;
    }
    return 0;
}
/// 获取音频总时长
export function getDuration(audioElement) {
    if (audioElement && typeof audioElement.duration !== 'undefined') {
        return audioElement.duration;
    }
    return 0;
}
/// 检查音频是否暂停
export function isPaused(audioElement) {
    if (audioElement && typeof audioElement.paused !== 'undefined') {
        return audioElement.paused;
    }
    return true;
}
// 检查音频是否可播放
export function isAudioReady(audioElement) {
    return audioElement.readyState > 0;
}
/// 获取音频加载状态
export function getAudioReadyState(audioElement) {
    return audioElement.readyState;
}
// 检查音频是否播放结束
export function isAudioEnded(audioElement) {
    if (audioElement && typeof audioElement.ended !== 'undefined') {
        return audioElement.ended;
    }
    return false;
}
// 监听音频加载事件
export function waitForAudioLoad(audioElement) {
    return new Promise((resolve, reject) => {
        if (audioElement.readyState > 0) {
            resolve(true);
            return;
        }

        const onLoaded = () => {
            audioElement.removeEventListener('loadeddata', onLoaded);
            audioElement.removeEventListener('error', onError);
            resolve(true);
        };

        const onError = (error) => {
            audioElement.removeEventListener('loadeddata', onLoaded);
            audioElement.removeEventListener('error', onError);
            reject(error);
        };

        audioElement.addEventListener('loadeddata', onLoaded);
        audioElement.addEventListener('error', onError);
    });
}
export function saveFile(base64Data, fileName, contentType) {
    // 创建下载链接
    const link = document.createElement('a');
    link.href = `data:${contentType};base64,${base64Data}`;
    link.download = fileName;

    // 触发下载
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}
// 同时播放两个音频（改进版混音实现）
export function playTwoAudios(url1, url2, options = {}) {
    return new Promise((resolve, reject) => {
        // 默认配置
        const config = {
            volume1: options.volume1 !== undefined ? options.volume1 : 0.7,
            volume2: options.volume2 !== undefined ? options.volume2 : 0.7,
            onProgress: options.onProgress || (() => {}),
            onEnded: options.onEnded || (() => {}),
            onError: options.onError || ((error) => console.error('音频播放错误:', error)),
            ...options
        };

        // 创建音频实例
        const audio1 = new Audio(url1);
        const audio2 = new Audio(url2);
        
        // 资源管理和状态跟踪
        let isPlaying = false;
        let isFinished = false;
        let progressInterval = null;
        let errorCount = 0;
        const MAX_ERRORS = 3;

        // 设置音量
        audio1.volume = Math.max(0, Math.min(1, config.volume1));
        audio2.volume = Math.max(0, Math.min(1, config.volume2));

        // 错误处理函数
        const handleError = (error, source) => {
            errorCount++;
            const errorMsg = `${source} 音频播放错误: ${error.message || error}`;
            console.error(errorMsg);
            config.onError(errorMsg);
            
            if (errorCount >= MAX_ERRORS && !isFinished) {
                cleanup();
                reject(new Error(`达到最大错误次数 (${MAX_ERRORS})`));
            }
        };

        // 清理函数
        const cleanup = () => {
            if (progressInterval) {
                clearInterval(progressInterval);
                progressInterval = null;
            }
            
            // 移除事件监听器
            audio1.removeEventListener('error', onAudio1Error);
            audio2.removeEventListener('error', onAudio2Error);
            audio1.removeEventListener('ended', onEnded);
            audio2.removeEventListener('ended', onEnded);
            
            // 停止播放并释放资源
            try {
                audio1.pause();
                audio1.src = '';
                audio1.load();
                
                audio2.pause();
                audio2.src = '';
                audio2.load();
            } catch (e) {
                console.error('清理音频资源时出错:', e);
            }
        };

        // 事件监听器
        const onAudio1Error = (error) => handleError(error, '第一个');
        const onAudio2Error = (error) => handleError(error, '第二个');
        
        const onEnded = () => {
            // 检查两个音频是否都已结束
            if (audio1.ended && audio2.ended && !isFinished) {
                isFinished = true;
                cleanup();
                config.onEnded();
                resolve();
            }
        };

        // 绑定错误和结束事件
        audio1.addEventListener('error', onAudio1Error);
        audio2.addEventListener('error', onAudio2Error);
        audio1.addEventListener('ended', onEnded);
        audio2.addEventListener('ended', onEnded);

        // 启动进度报告
        const startProgressReporting = () => {
            progressInterval = setInterval(() => {
                if (isPlaying && !isFinished && !audio1.paused && !audio2.paused) {
                    // 使用第一个音频作为进度基准
                    const progress = {
                        currentTime: audio1.currentTime,
                        duration: audio1.duration,
                        percent: audio1.duration > 0 ? (audio1.currentTime / audio1.duration) * 100 : 0
                    };
                    config.onProgress(progress);
                }
            }, 100); // 每100ms更新一次进度
        };

        // 同步播放函数
        const playSynchronized = async () => {
            try {
                // 预加载音频
                await Promise.all([
                    new Promise((res, rej) => {
                        audio1.addEventListener('canplaythrough', res, { once: true });
                        audio1.addEventListener('error', rej, { once: true });
                        audio1.load();
                    }),
                    new Promise((res, rej) => {
                        audio2.addEventListener('canplaythrough', res, { once: true });
                        audio2.addEventListener('error', rej, { once: true });
                        audio2.load();
                    })
                ]);

                // 确保两个音频都重置到开始位置
                audio1.currentTime = 0;
                audio2.currentTime = 0;

                // 尝试同步播放
                // 使用try-catch处理可能的播放失败（如浏览器自动播放策略限制）
                try {
                    const playPromises = [audio1.play(), audio2.play()];
                    await Promise.all(playPromises);
                    isPlaying = true;
                    startProgressReporting();
                    resolve({
                        pause: () => {
                            audio1.pause();
                            audio2.pause();
                            isPlaying = false;
                        },
                        resume: () => {
                            audio1.play();
                            audio2.play();
                            isPlaying = true;
                        },
                        stop: () => {
                            cleanup();
                            isFinished = true;
                            isPlaying = false;
                        },
                        setVolume: (vol1, vol2) => {
                            if (vol1 !== undefined) audio1.volume = Math.max(0, Math.min(1, vol1));
                            if (vol2 !== undefined) audio2.volume = Math.max(0, Math.min(1, vol2));
                        },
                        getState: () => ({
                            isPlaying,
                            isFinished,
                            currentTime: audio1.currentTime,
                            duration: audio1.duration
                        })
                    });
                } catch (playError) {
                    handleError(playError, '播放初始化');
                    cleanup();
                    reject(playError);
                }
            } catch (loadError) {
                handleError(loadError, '音频加载');
                cleanup();
                reject(loadError);
            }
        };

        // 开始播放流程
        playSynchronized();

        // 提供取消播放的能力
        return {
            cancel: () => {
                if (!isFinished) {
                    cleanup();
                    isFinished = true;
                    reject(new Error('播放已取消'));
                }
            }
        };
    });
}

// 停止所有正在播放的混音实例
export function stopAllMixes() {
    // 这个函数可以扩展为跟踪所有活跃的混音实例
    // 并在需要时停止它们
    console.log('停止所有混音播放');
}

// 单独播放音频
export function playAudioByUrl(url) {
    const audio = new Audio(url);
    audio.play();
}