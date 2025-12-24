using AIMusicCreator.Entity;
using AIMusicCreator.Utils;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.MusicTheory;
using NAudio.Midi;
using NoteEvent = AIMusicCreator.Entity.NoteEvent;
using System.Collections.Generic;

namespace AIMusicCreator.ApiService.Services.DryWetMidiGerenteMidi
{
    /// <summary>
    /// 旋律生成器类
    /// 根据参数生成主旋律音符序列
    /// </summary>
    /// <remarks>
    /// 该类负责根据给定的音乐风格、情绪、音阶等参数生成完整的旋律。
    /// 支持多种音乐风格（流行、摇滚、爵士、古典等）和情绪状态（快乐、悲伤、活力等）的组合。
    /// 实现了基于概率分布的音符选择、旋律轮廓控制和八度跳跃限制等功能。
    /// </remarks>
    public class MelodyGenerator
    {
        /// <summary>
        /// 随机数生成器
        /// </summary>
        /// <remarks>
        /// 此随机数生成器用于在旋律生成过程中进行随机选择和分布。
        /// 确保每次生成的旋律都是不同的。
        /// </remarks>
        private static readonly Random _random = new();

        /// <summary>
        /// 生成旋律的主方法
        /// </summary>
        /// <param name="parameters">旋律生成参数</param>
        /// <returns>生成的音符事件列表</returns>
        /// <remarks>
        /// 此方法根据提供的旋律生成参数，使用随机数生成器和概率分布，
        /// 生成一个完整的音符序列。每个音符的选择基于音阶、情绪、音乐风格等参数。
        /// 生成的音符序列长度为参数中指定的小节数（Bars）乘以16个16分音符。
        /// </remarks>
        public List<NoteEvent> GenerateMelody(MelodyParameters parameters)
        {
            //var notesC = new MidiEventCollection();
            var notes = new List<NoteEvent>();
            // 获取指定音阶的所有音符
            var scaleNotes = GetScaleNotes(parameters.Scale, parameters.Octave);

            long currentTime = 0;           // 当前时间位置（ticks）
            int notesGenerated = 0;         // 已生成的音符计数
            int totalNotes = parameters.Bars * 16; // 总音符数（假设每小节16个16分音符）

            // 循环生成所有音符
            while (notesGenerated < totalNotes)
            {
                var noteEvent = GenerateNextNote(parameters, scaleNotes, currentTime, notes);
                if (noteEvent != null)
                {
                    notes.Add(noteEvent);
                    currentTime += noteEvent.Duration;  // 更新时间位置
                    notesGenerated++;                   // 增加计数
                }
            }

            return notes;
        }
        
