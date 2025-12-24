// 视频播放器JavaScript助手函数

// 初始化视频播放器
window.initializeVideoPlayer = () => {
    console.log('🎬 视频播放器初始化完成');
};

// 创建视频对象URL
window.createVideoObjectURL = async (streamRef, contentType) => {
    try {
        console.log('🎬 开始创建视频对象URL，类型:', contentType);

        // 从.NET流引用读取数据
        const arrayBuffer = await streamRef.arrayBuffer();
        console.log('📊 读取的视频数据大小:', arrayBuffer.byteLength, 'bytes');

        if (arrayBuffer.byteLength === 0) {
            console.error('❌ 接收到的视频数据为空');
            throw new Error('接收到的视频数据为空');
        }

        // 创建Blob
        const blob = new Blob([arrayBuffer], { type: contentType });
        console.log('✅ 视频Blob创建成功，大小:', blob.size, 'bytes');

        // 创建对象URL
        const objectUrl = URL.createObjectURL(blob);
        console.log('🔗 视频对象URL创建成功:', objectUrl.substring(0, 50) + '...');

        return objectUrl;
    } catch (error) {
        console.error('❌ 创建视频对象URL失败:', error);
        throw error;
    }
};

// 获取视频信息
window.getVideoInfo = (videoElement) => {
    const duration = videoElement.duration || 0;
    const width = videoElement.videoWidth || 0;
    const height = videoElement.videoHeight || 0;

    console.log(`📐 视频信息 - 时长: ${duration}秒, 分辨率: ${width}x${height}`);

    return {
        Duration: duration,
        VideoWidth: width,
        VideoHeight: height
    };
};

// 获取当前播放时间
window.getCurrentTime = (videoElement) => {
    return {
        CurrentTime: videoElement.currentTime || 0
    };
};

// 播放控制
window.playVideo = async (videoElement) => {
    try {
        await videoElement.play();
        console.log('▶️ 视频开始播放');
        return true;
    } catch (error) {
        console.error('❌ 播放失败:', error);
        return false;
    }
};

window.pauseVideo = (videoElement) => {
    videoElement.pause();
    console.log('⏸️ 视频暂停');
};

window.setVideoMuted = (videoElement, muted) => {
    videoElement.muted = muted;
    console.log(muted ? '🔇 视频静音' : '🔊 取消静音');
};

window.setVideoVolume = (videoElement, volume) => {
    videoElement.volume = volume;
    console.log(`🔊 音量设置为: ${volume}`);
};

window.seekVideo = (videoElement, time) => {
    videoElement.currentTime = time;
    console.log(`⏩ 跳转到: ${time}秒`);
};

//window.setPlaybackRate = (videoElement, rate) => {
//    videoElement.playbackRate = rate;
//    console.log(`⚡ 播放速度: ${rate}x`);
//};

// 全屏控制
window.toggleFullscreen = (videoElement) => {
    if (!document.fullscreenElement) {
        if (videoElement.requestFullscreen) {
            videoElement.requestFullscreen();
        } else if (videoElement.webkitRequestFullscreen) {
            videoElement.webkitRequestFullscreen();
        } else if (videoElement.mozRequestFullScreen) {
            videoElement.mozRequestFullScreen();
        } else if (videoElement.msRequestFullscreen) {
            videoElement.msRequestFullscreen();
        }
        console.log('🖥️ 进入全屏');
    } else {
        if (document.exitFullscreen) {
            document.exitFullscreen();
        } else if (document.webkitExitFullscreen) {
            document.webkitExitFullscreen();
        } else if (document.mozCancelFullScreen) {
            document.mozCancelFullScreen();
        } else if (document.msExitFullscreen) {
            document.msExitFullscreen();
        }
        console.log('📱 退出全屏');
    }
};

// 获取视频错误信息
window.getVideoError = (videoElement) => {
    if (!videoElement.error) {
        return "未知错误";
    }

    let errorMessage = "未知错误";
    switch (videoElement.error.code) {
        case videoElement.error.MEDIA_ERR_ABORTED:
            errorMessage = "播放被中止";
            break;
        case videoElement.error.MEDIA_ERR_NETWORK:
            errorMessage = "网络错误";
            break;
        case videoElement.error.MEDIA_ERR_DECODE:
            errorMessage = "解码错误 - 视频文件可能已损坏或格式不受支持";
            break;
        case videoElement.error.MEDIA_ERR_SRC_NOT_SUPPORTED:
            errorMessage = "视频格式不受支持";
            break;
        default:
            errorMessage = "未知错误";
            break;
    }

    console.log('❌ 视频错误:', errorMessage);
    return errorMessage;
};
// 设置视频源并等待加载完成
//window.setVideoSourceAndWait = async (videoElement, videoUrl) => {
//    return new Promise((resolve) => {
//        console.log('🎬 开始设置视频源并等待加载...');

