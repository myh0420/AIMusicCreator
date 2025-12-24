using AIMusicCreator.Entity;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.MusicTheory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIMusicCreator.Entity
{
    /// <summary>
    /// 音阶工厂类
    /// 提供创建各种常见音阶的静态方法
    /// 封装了音阶构成的复杂逻辑，简化音阶创建过程
    /// </summary>
    public static class ScaleFactory
    {
        /// <summary>
        /// 音阶类型枚举
        /// 定义支持的音阶类型，用于指定要创建的音阶种类
        /// </summary>
        public enum ScaleType
        {
            /// <summary>
            /// 大调音阶 - 明亮、欢快的音色
            /// 音程结构：全音-全音-半音-全音-全音-全音-半音
            /// 常用于流行、古典、摇滚音乐
            /// </summary>
            Major,

            /// <summary>
            /// 小调音阶 - 悲伤、深沉的音色  
            /// 音程结构：全音-半音-全音-全音-半音-全音-全音
            /// 常用于悲伤、神秘情绪的音乐
            /// </summary>
            Minor,

            /// <summary>
            /// 五声音阶 - 简单、和谐的音色
            /// 音程结构：全音-全音-小三度-全音-小三度
            /// 常用于民谣、世界音乐、摇滚乐
            /// </summary>
            Pentatonic,

            /// <summary>
            /// 蓝调音阶 - 富有表现力、情感丰富的音色
            /// 音程结构：小三度-全音-半音-半音-小三度-全音
            /// 包含蓝调特有的降三、降五、降七音
            /// 常用于布鲁斯、爵士、摇滚音乐
            /// </summary>
            Blues,

            /// <summary>
            /// 和声小调音阶 - 具有东方色彩的音色
            /// 音程结构：全音-半音-全音-全音-半音-增二度-半音
            /// 小调音阶的变体，第六音和第七音之间为增二度
            /// 常用于古典音乐、弗拉门戈音乐
            /// </summary>
            HarmonicMinor,

            /// <summary>
            /// 旋律小调音阶 - 流畅的旋律线条
            /// 上行：全音-半音-全音-全音-全音-全音-半音
            /// 下行：全音-全音-半音-全音-全音-半音-全音
            /// 常用于爵士乐、即兴演奏
            /// </summary>
            MelodicMinor,

            /// <summary>
            /// 多利亚调式 - 明亮的小调音色
            /// 音程结构：全音-半音-全音-全音-全音-半音-全音
            /// 常用于爵士、放克、摇滚音乐
            /// </summary>
            Dorian,

            /// <summary>
            /// 混合利底亚调式 - 属七和弦的感觉
            /// 音程结构：全音-全音-半音-全音-全音-半音-全音
            /// 常用于布鲁斯、摇滚、爵士音乐
            /// </summary>
            Mixolydian,
            Phrygian,
            Lydian,
            Aeolian,
            Locrian,
            PentatonicMajor,
            PentatonicMinor
        }

        /// <summary>
        /// 创建指定类型和根音的音阶
        /// </summary>
        /// <param name="rootNote">根音音符名称，决定音阶的调性</param>
        /// <param name="scaleType">音阶类型，指定要创建的音阶种类</param>
        /// <returns>构建完成的音阶对象</returns>
        /// <example>
        /// // 创建C大调音阶
        /// var cMajorScale = ScaleFactory.CreateScale(NoteName.C, ScaleType.Major);
        /// 
        /// // 创建A小调音阶  
        /// var aMinorScale = ScaleFactory.CreateScale(NoteName.A, ScaleType.Minor);
        /// </example>
        public static Scale CreateScale(NoteName rootNote, ScaleType scaleType)
        {
            return scaleType switch
            {
                ScaleType.Major => CreateMajorScale(rootNote),
                ScaleType.Minor => CreateMinorScale(rootNote),
                ScaleType.Pentatonic => CreatePentatonicScale(rootNote),
                ScaleType.Blues => CreateBluesScale(rootNote),
                ScaleType.HarmonicMinor => CreateHarmonicMinorScale(rootNote),
                ScaleType.MelodicMinor => CreateMelodicMinorScale(rootNote),
                ScaleType.Dorian => CreateDorianScale(rootNote),
                ScaleType.Mixolydian => CreateMixolydianScale(rootNote),
                _ => CreateMajorScale(rootNote) // 默认回退到大调音阶
            };
        }

        /// <summary>
        /// 创建大调音阶
        /// 音程结构：全音(2)-全音(2)-半音(1)-全音(2)-全音(2)-全音(2)-半音(1)
        /// 特点：明亮、欢快，是最常用的音阶
        /// </summary>
        /// <param name="rootNote">根音音符</param>
        /// <returns>大调音阶对象</returns>
        private static Scale CreateMajorScale(NoteName rootNote)
        {
            var intervals = new[]
            {
            Interval.GetUp((SevenBitNumber)2), // 大二度 - 根音到第二音
            Interval.GetUp((SevenBitNumber)2), // 大二度 - 第二音到第三音  
            Interval.GetUp((SevenBitNumber)1), // 小二度 - 第三音到第四音
            Interval.GetUp((SevenBitNumber)2), // 大二度 - 第四音到第五音
            Interval.GetUp((SevenBitNumber)2), // 大二度 - 第五音到第六音
            Interval.GetUp((SevenBitNumber)2), // 大二度 - 第六音到第七音
            Interval.GetUp((SevenBitNumber)1)  // 小二度 - 第七音到八度音
        };
            return new Scale(intervals, rootNote);
        }

        /// <summary>
        /// 创建自然小调音阶
        /// 音程结构：全音(2)-半音(1)-全音(2)-全音(2)-半音(1)-全音(2)-全音(2)
        /// 特点：悲伤、深沉，常用于表达忧郁情绪
        /// </summary>
        /// <param name="rootNote">根音音符</param>
        /// <returns>小调音阶对象</returns>
        private static Scale CreateMinorScale(NoteName rootNote)
        {
            var intervals = new[]
            {
            Interval.GetUp((SevenBitNumber)2), // 大二度 - 根音到第二音
            Interval.GetUp((SevenBitNumber)1), // 小二度 - 第二音到第三音
            Interval.GetUp((SevenBitNumber)2), // 大二度 - 第三音到第四音
            Interval.GetUp((SevenBitNumber)2), // 大二度 - 第四音到第五音
            Interval.GetUp((SevenBitNumber)1), // 小二度 - 第五音到第六音
            Interval.GetUp((SevenBitNumber)2), // 大二度 - 第六音到第七音
            Interval.GetUp((SevenBitNumber)2)  // 大二度 - 第七音到八度音
        };
            return new Scale(intervals, rootNote);
        }

        /// <summary>
        /// 创建五声音阶（大调五声）
        /// 音程结构：全音(2)-全音(2)-小三度(3)-全音(2)-小三度(3)
        /// 特点：简单和谐，没有半音关系，避免不和谐音程
        /// 广泛应用于世界各地民间音乐
        /// </summary>
        /// <param name="rootNote">根音音符</param>
        /// <returns>五声音阶对象</returns>
        private static Scale CreatePentatonicScale(NoteName rootNote)
        {
            var intervals = new[]
            {
            Interval.GetUp((SevenBitNumber)2), // 大二度 - 根音到第二音
            Interval.GetUp((SevenBitNumber)2), // 大二度 - 第二音到第三音
            Interval.GetUp((SevenBitNumber)3), // 小三度 - 第三音到第四音
            Interval.GetUp((SevenBitNumber)2), // 大二度 - 第四音到第五音
            Interval.GetUp((SevenBitNumber)3)  // 小三度 - 第五音到六度音（实际上是八度）
        };
            return new Scale(intervals, rootNote);
        }

        /// <summary>
        /// 创建蓝调音阶
        /// 音程结构：小三度(3)-全音(2)-半音(1)-半音(1)-小三度(3)-全音(2)
        /// 特点：包含蓝调音符（降三、降五、降七音），富有表现力
        /// 是布鲁斯音乐的核心音阶
        /// </summary>
        /// <param name="rootNote">根音音符</param>
        /// <returns>蓝调音阶对象</returns>
        private static Scale CreateBluesScale(NoteName rootNote)
        {
            var intervals = new[]
            {
            Interval.GetUp((SevenBitNumber)3), // 小三度 - 根音到降三音（蓝调音符）
            Interval.GetUp((SevenBitNumber)2), // 大二度 - 降三音到第四音
            Interval.GetUp((SevenBitNumber)1), // 小二度 - 第四音到降五音（蓝调音符）
            Interval.GetUp((SevenBitNumber)1), // 小二度 - 降五音到第五音
            Interval.GetUp((SevenBitNumber)3), // 小三度 - 第五音到降七音（蓝调音符）
            Interval.GetUp((SevenBitNumber)2)  // 大二度 - 降七音到八度音
        };
            return new Scale(intervals, rootNote);
        }

        /// <summary>
        /// 创建和声小调音阶
        /// 音程结构：全音(2)-半音(1)-全音(2)-全音(2)-半音(1)-增二度(3)-半音(1)
        /// 特点：第七音升高，形成第六音和第七音之间的增二度
        /// 具有东方色彩，常用于古典音乐
        /// </summary>
        /// <param name="rootNote">根音音符</param>
        /// <returns>和声小调音阶对象</returns>
        private static Scale CreateHarmonicMinorScale(NoteName rootNote)
        {
            var intervals = new[]
            {
            Interval.GetUp((SevenBitNumber)2), // 大二度
            Interval.GetUp((SevenBitNumber)1), // 小二度
            Interval.GetUp((SevenBitNumber)2), // 大二度
            Interval.GetUp((SevenBitNumber)2), // 大二度
            Interval.GetUp((SevenBitNumber)1), // 小二度
            Interval.GetUp((SevenBitNumber)3), // 增二度 - 和声小调的特征音程
            Interval.GetUp((SevenBitNumber)1)  // 小二度
        };
            return new Scale(intervals, rootNote);
        }

        /// <summary>
        /// 创建旋律小调音阶（上行）
        /// 音程结构：全音(2)-半音(1)-全音(2)-全音(2)-全音(2)-全音(2)-半音(1)
        /// 特点：上行时第六音和第七音都升高，下行时还原
        /// 提供流畅的旋律线条，常用于爵士乐
        /// </summary>
        /// <param name="rootNote">根音音符</param>
        /// <returns>旋律小调音阶对象</returns>
        private static Scale CreateMelodicMinorScale(NoteName rootNote)
        {
            // 注意：这里只实现上行形式，实际使用中下行应还原第六、七音
            var intervals = new[]
            {
            Interval.GetUp((Melanchall.DryWetMidi.Common.SevenBitNumber)2), // 大二度
            Interval.GetUp((SevenBitNumber)1), // 小二度
            Interval.GetUp((SevenBitNumber)2), // 大二度
            Interval.GetUp((SevenBitNumber)2), // 大二度
            Interval.GetUp((SevenBitNumber)2), // 大二度 - 升高的第六音
            Interval.GetUp((SevenBitNumber)2), // 大二度 - 升高的第七音
            Interval.GetUp((SevenBitNumber)1)  // 小二度
        };
            return new Scale(intervals, rootNote);
        }

        /// <summary>
        /// 创建多利亚调式
        /// 音程结构：全音(2)-半音(1)-全音(2)-全音(2)-全音(2)-半音(1)-全音(2)
        /// 特点：自然小调升高第六音，具有明亮的小调色彩
        /// 常用于爵士、放克音乐
        /// </summary>
        /// <param name="rootNote">根音音符</param>
        /// <returns>多利亚调式音阶对象</returns>
        private static Scale CreateDorianScale(NoteName rootNote)
        {
            var intervals = new[]
            {
            Interval.GetUp((SevenBitNumber)2), // 大二度
            Interval.GetUp((SevenBitNumber)1), // 小二度
            Interval.GetUp((SevenBitNumber)2), // 大二度
            Interval.GetUp((SevenBitNumber)2), // 大二度
            Interval.GetUp((SevenBitNumber)2), // 大二度 - 升高的第六音（多利亚特征）
            Interval.GetUp((SevenBitNumber)1), // 小二度
            Interval.GetUp((SevenBitNumber)2)  // 大二度
        };
            return new Scale(intervals, rootNote); ;
        }

        /// <summary>
        /// 创建混合利底亚调式
        /// 音程结构：全音(2)-全音(2)-半音(1)-全音(2)-全音(2)-半音(1)-全音(2)
        /// 特点：大调音阶降第七音，具有属七和弦的感觉
        /// 常用于布鲁斯、摇滚音乐
        /// </summary>
        /// <param name="rootNote">根音音符</param>
        /// <returns>混合利底亚调式音阶对象</returns>
        private static Scale CreateMixolydianScale(NoteName rootNote)
        {
            var intervals = new[]
            {
            Interval.GetUp((SevenBitNumber)2), // 大二度
            Interval.GetUp((SevenBitNumber)2), // 大二度
            Interval.GetUp((SevenBitNumber)1), // 小二度
            Interval.GetUp((SevenBitNumber)2), // 大二度
            Interval.GetUp((SevenBitNumber)2), // 大二度
            Interval.GetUp((SevenBitNumber)1), // 小二度 - 降低的第七音（混合利底亚特征）
            Interval.GetUp((SevenBitNumber)2)  // 大二度
        };
            return new Scale(intervals, rootNote);
        }

        /// <summary>
        /// 根据音乐风格推荐合适的音阶类型
        /// </summary>
        /// <param name="style">音乐风格</param>
        /// <param name="emotion">情绪表达</param>
        /// <returns>推荐的音阶类型</returns>
        public static ScaleType RecommendScaleType(MusicStyle style, Emotion emotion)
        {
            return (style, emotion) switch
            {
                (MusicStyle.Pop, Emotion.Happy) => ScaleType.Major,
                (MusicStyle.Pop, Emotion.Sad) => ScaleType.Minor,
                (MusicStyle.Rock, _) => ScaleType.Mixolydian,
                (MusicStyle.Blues, _) => ScaleType.Blues,
                (MusicStyle.Jazz, _) => ScaleType.MelodicMinor,
                (MusicStyle.Classical, Emotion.Romantic) => ScaleType.Major,
                (MusicStyle.Classical, Emotion.Sad) => ScaleType.HarmonicMinor,
                (MusicStyle.Electronic, _) => ScaleType.Pentatonic,
                (_, Emotion.Mysterious) => ScaleType.Dorian,
                _ => ScaleType.Major
            };
        }
    }
}
