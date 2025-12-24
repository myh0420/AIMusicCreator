using AIMusicCreator.Entity;
using Melanchall.DryWetMidi.MusicTheory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIMusicCreator.Utils
{
    /// <summary>
    /// 旋律参数配置类
    /// </summary>
    /// <remarks>
    /// MelodyParameters类是AI音乐生成系统的核心配置类，用于管理生成旋律所需的所有参数设置。
    /// 该类设计采用了智能默认值系统，当某些关键参数未设置时，会根据音乐风格(Style)、情绪(Emotion)
    /// 和速度(BPM)自动匹配合适的值，从而简化了用户配置过程。
    /// 
    /// 该类管理的关键参数包括：音乐风格、情绪表达、速度、小节数量、使用的音阶和基准八度等。
    /// 通过精心设计的智能默认值算法，系统可以根据不同音乐风格和情绪生成合适的旋律参数组合，
    /// 为AI音乐创作提供了灵活而强大的配置基础。</remarks>
    public class MelodyParameters1
    {
        /// <summary>
        /// 存储用户自定义的小节数量
        /// </summary>
        /// <remarks>使用可空类型以区分用户是否显式设置了此值。</remarks>
        private int? _bars;
        
        /// <summary>
        /// 存储用户自定义的音阶
        /// </summary>
        /// <remarks>使用可空类型以区分用户是否显式设置了此值。</remarks>
        private Scale? _scale;
        
        /// <summary>
        /// 存储用户自定义的八度
        /// </summary>
        /// <remarks>使用可空类型以区分用户是否显式设置了此值。</remarks>
        private int? _octave;

        /// <summary>
        /// 音乐风格
        /// </summary>
        /// <value>定义生成旋律的音乐风格类型</value>
        /// <remarks>
        /// 音乐风格是影响旋律生成的关键因素，不同风格有不同的结构特征、和弦进行和节奏模式。
        /// 默认值为Pop(流行音乐)，表示生成具有现代流行音乐特征的旋律。
        /// 支持的风格包括Pop、Rock、Jazz、Classical、Electronic和Blues等。</remarks>
        public MusicStyle Style { get; set; } = MusicStyle.Pop;

        /// <summary>
        /// 情绪表达
        /// </summary>
        /// <value>定义生成旋律要表达的情绪类型</value>
        /// <remarks>
        /// 情绪参数直接影响旋律的调性、音域和节奏变化。系统会根据情绪选择合适的根音、
        /// 音阶类型和旋律起伏模式。默认值为Happy(快乐)，表示生成明亮欢快的旋律。
        /// 支持的情绪包括Happy、Sad、Energetic、Calm、Mysterious和Romantic等。</remarks>
        public Emotion Emotion { get; set; } = Emotion.Happy;

        /// <summary>
        /// 速度（每分钟节拍数）
        /// </summary>
        /// <value>定义音乐的播放速度，单位为BPM(Beats Per Minute)</value>
        /// <remarks>
        /// 速度参数影响旋律的节奏密度和音符时值。默认值为120 BPM，属于中等速度。
        /// 速度值通常范围：慢板(60-80 BPM)、行板(80-100 BPM)、中板(100-120 BPM)、
        /// 快板(120-140 BPM)和急板(140 BPM以上)。</remarks>
        public int BPM { get; set; } = 120;
        
        /// <summary>
        /// 速度（每分钟节拍数）- BPM的别名
        /// </summary>
        /// <value>与BPM相同的值，表示音乐的播放速度</value>
        /// <remarks>为兼容代码而添加的属性，与BPM保持同步</remarks>
        public int Tempo
        {
            get { return BPM; }
            set { BPM = value; }
        }

        /// <summary>
        /// 小节数量
        /// </summary>
        /// <value>定义生成旋律的小节总数</value>
        /// <remarks>
        /// 小节数量决定了旋律的长度和结构复杂度。当用户未显式设置时，系统会根据Style和Emotion
        /// 自动计算合适的值。例如，流行音乐通常为8小节，爵士和古典音乐为16小节，
        /// 而电子音乐可能只有4小节。</remarks>
        public int Bars
        {
            get => _bars ?? CalculateDefaultBars();
            set => _bars = value;
        }

        /// <summary>
        /// 使用的音阶
        /// </summary>
        /// <value>定义生成旋律时使用的音阶系统</value>
        /// <remarks>
        /// 音阶决定了旋律中可用的音符集合和它们之间的关系。当用户未显式设置时，
        /// 系统会根据Style和Emotion自动选择合适的根音和音阶类型。例如，Happy情绪倾向使用大调，
        /// Sad情绪倾向使用小调，Rock风格常使用Mixolydian调式等。</remarks>
        public Scale Scale
        {
            get => _scale ?? CreateDefaultScale();
            set => _scale = value;
        }

        /// <summary>
        /// 基准八度
        /// </summary>
        /// <value>定义旋律的主要音域范围</value>
        /// <remarks>
        /// 八度参数控制旋律的整体音高范围。当用户未显式设置时，系统会根据Style和BPM
        /// 自动计算合适的值。例如，慢速音乐通常使用较低的八度(3或4)，快速音乐
        /// 可能使用较高的八度(4或5)，而流行音乐和电子音乐倾向于使用稍高的音域。</remarks>
        public int Octave
        {
            get => _octave ?? CalculateDefaultOctave();
            set => _octave = value;
        }

        /// <summary>
        /// 检查是否有用户自定义的小节数量
        /// </summary>
        /// <value>如果用户显式设置了小节数量则为true，否则为false</value>
        public bool HasCustomBars => _bars.HasValue;
        
        /// <summary>
        /// 检查是否有用户自定义的音阶
        /// </summary>
        /// <value>如果用户显式设置了音阶则为true，否则为false</value>
        public bool HasCustomScale => _scale != null;
        
        /// <summary>
        /// 检查是否有用户自定义的八度
        /// </summary>
        /// <value>如果用户显式设置了八度则为true，否则为false</value>
        public bool HasCustomOctave => _octave.HasValue;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        /// <remarks>
        /// 创建具有默认参数值的MelodyParameters实例：
        /// - Style = MusicStyle.Pop
        /// - Emotion = Emotion.Happy
        /// - BPM = 120
        /// 其他参数将在首次访问时根据默认值规则自动计算。
        /// 此构造函数适用于快速创建具有通用设置的参数对象，然后可以按需修改特定属性。</remarks>
        public MelodyParameters1() { }

        /// <summary>
        /// 带参数的构造函数
        /// </summary>
        /// <param name="style">音乐风格</param>
        /// <param name="emotion">情绪表达</param>
        /// <param name="bpm">速度（BPM）</param>
        /// <param name="bars">小节数量（可选）</param>
        /// <param name="scale">使用的音阶（可选）</param>
        /// <param name="octave">基准八度（可选）</param>
        /// <remarks>
        /// 创建具有指定参数的MelodyParameters实例。必须提供style、emotion和bpm这三个基本参数，
        /// 其他参数可选。当可选参数为null时，将在首次访问时根据默认值规则自动计算。
        /// 此构造函数适用于需要精确控制多个参数的场景，提供了最大的灵活性。</remarks>
        public MelodyParameters1(MusicStyle style, Emotion emotion, int bpm,
                              int? bars = null, Scale? scale = null, int? octave = null)
        {
            Style = style;
            Emotion = emotion;
            BPM = bpm;
            _bars = bars;
            _scale = scale;
            _octave = octave;
        }

        /// <summary>
        /// 计算默认的小节数量
        /// </summary>
        /// <returns>根据当前Style和Emotion计算出的合适小节数量</returns>
        /// <remarks>
        /// 根据音乐风格和情绪组合，智能计算最适合的小节数量。使用模式匹配(switch表达式)
        /// 实现了丰富的映射规则：
        /// - 流行音乐：标准的8小节结构
        /// - 摇滚音乐：根据情绪不同，使用8或12小节
        /// - 爵士音乐：复杂的16小节结构
        /// - 古典音乐：较长的16小节结构
        /// - 电子音乐：简洁的4小节结构（适合循环使用）
        /// - 布鲁斯：标准的12小节结构
        /// 
        /// 此外，还针对不同情绪定义了小节数量的微调规则，确保生成的旋律结构与情绪表达相匹配。</remarks>
        private int CalculateDefaultBars()
        {
            return (Style, Emotion) switch
            {
                // 流行音乐：标准结构
                (MusicStyle.Pop, _) => 8,

                // 摇滚音乐：较长的段落
                (MusicStyle.Rock, Emotion.Energetic) => 12,
                (MusicStyle.Rock, _) => 8,

                // 爵士音乐：复杂的结构
                (MusicStyle.Jazz, _) => 16,

                // 古典音乐：较长的乐句
                (MusicStyle.Classical, _) => 16,

                // 电子音乐：重复的结构
                (MusicStyle.Electronic, _) => 4,

                // 布鲁斯：标准12小节布鲁斯
                (MusicStyle.Blues, _) => 12,

                // 根据情绪调整
                (_, Emotion.Calm) => 12,      // 平静情绪需要更长的段落
                (_, Emotion.Romantic) => 8,   // 浪漫情绪适中
                (_, Emotion.Mysterious) => 10, // 神秘情绪稍长
                (_, Emotion.Sad) => 8,        // 悲伤情绪标准
                (_, Emotion.Happy) => 8,      // 快乐情绪标准
                (_, Emotion.Energetic) => 12, // 活力情绪需要更多变化

                _ => 8
            };
        }

        /// <summary>
        /// 创建默认音阶
        /// </summary>
        /// <returns>根据当前Style和Emotion创建的合适音阶</returns>
        /// <remarks>
        /// 智能创建默认音阶的核心方法，结合了GetDefaultRootNote()和GetDefaultScaleType()方法的结果，
        /// 通过ScaleFactory创建完整的音阶对象。音阶由根音和音阶类型两部分组成，
        /// 系统会确保生成的音阶与当前选择的音乐风格和情绪表达相匹配。</remarks>
        private Scale CreateDefaultScale()
        {
            var rootNote = GetDefaultRootNote();
            var scaleType = GetDefaultScaleType();

            return ScaleFactory.CreateScale(rootNote, scaleType);
        }

        /// <summary>
        /// 获取默认根音
        /// </summary>
        /// <returns>根据当前Emotion选择的合适根音</returns>
        /// <remarks>
        /// 根据情绪选择合适的根音，不同根音在音乐心理学上具有不同的情感色彩：
        /// - C大调：明亮欢快，适合Happy情绪
        /// - A小调：悲伤忧郁，适合Sad情绪
        /// - G大调：活力充沛，适合Energetic情绪
        /// - D大调：平静温和，适合Calm情绪
        /// - F大调：神秘深沉，适合Mysterious情绪
        /// - E大调：浪漫温暖，适合Romantic情绪
        /// 
        /// 根音选择是构建音乐调性的基础，直接影响旋律的整体情感色彩。</remarks>
        private NoteName GetDefaultRootNote()
        {
            // 根据情绪选择根音
            return Emotion switch
            {
                Emotion.Happy => NoteName.C,      // C大调：明亮欢快
                Emotion.Sad => NoteName.A,        // A小调：悲伤忧郁
                Emotion.Energetic => NoteName.G,  // G大调：活力充沛
                Emotion.Calm => NoteName.D,       // D大调：平静温和
                Emotion.Mysterious => NoteName.F, // F大调：神秘深沉
                Emotion.Romantic => NoteName.E,   // E大调：浪漫温暖
                _ => NoteName.C
            };
        }

        /// <summary>
        /// 获取默认音阶类型
        /// </summary>
        /// <returns>根据当前Style和Emotion选择的合适音阶类型</returns>
        /// <remarks>
        /// 根据音乐风格和情绪组合，选择最适合的音阶类型。音阶类型决定了音符间的音程关系和半音位置，
        /// 是音乐色彩和风格的决定性因素之一。实现了丰富的映射规则：
        /// 
        /// 风格特定映射：
        /// - 流行音乐：Happy情绪使用大调，Sad情绪使用小调
        /// - 摇滚音乐：使用Mixolydian调式（带有属七和弦色彩）
        /// - 爵士音乐：使用旋律小调（提供更丰富的和声可能性）
        /// - 古典音乐：Romantic情绪使用大调，Sad情绪使用和声小调
        /// - 电子音乐：使用五声音阶（简化的音阶结构，便于循环和混音）
        /// - 布鲁斯音乐：使用布鲁斯音阶（带有特征性的降三级、降七级和蓝色音符）
        /// 
        /// 情绪特定映射：
        /// - Mysterious情绪：使用Dorian调式（具有神秘色彩的小调变体）
        /// - Energetic情绪：使用大调（明亮有力的调性）
        /// - Calm情绪：使用五声音阶（平静和谐的音程结构）
        /// 
        /// 这种复杂的映射系统确保了生成的旋律能够准确表达指定的音乐风格和情感。</remarks>
        private ScaleFactory.ScaleType GetDefaultScaleType()
        {
            return (Style, Emotion) switch
            {
                // 流行音乐
                (MusicStyle.Pop, Emotion.Happy) => ScaleFactory.ScaleType.Major,
                (MusicStyle.Pop, Emotion.Sad) => ScaleFactory.ScaleType.Minor,

                // 摇滚音乐
                (MusicStyle.Rock, _) => ScaleFactory.ScaleType.Mixolydian,

                // 爵士音乐
                (MusicStyle.Jazz, _) => ScaleFactory.ScaleType.MelodicMinor,

                // 古典音乐
                (MusicStyle.Classical, Emotion.Romantic) => ScaleFactory.ScaleType.Major,
                (MusicStyle.Classical, Emotion.Sad) => ScaleFactory.ScaleType.HarmonicMinor,

                // 电子音乐
                (MusicStyle.Electronic, _) => ScaleFactory.ScaleType.Pentatonic,

                // 布鲁斯音乐
                (MusicStyle.Blues, _) => ScaleFactory.ScaleType.Blues,

                // 根据情绪
                (_, Emotion.Mysterious) => ScaleFactory.ScaleType.Dorian,
                (_, Emotion.Energetic) => ScaleFactory.ScaleType.Major,
                (_, Emotion.Calm) => ScaleFactory.ScaleType.Pentatonic,

                _ => ScaleFactory.ScaleType.Major
            };
        }

        /// <summary>
        /// 计算默认八度
        /// </summary>
        /// <returns>根据当前Style、Emotion和BPM计算出的合适八度</returns>
        /// <remarks>
        /// 智能计算默认八度的算法分为两步：
        /// 1. 首先根据BPM计算基础八度：
        ///    - 慢速(<60 BPM)：使用低八度(3)
        ///    - 中慢速(60-90 BPM)：使用中低八度(4)
        ///    - 中速(90-120 BPM)：使用标准八度(4)
        ///    - 快速(120-140 BPM)：使用中高八度(5)
        ///    - 超快速(>140 BPM)：使用高八度(5)
        /// 
        /// 2. 然后根据音乐风格和情绪进行微调：
        ///    - 流行音乐和电子音乐：音域偏高(+1)
        ///    - 布鲁斯音乐：音域偏低(-1)
        ///    - 快乐情绪：音域偏高(+1)
        ///    - 悲伤和平静情绪：音域偏低(-1)
        /// 
        /// 同时确保最终八度值在3-6的合理范围内，以避免音域过高或过低导致的不自然效果。
        /// 八度选择直接影响旋律的音域和表现力，是控制旋律整体听觉感受的重要因素。</remarks>
        private int CalculateDefaultOctave()
        {
            // 基础八度根据BPM调整
            int baseOctave = BPM switch
            {
                < 60 => 3,   // 慢速：低八度
                < 90 => 4,   // 中慢速：中低八度
                < 120 => 4,  // 中速：标准八度
                < 140 => 5,  // 快速：中高八度
                _ => 5       // 超快速：高八度
            };

            // 根据风格和情绪微调
            return (Style, Emotion) switch
            {
                // 古典音乐：适中的音域
                (MusicStyle.Classical, _) => baseOctave,

                // 流行音乐：偏高的音域
                (MusicStyle.Pop, _) => Math.Min(6, baseOctave + 1),

                // 摇滚音乐：有力的中音域
                (MusicStyle.Rock, _) => baseOctave,

                // 爵士音乐：复杂的音域
                (MusicStyle.Jazz, _) => baseOctave,

                // 电子音乐：宽广的音域
                (MusicStyle.Electronic, _) => baseOctave + 1,

                // 布鲁斯：较低的音域
                (MusicStyle.Blues, _) => Math.Max(3, baseOctave - 1),

                // 根据情绪调整
                (_, Emotion.Happy) => Math.Min(6, baseOctave + 1),      // 快乐：偏高
                (_, Emotion.Sad) => Math.Max(3, baseOctave - 1),        // 悲伤：偏低
                (_, Emotion.Energetic) => baseOctave,                   // 活力：适中
                (_, Emotion.Calm) => Math.Max(3, baseOctave - 1),       // 平静：偏低
                (_, Emotion.Mysterious) => baseOctave,                  // 神秘：适中
                (_, Emotion.Romantic) => baseOctave,                    // 浪漫：适中

                _ => baseOctave
            };
        }

        /// <summary>
        /// 获取参数信息字符串
        /// </summary>
        /// <returns>当前所有参数配置的格式化字符串表示</returns>
        /// <remarks>
        /// 生成一个包含所有关键参数信息的格式化文本，用于调试和用户界面显示。
        /// 输出包括：音乐风格、情绪、速度(BPM)、小节数量、音阶信息和八度信息。
        /// 对于可智能计算的参数（小节、音阶、八度），还会标记其值是用户自定义的还是系统自动计算的，
        /// 便于用户了解当前配置状态。</remarks>
        public string GetParametersInfo()
        {
            var info = new StringBuilder();
            info.AppendLine($"风格: {Style}");
            info.AppendLine($"情绪: {Emotion}");
            info.AppendLine($"速度: {BPM} BPM");
            info.AppendLine($"小节: {Bars} {(HasCustomBars ? "(自定义)" : "(自动)")}");
            info.AppendLine($"音阶: {Scale.RootNote} {GetScaleTypeName(Scale)} {(HasCustomScale ? "(自定义)" : "(自动)")}");
            info.AppendLine($"八度: {Octave} {(HasCustomOctave ? "(自定义)" : "(自动)")}");
            return info.ToString();
        }

        /// <summary>
        /// 获取音阶类型名称
        /// </summary>
        /// <param name="scale">要识别的音阶对象</param>
        /// <returns>音阶类型的中文名称（大调、小调或通用音阶）</returns>
        /// <remarks>
        /// 通过分析音阶的音程序列识别其类型。当前实现支持识别：
        /// - 大调：音程序列[2, 2, 1, 2, 2, 2, 1]
        /// - 小调：音程序列[2, 1, 2, 2, 1, 2, 2]
        /// - 其他：统一标记为"音阶"
        /// 
        /// 注：此方法提供简化的音阶类型识别，实际项目中可能需要更复杂的逻辑来识别更多音阶类型。</remarks>
        private static string GetScaleTypeName(Scale scale)
        {
            // 简化的音阶类型识别（实际项目中可能需要更复杂的逻辑）
            var intervals = scale.Intervals.Select(i => i.HalfSteps).ToArray();

            if (intervals.SequenceEqual([2, 2, 1, 2, 2, 2, 1]))
                return "大调";
            else if (intervals.SequenceEqual([2, 1, 2, 2, 1, 2, 2]))
                return "小调";
            else
                return "音阶";
        }
    }
}