        /// <summary>
        /// 基于当前位置生成动机模式
        /// </summary>
        /// <param name="position">当前位置（用于选择合适的动机模式）</param>
        /// <returns>动机模式数组（每个元素表示音程变化）</returns>
        /// <remarks>
        /// 此方法根据当前位置（小节数或时间点）选择合适的动机模式。
        /// 动机模式用于控制音符之间的音程变化，增加旋律的变化性和动态效果。
        /// 主要动机模式和变化动机模式被定义在数组中，根据位置索引选择。
        /// 30%的概率随机选择一个模式，增加变化性。
        /// </remarks>
        private static int[] GetMotivicPattern(int position)
        {
            // 根据音乐位置选择不同的动机模式，增加旋律变化性
            // 主要动机模式和变化动机模式
            int[][] patterns =
            [
                [1, -1, 0, 2, -2, 1],  // 主要动机：上行-下行-保持-大上行-大下行-上行
                [2, 0, -1, 1, -2, 0],  // 变化1：大上行-保持-下行-上行-大下行-保持
                [0, 1, -2, 1, 0, -1]   // 变化2：保持-上行-大下行-上行-保持-下行
            ];
            
            // 基于位置选择模式，确保在不同位置有不同的动机变化
            int patternIndex = position % patterns.Length;
            
            // 30%概率随机选择一个模式增加变化
            if (_random.NextDouble() < 0.3)
            {
                patternIndex = _random.Next(patterns.Length);
            }
            
            return patterns[patternIndex];
        }
        /// <summary>
        /// 生成单个音符
        /// </summary>
        /// <param name="parameters">旋律生成参数</param>
        /// <param name="scaleNotes">可用的音阶音符列表</param>
        /// <param name="currentTime">当前时间位置（ticks）</param>
        /// <param name="previousNotes">之前生成的音符（用于上下文和旋律轮廓控制）</param>
        /// <returns>新生成的音符事件</returns>
        /// <remarks>
        /// 此方法根据提供的旋律生成参数、可用的音阶音符列表、当前时间位置和之前生成的音符，
        /// 使用随机数生成器和概率分布，生成一个新的音符事件。
        /// 生成的音符基于风格、情绪、音乐位置和前一个音符，考虑了音程变化和八度跳跃限制。
        /// </remarks>
        private static NoteEvent GenerateNextNote(MelodyParameters parameters, List<NoteName> scaleNotes,
                                         long currentTime, List<NoteEvent> previousNotes)
        {
            // 基于风格和情绪获取概率分布参数
            var styleParams = GetStyleParameters(parameters.Style, parameters.Emotion);
            var (noteProbabilities, rhythmPattern, maxOctaveJump) = styleParams;

            // 获取当前位置（用于动机模式）
            int position = previousNotes.Count;
            
            // 创建动机模式（如果有需要）
            int[] motivicPattern = GetMotivicPattern(position);

            NoteName noteName;
            int octave;

            // 改进的音符选择逻辑
            if (previousNotes.Count == 0)
            {
                // 第一个音符：优先选择根音或主音以建立调性
                double[] firstNoteProbabilities = ApplyHarmonicWeights((double[])noteProbabilities.Clone(), parameters.Emotion);
                var noteIndex = SelectNoteByProbability(firstNoteProbabilities, scaleNotes.Count);
                noteName = scaleNotes[noteIndex];
                octave = parameters.Octave;
            }
            else
            {
                // 基于前一个音符和动机模式选择下一个音符
                var lastNote = previousNotes.Last();
                int lastNoteIndex = scaleNotes.IndexOf(lastNote.Note);
                
                if (lastNoteIndex == -1)
                {
                    // 如果找不到上一个音符在音阶中的位置，使用概率选择
                    lastNoteIndex = 0;
                }
                
                // 应用动机模式中的音程变化（70%概率遵循动机）
                int newIndex;
                if (_random.NextDouble() < 0.7)
                {
                    // 遵循动机模式
                    int intervalChange = motivicPattern[position % motivicPattern.Length];
                    newIndex = lastNoteIndex + intervalChange;
                    
                    // 确保索引在有效范围内
                    while (newIndex < 0)
                        newIndex += scaleNotes.Count;
                    while (newIndex >= scaleNotes.Count)
                        newIndex -= scaleNotes.Count;
                }
                else
                {
                    // 应用和声权重的概率选择
                    double[] weightedProbabilities = ApplyHarmonicWeights((double[])noteProbabilities.Clone(), parameters.Emotion);
                    newIndex = SelectNoteByProbability(weightedProbabilities, scaleNotes.Count);
                }
                
                noteName = scaleNotes[newIndex];
                
                // 确定八度（使用改进的逻辑）
                octave = DetermineOctaveImproved(parameters.Octave, newIndex, scaleNotes.Count,
                                              maxOctaveJump, previousNotes);
            }

            // 使用改进的节奏时值确定
            var duration = GetVariedNoteDuration(parameters.Style, parameters.Emotion, rhythmPattern, previousNotes);

            // 使用改进的力度确定
            var velocity = GetExpressiveNoteVelocity(parameters.Emotion, previousNotes.Count, position);

            // 使用增强的旋律轮廓控制
            var (adjustedNote, adjustedOctave) = ApplyEnhancedMelodicContour(noteName, octave, previousNotes, maxOctaveJump, parameters.Style);

            return new NoteEvent
            {
                Note = adjustedNote,
                Octave = adjustedOctave,
                StartTime = currentTime,
                Duration = duration,
                Velocity = velocity
            };
        }
        /// <summary>
        /// 基于音乐理论应用和声权重
        /// </summary>
        /// <param name="probabilities">音符概率分布数组</param>
        /// <param name="emotion">当前情绪</param>
        /// <returns>应用和声权重后的概率分布数组</returns>
        /// <remarks>
        /// 此方法根据当前情绪，为音符概率分布数组应用和声权重。
        /// 增加根音、三音、五音的权重，减少不稳定音的权重。
        /// 不同情绪会导致不同的音符选择倾向，增强旋律的变化性和动态效果。
        /// </remarks>
        private static double[] ApplyHarmonicWeights(double[] probabilities, Emotion emotion)
        {
            // 基于音乐理论应用和声权重
            // 增加根音、三音、五音的权重，减少不稳定音的权重
            double[] harmonicWeights = emotion switch
            {
                Emotion.Happy => [1.6, 1.4, 1.0, 1.3, 1.1, 0.6, 0.9], // 强调大调色彩，明亮的大三和弦
                Emotion.Energetic => [1.7, 1.2, 0.8, 1.4, 1.1, 0.7, 0.8], // 充满活力，根音和五音更强
                Emotion.Sad => [1.6, 1.4, 0.8, 1.1, 1.0, 0.9, 0.6], // 强调小三和弦，增加六度音
                Emotion.Calm => [1.4, 1.2, 1.0, 1.1, 1.0, 0.8, 0.8], // 平衡，平滑过渡
                Emotion.Romantic => [1.5, 1.1, 0.9, 1.3, 1.0, 0.9, 0.7], // 更注重旋律性，三音略低
                Emotion.Mysterious => [1.3, 0.8, 1.4, 1.0, 0.9, 1.3, 1.0], // 增强不稳定音，创造神秘感
                _ => [1.5, 1.2, 1.0, 1.2, 1.0, 0.8, 0.7] // 默认权重
            };
            
            // 应用权重并重新归一化
            double sum = 0;
            for (int i = 0; i < probabilities.Length && i < harmonicWeights.Length; i++)
            {
                probabilities[i] *= harmonicWeights[i];
                sum += probabilities[i];
            }
            
            // 归一化概率
            if (sum > 0)
            {
                for (int i = 0; i < probabilities.Length; i++)
                {
                    probabilities[i] /= sum;
                }
            }
            
            return probabilities;
        }
        /// <summary>
        /// 确定音符的八度，考虑动机模式、 PreviousNotes 中的音符和最大八度跳跃。
        /// </summary>
        /// <param name="baseOctave">基础八度</param>
        /// <param name="noteIndex">当前音符在音阶中的索引</param>
        /// <param name="totalNotes">音阶总音符数</param>
        /// <param name="maxOctaveJump">最大八度跳跃</param>
        /// <param name="previousNotes">之前生成的音符列表</param>
        /// <returns>确定的八度</returns>
        /// <remarks>
        /// 此方法根据当前音符的索引、音阶总音符数、最大八度跳跃和之前生成的音符，
        /// 确定音符的八度。考虑了动机模式、 PreviousNotes 中的音符和最大八度跳跃。
        /// 增加了额外的音乐逻辑，避免不必要的八度跳跃，保持旋律的连贯性。
        /// </remarks>
        private static int DetermineOctaveImproved(int baseOctave, int noteIndex, int totalNotes,
                          int maxOctaveJump, List<NoteEvent> previousNotes)
        {
            // 基础八度确定（使用现有的方法）
            int octave = DetermineOctave(baseOctave, noteIndex, totalNotes, maxOctaveJump, previousNotes);
            
            // 额外的音乐逻辑：避免不必要的八度跳跃
            if (previousNotes.Count > 0)
            {
                var lastNote = previousNotes.Last();
                int jump = Math.Abs(octave - lastNote.Octave);
                
                // 如果跳跃较大但音符在音阶中的相对位置接近，保持在同一八度
                if (jump >= 2)
                {
                    int lastNoteIndex = totalNotes / 2; // 估计值
                    int currentNoteIndex = noteIndex;
                    
                    // 计算相对位置差
                    int relativeDiff = Math.Abs(currentNoteIndex - lastNoteIndex);
                    
                    // 如果相对位置接近但八度跳跃大，减少跳跃
                    if (relativeDiff <= 2)
                    {
                        // 50%概率保持在同一八度
                        if (_random.NextDouble() < 0.5)
                        {
                            octave = lastNote.Octave;
                        }
                    }
                }
            }
            
            return octave;
        }
        /// <summary>
        /// 确定音符的时值，考虑 PreviousNotes 中的音符和音乐风格。
        /// </summary>
        /// <param name="style">音乐风格</param>
        /// <param name="emotion">情绪</param>
        /// <param name="rhythmPattern">节奏模式</param>
        /// <param name="previousNotes">之前生成的音符列表</param>
        /// <returns>确定的时值</returns>
        /// <remarks>
        /// 此方法根据音乐风格、情绪、节奏模式和之前生成的音符，
        /// 确定音符的时值。考虑了 PreviousNotes 中的音符，
        /// 避免连续相同时值，增加变化性和动态效果。
        /// 不同风格和情绪会导致不同的时值选择倾向，
        /// 增强旋律的变化性和动态效果。
        /// </remarks>
        private static long GetVariedNoteDuration(MusicStyle style, Emotion emotion, double[] rhythmPattern, List<NoteEvent> previousNotes)
        {
            // 基础时值选择
            long duration = GetNoteDuration(style, emotion, rhythmPattern);
            
            // 添加节奏变化
            if (previousNotes.Count > 0)
            {
                var lastNote = previousNotes.Last();
                
                // 避免连续相同时值过多
                if (lastNote.Duration == duration && _random.NextDouble() < 0.7)
                {
                    // 70%的概率改变时值
                    int[] durationOptions = [120, 240, 480, 960];
                    duration = durationOptions[_random.Next(durationOptions.Length)];
                    
                    // 应用情绪因子
                    double emotionFactor = emotion switch
                    {
                        Emotion.Happy => 0.8,
                        Emotion.Sad => 1.3,
                        Emotion.Energetic => 0.7,
                        Emotion.Calm => 1.2,
                        Emotion.Mysterious => 1.1,
                        Emotion.Romantic => 1.0,
                        _ => 1.0
                    };
                    
                    duration = (long)(duration * emotionFactor);
                }
                
                // 在强拍位置（如小节开始）增加时值变化
                int position = previousNotes.Count;
                if (position % 4 == 0 && _random.NextDouble() < 0.3)
                {
                    // 30%概率在强拍使用更长的时值
                    duration = Math.Max(duration, 480); // 至少是四分音符
                }
            }
            
            // 添加一些附点音符效果
            if (_random.NextDouble() < 0.15)
            {
                duration = (long)(duration * 1.5);
            }
            
            return duration;
        }
        /// <summary>
        /// 确定音符的力度，考虑情绪、音符位置和整体位置。
        /// </summary>
        /// <param name="emotion">情绪</param>
        /// <param name="notePosition">音符在当前段落中的相对位置（用于局部力度变化）</param>
        /// <param name="totalPosition">总音符位置（用于全局力度变化）</param>
        /// <returns>确定的力度</returns>
        /// <remarks>
        /// 此方法根据情绪、音符位置和整体位置，
        /// 确定音符的力度。考虑了情绪、音符位置和整体位置，
        /// 实现了不同情绪下的力度变化和局部力度变化。
        /// 不同风格和情绪会导致不同的力度选择倾向，
        /// 增强旋律的变化性和动态效果。
        /// </remarks>
        private static int GetExpressiveNoteVelocity(Emotion emotion, int notePosition, int totalPosition)
        {
            // 基础力度
            int baseVelocity = GetNoteVelocity(emotion);
            
            // 1. 在强拍位置增加力度
            if (totalPosition > 0 && totalPosition % 4 == 0)
            {
                // 在小节强拍上增加力度
                baseVelocity = Math.Min(127, baseVelocity + 8);
            }
            
            // 2. 添加渐强渐弱效果
            if (totalPosition > 0 && totalPosition % 8 == 0 && _random.NextDouble() < 0.15)
            {
                if (_random.NextDouble() < 0.5)
                {
                    // 渐强：增加力度
                    baseVelocity = Math.Min(127, baseVelocity + 12);
                }
                else
                {
                    // 渐弱：减少力度
                    baseVelocity = Math.Max(30, baseVelocity - 12);
                }
            }
            
            // 3. 使用音符位置实现局部力度变化（新增）
            // 每8个音符形成一个小段落，在段落内部应用力度变化
            int positionInPhrase = notePosition % 8;
            if (positionInPhrase == 0 || positionInPhrase == 4)
            {
                // 段落强拍位置轻微增加力度
                baseVelocity = Math.Min(127, baseVelocity + 3);
            }
            
            // 4. 旋律走向的力度变化
            if (_random.NextDouble() < 0.2)
            {
                // 轻微的力度变化
                baseVelocity += _random.Next(-6, 7);
                baseVelocity = Math.Max(30, Math.Min(127, baseVelocity));
            }
            
            return baseVelocity;
        }
        /// <summary>
        /// 应用增强的旋律轮廓控制，考虑 PreviousNotes 中的音符和音乐风格。
        /// </summary>
        /// <param name="currentNote">当前音符</param>
        /// <param name="currentOctave">当前八度</param>
        /// <param name="previousNotes">之前生成的音符列表</param>
        /// <param name="maxOctaveJump">最大八度跳跃</param>
        /// <param name="style">音乐风格</param>
        /// <returns>调整后的音符和八度</returns>
        /// <remarks>
        /// 此方法根据当前音符、当前八度、之前生成的音符列表、最大八度跳跃和音乐风格，
        /// 应用增强的旋律轮廓控制。考虑了 PreviousNotes 中的音符和音乐风格，
        /// 避免不和谐的大跳，保持旋律的连贯性和音乐化。
        /// 不同风格会导致不同的旋律轮廓控制倾向，
        /// 增强旋律的变化性和动态效果。
        /// </remarks>
        private static (NoteName, int) ApplyEnhancedMelodicContour(NoteName currentNote, int currentOctave,
                                   List<NoteEvent> previousNotes, int maxOctaveJump, MusicStyle style)
        {
            // 先应用基本的旋律轮廓控制
            var (adjustedNote, adjustedOctave) = ApplyMelodicContour(currentNote, currentOctave, previousNotes, maxOctaveJump);
            
            // 只有在有前置音符时才进行额外处理
            if (previousNotes.Count > 0)
            {
                var lastNote = previousNotes.Last();
                
                // 计算音符间的半音距离
                int lastNoteNumber = NoteUtilities.GetNoteNumber(lastNote.Note, lastNote.Octave);
                int currentNoteNumber = NoteUtilities.GetNoteNumber(adjustedNote, adjustedOctave);
                int semitoneDistance = Math.Abs(currentNoteNumber - lastNoteNumber);
                
                // 根据风格调整大跳控制
                bool avoidLargeJumps = style switch
                {
                    MusicStyle.Classical => true,
                    MusicStyle.Jazz => false, // 爵士允许更大的跳跃
                    _ => true
                };
                
                // 避免不和谐的大跳
                if (avoidLargeJumps && semitoneDistance > 12)
                {
                    // 使用更音乐化的音程
                    int direction = Math.Sign(currentNoteNumber - lastNoteNumber);
                    int[] musicalIntervals = [3, 4, 5, 7, 9]; // 三、四、五、七、九度
                    int chosenInterval = musicalIntervals[_random.Next(musicalIntervals.Length)];
                    
                    int newNoteNumber = lastNoteNumber + direction * chosenInterval;
                    currentOctave = newNoteNumber / 12;
                    currentNote = (NoteName)(newNoteNumber % 12);
                    
                    return (currentNote, currentOctave);
                }
                
                // 添加旋律平滑度控制：优先选择级进（相邻音符）
                if (_random.NextDouble() < 0.1 && semitoneDistance > 5)
                {
                    // 10%概率使旋律更平滑
                    int direction = Math.Sign(currentNoteNumber - lastNoteNumber);
                    // 选择一个较小的音程
                    int[] smallIntervals = [1, 2, 3, 4, 5];
                    int chosenInterval = smallIntervals[_random.Next(smallIntervals.Length)];
                    
                    int newNoteNumber = lastNoteNumber + direction * chosenInterval;
                    currentOctave = newNoteNumber / 12;
                    currentNote = (NoteName)(newNoteNumber % 12);
                    
                    return (currentNote, currentOctave);
                }
            }
            
            return (adjustedNote, adjustedOctave);
        }
        


