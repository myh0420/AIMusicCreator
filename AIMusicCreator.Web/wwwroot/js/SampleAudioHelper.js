// 音频播放助手函数
// 创建音频对象URL
window.createAudioObjectURL = async (streamRef, contentType) => {
    try {
        console.log('🎵 开始创建音频对象URL，类型:', contentType);

        // 从.NET流引用读取数据
        const arrayBuffer = await streamRef.arrayBuffer();
        console.log('📊 读取的数据大小:', arrayBuffer.byteLength, 'bytes');

        if (arrayBuffer.byteLength === 0) {
            console.error('❌ 接收到的数据为空');
            throw new Error('接收到的数据为空');
        }

        // 创建Blob
        const blob = new Blob([arrayBuffer], { type: contentType });
        console.log('✅ Blob创建成功，大小:', blob.size, 'bytes');

        // 创建对象URL
        const objectUrl = URL.createObjectURL(blob);
        console.log('🔗 对象URL创建成功');

        return objectUrl;
    } catch (error) {
        console.error('❌ 创建对象URL失败:', error);
        throw error;
    }
};

// 验证音频URL是否有效
window.validateAudioURL = async (audioUrl) => {
    return new Promise((resolve) => {
        console.log('🔍 开始验证音频URL...');
        const audio = new Audio();
        audio.src = audioUrl;

        let timeoutId = setTimeout(() => {
            console.warn('⏰ 音频验证超时');
            audio.remove();
            resolve(false);
        }, 10000);

        audio.onloadedmetadata = () => {
            clearTimeout(timeoutId);
            console.log('✅ 音频元数据加载成功，时长:', audio.duration, '秒');
            audio.remove();
            resolve(audio.duration > 0);
        };

        audio.oncanplay = () => {
            clearTimeout(timeoutId);
            console.log('🎶 音频可以播放');
            audio.remove();
            resolve(true);
        };

        audio.onerror = (e) => {
            clearTimeout(timeoutId);
            console.error('❌ 音频验证失败:', audio.error);
            audio.remove();
            resolve(false);
        };

        audio.load();
    });
};

// 设置音频源并等待加载
window.setAudioSourceAndWait = async (audioElement, audioUrl) => {
    return new Promise((resolve) => {
        console.log('🎧 设置音频源并等待加载...');
        const audio = audioElement;
        audio.src = audioUrl;

        let timeoutId = setTimeout(() => {
            console.warn('⏰ 音频加载超时');
            resolve(false);
        }, 15000);

        audio.onloadedmetadata = () => {
            clearTimeout(timeoutId);
            console.log('✅ 音频元素元数据加载成功');
            resolve(true);
        };

        audio.oncanplay = () => {
            clearTimeout(timeoutId);
            console.log('🎶 音频可以播放');
            resolve(true);
        };

        audio.onerror = (e) => {
            clearTimeout(timeoutId);
            console.error('❌ 音频元素错误:', audio.error);
            resolve(false);
        };

        audio.load();
    });
};

// 获取音频时长
window.getAudioDuration = (audioElement) => {
    const duration = audioElement.duration || 0;
    console.log('⏱️ 获取音频时长:', duration, '秒');
    return duration;
};
// 尝试播放音频（处理自动播放策略）
window.tryPlayAudio = async (audioElement) => {
    try {
        const playPromise = audioElement.play();
        if (playPromise !== undefined) {
            await playPromise;
            console.log('✅ 自动播放成功');
            return true;
        }
    } catch (error) {
        console.log('ℹ️ 自动播放被阻止，需要用户交互:', error.message);
        return false;
    }
};

// 获取详细的音频元素状态
window.getAudioElementState = (audioElement) => {
    return {
        src: audioElement.src,
        currentSrc: audioElement.currentSrc,
        duration: audioElement.duration,
        currentTime: audioElement.currentTime,
        readyState: audioElement.readyState,
        networkState: audioElement.networkState,
        error: audioElement.error,
        paused: audioElement.paused,
        ended: audioElement.ended
    };
};

// 强制重新加载音频
window.reloadAudio = (audioElement) => {
    audioElement.load();
    console.log('🔄 音频元素重新加载');
};
// 获取音频错误信息
window.getAudioError = (audioElement) => {
    if (!audioElement.error) {
        console.log('❓ 未知音频错误');
        return "未知错误";
    }

    let errorMessage = "未知错误";
    switch (audioElement.error.code) {
        case audioElement.error.MEDIA_ERR_ABORTED:
            errorMessage = "播放被中止";
            break;
        case audioElement.error.MEDIA_ERR_NETWORK:
            errorMessage = "网络错误";
            break;
        case audioElement.error.MEDIA_ERR_DECODE:
            errorMessage = "解码错误 - 文件可能已损坏或格式不受支持";
            break;
        case audioElement.error.MEDIA_ERR_SRC_NOT_SUPPORTED:
            errorMessage = "文件格式不受支持";
            break;
        default:
            errorMessage = "未知错误";
            break;
    }

    console.log('❌ 音频错误:', errorMessage);
    return errorMessage;
};

// 清理所有对象URL（用于调试）
window.cleanupAllObjectURLs = () => {
    // 注意：在实际应用中，应该跟踪和管理所有创建的URL
    console.log('🧹 清理提示：请确保手动管理所有创建的objectURL');
};