//        const video = videoElement;
//        video.src = videoUrl;

//        let hasResolved = false;

//        const resolveWithResult = (success) => {
//            if (!hasResolved) {
//                hasResolved = true;
//                clearTimeout(timeoutId);
//                resolve(success);
//            }
//        };

//        const timeoutId = setTimeout(() => {
//            console.warn('⏰ 视频加载超时');
//            resolveWithResult(false);
//        }, 30000); // 30秒超时

//        video.onloadedmetadata = () => {
//            console.log('✅ 视频元数据加载完成');
//            console.log(`📊 视频信息 - 时长: ${video.duration}秒, 分辨率: ${video.videoWidth}x${video.videoHeight}`);

//            if (video.duration > 0 && video.videoWidth > 0) {
//                resolveWithResult(true);
//            } else {
//                console.warn('⚠️ 视频元数据异常');
//                resolveWithResult(false);
//            }
//        };

//        video.oncanplay = () => {
//            console.log('🎮 视频可以播放');
//            // 不在这里resolve，等待loadedmetadata
//        };

//        video.onerror = (e) => {
//            console.error('❌ 视频加载错误:', video.error);
//            resolveWithResult(false);
//        };

//        video.onstalled = () => {
//            console.warn('⚠️ 视频加载停滞');
//        };

//        // 开始加载视频
//        video.load();

//        // 额外检查：2秒后检查视频状态
//        setTimeout(() => {
//            if (!hasResolved) {
//                console.log('🔍 检查视频当前状态...');
//                console.log(`当前状态 - readyState: ${video.readyState}, networkState: ${video.networkState}`);

//                if (video.readyState >= 2) { // HAVE_CURRENT_DATA
//                    console.log('✅ 视频已有当前数据');
//                    resolveWithResult(true);
//                }
//            }
//        }, 2000);
//    });
//};
// 修复视频源设置函数
window.setVideoSourceAndWait = async (videoElement, videoUrl) => {
    return new Promise((resolve) => {
        console.log('🎬 开始设置视频源...');

        const video = videoElement;
        video.src = videoUrl;

        let hasResolved = false;
        let metadataLoaded = false;
        let canPlayTriggered = false;

        const resolveWithResult = (success) => {
            if (!hasResolved) {
                hasResolved = true;
                clearTimeout(timeoutId);
                console.log(success ? '✅ 视频源设置成功' : '❌ 视频源设置失败');
                resolve(success);
            }
        };

        // 更长的超时时间，特别是对大文件
        const timeoutId = setTimeout(() => {
            console.log('⏰ 视频加载超时，但检查当前状态...');
            // 超时不一定是失败，检查当前状态
            if (video.readyState >= 2) { // HAVE_CURRENT_DATA
                console.log('✅ 超时但视频已有数据');
                resolveWithResult(true);
            } else if (metadataLoaded || canPlayTriggered) {
                console.log('✅ 超时但元数据已加载或可以播放');
                resolveWithResult(true);
            } else {
                console.warn('⚠️ 超时且无可用数据');
                resolveWithResult(false);
            }
        }, 15000); // 15秒超时

        video.onloadedmetadata = () => {
            metadataLoaded = true;
            console.log(`✅ 视频元数据加载: ${video.duration}秒, ${video.videoWidth}x${video.videoHeight}`);

            // 立即返回成功，不等待其他事件
            resolveWithResult(true);
        };

        video.oncanplay = () => {
            canPlayTriggered = true;
            console.log('🎮 视频可以播放');
            // 如果metadata还没加载，但可以播放了，也算成功
            if (!metadataLoaded) {
                resolveWithResult(true);
            }
        };

        video.oncanplaythrough = () => {
            console.log('🚀 视频可以流畅播放');
            resolveWithResult(true);
        };

        video.onerror = (e) => {
            console.error('❌ 视频加载错误:', video.error);
            resolveWithResult(false);
        };

        video.onstalled = () => {
            console.log('⏸️ 视频缓冲中...');
            // 不视为错误，只是状态更新
        };

        video.onprogress = () => {
            // 显示加载进度
            if (video.buffered.length > 0) {
                const bufferedEnd = video.buffered.end(video.buffered.length - 1);
                const duration = video.duration || 1;
                const percent = (bufferedEnd / duration) * 100;
                console.log(`📊 缓冲进度: ${percent.toFixed(1)}%`);
            }
        };

        // 开始加载视频
        video.load();

        // 额外检查：如果视频很快就有数据，立即返回成功
        setTimeout(() => {
            if (!hasResolved) {
                console.log('🔍 快速状态检查...');
                console.log(`当前状态 - readyState: ${video.readyState}, networkState: ${video.networkState}`);

                if (video.readyState >= 1) { // HAVE_METADATA
                    console.log('✅ 快速检查: 视频已有元数据');
                    resolveWithResult(true);
                }
            }
        }, 1000);
    });
};

