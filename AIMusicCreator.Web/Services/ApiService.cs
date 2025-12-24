

using AIMusicCreator.Entity;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.JSInterop;
using NAudio.Midi;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
namespace AIMusicCreator.Web.Services
{
    /// <summary>
    /// service for interacting with the backend API
    /// </summary>
    /// /// <param name="http"></param>
    /// <param name="js"></param>
    public class ApiService(HttpClient http, JsInteropService js)
    {
        private readonly HttpClient _http = http;
        private readonly JsInteropService _js = js;

        // 生成旋律
        /// <summary>
        /// s根据风格、情绪和BPM生成旋律音频
        /// </summary>
        /// <param name="style"></param>
        /// <param name="mood"></param>
        /// <param name="bpm"></param>
        /// <returns>任务完成时返回旋律音频字节数组</returns>
        /// <exception cref="InvalidOperationException">当连接断开时抛出</exception>
        /// <exception cref="Exception">其他异常时抛出</exception>
        /// <remarks>
        /// 支持的风格：Pop, Rock, Jazz, Classical
        /// 支持的情绪：Happy, Sad, Energetic, Calm
        /// </remarks>
        public async Task<byte[]> GenerateMelody(string style, string mood, int bpm)
        {
            var request = new MelodyRequest { Style = style, Mood = mood, Bpm = bpm };
            var response = await _http.PostAsJsonAsync("/api/music/generate-melody", request);
            return await response.Content.ReadAsByteArrayAsync();
        }

        // 生成伴奏 - 根据MIDI文件
        /// <summary>
        /// 根据上传的MIDI文件生成伴奏MIDI
        /// </summary>
        /// <param name="midiFile"></param>
        /// <returns>任务完成时返回伴奏MIDI字节数组</returns>
        /// <exception cref="InvalidOperationException">当连接断开时抛出</exception>
        /// <exception cref="Exception">其他异常时抛出</exception>
        /// <remarks>
        /// 支持的MIDI文件格式：.mid, .midi
        /// </remarks>
        public async Task<byte[]> GenerateAccompaniment(IBrowserFile midiFile)
        {
            using var content = new MultipartFormDataContent();
            long maxAllowedSize = Math.Max(1024 * 1024 * 10, midiFile.Size);
            var fileContent = new StreamContent(midiFile.OpenReadStream(maxAllowedSize));
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(midiFile.ContentType);
            content.Add(fileContent, "melodyMidi", midiFile.Name);

            var response = await _http.PostAsync("/api/music/generate-accompaniment", content);
            return await response.Content.ReadAsByteArrayAsync();
        }
        
        // 生成伴奏 - 根据参数
        /// <summary>
        /// 根据风格、和弦进行、BPM、乐器配置和鼓点设置生成伴奏
        /// </summary>
        /// <param name="style"></param>
        /// <param name="chordProgression"></param>
        /// <param name="bpm"></param>
        /// <param name="instrumentation"></param>
        /// <param name="includeDrums"></param>
        /// <returns>任务完成时返回伴奏MIDI字节数组</returns>
        /// <exception cref="InvalidOperationException">当连接断开时抛出</exception>
        /// <exception cref="Exception">其他异常时抛出</exception>
        /// <remarks>
        /// 支持的乐器：Piano, Guitar, Bass, Drums
        /// </remarks>
        public async Task<byte[]> GenerateAccompaniment(string style, string chordProgression, int bpm, string instrumentation, bool includeDrums)
        {
            // 创建伴奏请求对象
            var request = new {
                Style = style,
                ChordProgression = chordProgression,
                Bpm = bpm,
                Instrumentation = instrumentation,
                IncludeDrums = includeDrums
            };
            
            // 发送POST请求到API
            var response = await _http.PostAsJsonAsync("/api/music/generate-accompaniment", request);
            
            // 读取响应内容
            return await response.Content.ReadAsByteArrayAsync();
        }