        /// <summary>
        /// 根据概率分布选择音符
        /// </summary>
        /// <param name="probabilities">音符概率分布</param>
        /// <param name="noteCount">可用音符数量</param>
        /// <returns>选中的音符索引</returns>
        /// <remarks>
        /// 此方法根据概率分布选择音符，考虑了音符数量的限制。
        /// 每个音符的概率被累加，直到随机值小于等于累加值，
        /// 则选择该音符作为选中的音符索引。
        /// 如果随机值超出所有音符的概率分布，
        /// 则随机选择一个音符作为选中的音符索引。
        /// </remarks>
        private static int SelectNoteByProbability(double[] probabilities, int noteCount)
        {
            var randomValue = _random.NextDouble();
            double cumulative = 0.0;

            for (int i = 0; i < probabilities.Length && i < noteCount; i++)
            {
                cumulative += probabilities[i];
                if (randomValue <= cumulative)
                    return i;
            }

            return _random.Next(noteCount);
        }

        /// <summary>
        /// 确定八度，考虑旋律轮廓和跳跃限制
        /// </summary>
        /// <param name="baseOctave">基础八度</param>
        /// <param name="noteIndex">当前音符索引</param>
        /// <param name="totalNotes">总音符数量</param>
        /// <param name="maxOctaveJump">最大八度跳跃</param>
        /// <param name="previousNotes">之前生成的音符列表</param>
        /// <returns>确定的八度</returns>
        /// <remarks>
        /// 此方法根据基础八度、音符索引、总音符数量、最大八度跳跃和之前生成的音符列表，
        /// 确定音符的八度。考虑了音符索引和总音符数量，
        /// 确保在音符接近音阶顶部或底部时，有机会升高或降低八度。
        /// 考虑了 PreviousNotes 中的音符，
        /// 避免不和谐的大跳，保持旋律的连贯性和音乐化。
        /// 不同风格和音符索引会导致不同的八度选择倾向，
        /// 增强旋律的变化性和动态效果。
        /// </remarks>
        private static int DetermineOctave(int baseOctave, int noteIndex, int totalNotes,
                          int maxOctaveJump, List<NoteEvent> previousNotes)
        {
            int octave = baseOctave;

            // 如果接近音阶顶部，可能升高八度
            if (noteIndex >= totalNotes - 2)
            {
                double jumpProbability = 0.3;

                // 如果之前有音符，考虑旋律走向和跳跃限制
                if (previousNotes.Count != 0)
                {
                    var lastNote = previousNotes.Last();
                    // 如果上一个音符已经在高音区，保持趋势
                    if (lastNote.Octave > baseOctave)
                        jumpProbability = 0.6;

                    // 检查跳跃是否超过限制
                    var potentialJump = Math.Abs((baseOctave + 1) - lastNote.Octave);
                    if (potentialJump > maxOctaveJump)
                    {
                        jumpProbability *= 0.3; // 减少跳跃概率
                    }
                }

                if (_random.NextDouble() < jumpProbability)
                    octave = baseOctave + 1;
            }
            // 如果接近音阶底部，可能降低八度
            else if (noteIndex <= 1)
            {
                double lowerProbability = 0.2;

                // 如果之前有音符，检查跳跃限制
                if (previousNotes.Count != 0)
                {
                    var lastNote = previousNotes.Last();
                    var potentialJump = Math.Abs((baseOctave - 1) - lastNote.Octave);
                    if (potentialJump > maxOctaveJump)
                    {
                        lowerProbability *= 0.3; // 减少降低概率
                    }
                }

                if (_random.NextDouble() < lowerProbability)
                    octave = Math.Max(1, baseOctave - 1);
            }

            // 进一步检查与上一个音符的跳跃
            if (previousNotes.Count != 0)
            {
                var lastNote = previousNotes.Last();
                var jump = Math.Abs(octave - lastNote.Octave);

                // 如果跳跃仍然太大，调整到合适的八度
                if (jump > maxOctaveJump)
                {
                    int direction = octave > lastNote.Octave ? -1 : 1;
                    octave = lastNote.Octave + (maxOctaveJump * direction);
                    octave = Math.Max(1, Math.Min(7, octave)); // 限制在合理范围内
                }
            }

            return octave;
        }
        //private int DetermineOctave(int baseOctave, int noteIndex, int totalNotes,
        //                          int maxOctaveJump, List<NoteEvent> previousNotes)
        //{
        //    int octave = baseOctave;