// 更宽松的视频验证函数
window.validateVideo = (videoElement) => {
    return new Promise((resolve) => {
        const video = videoElement;

        console.log(`🔍 验证视频状态 - readyState: ${video.readyState}, networkState: ${video.networkState}`);

        // 如果视频已经有足够的数据，立即返回成功
        if (video.readyState >= 2) { // HAVE_CURRENT_DATA
            console.log('✅ 视频已有当前数据');
            resolve(true);
            return;
        }

        if (video.readyState >= 1 && video.duration > 0) { // HAVE_METADATA
            console.log('✅ 视频已有元数据');
            resolve(true);
            return;
        }

        const timeoutId = setTimeout(() => {
            console.log('⏰ 视频验证超时，检查最终状态');
            // 超时不一定失败，检查最终状态
            const success = video.readyState >= 1 || video.duration > 0;
            console.log(success ? '✅ 超时但视频可用' : '❌ 超时且视频不可用');
            resolve(success);
        }, 10000);

        video.onloadedmetadata = () => {
            clearTimeout(timeoutId);
            console.log(`✅ 视频元数据验证成功: ${video.duration}秒`);
            resolve(true);
        };

        video.oncanplay = () => {
            clearTimeout(timeoutId);
            console.log('✅ 视频可以播放');
            resolve(true);
        };

        video.onerror = (e) => {
            clearTimeout(timeoutId);
            console.error('❌ 视频验证错误:', video.error);
            resolve(false);
        };

        // 如果视频已经在加载中，不需要额外操作
        if (video.networkState === 1) { // NETWORK_LOADING
            console.log('📡 视频正在加载中...');
        }
    });
};

// 简化的视频源设置（不等待验证）
window.setVideoSourceSimple = (videoElement, videoUrl) => {
    console.log('🎬 简单设置视频源');
    const video = videoElement;
    video.src = videoUrl;
    video.load();
    return true; // 总是返回成功，让视频自己处理加载
};
// 强制重新加载视频
window.reloadVideo = (videoElement) => {
    videoElement.load();
    console.log('🔄 视频重新加载');
};

// 获取详细的视频状态
window.getVideoDetailedState = (videoElement) => {
    const video = videoElement;
    return {
        src: video.src,
        currentSrc: video.currentSrc,
        duration: video.duration,
        currentTime: video.currentTime,
        readyState: video.readyState,
        networkState: video.networkState,
        videoWidth: video.videoWidth,
        videoHeight: video.videoHeight,
        error: video.error ? {
            code: video.error.code,
            message: video.error.message
        } : null,
        paused: video.paused,
        ended: video.ended,
        seeking: video.seeking
    };
};
// 验证视频URL
window.validateVideoURL = async (videoUrl) => {
    return new Promise((resolve) => {
        console.log('🔍 开始验证视频URL...');
        const video = document.createElement('video');
        video.src = videoUrl;

        let timeoutId = setTimeout(() => {
            console.warn('⏰ 视频验证超时');
            video.remove();
            resolve(false);
        }, 15000);

        video.onloadedmetadata = () => {
            clearTimeout(timeoutId);
            console.log('✅ 视频元数据加载成功，时长:', video.duration, '秒');
            console.log('📐 视频分辨率:', video.videoWidth, '×', video.videoHeight);
            video.remove();
            resolve(video.duration > 0);
        };

        video.onerror = (e) => {
            clearTimeout(timeoutId);
            console.error('❌ 视频验证失败:', video.error);
            video.remove();
            resolve(false);
        };

        video.load();
    });
};

