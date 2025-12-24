// 音频事件处理
export function setupAudioEvents(audioElement, dotNetHelper) {
    if (!audioElement) return;

    const audio = audioElement;

    // 保存原始的事件处理函数引用，以便后续移除
    audio._playHandler = () => {
        dotNetHelper.invokeMethodAsync('HandlePlayEvent');
    };

    audio._pauseHandler = () => {
        dotNetHelper.invokeMethodAsync('HandlePauseEvent');
    };

    audio._endedHandler = () => {
        dotNetHelper.invokeMethodAsync('HandleEndedEvent');
    };

    // 添加事件监听器
    audio.addEventListener('play', audio._playHandler);
    audio.addEventListener('pause', audio._pauseHandler);
    audio.addEventListener('ended', audio._endedHandler);
}

export function removeAudioEvents(audioElement) {
    if (!audioElement || !audioElement._playHandler) return;

    const audio = audioElement;

    // 移除事件监听器
    audio.removeEventListener('play', audio._playHandler);
    audio.removeEventListener('pause', audio._pauseHandler);
    audio.removeEventListener('ended', audio._endedHandler);

    // 清理引用
    delete audio._playHandler;
    delete audio._pauseHandler;
    delete audio._endedHandler;
}