        //    // 如果接近音阶顶部，可能升高八度
        //    if (noteIndex >= totalNotes - 2)
        //    {
        //        double jumpProbability = 0.3;

        //        // 如果之前有音符，考虑旋律走向
        //        if (previousNotes.Count != 0)
        //        {
        //            var lastNote = previousNotes.Last();
        //            // 如果上一个音符已经在高音区，保持趋势
        //            if (lastNote.Octave > baseOctave)
        //                jumpProbability = 0.6;
        //        }

        //        if (_random.NextDouble() < jumpProbability)
        //            octave = baseOctave + 1;
        //    }
        //    // 如果接近音阶底部，可能降低八度
        //    else if (noteIndex <= 1)
        //    {
        //        if (_random.NextDouble() < 0.2)
        //            octave = Math.Max(1, baseOctave - 1);
        //    }

        //    return octave;
        //}

        /// <summary>
        /// 根据风格和情绪获取音符时值
        /// </summary>
        /// <param name="style">音乐风格</param>
        /// <param name="emotion">情绪</param>
        /// <param name="rhythmPattern">节奏模式概率分布数组</param>
        /// <returns>音符时值（ticks）</returns>
        /// <remarks>
        /// 此方法根据音乐风格、情绪和节奏模式概率分布，
        /// 选择音符的时值。考虑了风格、情绪和节奏模式的影响，
        /// 确保生成的音符时值符合音乐化的要求。
        /// 不同风格和情绪会导致不同的时值选择倾向，
        /// 增强旋律的变化性和动态效果。
        /// </remarks>
        private static long GetNoteDuration(MusicStyle style, Emotion emotion, double[] rhythmPattern)
        {
            // 使用节奏模式概率选择时值类型
            var rhythmIndex = SelectNoteByProbability(rhythmPattern, 4);

            // 基础时值选项（以ticks为单位，假设分辨率为480）
            var baseDurations = new[] { 120, 240, 480, 960 }; // 8分, 4分, 2分, 全音符

            long baseDuration = baseDurations[rhythmIndex];

            // 根据情绪调整时值
            double emotionFactor = emotion switch
            {
                Emotion.Happy => 0.8,      // 快乐：稍快的节奏
                Emotion.Sad => 1.3,        // 悲伤：较慢的节奏
                Emotion.Energetic => 0.7,  // 活力：很快的节奏
                Emotion.Calm => 1.2,       // 平静：较慢的节奏
                Emotion.Mysterious => 1.1, // 神秘：稍慢的节奏
                Emotion.Romantic => 1.0,   // 浪漫：正常节奏
                _ => 1.0
            };

            // 根据风格进一步调整
            double styleFactor = style switch
            {
                MusicStyle.Rock => 0.8,
                MusicStyle.Electronic => 0.7,
                MusicStyle.Classical => 1.2,
                MusicStyle.Blues => 0.9,
                _ => 1.0
            };

            return (long)(baseDuration * emotionFactor * styleFactor);
        }