// 流式视频URL创建
window.createStreamingVideoURL = async (streamRef, contentType, fileName, fileSize) => {
    try {
        console.log(`🎬 开始流式处理: ${fileName} (${fileSize} bytes)`);

        // 方法1: 使用MediaSource API (支持MP4等格式的流式播放)
        if (window.MediaSource && MediaSource.isTypeSupported(contentType)) {
            return await createMediaSourceStream(streamRef, contentType, fileName, fileSize);
        }

        // 方法2: 直接创建Blob URL (适用于大多数现代浏览器)
        return await createDirectBlobURL(streamRef, contentType, fileSize);

    } catch (error) {
        console.error('❌ 流式处理失败:', error);
        throw error;
    }
};

// 使用MediaSource API进行流式播放
async function createMediaSourceStream(streamRef, contentType, fileName, fileSize) {
    return new Promise((resolve, reject) => {
        console.log('📡 使用MediaSource API进行流式播放');

        const mediaSource = new MediaSource();
        const objectUrl = URL.createObjectURL(mediaSource);

        mediaSource.addEventListener('sourceopen', async () => {
            try {
                const sourceBuffer = mediaSource.addSourceBuffer(contentType);

                // 开始读取流数据
                const arrayBuffer = await streamRef.arrayBuffer();
                sourceBuffer.appendBuffer(arrayBuffer);

                sourceBuffer.addEventListener('updateend', () => {
                    if (!sourceBuffer.updating) {
                        mediaSource.endOfStream();
                        console.log('✅ MediaSource流式播放准备完成');
                    }
                });

                resolve(objectUrl);

            } catch (error) {
                reject(error);
            }
        });

        mediaSource.addEventListener('sourceended', () => {
            console.log('🔚 MediaSource流结束');
        });

        // 设置超时
        setTimeout(() => {
            if (mediaSource.readyState === 'open') {
                console.log('✅ MediaSource超时但已打开');
                resolve(objectUrl);
            }
        }, 5000);
    });
}

// 直接创建Blob URL (适用于大文件)
async function createDirectBlobURL(streamRef, contentType, fileSize) {
    console.log('🔗 创建直接Blob URL');

    // 对于大文件，我们仍然创建Blob URL，但让浏览器按需加载
    const arrayBuffer = await streamRef.arrayBuffer();
    const blob = new Blob([arrayBuffer], { type: contentType });
    const objectUrl = URL.createObjectURL(blob);

    console.log(`✅ 直接Blob URL创建完成: ${fileSize} bytes`);
    return objectUrl;
}

// 分块视频URL创建
window.createChunkedVideoURL = async (firstChunkRef, contentType, fileName, totalSize, chunkSize) => {
    console.log(`🧩 分块处理: ${fileName}, 总大小: ${totalSize}, 块大小: ${chunkSize}`);

    // 读取第一个块快速启动
    const firstChunkArrayBuffer = await firstChunkRef.arrayBuffer();
    const blob = new Blob([firstChunkArrayBuffer], { type: contentType });
    const objectUrl = URL.createObjectURL(blob);

    console.log('✅ 第一个块加载完成，视频可以开始播放');
    return objectUrl;
};

// 直接流URL
window.createDirectStreamURL = async (streamRef, contentType) => {
    console.log('🌊 创建直接流URL');

    const arrayBuffer = await streamRef.arrayBuffer();
    const blob = new Blob([arrayBuffer], { type: contentType });
    const objectUrl = URL.createObjectURL(blob);

    console.log('✅ 直接流URL创建完成');
    return objectUrl;
};

// 增强的视频源设置，支持大文件
window.setVideoSourceForLargeFiles = async (videoElement, videoUrl, fileName) => {
    return new Promise((resolve) => {
        console.log(`🎬 为大文件设置视频源: ${fileName}`);

        const video = videoElement;
        video.src = videoUrl;

        // 预加载元数据以减少缓冲
        video.preload = "auto";

        let timeoutId = setTimeout(() => {
            console.warn('⏰ 大文件加载超时，但可能仍在缓冲');
            resolve(true); // 对于大文件，超时不一定是失败
        }, 30000);

        video.onloadedmetadata = () => {
            clearTimeout(timeoutId);
            console.log(`✅ 大文件元数据加载: ${video.duration}秒, ${video.videoWidth}x${video.videoHeight}`);
            resolve(true);
        };

        video.oncanplay = () => {
            console.log('🎮 大文件可以开始播放');
            // 不清除超时，等待元数据加载完成
        };

        video.onerror = () => {
            clearTimeout(timeoutId);
            console.error('❌ 大文件加载错误');
            resolve(false);
        };

        // 对于大文件，我们更宽容
        video.onstalled = () => {
            console.log('⏸️ 大文件缓冲中...');
        };

        video.load();
    });
};