        // 生成人声
        /// <summary>
        /// s根据MIDI文件和歌词生成对应的人声音频
        /// </summary>
        /// <param name="midiFile"></param>
        /// <param name="lyrics"></param>
        /// <param name="language"></param>
        /// <returns>任务完成时返回人声音频字节数组</returns>
        /// <exception cref="InvalidOperationException">当连接断开时抛出</exception>
        /// <exception cref="Exception">其他异常时抛出</exception>
        /// <remarks>
        /// 支持的语言：zh（中文）, en（英文）
        /// </remarks>
        public async Task<byte[]> GenerateVocal(IBrowserFile? midiFile, string lyrics, string language)
        {
            if(string.IsNullOrWhiteSpace(lyrics))
                throw new ArgumentException("歌词不能为空", nameof(lyrics));
            if(string.IsNullOrWhiteSpace(language)) 
                language = "zh";
            if(language != "zh" && language != "en")
                throw new ArgumentException("仅支持中文（zh）和英文（en）", nameof(language));
            if(midiFile == null)
                throw new ArgumentNullException(nameof(midiFile), "MIDI文件不能为空");
            using var content = new MultipartFormDataContent();
            // 添加MIDI文件
            long maxAllowedSize = Math.Max(1024 * 1024 * 10, midiFile.Size);
            var midiContent = new StreamContent(midiFile.OpenReadStream(maxAllowedSize));
            midiContent.Headers.ContentType = MediaTypeHeaderValue.Parse(midiFile.ContentType);
            content.Add(midiContent, "MelodyMidi", midiFile.Name);
            // 添加歌词和语言
            content.Add(new StringContent(lyrics), "Lyrics");
            content.Add(new StringContent(language), "Language");

            var response = await _http.PostAsync("/api/music/generate-vocal", content);
            return await response.Content.ReadAsByteArrayAsync();
        }

        // 多轨混音
        /// <summary>
        /// s将多条音轨混合为一条音频
        /// </summary>
        /// <param name="tracks"></param>
        /// <param name="volumes"></param>
        /// <returns>任务完成时返回混合后的音频字节数组和MIME类型</returns>
        /// <exception cref="InvalidOperationException">当连接断开时抛出</exception>
        /// <exception cref="Exception">其他异常时抛出</exception>
        /// <remarks>
        /// 支持的音频格式：.mp3, .wav, .flac
        /// </remarks>
        public async Task<(byte[],string?)> MixTracks(List<IBrowserFile> tracks, List<float> volumes)
        {
            using var content = new MultipartFormDataContent();
            long maxAllowedSize = 1024 * 1024 * 10;
            for (int i = 0; i < tracks.Count; i++)
            {
                maxAllowedSize = Math.Max(1024 * 1024 * 10, tracks[i].Size);
                var fileContent = new StreamContent(tracks[i].OpenReadStream(maxAllowedSize));
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(tracks[i].ContentType);
                content.Add(fileContent, "tracks", tracks[i].Name);
                content.Add(new StringContent(volumes[i].ToString()), "volumes");
            }

            var response = await _http.PostAsync("/api/music/mix-tracks", content);
            var contentType = response.Content.Headers.ContentType?.MediaType;
            return (await response.Content.ReadAsByteArrayAsync(),contentType);
        }
        // 添加音频特效
        /// <summary>
        /// s为音频文件添加指定的音频特效
        /// </summary>
        /// <param name="audioFile"></param>
        /// <param name="effectType"></param>
        /// <returns>任务完成时返回添加特效后的音频字节数组</returns>
        /// <exception cref="InvalidOperationException">当连接断开时抛出</exception>
        /// <exception cref="Exception">其他异常时抛出</exception>
        /// <remarks>
        /// 支持的特效类型：
        /// - BassBoost
        /// - TrebleBoost
        /// - Reverb
        /// - Delay
        /// - PitchShift
        /// - TimeStretch
        /// </remarks>
        public async Task<byte[]> AddAudioEffect(byte[] audioFileContent, string effectType,string contentType)
        {
            using var content = new MultipartFormDataContent();
            //long maxAllowedSize = Math.Max(1024 * 1024 * 10, audioFile.Size);
            var stream = new MemoryStream();
            stream.Write(audioFileContent);
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            content.Add(fileContent, "audio");
            content.Add(new StringContent(effectType), "effectType");

            var response = await _http.PostAsync("/api/music/add-effect", content);
            return await response.Content.ReadAsByteArrayAsync();
        }
        // 添加音频特效
        /// <summary>
        /// s为音频文件添加指定的音频特效
        /// </summary>
        /// <param name="audioFile"></param>
        /// <param name="effectType"></param>
        /// <returns>任务完成时返回添加特效后的音频字节数组</returns>
        /// <exception cref="InvalidOperationException">当连接断开时抛出</exception>
        /// <exception cref="Exception">其他异常时抛出</exception>
        /// <remarks>
        /// 支持的特效类型：
        /// - BassBoost
        /// - TrebleBoost
        /// - Reverb
        /// - Delay
        /// - PitchShift
        /// - TimeStretch
        /// </remarks>
        public async Task<byte[]> AddAudioEffect(IBrowserFile audioFile, string effectType )
        {
            using var content = new MultipartFormDataContent();
            long maxAllowedSize = Math.Max(1024 * 1024 * 10, audioFile.Size);
            var fileContent = new StreamContent(audioFile.OpenReadStream(maxAllowedSize));
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(audioFile.ContentType);
            content.Add(fileContent, "audio", audioFile.Name);
            content.Add(new StringContent(effectType), "effectType");
            content.Add(new StringContent(audioFile.ContentType), "contentType");

            var response = await _http.PostAsync("/api/music/add-effect", content);
            return await response.Content.ReadAsByteArrayAsync();
        }