        /// <summary>
        /// 应用旋律轮廓控制，避免不自然的跳跃
        /// </summary>
        /// <param name="currentNote">当前音符名称</param>
        /// <param name="currentOctave">当前八度</param>
        /// <param name="previousNotes">之前生成的音符列表</param>
        /// <param name="maxOctaveJump">最大允许的八度跳跃数</param>
        /// <returns>调整后的音符名称和八度</returns>
        /// <remarks>
        /// 此方法根据当前音符、当前八度、之前生成的音符列表和最大八度跳跃数，
        /// 应用旋律轮廓控制，避免不自然的跳跃。考虑了音符之间的间隔，
        /// 确保在音符接近音阶顶部或底部时，有机会升高或降低八度。
        /// 不同风格和音符索引会导致不同的旋律轮廓选择倾向，
        /// 增强旋律的变化性和动态效果。
        /// </remarks>
        private static (NoteName note, int octave) ApplyMelodicContour(NoteName currentNote, int currentOctave,
                                   List<NoteEvent> previousNotes, int maxOctaveJump)
        {
            if (previousNotes.Count == 0)
                return (currentNote, currentOctave);

            var lastNote = previousNotes.Last();
            var lastNoteValue = (int)lastNote.Note + lastNote.Octave * 12;
            var currentNoteValue = (int)currentNote + currentOctave * 12;

            // 如果跳跃太大，调整到合适的音符
            int jump = Math.Abs(currentNoteValue - lastNoteValue);
            if (jump > maxOctaveJump * 7) // 7个半音大约是一个五度
            {
                // 选择更接近的音符
                int direction = currentNoteValue > lastNoteValue ? -1 : 1;
                int adjustedValue = lastNoteValue + (maxOctaveJump * 3 * direction);

                // 转换回音符和八度
                currentOctave = adjustedValue / 12;
                currentNote = (NoteName)(adjustedValue % 12);
            }

            return (currentNote, currentOctave);
        }
        ///// <summary>
        ///// 生成单个音符
        ///// </summary>
        ///// <param name="parameters">旋律参数</param>
        ///// <param name="scaleNotes">音阶音符列表</param>
        ///// <param name="currentTime">当前时间</param>
        ///// <param name="previousNotes">之前生成的音符（用于上下文）</param>
        ///// <returns>新的音符事件</returns>
        //private NoteEvent GenerateNextNote(MelodyParameters parameters, List<NoteName> scaleNotes,
        //                                 long currentTime, List<NoteEvent> previousNotes)
        //{
        //    // 基于风格和情绪获取概率分布参数
        //    var styleParams = GetStyleParameters(parameters.Style, parameters.Emotion);

