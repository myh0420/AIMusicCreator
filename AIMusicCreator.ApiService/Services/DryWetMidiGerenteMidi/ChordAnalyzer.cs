using AIMusicCreator.Entity;
using AIMusicCreator.Utils;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.MusicTheory;
using System.Collections.Generic;
using System.Linq;
using Chord = AIMusicCreator.Entity.Chord;
using ChordProgression = AIMusicCreator.Entity.ChordProgression;

namespace AIMusicCreator.ApiService.Services.DryWetMidiGerenteMidi
{
    /// <summary>
    /// 和弦分析器类
    /// 分析旋律音符并生成合适的和弦进行
    /// </summary>
    /// <remarks>
    /// 该类负责根据给定的旋律和音阶，分析并生成适合的和弦进行。
    /// 主要功能包括：分析旋律小节、计算音符权重、构建和弦等。
    /// 使用和声理论中的I-IV-V进行作为基础和声框架。
    /// </remarks>
    public class ChordAnalyzer
    {
        /// <summary>
        /// 分析旋律并生成和弦进行
        /// </summary>
        /// <param name="melody">旋律音符列表</param>
        /// <param name="scale">使用的音阶</param>
        /// <param name="bars">小节数量</param>
        /// <returns>和弦进行</returns>
        public ChordProgression AnalyzeChords(List<NoteEvent> melody, Scale scale, int bars)
        {
            var chords = new ChordProgression();
            var chordEvents = new List<Chord>();
            var durations = new List<int>();

            // 将旋律分割成小节进行分析
            int notesPerBar = melody.Count / bars;

            for (int i = 0; i < bars; i++)
            {
                // 获取当前小节的音符
                var barNotes = melody.Skip(i * notesPerBar).Take(notesPerBar).ToList();
                // 分析小节内的和弦
                var chord = AnalyzeBarChords(barNotes, scale);
                chordEvents.Add(chord);
                durations.Add(4); // 默认每个和弦持续一小节（4个四分音符）
            }

            chords.Chords = chordEvents;
            chords.Durations = durations;

            return chords;
        }

        ///// <summary>
        ///// 分析单个小节的和弦
        ///// </summary>
        ///// <param name="barNotes">小节内的音符</param>
        ///// <param name="scale">音阶</param>
        ///// <returns>最适合的和弦</returns>
        //private Chord AnalyzeBarChords(List<NoteEvent> barNotes, Scale scale)
        //{
        //    // 按音阶音符分组并统计出现次数
        //    var noteGroups = barNotes.GroupBy(n => GetNoteInScale(n.Note, scale))
        //                            .OrderByDescending(g => g.Count());

        //    // 选择出现次数最多的音符作为和弦基础
        //    var mostCommonNote = noteGroups.First().Key;

        //    // 基于音阶构建和弦
        //    return BuildChordFromScaleDegree(mostCommonNote, scale);
        //}
        /// <summary>
        /// 分析单个小节的和弦
        /// </summary>
        /// <param name="barNotes">小节内的音符列表</param>
        /// <param name="scale">使用的音阶</param>
        /// <returns>分析得出的最适合该小节的和弦</returns>
        private static Chord AnalyzeBarChords(List<NoteEvent> barNotes, Scale scale)
        {
            
            if (barNotes.Count == 0)
                return BuildChordFromScaleDegree(scale.GetNotesNames().First(), scale);

            // 统计音符出现频率和时长
            var noteWeights = new Dictionary<NoteName, double>();

            foreach (var note in barNotes)
            {
                var scaleNote = GetNoteInScale(note.Note, scale);
                var noteValue = (int)scaleNote + note.Octave * 12;

                if (!noteWeights.ContainsKey(scaleNote))
                    noteWeights[scaleNote] = 0;

                // 权重考虑音符时长和力度
                noteWeights[scaleNote] += note.Duration * (note.Velocity / 127.0);
            }

            // 选择权重最高的音符作为和弦基础
            var mostImportantNote = noteWeights.OrderByDescending(kv => kv.Value).First().Key;

            // 考虑和弦进行的逻辑（简单的I-IV-V-I进行）
            return BuildAppropriateChord(mostImportantNote, scale, barNotes);
        }

        /// <summary>
        /// 构建合适的和弦，考虑和声进行
        /// </summary>
        /// <param name="degree">基础音阶级数</param>
        /// <param name="scale">使用的音阶</param>
        /// <param name="barNotes">小节内的音符列表</param>
        /// <returns>构建的和弦对象</returns>
        /// <summary>
        /// 构建合适的和弦，考虑和声进行
        /// </summary>
        private static Chord BuildAppropriateChord(NoteName degree, Scale scale, List<NoteEvent> barNotes)
        {
            var scaleNotes = MidiUtils.GetScaleNoteNames(scale);
            var degreeIndex = scaleNotes.IndexOf(degree);

            // 使用barNotes信息来分析和弦选择
            var barNoteWeights = AnalyzeBarNoteWeights(barNotes, scale);

            // 简单的和声进行逻辑：倾向于使用I, IV, V级和弦
            int[] preferredDegrees = [0, 3, 4]; // I, IV, V

            // 找到最接近的优选级数，但考虑barNotes中的音符权重
            int closestPreferred = preferredDegrees
                .OrderBy(p => CalculateChordFitness(p, barNoteWeights, scaleNotes))
                .First();

            var root = scaleNotes[closestPreferred];

            // 构建三和弦
            var third = scaleNotes[(closestPreferred + 2) % scaleNotes.Count];
            var fifth = scaleNotes[(closestPreferred + 4) % scaleNotes.Count];

            return new Chord(root, third, fifth);
        }

