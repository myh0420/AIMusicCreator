// 纯客户端音频帮助器
export function playAudio(audioId) {
    const audio = document.getElementById(audioId);
    if (audio) {
        return audio.play().catch(error => {
            console.error('播放失败:', error);
        });
    }
}

export function pauseAudio(audioId) {
    const audio = document.getElementById(audioId);
    if (audio) {
        audio.pause();
    }
}

export function setVolume(audioId, volume) {
    const audio = document.getElementById(audioId);
    if (audio) {
        audio.volume = Math.max(0, Math.min(1, volume));
    }
}

export function setCurrentTime(audioId, time) {
    const audio = document.getElementById(audioId);
    if (audio) {
        audio.currentTime = time;
    }
}

export function getCurrentTime(audioId) {
    const audio = document.getElementById(audioId);
    return audio ? audio.currentTime : 0;
}

export function getDuration(audioId) {
    const audio = document.getElementById(audioId);
    return audio ? audio.duration : 0;
}