        //    // 从音阶中随机选择音符
        //    var noteIndex = _random.Next(scaleNotes.Count);
        //    var noteName = scaleNotes[noteIndex];

        //    // 确定音高八度（避免音域过宽）
        //    int octave = parameters.Octave;
        //    if (noteIndex >= scaleNotes.Count - 2) // 如果接近音阶顶部
        //    {
        //        // 30%的概率升高八度
        //        octave = _random.NextDouble() < 0.3 ? parameters.Octave + 1 : parameters.Octave;
        //    }

        //    // 根据风格和情绪确定音符时值
        //    var duration = GetNoteDuration(parameters.Style, parameters.Emotion);

        //    // 根据情绪确定音符力度
        //    var velocity = GetNoteVelocity(parameters.Emotion);

        //    return new NoteEvent
        //    {
        //        Note = noteName,
        //        Octave = octave,
        //        StartTime = currentTime,
        //        Duration = duration,
        //        Velocity = velocity
        //    };
        //}

        ///// <summary>
        ///// 根据风格和情绪获取音符时值
        ///// </summary>
        ///// <param name="style">音乐风格</param>
        ///// <param name="emotion">情绪</param>
        ///// <returns>时值（ticks）</returns>
        //private long GetNoteDuration(MusicStyle style, Emotion emotion)
        //{
        //    // 定义不同风格的基础时值选项（以ticks为单位，假设分辨率为480）
        //    var baseDuration = style switch
        //    {
        //        MusicStyle.Pop => [120, 240, 480],     // 8分, 4分, 2分音符
        //        MusicStyle.Rock => [120, 240],         // 较短的音符，强调节奏
        //        MusicStyle.Jazz => [120, 180, 240],    // 摇摆节奏
        //        MusicStyle.Classical => [240, 480, 960], // 较长的音符，流畅的旋律线
        //        MusicStyle.Electronic => [120, 240, 480], // 重复的节奏模式
        //        MusicStyle.Blues => [180, 240, 360],   // 三连音感觉
        //        _ => new[] { 240 }                             // 默认4分音符
        //    };

        //    // 从时值选项中随机选择一个
        //    return baseDuration[_random.Next(baseDuration.Length)];
        //}

        /// <summary>
        /// 根据情绪获取音符力度
        /// </summary>
        /// <param name="emotion">情绪</param>
        /// <returns>力度值（0-127）</returns>
        /// <remarks>
        /// 此方法根据情绪，随机生成音符的力度值。考虑了情绪的不同状态，
        /// 确保生成的音符力度符合音乐化的要求。
        /// 不同情绪会导致不同的力度选择倾向，
        /// 增强旋律的变化性和动态效果。
        /// </remarks>
        private static int GetNoteVelocity(Emotion emotion)
        {
            return emotion switch
            {
                Emotion.Happy => _random.Next(80, 110),        // 中等偏强的力度
                Emotion.Sad => _random.Next(50, 80),           // 柔和的力度
                Emotion.Energetic => _random.Next(90, 127),    // 很强的力度
                Emotion.Calm => _random.Next(40, 70),          // 很轻的力度
                Emotion.Mysterious => _random.Next(60, 90),    // 变化的力度
                Emotion.Romantic => _random.Next(70, 100),     // 中等力度
                _ => 80                                        // 默认力度
            };
        }

        /// <summary>
        /// 获取指定音阶的音符列表
        /// </summary>
        /// <param name="scale">音阶对象</param>
        /// <param name="octave">基础八度</param>
        /// <returns>音阶音符名称列表</returns>
        /// <remarks>
        /// 此方法根据音阶对象和基础八度，获取该音阶的音符名称列表。
        /// 考虑了音阶的根音、间隔和八度，确保生成的音符名称符合音乐化的要求。
        /// 不同音阶会导致不同的音符选择倾向，
        /// 增强旋律的变化性和动态效果。
        /// </remarks>
        private static List<NoteName> GetScaleNotes(Scale? scale, int octave)
        {
            if (scale == null) return [];
            var scaleNotes = new List<NoteName>();

            try
            {
                // 在 DryWetMidi 8.x 中，使用 GetSteps 和 NoteUtilities
                var intervals = scale.Intervals;
                var rootNoteNumber = NoteUtilities.GetNoteNumber(scale.RootNote, octave);

                foreach (var interval in intervals)
                {
                    var noteNumber = rootNoteNumber + interval.HalfSteps;
                    var noteName = NoteUtilities.GetNoteName((SevenBitNumber)noteNumber);
                    scaleNotes.Add(noteName);
                }

                // 添加高八度的音符以提供更多音高选择
                var nextOctaveRoot = NoteUtilities.GetNoteNumber(scale.RootNote, octave + 1);

                foreach (var interval in intervals)
                {
                    var noteNumber = nextOctaveRoot + interval.HalfSteps;
                    var noteName = NoteUtilities.GetNoteName((SevenBitNumber)noteNumber);
                    scaleNotes.Add(noteName);
                }

                // 还可以添加低八度的根音和五音，为旋律提供更多变化
                var prevOctaveRoot = NoteUtilities.GetNoteNumber(scale.RootNote, octave - 1);
                scaleNotes.Add(NoteUtilities.GetNoteName(prevOctaveRoot)); // 低八度根音
                scaleNotes.Add(NoteUtilities.GetNoteName((SevenBitNumber)(prevOctaveRoot + 7))); // 低八度五音
            }
            catch (Exception ex)
            {
                // 备用方案：手动构建常见音阶
                Console.WriteLine($"使用备用音阶构建方法: {ex.Message}");
                scaleNotes = GetScaleNotesFallback(scale, octave);
            }

            return [.. scaleNotes.Distinct()];
        }