        // 解析MIDI信息
        /// <summary>
        /// s解析MIDI文件并返回其信息
        /// </summary>
        /// <param name="midiFile"></param>
        /// <returns>任务完成时返回MIDI文件的信息</returns>
        /// <exception cref="InvalidOperationException">当连接断开时抛出</exception>
        /// <exception cref="Exception">其他异常时抛出</exception>
        /// <remarks>
        /// 支持的MIDI格式：.mid, .midi
        /// </remarks>
        public async Task<MidiInfo> ParseMidiInfo(IBrowserFile midiFile)
        {
            using var content = new MultipartFormDataContent();
            long maxAllowedSize = Math.Max(1024 * 1024 * 10, midiFile.Size);
            var fileContent = new StreamContent(midiFile.OpenReadStream(maxAllowedSize));
            content.Add(fileContent, "midiFile", midiFile.Name);

            var response = await _http.PostAsync("/api/music/parse-midi", content);
            return await response.Content.ReadFromJsonAsync<MidiInfo>() ?? new MidiInfo();
        }

        // 调整MIDI速度
        /// <summary>
        /// s更改MIDI的速度（BPM）
        /// </summary>
        /// <param name="midiFile"></param>
        /// <param name="newBpm"></param>
        /// <returns>任务完成时返回调整速度后的MIDI字节数组</returns>
        /// <exception cref="InvalidOperationException">当连接断开时抛出</exception>
        /// <exception cref="Exception">其他异常时抛出</exception>
        /// <remarks>
        /// 支持的MIDI格式：.mid, .midi
        /// </remarks>
        public async Task<byte[]> ChangeMidiTempo(IBrowserFile midiFile, int newBpm)
        {
            using var content = new MultipartFormDataContent();
            long maxAllowedSize = Math.Max(1024 * 1024 * 10, midiFile.Size);
            var fileContent = new StreamContent(midiFile.OpenReadStream(maxAllowedSize));
            content.Add(fileContent, "midiFile", midiFile.Name);
            content.Add(new StringContent(newBpm.ToString()), "newBpm");

            var response = await _http.PostAsync("/api/music/change-midi-tempo", content);
            return await response.Content.ReadAsByteArrayAsync();
        }

        
        /// <summary>
        /// save file to local disk
        /// 保存文件到本地（通过JS）
        /// </summary>
        /// <param name="data"></param>
        /// <param name="fileName"></param>
        /// <param name="contentType"></param>
        /// <returns>任务完成时返回</returns>
        /// <exception cref="InvalidOperationException">当连接断开时抛出</exception>
        /// <exception cref="Exception">其他异常时抛出</exception>
        /// <remarks>
        /// 支持的文件类型：.mid, .midi, .wav, .mp3
        /// </remarks>
        public async Task SaveFile(byte[] data, string fileName, string contentType)
        {
            var base64 = Convert.ToBase64String(data);
            await _js.InvokeVoidAsync("saveFile", base64, fileName, contentType);
        }
        // 在ApiService中添加以下方法
        /// <summary>
        /// s更改MIDI轨道的乐器
        /// </summary>
        /// <param name="midiFile"></param>
        /// <param name="trackIndex"></param>
        /// <param name="instrument"></param>
        /// <returns>任务完成时返回调整乐器后的MIDI字节数组</returns>
        /// <exception cref="InvalidOperationException">当连接断开时抛出</exception>
        /// <exception cref="Exception">其他异常时抛出</exception>
        /// <remarks>
        /// 支持的MIDI格式：.mid, .midi
        /// </remarks>
        public async Task<byte[]> ChangeMidiInstrument(IBrowserFile midiFile, int trackIndex, int instrument)
        {
            using var content = new MultipartFormDataContent();
            long maxAllowedSize = Math.Max( 1024 * 1024 * 10,midiFile.Size);
            var fileContent = new StreamContent(midiFile.OpenReadStream(maxAllowedSize));
            content.Add(fileContent, "midiFile", midiFile.Name);
            content.Add(new StringContent(trackIndex.ToString()), "trackIndex");
            content.Add(new StringContent(instrument.ToString()), "instrument");

            var response = await _http.PostAsync("/api/music/change-midi-instrument", content);
            return await response.Content.ReadAsByteArrayAsync();
        }
        // 新增：ConvertToMp3 方法（前端调用后端格式转换接口）
        /// <summary>
        /// s将WAV字节数组转换为MP3字节数组
        /// </summary>
        /// <param name="wavBytes"></param>
        /// <returns>任务完成时返回转换后的MP3字节数组</returns>
        /// <exception cref="InvalidOperationException">当连接断开时抛出</exception>
        /// <exception cref="Exception">其他异常时抛出</exception>
        /// <remarks>
        /// 支持的文件类型：.wav
        /// </remarks>
        public async Task<byte[]> ConvertToMp3(byte[] wavBytes)
        {
            // 后端接口需接收WAV字节数组，返回MP3字节数组
            using var content = new ByteArrayContent(wavBytes);
            content.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");

            var response = await _http.PostAsync("/api/music/convert-to-mp3", content);
            response.EnsureSuccessStatusCode(); // 确保请求成功
            return await response.Content.ReadAsByteArrayAsync();
        }
        // 音频裁剪
        /// <summary>
        /// s将音频数据裁剪为指定时间段
        /// </summary>
        /// <param name="audioData"></param>
        /// <param name="startSeconds"></param>
        /// <param name="endSeconds"></param>
        /// <returns>任务完成时返回裁剪后的音频字节数组</returns>
        /// <exception cref="InvalidOperationException">当连接断开时抛出</exception>
        /// <exception cref="Exception">其他异常时抛出</exception>
        /// <remarks>
        /// 支持的文件类型：.wav, .mp3
        /// </remarks>
        public async Task<byte[]> CutAudio(byte[] audioData, double startSeconds, double endSeconds)
        {
            var request = new CutAudioRequest
            {
                AudioData = Convert.ToBase64String(audioData),
                StartSeconds = startSeconds,
                EndSeconds = endSeconds
            };

            var response = await _http.PostAsJsonAsync("/api/music/cut-audio", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }

        // 音频拼接
        /// <summary>
        /// s将多段音频数据拼接为一段音频
        /// </summary>
        /// <param name="audioDatas"></param>
        /// <returns>任务完成时返回拼接后的音频字节数组</returns>
        /// <exception cref="InvalidOperationException">当连接断开时抛出</exception>
        /// <exception cref="Exception">其他异常时抛出</exception>
        /// <remarks>
        /// 支持的文件类型：.wav, .mp3
        /// </remarks>
        public async Task<byte[]> JoinAudios(List<byte[]> audioDatas)
        {
            var request = new JoinAudioRequest
            {
                AudioDatas = [.. audioDatas.Select(d => Convert.ToBase64String(d))]
            };

            var response = await _http.PostAsJsonAsync("/api/music/join-audios", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }

        // AI生成歌词
        /// <summary>
        /// s根据指定的主题和风格生成歌词
        /// </summary>
        /// <param name="theme"></param>
        /// <param name="style"></param>
        /// <param name="paragraphs"></param>
        /// <returns>任务完成时返回生成的歌词字符串</returns>
        /// <exception cref="InvalidOperationException">当连接断开时抛出</exception>
        /// <exception cref="Exception">其他异常时抛出</exception>
        /// <remarks>
        /// 支持的风格：中文、英文、日文
        /// </remarks>
        public async Task<string> GenerateLyrics(string theme, string style, int paragraphs)
        {
            var request = new AiLyricRequest
            {
                Theme = theme,
                Style = style,
                ParagraphCount = paragraphs
            };

            var response = await _http.PostAsJsonAsync("/api/ai/generate-lyrics", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        // AI生成旋律灵感
        /// <summary>
        /// s根据指定的情绪和风格生成旋律灵感音频
        /// </summary>
        /// <param name="mood"></param>
        /// <param name="style"></param>
        /// <returns>任务完成时返回生成的旋律灵感音频字节数组</returns>
        /// <exception cref="InvalidOperationException">当连接断开时抛出</exception>
        /// <exception cref="Exception">其他异常时抛出</exception>
        /// <remarks>
        /// 支持的风格：中文、英文、日文
        /// </remarks>
        public async Task<byte[]> GenerateMelodyInspiration(string mood, string style)
        {
            var request = new AiMelodyRequest
            {
                Mood = mood,
                Style = style
            };

            var response = await _http.PostAsJsonAsync("/api/ai/generate-melody-inspiration", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }

        // AI生成和弦进行
        /// <summary>
        /// s根据指定的调性、风格和乐段生成和弦进行
        /// </summary>
        /// <param name="key"></param>
        /// <param name="style"></param>
        /// <param name="section"></param>
        /// <returns>任务完成时返回生成的和弦进行结果</returns>
        /// <exception cref="InvalidOperationException">当连接断开时抛出</exception>
        /// <exception cref="Exception">其他异常时抛出</exception>
        /// <remarks>
        /// 支持的风格：中文、英文、日文
        /// </remarks>
        public async Task<ChordProgressionResult> GenerateChordProgression(string key, string style, string section)
        {
            var request = new AiChordRequest
            {
                Key = key,
                Style = style,
                Section = section
            };

            var response = await _http.PostAsJsonAsync("/api/ai/generate-chord-progression", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ChordProgressionResult>() ?? new ChordProgressionResult();
        }

        // 多轨混音
        /// <summary>
        /// s将多条音轨混合为一条音频
        /// </summary>
        /// <param name="tracks"></param>
        /// <returns>任务完成时返回混合后的音频字节数组</returns>
        /// <exception cref="InvalidOperationException">当连接断开时抛出</exception>
        /// <exception cref="Exception">其他异常时抛出</exception>
        /// <remarks>
        /// 支持的文件类型：.wav, .mp3
        /// </remarks>
        public async Task<byte[]> MixAudioTracks(List<MixTrackRequest> tracks)
        {
            var request = new
            {
                Tracks = tracks
            };

            var response = await _http.PostAsJsonAsync("/api/music/mix-tracks", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }

        // 音频格式转换
        /// <summary>
        /// s将音频数据从一种格式转换为另一种格式
        /// </summary>
        /// <param name="audioData"></param>
        /// <param name="sourceFormat"></param>
        /// <param name="targetFormat"></param>
        /// <param name="mp3Quality"></param>
        /// <returns>任务完成时返回转换后的音频字节数组</returns>
        /// <exception cref="InvalidOperationException">当连接断开时抛出</exception>
        /// <exception cref="Exception">其他异常时抛出</exception>
        /// <remarks>
        /// 支持的转换：.wav to .mp3, .mp3 to .wav
        /// </remarks>
        public async Task<byte[]> ConvertAudioFormat(byte[] audioData, string sourceFormat, string targetFormat, int mp3Quality = 192)
        {
            var request = new
            {
                AudioData = Convert.ToBase64String(audioData),
                SourceFormat = sourceFormat,
                TargetFormat = targetFormat,
                Mp3Quality = mp3Quality
            };

            var response = await _http.PostAsJsonAsync("/api/music/convert-format", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }

        // 应用音频特效
        /// <summary>
        /// s
        /// </summary>
        /// <param name="request"></param>
        /// <returns>任务完成时返回应用特效后的音频字节数组</returns>
        /// <exception cref="InvalidOperationException">当连接断开时抛出</exception>
        /// <exception cref="Exception">其他异常时抛出</exception>
        /// <remarks>
        /// 支持的特效：均衡器、压缩器、混响、延迟、颤音
        /// </remarks>
        public async Task<byte[]> ApplyAudioEffects(AudioEffectRequest request)
        {
            var response = await _http.PostAsJsonAsync("/api/music/apply-effects", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }

        // 人声分离
        /// <summary>
        /// s将混合音频中的人声和伴奏分离成独立的音轨
        /// </summary>
        /// <param name="request"></param>
        /// <returns>任务完成时返回分离后的人声和伴奏音轨数据</returns>
        /// <exception cref="InvalidOperationException">当连接断开时抛出</exception>
        /// <exception cref="Exception">其他异常时抛出</exception>
        /// <remarks>
        /// 支持的文件类型：.wav, .mp3
        /// </remarks>
        public async Task<List<SeparatedTrackData>> SeparateVocalTracks(VocalSeparationRequest request)
        {
            var response = await _http.PostAsJsonAsync("/api/music/separate-vocals", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<SeparatedTrackData>>() ?? [];
        }

        // 音频降噪
        /// <summary>
        /// s对音频数据进行降噪处理
        /// </summary>
        /// <param name="audioData"></param>
        /// <param name="strength"></param>
        /// <returns>任务完成时返回降噪后的音频字节数组</returns>
        /// <exception cref="InvalidOperationException">当连接断开时抛出</exception>
        /// <exception cref="Exception">其他异常时抛出</exception>
        /// <remarks>
        /// 支持的文件类型：.wav, .mp3
        /// </remarks>
        public async Task<byte[]> DenoiseAudio(byte[] audioData, double strength)
        {
            var request = new
            {
                AudioData = Convert.ToBase64String(audioData),
                Strength = strength
            };

            var response = await _http.PostAsJsonAsync("/api/music/denoise-audio", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }

        

        // 转换音频数据为可播放的URL
        /// <summary>
        /// s将音频字节数组转换为可播放的Data URL
        /// </summary>
        /// <param name="data"></param>
        /// <returns>任务完成时返回可播放的Data URL</returns>
        /// <exception cref="InvalidOperationException">当连接断开时抛出</exception>
        /// <exception cref="Exception">其他异常时抛出</exception>
        /// <remarks>
        /// 支持的文件类型：.wav, .mp3
        /// </remarks>
        public string GetAudioUrl(byte[] data) =>
            $"data:audio/wav;base64,{Convert.ToBase64String(data)}";
        /// <summary>
        /// s将音频字节数组转换为指定MIME类型的可播放Data URL
        /// </summary>
        /// <param name="data"></param>
        /// <param name="mimeType"></param>
        /// <returns>任务完成时返回可播放的Data URL</returns>
        /// <exception cref="InvalidOperationException">当连接断开时抛出</exception>
        /// <exception cref="Exception">其他异常时抛出</exception>
        /// <remarks>
        /// 支持的文件类型：.wav, .mp3
        /// </remarks>
        public string GetAudioUrl(byte[] data, string mimeType)
        {
            return $"data:{mimeType};base64,{Convert.ToBase64String(data)}";
        }
    }
}