// 处理大文件的JavaScript方案
window.handleLargeFileWithJS = async (file) => {
    return new Promise((resolve, reject) => {
        console.log('🔄 使用JavaScript处理大文件');

        // 这里需要在前端通过其他方式获取文件，比如隐藏的file input
        const fileInput = document.createElement('input');
        fileInput.type = 'file';
        fileInput.accept = 'video/*';
        fileInput.style.display = 'none';

        fileInput.onchange = (e) => {
            const selectedFile = e.target.files[0];
            if (selectedFile) {
                const objectUrl = URL.createObjectURL(selectedFile);
                console.log(`✅ JavaScript文件处理完成: ${selectedFile.name}`);
                resolve(objectUrl);
            } else {
                reject(new Error('没有选择文件'));
            }
        };

        document.body.appendChild(fileInput);
        fileInput.click();
        document.body.removeChild(fileInput);
    });
};

// 直接从文件输入创建对象URL
window.createObjectURLFromFileInput = (fileName, fileSize, contentType) => {
    return new Promise((resolve, reject) => {
        console.log(`📁 请求文件: ${fileName} (${fileSize} bytes)`);

        // 创建文件输入元素
        const fileInput = document.createElement('input');
        fileInput.type = 'file';
        fileInput.accept = 'video/*';
        fileInput.style.display = 'none';

        // 设置超时
        const timeoutId = setTimeout(() => {
            reject(new Error('文件选择超时'));
        }, 30000);

        fileInput.onchange = (e) => {
            clearTimeout(timeoutId);
            const selectedFile = e.target.files[0];
            if (selectedFile) {
                try {
                    const objectUrl = URL.createObjectURL(selectedFile);
                    console.log(`✅ 文件对象URL创建成功: ${selectedFile.name}`);
                    resolve(objectUrl);
                } catch (error) {
                    reject(error);
                }
            } else {
                reject(new Error('没有选择文件'));
            }
        };

        // 添加到页面并触发点击
        document.body.appendChild(fileInput);
        fileInput.click();

        // 清理
        setTimeout(() => {
            if (fileInput.parentNode) {
                document.body.removeChild(fileInput);
            }
        }, 1000);
    });
};

// 增强的分块视频URL创建
window.createChunkedVideoURL = async (firstChunkRef, contentType, fileName, totalSize, chunkSize) => {
    console.log(`🧩 分块处理大文件: ${fileName}, 总大小: ${(totalSize / 1024 / 1024).toFixed(2)}MB`);

    try {
        // 读取第一个块
        const firstChunkArrayBuffer = await firstChunkRef.arrayBuffer();

        // 创建媒体源进行流式播放
        if (window.MediaSource && MediaSource.isTypeSupported(contentType)) {
            return await createMediaSourceWithChunks(firstChunkArrayBuffer, contentType, fileName, totalSize);
        } else {
            // 回退到直接Blob URL
            const blob = new Blob([firstChunkArrayBuffer], { type: contentType });
            const objectUrl = URL.createObjectURL(blob);
            console.log('✅ 第一个块Blob URL创建完成');
            return objectUrl;
        }
    } catch (error) {
        console.error('❌ 分块处理失败:', error);
        throw error;
    }
};