        /// <summary>
        /// 备用音阶构建方法
        /// </summary>
        /// <param name="scale">音阶对象</param>
        /// <param name="octave">基础八度</param>
        /// <returns>构建的音阶音符列表</returns>
        /// <remarks>
        /// 此方法根据音阶对象和基础八度，手动构建该音阶的音符名称列表。
        /// 考虑了音阶的根音、间隔和八度，确保生成的音符名称符合音乐化的要求。
        /// 不同音阶会导致不同的音符选择倾向，
        /// 增强旋律的变化性和动态效果。
        /// </remarks>
        private static List<NoteName> GetScaleNotesFallback(Scale scale, int octave)
        {
            var scaleNotes = new List<NoteName>();
            var rootNote = scale.RootNote;

            // 根据音阶类型构建音符
            if (IsMajorScale(scale))
            {
                // 大调音阶: 全全半全全全半 (2,2,1,2,2,2,1)
                scaleNotes.Add(rootNote);
                scaleNotes.Add(GetNoteBySteps(rootNote, 2, octave));  // 大二度
                scaleNotes.Add(GetNoteBySteps(rootNote, 4, octave));  // 大三度
                scaleNotes.Add(GetNoteBySteps(rootNote, 5, octave));  // 纯四度
                scaleNotes.Add(GetNoteBySteps(rootNote, 7, octave));  // 纯五度
                scaleNotes.Add(GetNoteBySteps(rootNote, 9, octave));  // 大六度
                scaleNotes.Add(GetNoteBySteps(rootNote, 11, octave)); // 大七度

                // 添加高八度的音符以提供更多选择
                scaleNotes.Add(GetNoteBySteps(rootNote, 0, octave + 1));  // 高八度根音
                scaleNotes.Add(GetNoteBySteps(rootNote, 2, octave + 1));  // 高八度大二度
                scaleNotes.Add(GetNoteBySteps(rootNote, 4, octave + 1));  // 高八度大三度
            }
            else
            {
                // 默认使用小调音阶: 全半全全半全全 (2,1,2,2,1,2,2)
                scaleNotes.Add(rootNote);
                scaleNotes.Add(GetNoteBySteps(rootNote, 2, octave));  // 大二度
                scaleNotes.Add(GetNoteBySteps(rootNote, 3, octave));  // 小三度
                scaleNotes.Add(GetNoteBySteps(rootNote, 5, octave));  // 纯四度
                scaleNotes.Add(GetNoteBySteps(rootNote, 7, octave));  // 纯五度
                scaleNotes.Add(GetNoteBySteps(rootNote, 8, octave));  // 小六度
                scaleNotes.Add(GetNoteBySteps(rootNote, 10, octave)); // 小七度

                // 添加高八度的音符以提供更多选择
                scaleNotes.Add(GetNoteBySteps(rootNote, 0, octave + 1));  // 高八度根音
                scaleNotes.Add(GetNoteBySteps(rootNote, 2, octave + 1));  // 高八度大二度
                scaleNotes.Add(GetNoteBySteps(rootNote, 3, octave + 1));  // 高八度小三度
            }

            return scaleNotes;
        }

        /// <summary>
        /// 检查是否为大调音阶
        /// </summary>
        /// <param name="scale">要检查的音阶对象</param>
        /// <returns>如果是大调音阶返回true，否则返回false</returns>
        /// <remarks>
        /// 此方法根据音阶对象的音程模式，判断是否为大调音阶。
        /// 大调音阶的音程模式为：2,2,1,2,2,2,1。
        /// 不同音阶会导致不同的音程模式，
        /// 从而影响是否为大调音阶。
        /// </remarks>
        private static bool IsMajorScale(Scale scale)
        {
            // 通过检查音程模式来判断是否为大调音阶
            try
            {
                var intervals = scale.Intervals;
                // 大调音阶的音程模式: 2,2,1,2,2,2,1
                var majorPattern = new[] { 2, 2, 1, 2, 2, 2, 1 };
                if (intervals.Count() == majorPattern.Length)
                {
                    for (int i = 0; i < majorPattern.Length; i++)
                    {
                        if (intervals.ElementAt(i).HalfSteps != majorPattern[i])
                            return false;
                    }
                    return true;
                }
            }
            catch
            {
                // 如果无法判断，默认返回true（大调）
            }
            return true;
        }