        /// <summary>
        /// 分析小节内音符的权重
        /// </summary>
        /// <param name="barNotes">小节内的音符列表</param>
        /// <param name="scale">使用的音阶</param>
        /// <returns>音符名称及其对应的权重字典</returns>
        private static Dictionary<NoteName, double> AnalyzeBarNoteWeights(List<NoteEvent> barNotes, Scale scale)
        {
            var weights = new Dictionary<NoteName, double>();

            foreach (var note in barNotes)
            {
                var scaleNote = GetNoteInScale(note.Note, scale);
                if (!weights.ContainsKey(scaleNote))
                    weights[scaleNote] = 0;

                // 权重考虑音符时长和力度
                weights[scaleNote] += note.Duration * (note.Velocity / 127.0);
            }

            return weights;
        }

        /// <summary>
        /// 计算和弦与音符权重的匹配度
        /// </summary>
        /// <param name="degree">和弦根音在音阶中的度数</param>
        /// <param name="barNoteWeights">小节内音符的权重字典</param>
        /// <param name="scaleNotes">音阶中的音符列表</param>
        /// <returns>匹配度分数（值越小表示匹配度越高）</returns>
        private static double CalculateChordFitness(int degree, Dictionary<NoteName, double> barNoteWeights, List<NoteName> scaleNotes)
        {
            double fitness = 0;
            var chordNotes = new List<NoteName>
            {
                scaleNotes[degree],           // 根音
                scaleNotes[(degree + 2) % scaleNotes.Count], // 三音
                scaleNotes[(degree + 4) % scaleNotes.Count]  // 五音
            };

            // 计算和弦音符在barNotes中的总权重
            foreach (var chordNote in chordNotes)
            {
                if (barNoteWeights.TryGetValue(chordNote, out double value))
                {
                    fitness += value;
                }
            }

            // 倾向于使用I, IV, V级和弦
            int[] preferredDegrees = [0, 3, 4];
            if (preferredDegrees.Contains(degree))
            {
                fitness += 0.5; // 给优选级数额外加分
            }

            return -fitness; // 返回负值以便OrderBy升序排列
        }
        //private Chord BuildAppropriateChord(NoteName degree, Scale scale, List<NoteEvent> barNotes)
        //{
        //    var scaleNotes = scale.GetNotesNames().ToList();
        //    var degreeIndex = scaleNotes.IndexOf(degree);

        //    // 简单的和声进行逻辑：倾向于使用I, IV, V级和弦
        //    int[] preferredDegrees = [0, 3, 4]; // I, IV, V

        //    // 找到最接近的优选级数
        //    int closestPreferred = preferredDegrees
        //        .OrderBy(p => Math.Abs(p - degreeIndex))
        //        .First();

        //    var root = scaleNotes[closestPreferred];

        //    // 构建三和弦
        //    var third = scaleNotes[(closestPreferred + 2) % scaleNotes.Count];
        //    var fifth = scaleNotes[(closestPreferred + 4) % scaleNotes.Count];

        //    return new Chord(root, third, fifth);
        //}
        /// <summary>
        /// 根据音阶级数构建和弦
        /// </summary>
        /// <param name="degree">音阶级数</param>
        /// <param name="scale">音阶</param>
        /// <returns>构建的和弦</returns>
        private static Chord BuildChordFromScaleDegree(NoteName degree, Scale scale)
        {
            var scaleNotes = MidiUtils.GetScaleNoteNames(scale);
            var degreeIndex = scaleNotes.IndexOf(degree);

            if (degreeIndex == -1)
            {
                // 如果找不到度数，使用根音
                degreeIndex = 0;
                degree = scaleNotes[0];
            }

            // 构建三和弦：根音、三音、五音
            var root = degree;
            var third = scaleNotes[(degreeIndex + 2) % scaleNotes.Count];
            var fifth = scaleNotes[(degreeIndex + 4) % scaleNotes.Count];

            return new Chord(root, third, fifth);
        }

        /// <summary>
        /// 将音符映射到音阶内最接近的音符
        /// </summary>
        /// <param name="note">原始音符</param>
        /// <param name="scale">音阶</param>
        /// <returns>音阶内的最接近音符</returns>
        private static NoteName GetNoteInScale(NoteName note, Scale scale)
        {
            var scaleNotes = MidiUtils.GetScaleNoteNames(scale);

            // 如果音符已经在音阶中，直接返回
            if (scaleNotes.Contains(note))
                return note;

            // 否则找到最接近的音阶音符
            var noteValue = (int)note;
            var closestNote = scaleNotes
                .OrderBy(scaleNote =>
                {
                    var scaleNoteValue = (int)scaleNote;
                    var diff = Math.Abs(scaleNoteValue - noteValue);
                    // 处理环绕情况（比如 C 和 B）
                    return Math.Min(diff, 12 - diff);
                })
                .First();

            return closestNote;
        }
    }

}