// 使用MediaSource进行分块播放
async function createMediaSourceWithChunks(firstChunk, contentType, fileName, totalSize) {
    return new Promise((resolve, reject) => {
        const mediaSource = new MediaSource();
        const objectUrl = URL.createObjectURL(mediaSource);

        mediaSource.addEventListener('sourceopen', () => {
            try {
                console.log('📡 MediaSource已打开，准备添加数据');
                const sourceBuffer = mediaSource.addSourceBuffer(contentType);

                // 添加第一个块
                sourceBuffer.appendBuffer(firstChunk);

                sourceBuffer.addEventListener('updateend', () => {
                    console.log('✅ 第一个块添加完成，视频可以开始播放');
                    // 这里可以继续添加更多块
                    mediaSource.endOfStream();
                    resolve(objectUrl);
                });

                sourceBuffer.addEventListener('error', (e) => {
                    console.error('❌ SourceBuffer错误:', e);
                    reject(new Error('SourceBuffer错误'));
                });

            } catch (error) {
                reject(error);
            }
        });

        mediaSource.addEventListener('sourceclose', () => {
            console.log('🔚 MediaSource关闭');
        });

        // 超时处理
        setTimeout(() => {
            if (mediaSource.readyState === 'open') {
                console.log('⚠️ MediaSource超时，但已打开');
                resolve(objectUrl);
            }
        }, 10000);
    });
}

// 设置视频循环播放
window.setVideoLoop = (videoElement, loop) => {
    videoElement.loop = loop;
    console.log(loop ? '🔁 循环播放已开启' : '🔁 循环播放已关闭');
    return true;
};

// 重新开始播放
window.restartVideo = (videoElement) => {
    videoElement.currentTime = 0;
    videoElement.play().catch(error => {
        console.error('重新播放失败:', error);
    });
    console.log('⏮️ 重新开始播放');
    return true;
};
// 显示播放速度反馈
window.showRateFeedback = (rate) => {
    // 可以在这里添加一些视觉反馈，比如轻微震动效果
    const activeButtons = document.querySelectorAll('.rate-btn-active');
    activeButtons.forEach(btn => {
        btn.style.transform = 'scale(0.95)';
        setTimeout(() => {
            btn.style.transform = 'scale(1)';
        }, 150);
    });

    console.log(`🎯 播放速度反馈: ${rate}x`);
};
// 设置播放速度
//window.setPlaybackRate = (videoElement, rate) => {
//    videoElement.playbackRate = rate;
//    console.log(`⚡ 播放速度设置为: ${rate}x`);
//    return true;
//};

// 获取播放信息
window.getPlaybackInfo = (videoElement) => {
    return {
        currentTime: videoElement.currentTime,
        duration: videoElement.duration,
        playbackRate: videoElement.playbackRate,
        loop: videoElement.loop,
        paused: videoElement.paused,
        ended: videoElement.ended
    };
};

// 跳转到指定时间
window.seekToTime = (videoElement, time) => {
    videoElement.currentTime = time;
    console.log(`⏩ 跳转到: ${time}秒`);
    return true;
};

// 添加快捷键支持
window.addVideoKeyboardControls = (videoElement) => {
    document.addEventListener('keydown', (e) => {
        // 如果用户在输入框中，不处理快捷键
        if (document.activeElement.tagName === 'INPUT' ||
            document.activeElement.tagName === 'TEXTAREA') {
            return;
        }

        switch (e.code) {
            case 'Space':
                e.preventDefault();
                if (videoElement.paused) {
                    videoElement.play();
                } else {
                    videoElement.pause();
                }
                break;
            case 'KeyL':
                e.preventDefault();
                videoElement.loop = !videoElement.loop;
                console.log(videoElement.loop ? '🔁 循环播放开启' : '🔁 循环播放关闭');
                break;
            case 'ArrowLeft':
                e.preventDefault();
                videoElement.currentTime = Math.max(0, videoElement.currentTime - 10);
                break;
            case 'ArrowRight':
                e.preventDefault();
                videoElement.currentTime = Math.min(videoElement.duration, videoElement.currentTime + 10);
                break;
            case 'Home':
                e.preventDefault();
                videoElement.currentTime = 0;
                break;
            case 'End':
                e.preventDefault();
                videoElement.currentTime = videoElement.duration;
                break;
        }
    });

    console.log('⌨️ 键盘快捷键已启用: 空格(播放/暂停), L(循环), 左右箭头(快退/快进)');
};
// 修复播放速度设置函数
window.setPlaybackRate = (videoElement, rate) => {
    try {
        videoElement.playbackRate = rate;
        console.log(`⚡ 播放速度设置为: ${rate}x`);
        return true;
    } catch (error) {
        console.error('❌ 播放速度设置失败:', error);
        return false;
    }
};

// 获取当前播放速度
window.getPlaybackRate = (videoElement) => {
    return videoElement.playbackRate || 1.0;
};