        /// <summary>
        /// 根据半音步数获取音符
        /// </summary>
        /// <param name="root">根音音符</param>
        /// <param name="halfSteps">半音步数</param>
        /// <param name="octave">八度</param>
        /// <returns>计算得到的音符名称</returns>
        /// <remarks>
        /// 此方法根据根音音符、半音步数和八度，计算得到目标音符的名称。
        /// 考虑了音符的范围（0-127），确保计算结果符合音乐化的要求。
        /// 不同半音步数会导致不同的音符选择倾向，
        /// 增强旋律的变化性和动态效果。
        /// </remarks>
        private static NoteName GetNoteBySteps(NoteName root, int halfSteps, int octave)
        {
            try
            {
                // 使用 DryWetMidi 的 NoteUtilities 来计算音符
                var rootNumber = NoteUtilities.GetNoteNumber(root, octave);
                var targetNumber = rootNumber + halfSteps;
                return NoteUtilities.GetNoteName((SevenBitNumber)targetNumber);
            }
            catch
            {
                try
                {
                    // 备用方法：手动计算
                    var rootNumber = (int)root;
                    var targetNumber = (rootNumber + halfSteps) % 12;
                    return (NoteName)targetNumber;
                }
                catch
                {
                    return root;
                }
            }
        }
        /// <summary>
        /// 获取风格和情绪特定的参数
        /// </summary>
        /// <param name="style">音乐风格</param>
        /// <param name="emotion">情绪</param>
        /// <returns>音符概率和节奏模式</returns>
        /// <remarks>
        /// 此方法根据音乐风格和情绪组合，返回相应的音符概率分布和节奏模式。
        /// 不同的风格和情绪会导致不同的音乐化效果，
        /// 增强了音乐创作的灵活性和个性化。
        /// </remarks>
        private static (double[] noteProbabilities, double[] rhythmPattern, int maxOctaveJump) GetStyleParameters(
            MusicStyle style, Emotion emotion)
        {
            // 根据风格和情绪组合返回不同的参数
            return (style, emotion) switch
            {

                // 流行音乐 + 快乐情绪
                /// <remarks>
                /// 此风格和情绪组合倾向于生成 melodic 音乐，
                /// 强调中高音区的音符选择。
                /// 同时，增加了短音符的出现概率，
                /// 增强了音乐的动态效果。
                /// </remarks>
                (MusicStyle.Pop, Emotion.Happy) => (
                    [ 0.15, 0.25, 0.20, 0.15, 0.10, 0.08, 0.07 ], // 倾向于中音区
                    [ 0.4, 0.3, 0.2, 0.1 ], // 较多的短音符
                    2 // 适中的八度跳跃
                ),
                /// <remarks>
                /// 此风格和情绪组合倾向于生成 melodic 音乐，
                /// 强调低音区的音符选择。
                /// 同时，增加了长音符的出现概率，
                /// 增强了音乐的动态效果。
                /// </remarks>
                // 流行音乐 + 悲伤情绪
                (MusicStyle.Pop, Emotion.Sad) => (
                    [0.10, 0.15, 0.25, 0.20, 0.15, 0.10, 0.05], // 倾向于低音区
                    [0.2, 0.3, 0.3, 0.2], // 较多的长音符
                    1 // 较小的八度跳跃
                ),
                /// <remarks>
                /// 此风格和情绪组合倾向于生成 melodic 音乐，
                /// 强调中高音区的音符选择。
                /// 同时，增加了短音符的出现概率，
                /// 增强了音乐的动态效果。
                /// </remarks>
                // 摇滚音乐 + 活力情绪
                (MusicStyle.Rock, Emotion.Energetic) => (
                    [0.20, 0.25, 0.20, 0.15, 0.10, 0.05, 0.05], // 强烈的根音倾向
                    [0.5, 0.3, 0.15, 0.05], // 很多短音符
                    3 // 较大的八度跳跃
                ),
                /// <remarks>
                /// 此风格和情绪组合倾向于生成 melodic 音乐，
                /// 强调低音区的音符选择。
                /// 同时，增加了长音符的出现概率，
                /// 增强了音乐的动态效果。
                /// </remarks>
                // 爵士音乐
                (MusicStyle.Jazz, _) => (
                    [0.10, 0.15, 0.15, 0.15, 0.15, 0.15, 0.15], // 均匀分布
                    [0.25, 0.25, 0.25, 0.25], // 复杂的节奏
                    2
                ),
                /// <remarks>
                /// 此风格和情绪组合倾向于生成 melodic 音乐，
                /// 强调低音区的音符选择。
                /// 同时，增加了长音符的出现概率，
                /// 增强了音乐的动态效果。
                /// </remarks>
                // 古典音乐 + 浪漫情绪
                (MusicStyle.Classical, Emotion.Romantic) => (
                    [0.08, 0.12, 0.20, 0.20, 0.18, 0.12, 0.10], // 流畅的旋律线
                    [0.1, 0.2, 0.4, 0.3], // 较多的长音符
                    1
                ),
                /// <remarks>
                /// 此风格和情绪组合倾向于生成 melodic 音乐，
                /// 强调低音区的音符选择。
                /// 同时，增加了长音符的出现概率，
                /// 增强了音乐的动态效果。
                /// </remarks>
                // 电子音乐
                (MusicStyle.Electronic, _) => (
                    [0.25, 0.20, 0.15, 0.15, 0.10, 0.10, 0.05], // 重复的模式
                    [0.6, 0.2, 0.15, 0.05], // 非常节奏化
                    2
                ),
                /// <remarks>
                /// 此风格和情绪组合倾向于生成 melodic 音乐，
                /// 强调低音区的音符选择。
                /// 同时，增加了长音符的出现概率，
                /// 增强了音乐的动态效果。
                /// </remarks>
                // 布鲁斯音乐
                (MusicStyle.Blues, _) => (
                    [0.20, 0.15, 0.15, 0.15, 0.15, 0.10, 0.10], // 布鲁斯音阶特点
                    [0.3, 0.4, 0.2, 0.1], // 摇摆节奏
                    2
                ),
                /// <remarks>
                /// 此风格和情绪组合倾向于生成 melodic 音乐，
                /// 强调低音区的音符选择。
                /// 同时，增加了长音符的出现概率，
                /// 增强了音乐的动态效果。
                /// </remarks>
                // 默认配置
                _ => (
                    [0.15, 0.20, 0.18, 0.15, 0.12, 0.10, 0.10],
                    [0.3, 0.3, 0.2, 0.2],
                    2
                )
            };
        }
    }
}
