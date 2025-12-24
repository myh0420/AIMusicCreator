using AIMusicCreator.Entity;
using AIMusicCreator.Utils;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.MusicTheory;

namespace AIMusicCreator.ApiService.Services.DryWetMidiGerenteMidi;

/// <summary>
/// 改进的旋律生成器 - 基于音乐理论和模式
/// </summary>
/// <remarks>
/// 该类实现了更高级的旋律生成算法，基于音乐理论原则、乐句结构和情感表达。
/// 与基础旋律生成器相比，它提供了更具音乐性的输出，包括乐句构建、旋律轮廓控制和动态力度变化。
/// 支持多种音乐风格和情绪状态的组合，生成更自然、流畅的旋律线条。
/// </remarks>
public class ImprovedMelodyGenerator
{
    /// <summary>
    /// 随机数生成器
    /// </summary>
    /// <remarks>
    /// 此随机数生成器用于在旋律生成过程中引入随机元素，增加音乐的多样性和趣味性。
    /// 它被初始化为一个新的随机数生成器实例，确保每次运行时都能获得不同的随机序列。
    /// </remarks>
    private static readonly Random _random = new();
    /// <summary>
    /// 前一个音符事件列表
    /// </summary>
    /// <remarks>
    /// 此列表用于存储上一个生成的音符事件，用于构建旋律轮廓和控制动态力度。
    /// 它在旋律生成过程中被更新，确保每个音符事件都与前一个事件相关联。
    /// </remarks>
    private readonly List<NoteEvent> _previousNotes = [];
    /// <summary>
    /// 缓存的音符事件列表
    /// </summary>
    /// <remarks>
    /// 此列表用于存储已生成的音符事件，用于在需要时快速检索和重用。
    /// 它在旋律生成过程中被更新，确保每个音符事件都被缓存起来，以便后续使用。
    /// </remarks>
    private readonly List<NoteEvent> _cachedNotes = []; // 重用的音符集合
    /// <summary>
    /// 当前节拍
    /// </summary>
    /// <remarks>
    /// 此变量用于跟踪当前正在生成的音符事件的节拍位置。
    /// 它在旋律生成过程中被更新，确保每个音符事件都在正确的节拍位置。
    /// </remarks>
    private int _currentBeat = 0;
    /// <summary>
    /// 当前旋律生成参数
    /// </summary>
    /// <remarks>
    /// 此变量用于存储当前正在使用的旋律生成参数。
    /// 它在旋律生成过程中被更新，确保每个音符事件都根据当前参数进行生成。
    /// </remarks>
    private MelodyParameters? _currentParameters; // 当前旋律生成参数（可为null）
    /// <summary>
    /// 当前装饰音类型
    /// </summary>
    /// <remarks>
    /// 此变量用于存储当前正在使用的装饰音类型。
    /// 它在旋律生成过程中被更新，确保每个音符事件都根据当前装饰音类型进行生成。
    /// </remarks>
    private OrnamentType _ornamentType = OrnamentType.None; // 装饰音类型
    /// <summary>
    /// MIDI配置参数
    /// </summary>
    private readonly MidiConfig _config; // MIDI配置参数

    /// <summary>
    /// 默认构造函数，使用默认配置
    /// </summary>
    public ImprovedMelodyGenerator()
    {
        _config = new MidiConfig();
    }

    /// <summary>
    /// 构造函数，使用指定的配置路径
    /// </summary>
    /// <param name="configPath">配置文件路径</param>
    public ImprovedMelodyGenerator(string configPath)
    {
        _config = MidiConfig.LoadFromJson(configPath);
    }

    /// <summary>
    /// 构造函数，使用指定的配置对象
    /// </summary>
    /// <param name="config">配置对象</param>
    public ImprovedMelodyGenerator(MidiConfig config)
    {
        _config = config ?? new MidiConfig();
    }

    // 缓存常见的旋律轮廓
    /// <summary>
    /// 旋律轮廓缓存
    /// </summary>
    /// <remarks>
    /// 此字典用于缓存常见的旋律轮廓，键为轮廓名称，值为对应的音符事件列表。
    /// 它在旋律生成过程中被使用，确保每个旋律轮廓都能快速检索和重用。
    /// </remarks>
    private static readonly Dictionary<string, List<int>> _melodicContourCache = [];
    // 缓存常见的节奏模式
    /// <summary>
    /// 节奏模式缓存
    /// </summary>
    /// <remarks>
    /// 此字典用于缓存常见的节奏模式，键为模式名称，值为对应的拍数列表。
    /// 它在旋律生成过程中被使用，确保每个节奏模式都能快速检索和重用。
    /// </remarks>  
    private static readonly Dictionary<string, List<int>> _rhythmPatternCache = [];
    /// <summary>
    /// 参数缓存
    /// </summary>
    /// <remarks>
    /// 此字典用于缓存不同的旋律生成参数，键为参数名称，值为对应的参数对象。
    /// 它在旋律生成过程中被使用，确保每个参数都能快速检索和重用。
    /// </remarks>
    private readonly Dictionary<string, MelodyParameters> _parameterCache = [];

    /// <summary>
    /// 音乐结构类型枚举
    /// </summary>
    /// <remarks>
    /// 此枚举定义了不同的音乐结构类型，用于组织旋律的段落。
    /// 每种结构类型都有其独特的旋律轮廓和节奏模式，以适应不同的音乐需求和.listeners。
    /// 例如，AABA结构常用于流行音乐，而VerseChorus结构则常用于古典音乐。
    /// </remarks>
    private enum MusicalStructure
    {
        /// <summary>
        /// 起承转合结构
        /// </summary>
        /// <remarks>
        /// 此结构通常用于流行音乐，由四小节组成，每个小节包含一个音符，形成起承转合的旋律轮廓。
        /// 例如，C Major AABA 结构为 C-D-E-F-G-A-B-C。
        /// </remarks>
        AABA,   // 起承转合结构
        /// <summary>
        /// 重复对句结构
        /// </summary>
        /// <remarks>
        /// 此结构通常用于古典音乐，由两小节组成，每个小节包含两个音符，形成重复对句的旋律轮廓。
        /// 例如，C Major ABAB 结构为 C-D-C-D。
        /// </remarks>
        ABAB,   // 重复对句结构
        /// <summary>
        /// 主歌副歌结构
        /// </summary>
        /// <remarks>
        /// 此结构通常用于古典音乐，由主歌和副歌两部分组成，主歌部分通常较长，副歌部分较短。
        /// 例如，C Major VerseChorus 结构为 C-D-E-F-G-A-B-C-D-E-F-G。
        /// </remarks>
        VerseChorus, // 主歌副歌结构
        /// <summary>
        /// 回旋曲结构
        /// </summary>
        /// <remarks>
        /// 此结构通常用于古典音乐，由四小节组成，每个小节包含一个音符，形成回旋曲的旋律轮廓。
        /// 例如，C Major Rondo 结构为 C-D-E-F-G-A-B-C。
        /// </remarks>
        Rondo,  // 回旋曲结构
        /// <summary>
        /// 主题变奏结构
        /// </summary>
        /// <remarks>
        /// 此结构通常用于古典音乐，由主歌和副歌两部分组成，主歌部分通常较长，副歌部分较短。
        /// 例如，C Major ThemeVariation 结构为 C-D-E-F-G-A-B-C-D-E-F-G。
        /// </remarks>
        ThemeVariation // 主题变奏结构
    }


    /// <summary>
    /// 生成符合音乐理论的优美旋律
    /// </summary>
    /// <param name="parameters">
    /// 旋律生成参数对象，包含以下关键属性：
    /// - Style: 音乐风格（Pop、Classical、Jazz等）
    /// - Emotion: 情绪状态（Happy、Sad、Serene等）
    /// - Scale: 使用的音阶类型
    /// - Octave: 主要八度范围
    /// - Complexity: 旋律复杂度级别
    /// - Length: 旋律长度（以小节为单位）
    /// </param>
    /// <returns>
    /// 生成的音符事件列表，每个事件包含：
    /// - 音符名称
    /// - 八度
    /// - 开始时间
    /// - 持续时间
    /// - 力度值
    /// - 乐器信息
    /// </returns>
    /// <exception cref="ArgumentNullException">当parameters参数为null时抛出，消息为"旋律参数不能为空"
    /// </exception>
    /// <remarks>
    /// 生成过程遵循以下步骤：
    /// 1. 初始化内部状态变量
    /// 2. 生成和声进行（和弦序列）
    /// 3. 获取指定音阶的音符列表
    /// 4. 根据风格和情绪选择合适的音乐结构
    /// 5. 确定乐句长度
    /// 6. 根据选定的结构生成结构化旋律
    /// 7. 返回生成的音符事件列表
    /// 
    /// 该方法确保生成的旋律符合音乐理论原则，包括合理的音符间隔、流畅的旋律轮廓和适合的节奏模式。
    /// 支持多种音乐风格和情绪状态的组合，生成更自然、流畅的旋律线条。
    /// </remarks>
    public List<NoteEvent> GenerateMelody(MelodyParameters parameters)
    {
        _previousNotes.Clear();
        _currentBeat = 0;
        // 确保参数不为null，避免null引用赋值
        _currentParameters = parameters ?? throw new ArgumentNullException(nameof(parameters), "旋律参数不能为空");
        
        // 使用配置中的默认值补充缺失的参数
        if (parameters.Octave < 1 || parameters.Octave > 9) // 使用合理的八度范围默认值
        {
            parameters.Octave = _config.DefaultOctave;
        }
        if (parameters.BPM <= 0)
        {
            parameters.BPM = _config.DefaultBPM;
        }

        // 生成和声进行
        GenerateChordProgression(parameters);

        List<NoteEvent> notes;
        var scaleNotes = GetScaleNotes(parameters.Scale, parameters.Octave);

        // 选择音乐结构
        MusicalStructure structure = SelectMusicalStructure(parameters.Style, parameters.Emotion);

        // 获取乐句长度
        int phraseLength = GetPhraseLength(parameters.Style);

        // 根据音乐结构生成旋律
        notes = GenerateStructuredMelody(parameters, scaleNotes, structure, phraseLength);

        return notes;
    }

    /// <summary>
    /// 选择适合的音乐结构
    /// </summary>
    /// <param name="style">音乐风格</param>
    /// <param name="emotion">情绪状态</param>
    /// <returns>选择的音乐结构类型</returns>
    /// <remarks>
    /// 此方法根据音乐风格和情绪状态选择一个合适的音乐结构类型。
    /// 不同的风格和情绪会导致不同的结构选择，以适应不同的音乐需求和.listeners。
    /// 例如，流行音乐通常使用AABA或VerseChorus结构，而古典音乐则偏好ThemeVariation或Rondo结构。
    /// </remarks>
    private MusicalStructure SelectMusicalStructure(MusicStyle style, Emotion emotion)
    {
        // 根据音乐风格和情绪选择合适的结构
        return style switch
        {
            MusicStyle.Pop => _random.NextDouble() < 0.6 ? MusicalStructure.AABA : MusicalStructure.VerseChorus,// 流行音乐常用AABA或VerseChorus结构
            MusicStyle.Classical => _random.NextDouble() < 0.5 ? MusicalStructure.ThemeVariation : MusicalStructure.Rondo,// 古典音乐常用ThemeVariation或Rondo结构
            MusicStyle.Jazz => MusicalStructure.AABA,// 爵士乐常用AABA结构
            MusicStyle.Rock => _random.NextDouble() < 0.7 ? MusicalStructure.VerseChorus : MusicalStructure.ABAB,// 摇滚乐常用VerseChorus或ABAB结构
            MusicStyle.Electronic => _random.NextDouble() < 0.6 ? MusicalStructure.VerseChorus : MusicalStructure.ThemeVariation,// 电子音乐常用VerseChorus或ThemeVariation结构
            _ => MusicalStructure.AABA,// 默认使用AABA结构
        };
    }
    /// <summary>
    /// 根据音乐结构生成旋律
    /// </summary>
    /// <param name="parameters">旋律生成参数，包含风格、情绪、音阶和八度等信息</param>
    /// <param name="scaleNotes">音高序列，用于构建旋律</param>
    /// <param name="structure">音乐结构类型，决定了段落的组织方式</param>
    /// <param name="phraseLength">乐句长度，决定了每个段落的音符数量</param>
    /// <returns>生成的音符事件列表，包含时间、持续时间、力度等信息</returns>
    /// <remarks>
    /// 此方法根据音乐结构类型、旋律参数、音高序列、乐句长度和小节数生成一个符合音乐理论的旋律。
    /// 不同的结构类型会导致不同的段落组织方式，如AABA、ABAB、VerseChorus、Rondo和ThemeVariation等。
    /// 每个段落都有不同的旋律轮廓和节奏模式，以适应不同的结构类型和情感。
    /// </remarks>
    private List<NoteEvent> GenerateStructuredMelody(MelodyParameters parameters, List<NoteName> scaleNotes,
                                                   MusicalStructure structure, int phraseLength)
    {
        // 预分配足够的空间，根据结构类型预估需要的空间
        int estimatedSections = 0;
        int barsPerSection = 4; // 每段通常为4小节

        switch (structure)
        {
            case MusicalStructure.AABA:
            case MusicalStructure.ABAB:
            case MusicalStructure.Rondo:
                estimatedSections = 5;
                break;
            case MusicalStructure.VerseChorus:
                estimatedSections = 6;
                break;
            case MusicalStructure.ThemeVariation:
                estimatedSections = 4;
                break;
        }

        // 预分配结果集合，预估每段约有32个音符（4小节 * 16拍 * 0.5音符密度）
        var notes = new List<NoteEvent>(estimatedSections * 32);

        // 使用本地方法来减少重复代码
        void AddSection(string sectionType, int bars = 4)
        {
            var sectionNotes = GenerateSection(parameters, scaleNotes, sectionType, phraseLength, bars);
            notes.AddRange(sectionNotes);
        }

        switch (structure)
        {
            case MusicalStructure.AABA:
                // AABA结构: 主题-主题-对比-主题
                AddSection("A", barsPerSection);
                AddSection("A", barsPerSection);
                AddSection("B", barsPerSection);
                AddSection("A", barsPerSection);
                break;
            case MusicalStructure.ABAB:
                // ABAB结构: 主题-对比-主题-对比
                AddSection("A", barsPerSection);
                AddSection("B", barsPerSection);
                AddSection("A", barsPerSection);
                AddSection("B", barsPerSection);
                break;
            case MusicalStructure.VerseChorus:
                // 主歌副歌结构: 前奏-主歌-副歌-主歌-副歌-尾声
                AddSection("Intro", barsPerSection / 2);
                AddSection("Verse", barsPerSection);
                AddSection("Chorus", barsPerSection);
                AddSection("Verse", barsPerSection);
                AddSection("Chorus", barsPerSection);
                AddSection("Outro", barsPerSection / 2);
                break;
            case MusicalStructure.Rondo:
                // 回旋曲结构: A-B-A-C-A
                AddSection("A", barsPerSection);
                AddSection("B", barsPerSection);
                AddSection("A", barsPerSection);
                AddSection("C", barsPerSection);
                AddSection("A", barsPerSection);
                break;
            case MusicalStructure.ThemeVariation:
                // 主题变奏结构: Theme-Variation1-Variation2-Variation3
                AddSection("Theme", barsPerSection);
                var variation1 = GenerateVariation(parameters, scaleNotes, 1, phraseLength, barsPerSection);
                var variation2 = GenerateVariation(parameters, scaleNotes, 2, phraseLength, barsPerSection);
                var variation3 = GenerateVariation(parameters, scaleNotes, 3, phraseLength, barsPerSection);

                notes.AddRange(variation1);
                notes.AddRange(variation2);
                notes.AddRange(variation3);
                break;
        }

        return notes;
    }

    /// <summary>
    /// 生成音乐段落
    /// </summary>
    /// <param name="parameters">旋律参数</param>
    /// <param name="scaleNotes">音高序列</param>
    /// <param name="sectionType">段落类型</param>
    /// <param name="phraseLength">乐句长度</param>
    /// <param name="bars">小节数</param>
    /// <returns>生成的音符事件列表</returns>
    /// <remarks>
    /// 此方法根据段落类型、旋律参数、音高序列、乐句长度和小节数生成一个音乐段落。
    /// 段落类型决定了段落的结构和情感，而旋律参数则影响了段落的音乐风格和情感。
    /// 每个段落都有不同的旋律轮廓和节奏模式，以适应不同的段落类型和情感。
    /// </remarks>
    private List<NoteEvent> GenerateSection(MelodyParameters parameters, List<NoteName> scaleNotes,
                                          string sectionType, int phraseLength, int bars)
    {
        // 重用缓存的音符集合
        _cachedNotes.Clear();
        int sectionBeats = bars * 16; // 每小节16个16分音符

        // 根据段落类型调整参数
        var sectionParameters = AdjustParametersForSection(parameters, sectionType);

        // 为每个段落生成独特的旋律轮廓和节奏模式
        var sectionRhythm = GetSectionRhythmPattern(sectionType, sectionParameters.Style, sectionParameters.Emotion);
        var sectionContour = GetSectionMelodicContour(sectionType, sectionParameters.Emotion);

        // 预分配必要的集合空间以减少动态扩容
        if (_previousNotes.Capacity < sectionBeats / 2) // 预估音符数量为节拍的一半
        {
            _previousNotes.Capacity = sectionBeats / 2;
        }

        // 按乐句生成段落
        for (int phraseStart = 0; phraseStart < sectionBeats; phraseStart += phraseLength)
        {
            // 生成一个乐句
            for (int beatPosition = 0; beatPosition < phraseLength; beatPosition++)
            {
                int absolutePosition = phraseStart + beatPosition;
                _currentBeat = absolutePosition;

                // 检查是否应该添加音符
                if (ShouldAddNoteAtBeat(absolutePosition, sectionRhythm, sectionParameters.Style))
                {
                    var noteEvent = CreateMusicalNote(sectionParameters, scaleNotes, sectionContour, absolutePosition, phraseLength);

                    if (noteEvent != null)
                    {
                        _cachedNotes.Add(noteEvent);
                        _previousNotes.Add(noteEvent);
                    }
                }
            }
        }

        // 返回缓存的音符集合的副本
        return [.. _cachedNotes];
    }

    /// <summary>
    /// 为不同段落类型调整参数
    /// </summary>
    /// <param name="original">原始参数</param>
    /// <param name="sectionType">段落类型</param>
    /// <returns>调整后的参数</returns>
    /// <remarks>
    /// 此方法根据段落类型调整原始参数，以适应不同段落的音乐风格和情感。
    /// 每个段落类型都有不同的参数调整规则，如主题段落通常保持原始参数，对比段落可能在情绪上有所变化。
    /// </remarks>
    private MelodyParameters AdjustParametersForSection(MelodyParameters original, string sectionType)
    {
        // 生成缓存键
        string scaleStr = original.Scale?.ToString() ?? "Default";
        string cacheKey = $"{(sectionType ?? "Unknown")}_{original.Style}_{original.Emotion}_{scaleStr}_{original.Octave}"; // 确保sectionType有null检查

        // 检查缓存中是否存在
        if (_parameterCache.TryGetValue(cacheKey, out MelodyParameters? cachedParameters) && cachedParameters is not null)
        {
            return cachedParameters;
        }

        // 创建参数副本
        var adjusted = new MelodyParameters
        {
            // 不直接设置Scale属性，让MelodyParameters的构造函数和getter处理默认值逻辑
            Octave = original.Octave, // Octave属性是int类型，不需要null检查
            Bars = original.Bars,
            Style = original.Style,
            Emotion = original.Emotion,
            // 复制其他必要的参数
        };

        // 根据段落类型调整参数
        switch (sectionType)
        {
            case "A":
                // A段通常是主题，保持原始参数
                break;
            case "B":
                // B段通常是对比段，可能在情绪上有所变化
                if (original.Emotion == Emotion.Happy)
                    adjusted.Emotion = Emotion.Energetic;
                else if (original.Emotion == Emotion.Sad)
                    adjusted.Emotion = Emotion.Mysterious;
                break;
            case "Verse":
                // 主歌通常较为平静
                if (original.Emotion == Emotion.Energetic)
                    adjusted.Emotion = Emotion.Happy;
                else if (original.Emotion == Emotion.Mysterious)
                    adjusted.Emotion = Emotion.Calm;
                break;
            case "Chorus":
                // 副歌通常更有表现力
                if (original.Emotion == Emotion.Happy)
                    adjusted.Emotion = Emotion.Energetic;
                else if (original.Emotion == Emotion.Calm)
                    adjusted.Emotion = Emotion.Romantic;
                break;
            case "Intro":
            case "Outro":
                // 前奏和尾声通常较为平静
                adjusted.Emotion = Emotion.Calm;
                break;
            case "Theme":
                // 主题保持原始参数
                break;
            case "C":
                // 回旋曲的C段通常是新的对比段
                adjusted.Emotion = original.Emotion == Emotion.Happy ? Emotion.Romantic : Emotion.Happy;
                break;
        }

        // 缓存结果
        _parameterCache[cacheKey] = adjusted;
        return adjusted;
    }

    /// <summary>
    /// 获取段落特定的节奏模式
    /// </summary>
    /// <param name="sectionType">段落类型</param>
    /// <param name="style">音乐风格</param>
    /// <param name="emotion">段落情感</param>
    /// <returns>段落特定的节奏模式</returns>
    /// <remarks>
    /// 此方法根据段落类型、音乐风格和情感选择不同的节奏模式。
    /// 每个模式都有不同的节奏效果，如同步、异步、循环等。
    /// </remarks>
    private List<int> GetSectionRhythmPattern(string sectionType, MusicStyle style, Emotion emotion)
    {
        // 生成缓存键，添加null检查
        string cacheKey = $"{(sectionType ?? "Unknown")}_{style}_{emotion}";

        // 检查缓存中是否存在
        if (_rhythmPatternCache.TryGetValue(cacheKey, out List<int>? cachedPattern) && cachedPattern is not null)
        {
            return cachedPattern;
        }

        // 获取基础节奏模式
        var basePattern = GetMelodicRhythmPattern(style, emotion);
        List<int> resultPattern;

        // 根据段落类型调整节奏模式
        switch (sectionType)
        {
            case "A":
            case "Theme":
                resultPattern = basePattern;
                break;
            case "B":
                // B段可以使用更复杂或不同的节奏
                // 直接使用basePattern，不添加变化
                resultPattern = basePattern;
                break;
            case "Verse":
                // 主歌使用较为平稳的节奏
                // 直接使用basePattern，不调用SimplifyRhythmPattern
                resultPattern = basePattern;
                break;
            case "Chorus":
                // 副歌使用更有表现力的节奏
                // 调用AddSyncopation（它会修改传入的pattern）
                AddSyncopation(basePattern);
                resultPattern = basePattern;
                break;
            case "Intro":
            case "Outro":
                // 前奏和尾声使用简单节奏
                // 直接使用basePattern，不调用GetSimpleRhythmPattern
                resultPattern = basePattern;
                break;
            case "C":
                // 回旋曲的C段使用新的节奏变化
                // 直接使用basePattern，不添加变化
                resultPattern = basePattern;
                break;
            default:
                resultPattern = basePattern;
                break;
        }

        // 缓存结果
        _rhythmPatternCache[cacheKey] = resultPattern;
        return resultPattern;
    }

    /// <summary>
    /// 获取段落特定的旋律轮廓
    /// </summary>
    /// <param name="sectionType">段落类型</param>
    /// <param name="emotion">段落情感</param>
    /// <returns>段落特定的旋律轮廓</returns>
    /// <remarks>
    /// 此方法根据段落类型和情感选择不同的旋律轮廓模式。
    /// 每个模式都有不同的旋律效果，如平稳、波、谐波等。
    /// </remarks>
    private List<int> GetSectionMelodicContour(string sectionType, Emotion emotion)
    {
        // 生成缓存键，添加null检查
        string cacheKey = $"{(sectionType ?? "Unknown")}_{emotion}";

        // 检查缓存中是否存在
        if (_melodicContourCache.TryGetValue(cacheKey, out List<int>? cachedContour) && cachedContour is not null)
        {
            return cachedContour;
        }

        // 基础轮廓
        var baseContour = GetMelodicContour(emotion);
        List<int> resultContour;

        // 根据段落类型调整轮廓
        switch (sectionType)
        {
            case "A":
            case "Theme":
                resultContour = baseContour;
                break;
            case "B":
                // B段使用反向轮廓
                var reversedContour = new List<int>(baseContour);
                reversedContour.Reverse();
                resultContour = reversedContour;
                break;
            case "Verse":
                // 主歌使用多样化的平稳轮廓
                if (_random.NextDouble() < 0.4)
                    resultContour = GetCalmMelodicContour();
                else if (_random.NextDouble() < 0.7)
                    resultContour = GetWaveContour();
                else
                    resultContour = GetHarmonicContour();
                break;
            case "Chorus":
                // 副歌使用多样化的富有表现力的轮廓
                if (_random.NextDouble() < 0.3)
                    resultContour = GetExpressiveMelodicContour();
                else if (_random.NextDouble() < 0.6)
                    resultContour = GetArchContour();
                else if (_random.NextDouble() < 0.8)
                    resultContour = GetAscendingContour();
                else
                    resultContour = GetOrnamentalContour();
                break;
            case "Intro":
            case "Outro":
                // 前奏和尾声使用多样化的简单轮廓
                if (_random.NextDouble() < 0.4)
                    resultContour = [0, 1, 2, 1, 0];
                else if (_random.NextDouble() < 0.7)
                    resultContour = GetCalmMelodicContour();
                else
                    resultContour = GetHarmonicContour();
                break;
            case "C":
                // 回旋曲的C段使用多样化轮廓
                if (_random.NextDouble() < 0.3)
                    resultContour = GetMixedMelodicContour();
                else if (_random.NextDouble() < 0.6)
                    resultContour = GetHarmonicContour();
                else if (_random.NextDouble() < 0.8)
                    resultContour = GetMotivicContour();
                else
                    resultContour = GetOrnamentalContour();
                break;
            default:
                resultContour = baseContour;
                break;
        }

        // 缓存结果
        _melodicContourCache[cacheKey] = resultContour;
        return resultContour;
    }

    /// <summary>
    /// 获取平静的旋律轮廓
    /// </summary>
    /// <returns>平静的旋律轮廓</returns>
    /// <remarks>
    /// 此方法根据随机数选择不同的平静轮廓模式。
    /// 每个模式都有不同的平静效果，如平静音、平静节奏等。
    /// </remarks>
    private List<int> GetCalmMelodicContour()
    {
        // 根据概率选择不同的平静轮廓模式
        if (_random.NextDouble() < 0.3)
            return [0, 1, 0, 1, 2, 1, 0];
        else if (_random.NextDouble() < 0.6)
            return [0, 1, 2, 1, 0, 1, 0];
        else
            return [1, 0, 2, 1, 3, 2, 1];
    }

    /// <summary>
    /// 获取富有表现力的旋律轮廓
    /// </summary>
    /// <returns>富有表现力的旋律轮廓</returns>
    /// <remarks>
    /// 此方法根据随机数选择不同的表现力轮廓模式。
    /// 每个模式都有不同的表现力效果，如表现力音、表现力节奏等。
    /// </remarks>
    private List<int> GetExpressiveMelodicContour()
    {
        // 根据概率选择不同的表现力轮廓模式
        if (_random.NextDouble() < 0.3)
            return [0, 3, 1, 4, 2, 5, 3, 0];
        else if (_random.NextDouble() < 0.6)
            return [0, 2, 4, 6, 4, 2, 0, 1, 3];
        else
            return [0, 4, 2, 5, 1, 6, 3, 0];
    }

    /// <summary>
    /// 获取混合的旋律轮廓
    /// </summary>
    /// <returns>混合的旋律轮廓</returns>
    /// <remarks>
    /// 此方法根据随机数选择不同的混合轮廓模式。
    /// 每个模式都有不同的混合效果，如混合音、混合节奏等。
    /// </remarks>
    private List<int> GetMixedMelodicContour()
    {
        // 根据概率选择不同的混合轮廓模式
        if (_random.NextDouble() < 0.3)
            return [0, 2, 4, 2, 0, 1, 3, 1];
        else if (_random.NextDouble() < 0.6)
            return [1, 3, 0, 2, 4, 1, 3, 5, 2];
        else
            return [2, 0, 4, 1, 5, 3, 7, 5, 2];
    }

    /// <summary>
    /// 获取上升式旋律轮廓
    /// </summary>
    /// <returns>上升式旋律轮廓</returns>
    /// <remarks>
    /// 此方法根据随机数选择不同的上升式旋律轮廓模式。
    /// 每个模式都有不同的上升效果，如上升音、上升节奏等。
    /// </remarks>
    private List<int> GetAscendingContour()
    {
        if (_random.NextDouble() < 0.3)
            return [0, 1, 2, 3, 4, 5, 6, 7];
        else if (_random.NextDouble() < 0.6)
            return [0, 2, 1, 3, 2, 4, 3, 5, 4, 6, 5, 7];
        else
            return [0, 3, 1, 4, 2, 5, 3, 6, 4, 7];
    }

    /// <summary>
    /// 获取下降式旋律轮廓
    /// </summary>
    /// <returns>下降式旋律轮廓</returns>
    /// <remarks>
    /// 此方法根据随机数选择不同的下降式旋律轮廓模式。
    /// 每个模式都有不同的下降效果，如下降音、下降节奏等。
    /// </remarks>
    private List<int> GetDescendingContour()
    {
        if (_random.NextDouble() < 0.3)
            return [7, 6, 5, 4, 3, 2, 1, 0];
        else if (_random.NextDouble() < 0.6)
            return [7, 5, 6, 4, 5, 3, 4, 2, 3, 1, 2, 0];
        else
            return [7, 4, 6, 3, 5, 2, 4, 1, 3, 0];
    }

    /// <summary>
    /// 获取拱形式旋律轮廓
    /// </summary>
    /// <returns>拱形式旋律轮廓</returns>
    /// <remarks>
    /// 此方法根据随机数选择不同的拱形式旋律轮廓模式。
    /// 每个模式都有不同的拱效果，如拱音、拱节奏等。
    /// </remarks>
    private List<int> GetArchContour()
    {
        if (_random.NextDouble() < 0.4)
            return [0, 2, 4, 6, 7, 6, 4, 2, 0];
        else
            return [1, 3, 5, 7, 5, 3, 1];
    }

    /// <summary>
    /// 获取波形旋律轮廓
    /// </summary>
    /// <returns>波形旋律轮廓</returns>
    /// <remarks>
    /// 此方法根据随机数选择不同的波形旋律轮廓模式。
    /// 每个模式都有不同的波形效果，如波形音、波形节奏等。
    /// </remarks>
    private List<int> GetWaveContour()
    {
        if (_random.NextDouble() < 0.3)
            return [3, 5, 3, 1, 3, 5, 3, 1];
        else if (_random.NextDouble() < 0.6)
            return [4, 2, 5, 3, 6, 4, 7, 5, 3];
        else
            return [2, 4, 1, 5, 0, 6, 3, 7];
    }

    /// <summary>
    /// 获取动机式旋律轮廓
    /// </summary>
    /// <returns>动机式旋律轮廓</returns>
    /// <remarks>
    /// 此方法根据随机数选择不同的动机式旋律轮廓模式。
    /// 每个模式都有不同的动机效果，如动机音、动机节奏等。
    /// </remarks>
    private List<int> GetMotivicContour()
    {
        if (_random.NextDouble() < 0.3)
            return [0, 1, 3, 0, 1, 4, 0, 1, 5];
        else if (_random.NextDouble() < 0.6)
            return [0, 2, 1, 0, 2, 1, 3, 5, 4];
        else
            return [0, 3, 2, 1, 3, 2, 4, 6, 5];
    }

    /// <summary>
    /// 获取和声式旋律轮廓
    /// </summary>
    /// <returns>和声式旋律轮廓</returns>
    /// <remarks>
    /// 此方法根据随机数选择不同的和声式旋律轮廓模式。
    /// 每个模式都有不同的和声效果，如和声音、和声节奏等。
    /// </remarks>
    private List<int> GetHarmonicContour()
    {
        if (_random.NextDouble() < 0.3)
            return [0, 2, 4, 0, 4, 2, 0];
        else if (_random.NextDouble() < 0.6)
            return [0, 2, 4, 6, 4, 2, 0];
        else
            return [0, 3, 4, 7, 4, 3, 0];
    }

    /// <summary>
    /// 获取装饰性旋律轮廓
    /// </summary>
    /// <returns>装饰性旋律轮廓</returns>
    /// <remarks>
    /// 此方法根据随机数选择不同的装饰性旋律轮廓模式。
    /// 每个模式都有不同的装饰效果，如装饰音、装饰节奏等。
    /// </remarks>
    private List<int> GetOrnamentalContour()
    {
        if (_random.NextDouble() < 0.3)
            return [0, 1, 0, 2, 1, 0, 3, 2, 1, 0];
        else if (_random.NextDouble() < 0.6)
            return [0, 1, 2, 1, 2, 3, 2, 3, 4, 3, 2, 1, 0];
        else
            return [0, 1, 0, 2, 3, 2, 4, 5, 4, 6, 7, 6, 4, 2, 0];
    }

    /// <summary>
    /// 获取简单的节奏模式
    /// </summary>
    /// <returns>简单的节奏模式</returns>
    /// <remarks>
    /// 此方法返回一个简单的四分音符和八分音符模式，用于生成变奏。
    /// 每个拍数如果为1，则表示四分音符，为0则表示八分音符。
    /// </remarks>
    private List<int> GetSimpleRhythmPattern()
    {
        // 简单的四分音符和八分音符模式
        return [1, 0, 1, 0, 1, 0, 1, 0];
    }

    /// <summary>
    /// 简化节奏模式
    /// </summary>
    /// <param name="pattern">原始节奏模式</param>
    /// <returns>简化后的节奏模式</returns>
    /// <remarks>
    /// 此方法根据原始节奏模式简化节奏，减少音符密度，使变奏更简单。
    /// 每个拍数如果大于0且随机数小于0.8，则简化为1（四分音符），否则简化为0（无音符）。
    /// </remarks>
    private List<int> SimplifyRhythmPattern(List<int> pattern)
    {
        var simplified = new List<int>();
        foreach (var beat in pattern)
        {
            // 减少音符密度
            simplified.Add(beat > 0 && _random.NextDouble() < 0.8 ? 1 : 0);
        }
        return simplified;
    }

    /// <summary>
    /// 生成主题变奏
    /// </summary>
    /// <param name="parameters">变奏参数</param>
    /// <param name="scaleNotes">变奏音阶音符</param>
    /// <param name="variationNumber">变奏编号（1-3）</param>
    /// <param name="phraseLength">变奏段落长度（拍数）</param>
    /// <param name="bars">变奏总拍数（小节数）</param>
    /// <returns>变奏音符事件列表</returns>
    /// <remarks>
    /// 此方法根据变奏参数、变奏音阶音符、变奏编号、变奏段落长度和变奏总拍数生成一个变奏。
    /// 每个变奏都有不同的变奏技术，如改变节奏、旋律装饰、反向或逆行等。
    /// </remarks>
    private List<NoteEvent> GenerateVariation(MelodyParameters parameters, List<NoteName> scaleNotes,
                                            int variationNumber, int phraseLength, int bars)
    {
        var variationNotes = new List<NoteEvent>();
        int variationBeats = bars * 16;

        // 创建变奏参数
        var variationParams = new MelodyParameters
        {
            Scale = parameters.Scale,
            Octave = parameters.Octave,
            Bars = parameters.Bars,
            Style = parameters.Style,
            Emotion = parameters.Emotion,
            // 复制其他必要的参数
        };

        // 根据变奏编号应用不同的变奏技术
        switch (variationNumber)
        {
            case 1:
                // 第一次变奏：改变节奏但保持旋律轮廓
                variationParams.Style = GetVariationStyle(1);
                break;
            case 2:
                // 第二次变奏：旋律装饰
                variationParams.Emotion = GetVariationEmotion(2);
                break;
            case 3:
                // 第三次变奏：反向或逆行
                variationParams.Scale = GetVariationScale(parameters.Scale);
                break;
        }

        // 生成变奏轮廓和节奏
        var variationRhythm = GetVariationRhythm(variationNumber, variationParams.Style, variationParams.Emotion);
        var variationContour = GetVariationContour(variationNumber);

        // 生成变奏段落
        for (int phraseStart = 0; phraseStart < variationBeats; phraseStart += phraseLength)
        {
            for (int beatPosition = 0; beatPosition < phraseLength; beatPosition++)
            {
                int absolutePosition = phraseStart + beatPosition;
                _currentBeat = absolutePosition;

                if (ShouldAddNoteAtBeat(absolutePosition, variationRhythm, variationParams.Style))
                {
                    var noteEvent = CreateMusicalNote(variationParams, scaleNotes, variationContour,
                                                     absolutePosition, phraseLength);

                    if (noteEvent != null)
                    {
                        variationNotes.Add(noteEvent);
                        _previousNotes.Add(noteEvent);
                    }
                }
            }
        }

        return variationNotes;
    }

    /// <summary>
    /// 获取变奏风格
    /// </summary>
    /// <param name="variationNumber">变奏编号（1-3）</param>
    /// <returns>变奏风格</returns>
    /// <remarks>
    /// 此方法根据变奏编号返回不同的风格。
    /// 每个风格都代表了变奏中不同的音乐类型或音乐风格。
    /// </remarks>
    private MusicStyle GetVariationStyle(int variationNumber)
    {
        return variationNumber switch
        {
            1 => MusicStyle.Jazz,// 改变节奏
            2 => MusicStyle.Classical,// 旋律装饰
            3 => MusicStyle.Electronic,// 不同的风格处理
            _ => MusicStyle.Pop,
        };
    }

    /// <summary>
    /// 获取变奏情绪
    /// </summary>
    /// <param name="variationNumber">变奏编号（1-3）</param>
    /// <returns>变奏情绪</returns>
    /// <remarks>
    /// 此方法根据变奏编号返回不同的情绪。
    /// 每个情绪都代表了变奏中不同的情感或情感状态。
    /// </remarks>
    private Emotion GetVariationEmotion(int variationNumber)
    {
        return variationNumber switch
        {
            1 => Emotion.Energetic,
            2 => Emotion.Romantic,
            3 => Emotion.Mysterious,
            _ => Emotion.Happy,
        };
    }

    /// <summary>
    /// 获取变奏音阶
    /// 根据原始音阶的音程结构创建适当的变奏版本
    /// </summary>
    /// <param name="originalScale">原始音阶</param>
    /// <returns>变奏音阶</returns>
    /// <remarks>
    /// 此方法根据原始音阶的音程结构创建适当的变奏版本。
    /// 例如，大调音阶变为混合利底亚调式（降低第七音），
    /// 小调音阶变为多利亚调式（升高第六音）。
    /// </remarks>
    private Scale GetVariationScale(Scale? originalScale)
    {
        if (originalScale == null)
            return null;

        // 获取原始音阶的音程半音数数组
        var originalIntervals = originalScale.Intervals.Select(i => i.HalfSteps).ToArray();
        
        // 根据不同音阶类型创建变奏
        if (originalIntervals.SequenceEqual(new[] { 2, 2, 1, 2, 2, 2, 1 }))
        {
            // 大调音阶变为混合利底亚调式（降低第七音）
            var mixolydianIntervals = new[]
            {
                Melanchall.DryWetMidi.MusicTheory.Interval.GetUp((Melanchall.DryWetMidi.Common.SevenBitNumber)2),
                Melanchall.DryWetMidi.MusicTheory.Interval.GetUp((Melanchall.DryWetMidi.Common.SevenBitNumber)2),
                Melanchall.DryWetMidi.MusicTheory.Interval.GetUp((Melanchall.DryWetMidi.Common.SevenBitNumber)1),
                Melanchall.DryWetMidi.MusicTheory.Interval.GetUp((Melanchall.DryWetMidi.Common.SevenBitNumber)2),
                Melanchall.DryWetMidi.MusicTheory.Interval.GetUp((Melanchall.DryWetMidi.Common.SevenBitNumber)2),
                Melanchall.DryWetMidi.MusicTheory.Interval.GetUp((Melanchall.DryWetMidi.Common.SevenBitNumber)1),
                Melanchall.DryWetMidi.MusicTheory.Interval.GetUp((Melanchall.DryWetMidi.Common.SevenBitNumber)2)
            };
            return new Melanchall.DryWetMidi.MusicTheory.Scale(mixolydianIntervals, originalScale.RootNote);
        }
        else if (originalIntervals.SequenceEqual(new[] { 2, 1, 2, 2, 1, 2, 2 }))
        {
            // 小调音阶变为多利亚调式（升高第六音）
            var dorianIntervals = new[]
            {
                Melanchall.DryWetMidi.MusicTheory.Interval.GetUp((Melanchall.DryWetMidi.Common.SevenBitNumber)2),
                Melanchall.DryWetMidi.MusicTheory.Interval.GetUp((Melanchall.DryWetMidi.Common.SevenBitNumber)1),
                Melanchall.DryWetMidi.MusicTheory.Interval.GetUp((Melanchall.DryWetMidi.Common.SevenBitNumber)2),
                Melanchall.DryWetMidi.MusicTheory.Interval.GetUp((Melanchall.DryWetMidi.Common.SevenBitNumber)2),
                Melanchall.DryWetMidi.MusicTheory.Interval.GetUp((Melanchall.DryWetMidi.Common.SevenBitNumber)2),
                Melanchall.DryWetMidi.MusicTheory.Interval.GetUp((Melanchall.DryWetMidi.Common.SevenBitNumber)1),
                Melanchall.DryWetMidi.MusicTheory.Interval.GetUp((Melanchall.DryWetMidi.Common.SevenBitNumber)2)
            };
            return new Melanchall.DryWetMidi.MusicTheory.Scale(dorianIntervals, originalScale.RootNote);
        }
        else if (originalIntervals.Length == 5 && originalIntervals.SequenceEqual(new[] { 2, 2, 3, 2, 3 }))
        {
            // 五声音阶变奏 - 添加一个变化音
            var variationIntervals = new[]
            {
                Melanchall.DryWetMidi.MusicTheory.Interval.GetUp((Melanchall.DryWetMidi.Common.SevenBitNumber)2),
                Melanchall.DryWetMidi.MusicTheory.Interval.GetUp((Melanchall.DryWetMidi.Common.SevenBitNumber)1), // 将第二个全音变为半音
                Melanchall.DryWetMidi.MusicTheory.Interval.GetUp((Melanchall.DryWetMidi.Common.SevenBitNumber)2),
                Melanchall.DryWetMidi.MusicTheory.Interval.GetUp((Melanchall.DryWetMidi.Common.SevenBitNumber)2),
                Melanchall.DryWetMidi.MusicTheory.Interval.GetUp((Melanchall.DryWetMidi.Common.SevenBitNumber)3),
                Melanchall.DryWetMidi.MusicTheory.Interval.GetUp((Melanchall.DryWetMidi.Common.SevenBitNumber)2)
            };
            return new Melanchall.DryWetMidi.MusicTheory.Scale(variationIntervals, originalScale.RootNote);
        }
        else if (originalIntervals.Length == 6 && originalIntervals.SequenceEqual(new[] { 3, 2, 1, 1, 3, 2 }))
        {
            // 蓝调音阶变奏 - 保持基本特征但稍作调整
            var variationIntervals = new[]
            {
                Melanchall.DryWetMidi.MusicTheory.Interval.GetUp((Melanchall.DryWetMidi.Common.SevenBitNumber)3),
                Melanchall.DryWetMidi.MusicTheory.Interval.GetUp((Melanchall.DryWetMidi.Common.SevenBitNumber)2),
                Melanchall.DryWetMidi.MusicTheory.Interval.GetUp((Melanchall.DryWetMidi.Common.SevenBitNumber)2), // 将半音变为全音
                Melanchall.DryWetMidi.MusicTheory.Interval.GetUp((Melanchall.DryWetMidi.Common.SevenBitNumber)1),
                Melanchall.DryWetMidi.MusicTheory.Interval.GetUp((Melanchall.DryWetMidi.Common.SevenBitNumber)2), // 将小三度变为大二度
                Melanchall.DryWetMidi.MusicTheory.Interval.GetUp((Melanchall.DryWetMidi.Common.SevenBitNumber)2)
            };
            return new Melanchall.DryWetMidi.MusicTheory.Scale(variationIntervals, originalScale.RootNote);
        }
        else
        {
            // 对于其他音阶类型，创建一个轻微变化的版本
            // 通过随机调整1-2个音程来创建变奏
            var variationIntervals = new List<Melanchall.DryWetMidi.MusicTheory.Interval>();
            int[] intervalValues = (int[])originalIntervals.Clone();
            
            // 随机选择1-2个位置进行调整
            int changesToMake = _random.Next(1, 3);
            var positionsToChange = new HashSet<int>();
            
            while (positionsToChange.Count < changesToMake && positionsToChange.Count < intervalValues.Length)
            {
                positionsToChange.Add(_random.Next(intervalValues.Length));
            }
            
            // 对选中的位置进行轻微调整
            foreach (int pos in positionsToChange)
            {
                // 避免调整过大，保持在合理范围内
                int adjustment = _random.Next(-1, 2); // -1, 0, 或 1
                if (intervalValues[pos] + adjustment >= 1 && intervalValues[pos] + adjustment <= 4)
                {
                    intervalValues[pos] += adjustment;
                }
            }
            
            // 转换回Interval对象
            foreach (int value in intervalValues)
            {
                variationIntervals.Add(Melanchall.DryWetMidi.MusicTheory.Interval.GetUp((Melanchall.DryWetMidi.Common.SevenBitNumber)value));
            }
            
            return new Melanchall.DryWetMidi.MusicTheory.Scale(variationIntervals.ToArray(), originalScale.RootNote);
        }
    }

    /// <summary>
    /// 获取变奏节奏
    /// </summary>
    /// <param name="variationNumber">变奏编号（1-3）</param>
    /// <param name="style">音乐风格</param>
    /// <param name="emotion">情绪</param>
    /// <returns>变奏节奏列表</returns>
    /// <remarks>
    /// 此方法根据变奏编号、音乐风格和情绪返回不同的节奏模式。
    /// 每个节奏模式都是一个整数列表，代表在变奏中每个节拍是否有音符。
    /// 模式的长度通常与变奏的节拍数相同。
    /// </remarks>
    private List<int> GetVariationRhythm(int variationNumber, MusicStyle style, Emotion emotion)
    {
        var basePattern = GetMelodicRhythmPattern(style, emotion);

        switch (variationNumber)
        {
            /// <summary>
            /// 第一次变奏：改变节奏
            /// </summary>
            case 1:
                // 创建副本以避免修改原始模式
                var syncopatedPattern = new List<int>(basePattern);
                AddSyncopation(syncopatedPattern);
                return syncopatedPattern;
            /// <summary>
            /// 第二次变奏：旋律装饰
            /// </summary>
            case 2:
                return AddRhythmVariations(basePattern, style, emotion);
            /// <summary>
            /// 第三次变奏：不同的风格处理
            /// </summary>
            case 3:
                return InvertRhythmPattern(basePattern);
                /// <summary>
                /// 默认变奏：基本旋律
                /// </summary>
            default:
                return basePattern;
        }
    }

    /// <summary>
    /// 反转节奏模式
    /// </summary>
    /// <param name="pattern">原始节奏模式</param>
    /// <returns>反转后的节奏模式</returns>
    /// <remarks>
    /// 此方法用于反转给定的节奏模式。
    /// 它将非零值转换为零值，将零值转换为非零值。
    /// 这在变奏中用于创建反向或逆行的旋律。
    /// </remarks>
    private List<int> InvertRhythmPattern(List<int> pattern)
    {
        var inverted = new List<int>();
        foreach (var beat in pattern)
        {
            // 对于List<int>，我们可以反转非零值和零值的含义
            // 如果是0则变为1，如果是正数则变为0
            inverted.Add(beat == 0 ? 1 : 0);
        }
        return inverted;
    }

    /// <summary>
    /// 获取变奏轮廓
    /// </summary>
    /// <param name="variationNumber">变奏编号（1-3）</param>
    /// <returns>变奏轮廓列表</returns>
    /// <remarks>
    /// 此方法根据变奏编号返回不同的轮廓。
    /// 每个轮廓都是一个整数列表，代表在变奏中每个节拍的音符索引。
    /// 轮廓的长度通常与变奏的节拍数相同。
    /// </remarks>
    private List<int> GetVariationContour(int variationNumber)
    {
        return variationNumber switch
        {
            /// <summary>
            /// 第一次变奏：改变节奏
            /// </summary>
            1 => [0, 2, 4, 6, 4, 2, 0],
            /// <summary>
            /// 第二次变奏：旋律装饰
            /// </summary>
            2 => [0, 1, 3, 2, 4, 5, 3, 1, 0],
            /// <summary>
            /// 第三次变奏：不同的风格处理
            /// </summary>
            3 => [7, 5, 3, 1, 0, 1, 3, 5, 7],
            /// <summary>
            /// 默认变奏：基本旋律
            /// </summary>
            _ => [0, 1, 2, 3, 4, 3, 2, 1, 0],
        };
    }

    /// <summary>
    /// 获取音阶的音符列表（包含八度信息）
    /// </summary>
    /// <param name="scale">音阶对象</param>
    /// <param name="octave">基础八度</param>
    /// <returns>音阶音符名称列表</returns>
    /// <remarks>
    /// 此方法用于获取音阶的音符列表，包含八度信息。
    /// 它首先尝试使用 DryWetMidi 8.x 中的 GetSteps 和 NoteUtilities 来获取音符。
    /// 如果获取失败（例如，音符超出范围），则使用备用方法手动构建音符列表。
    /// 最后，返回包含所有音符名称的列表（去重）。
    /// </remarks>
    private List<NoteName> GetScaleNotes(Scale scale, int octave)
    {
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

            // 添加高八度的音符
            var nextOctaveRoot = NoteUtilities.GetNoteNumber(scale.RootNote, octave + 1);

            foreach (var interval in intervals)
            {
                var noteNumber = nextOctaveRoot + interval.HalfSteps;
                var noteName = NoteUtilities.GetNoteName((SevenBitNumber)noteNumber);
                scaleNotes.Add(noteName);
            }
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
    /// 此方法用于在DryWetMidi 8.x中无法直接获取音阶音符时使用。
    /// 它根据音阶类型（大调或小调）手动构建音符列表。
    /// 构建过程中，会考虑到八度信息，确保音符在正确的音高范围。
    /// </remarks>
    private List<NoteName> GetScaleNotesFallback(Scale scale, int octave)
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
        }

        return scaleNotes;
    }

    /// <summary>
    /// 检查是否为大调音阶
    /// </summary>
    /// <param name="scale">要检查的音阶对象</param>
    /// <returns>如果是大调音阶返回true，否则返回false</returns>
    /// <remarks>
    /// 此方法通过检查音阶的音程模式来判断是否为大调音阶。
    /// 大调音阶的音程模式为：2,2,1,2,2,2,1。
    /// 如果音阶的音程模式与大调音阶匹配，则返回true；否则返回false。
    /// 如果无法判断，默认返回true（大调）。
    /// </remarks>
    private bool IsMajorScale(Scale scale)
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
    /// 根据半音步数和八度获取音符
    /// </summary>
    /// <param name="root">根音音符</param>
    /// <param name="halfSteps">半音步数</param>
    /// <param name="octave">八度</param>
    /// <returns>计算得到的音符名称</returns>
    /// <remarks>
    /// 此方法根据根音、半音步数和八度计算得到目标音符。
    /// 它首先尝试使用 DryWetMidi 的 NoteUtilities 来计算音符。
    /// 如果计算失败（例如，音符超出范围），则使用备用方法手动计算。
    /// 最后，返回计算得到的音符名称。
    /// </remarks>
    private NoteName GetNoteBySteps(NoteName root, int halfSteps, int octave)
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
    /// 获取音阶的音符名称列表（仅音符名称，不含八度）
    /// </summary>
    /// <param name="scale">音阶对象</param>
    /// <returns>音阶音符名称列表</returns>
    /// <remarks>
    /// 此方法根据音阶对象获取其音符名称列表（仅音符名称，不含八度）。
    /// 它首先检查缓存是否存在，如果存在则直接返回缓存结果。
    /// 如果缓存不存在，则根据音阶类型（大调或小调）使用不同的方法构建音符列表。
    /// 构建完成后，将结果缓存起来，以便后续调用。
    /// </remarks>
    private static readonly Dictionary<string, List<NoteName>> _scaleNotesCache = [];
    /// <summary>
    /// 获取音阶的音符名称列表（仅音符名称，不含八度）
    /// </summary>
    /// <param name="scale">音阶对象</param>
    /// <returns>音阶音符名称列表</returns>
    /// <remarks>
    /// 此方法根据音阶对象获取其音符名称列表（仅音符名称，不含八度）。
    /// 它首先检查缓存是否存在，如果存在则直接返回缓存结果。
    /// 如果缓存不存在，则根据音阶类型（大调或小调）使用不同的方法构建音符列表。
    /// 构建完成后，将结果缓存起来，以便后续调用。
    /// </remarks>
    private List<NoteName> GetScaleNoteNames(Scale scale)
    {
        var root = scale.RootNote;

        // 生成缓存键：根音符_音程序列
        string cacheKey = $"{root}_{string.Join(",", scale.Intervals.Select(i => i.HalfSteps))}";

        // 检查缓存是否存在
        if (_scaleNotesCache.TryGetValue(cacheKey, out var cachedNotes))
        {
            // 返回缓存的副本，避免外部修改影响缓存
            return [.. cachedNotes];
        }

        var notes = new List<NoteName>();

        try
        {
            // 使用 DryWetMidi 8.x 的 API 获取音阶音符
            var intervals = scale.Intervals;
            foreach (var interval in intervals)
            {
                // 计算音符编号并转换为音符名称
                var rootNumber = NoteUtilities.GetNoteNumber(root, 0);
                var noteNumber = rootNumber + interval.HalfSteps;
                var noteName = NoteUtilities.GetNoteName((SevenBitNumber)noteNumber);
                notes.Add(noteName);
            }
        }
        catch
        {
            // 备用方案：手动构建大调音阶
            notes.Add(root);
            notes.Add((NoteName)(((int)root + 2) % 12));
            notes.Add((NoteName)(((int)root + 4) % 12));
            notes.Add((NoteName)(((int)root + 5) % 12));
            notes.Add((NoteName)(((int)root + 7) % 12));
            notes.Add((NoteName)(((int)root + 9) % 12));
            notes.Add((NoteName)(((int)root + 11) % 12));
        }

        // 缓存结果
        _scaleNotesCache[cacheKey] = [.. notes];

        return notes;
    }

    /// <summary>
    /// 创建音乐化的音符
    /// </summary>
    /// <param name="parameters">旋律生成参数</param>
    /// <param name="scaleNotes">可用的音阶音符列表</param>
    /// <param name="melodicContour">旋律轮廓模式</param>
    /// <param name="beat">当前拍子位置</param>
    /// <param name="phraseLength">乐句长度</param>
    /// <returns>创建的音符事件，如果不应该添加音符则返回null</returns>
    /// /// <remarks>
    /// 此方法根据旋律生成参数、音阶音符列表、旋律轮廓模式、当前拍子位置和乐句长度创建一个音乐化的音符事件。
    /// 它考虑了音乐理论的原则，如旋律轮廓、和弦 Progression 等，以确保生成的音符符合预期的音乐风格。
    /// </remarks>
    private NoteEvent? CreateMusicalNote(MelodyParameters parameters, List<NoteName> scaleNotes,
                                      List<int> melodicContour, int beat, int phraseLength)
    {
        // 确定在乐句中的位置 - 优化计算
        int phrasePosition = beat % phraseLength;
        bool isPhraseStart = phrasePosition == 0;
        bool isPhraseEnd = phrasePosition == phraseLength - 1;

        // 优化：只在关键位置更新和弦（减少计算频率）
        if (phrasePosition == 0 || phrasePosition == phraseLength / 2)
        {
            UpdateCurrentChord(phrasePosition, phraseLength);
        }

        // 选择音符 - 基于音乐理论
        var (note, octave) = SelectMusicalNote(parameters, scaleNotes, melodicContour, phrasePosition, phraseLength);

        if (note == null)
            return null;

        // 优化：预计算常用的tick值
        const int TICKS_PER_BEAT = 120;
        int startTime = beat * TICKS_PER_BEAT;

        // 确定时值 - 基于节奏模式
        var duration = GetMusicalDuration(parameters, phrasePosition, isPhraseEnd);

        // 确定力度 - 基于乐句位置
        var velocity = GetMusicalVelocity(parameters, phrasePosition, phraseLength);

        // 优化：使用对象初始化器直接创建
        // 由于前面已经检查过noteInfo.note不为null，这里可以安全地使用Value
        return new NoteEvent
        {
            Note = note.Value, // 已在前面进行null检查
            Octave = octave,
            StartTime = startTime,
            Duration = duration,
            Velocity = velocity
        };
    }

    // 旋律动机相关字段
    /// <summary>
    /// 当前旋律动机
    /// </summary>
    private List<int> _melodyMotif = []; // 当前旋律动机
    /// <summary>
    /// 当前旋律动机中的位置
    /// </summary>
    private int _motifPosition = 0; // 在动机中的位置
    /// <summary>
    /// 是否已建立动机
    /// </summary>
    private bool _hasEstablishedMotif = false; // 是否已建立动机
    /// <summary>
    /// 动机重复次数
    /// </summary>
    private int _motifRepetitionCount = 0; // 动机重复次数
    /// <summary>
    /// 最大动机重复次数
    /// </summary>
    private const int MAX_MOTIF_REPETITIONS = 3; // 最大动机重复次数

    // 和声相关字段
    /// <summary>
    /// 当前和声进行
    /// </summary>
    private List<List<int>> _chordProgression = []; // 当前和声进行
    /// <summary>
    /// 当前和弦的音阶度数
    /// </summary>
    private List<int> _currentChord = []; // 当前和弦的音阶度数
    /// <summary>
    /// 在和声进行中的位置
    /// </summary>
    private int _chordPosition = 0; // 在和声进行中的位置
    /// <summary>
    /// 每乐句的和弦数量
    /// </summary>  
    private int _chordsPerPhrase = 2; // 每乐句的和弦数量

    /// <summary>
    /// 选择音乐化的音符
    /// </summary>
    /// <param name="parameters">旋律生成参数</param>
    /// <param name="scaleNotes">可用的音阶音符列表</param>
    /// <param name="melodicContour">旋律轮廓模式</param>
    /// <param name="phrasePosition">在乐句中的位置</param>
    /// <param name="phraseLength">乐句长度</param>
    /// <returns>包含音符名称和八度的元组</returns>
    /// <remarks>
    /// 此方法根据旋律生成参数、可用的音阶音符、旋律轮廓模式、在乐句中的位置和乐句长度选择音乐化的音符。
    /// 它考虑了情绪、动机、和弦进行和旋律轮廓，以生成符合音乐理论的音符。
    /// </remarks>
    private (NoteName? note, int octave) SelectMusicalNote(MelodyParameters parameters, List<NoteName> scaleNotes,
                                                         List<int> melodicContour, int phrasePosition, int phraseLength)
    {
        // 直接使用传入的scaleNotes参数，避免重复调用GetScaleNoteNames
        int scaleSize = scaleNotes.Count;
        int scaleDegree;

        // 建立或使用旋律动机
        if (phrasePosition == 0 || _currentBeat % (phraseLength * 2) == 0) // 乐句开始或需要更新动机
        {
            // 检查是否需要创建新动机
            if (!_hasEstablishedMotif || _motifRepetitionCount >= MAX_MOTIF_REPETITIONS)
            {
                CreateMelodyMotif(parameters, scaleSize, phraseLength);
                _motifRepetitionCount = 0;
            }
            else
            {
                _motifRepetitionCount++;
            }
            _motifPosition = 0;
        }

        // 基于乐句位置和动机选择音符
        if (phrasePosition == 0) // 乐句开始 - 使用稳定音
        {
            scaleDegree = GetStableTone(parameters.Emotion);
        }
        else if (phrasePosition == phraseLength - 1) // 乐句结束 - 使用主音或解决音
        {
            scaleDegree = GetResolutionTone(parameters.Emotion);
        }
        else if (phrasePosition == phraseLength / 2) // 乐句中点 - 使用高潮音
        {
            scaleDegree = GetClimaxTone(parameters.Emotion);
        }
        else // 其他位置 - 结合动机和旋律轮廓
        {
            // 优化：只有当旋律轮廓有效且位置在范围内时才使用它
            if (melodicContour != null && _motifPosition < melodicContour.Count)
            {
                scaleDegree = GetMotifBasedScaleDegree(parameters, scaleSize, melodicContour, phrasePosition, phraseLength);
            }
            else
            {
                // 回退到基本的音符选择逻辑
                scaleDegree = GetBasicScaleDegree(parameters, scaleSize, phrasePosition);
            }
        }

        // 一次性确保音阶度数在范围内
        scaleDegree = Math.Max(0, Math.Min(scaleDegree, scaleSize - 1));

        // 优化：减少和弦适应的频率，只在关键位置进行
        bool isChordAdaptationNeeded = phrasePosition % 4 == 0 || phrasePosition == phraseLength - 1;
        if (isChordAdaptationNeeded)
        {
            scaleDegree = AdaptToCurrentChord(scaleDegree, scaleSize, phrasePosition, phraseLength);
        }

        // 添加音乐性的经过音和邻音
        if (_random.NextDouble() < 0.35) // 35%概率添加经过音或邻音
        {
            scaleDegree = AddMusicalOrnament(scaleDegree, scaleSize, phrasePosition, phraseLength);
            // 再次确保范围，但只在实际修改后
            scaleDegree = Math.Max(0, Math.Min(scaleDegree, scaleSize - 1));
        }

        // 确保音符之间的连贯性
        if (_previousNotes.Count > 0)
        {
            scaleDegree = EnsureMelodicCoherence(scaleDegree, scaleSize, parameters);
            // 再次确保范围，但只在实际修改后
            scaleDegree = Math.Max(0, Math.Min(scaleDegree, scaleSize - 1));
        }

        // 直接使用传入的scaleNotes获取音符名称
        var noteName = scaleNotes[scaleDegree];
        int octave = DetermineMusicalOctave(parameters, scaleDegree, phrasePosition);

        // 更新动机位置
        _motifPosition++;

        return (noteName, octave);
    }

    // 添加一个基本的音阶度数选择方法，作为备用逻辑
    /// <summary>
    /// 基本音阶度数选择
    /// </summary>
    /// <param name="parameters">旋律生成参数</param>
    /// <param name="scaleSize">音阶大小</param>
    /// <param name="phrasePosition">在乐句中的位置</param>
    /// <returns>音阶度数</returns>
    /// <remarks>
    /// 此方法基于情绪和位置选择音阶度数。
    /// 标准情绪：随机选择音阶度数。
    /// 快乐情绪：倾向于较高音区。
    /// 悲伤情绪：倾向于较低音区。
    /// 活力情绪：倾向于较高音区。
    /// 平静情绪：中等音程。
    /// 神秘情绪：随机选择音阶度数。
    /// 浪漫情绪：随机选择音阶度数。
    /// 未知情绪：默认使用标准情绪。
    /// </remarks>
    private int GetBasicScaleDegree(MelodyParameters parameters, int scaleSize, int phrasePosition)
    {
        // 简单的基于情绪和位置的音符选择
        return parameters.Emotion switch
        {
            /// <summary>
            /// 标准：符合传统音乐理论的进行
            /// </summary>
            Emotion.Standard => _random.Next(0, scaleSize),
            /// <summary>
            /// 快乐：有活力的上行动
            /// </summary>
            Emotion.Happy => _random.Next(0, scaleSize),
            /// <summary>
            /// 悲伤：下行动机，略带起伏
            /// </summary>
            Emotion.Sad => _random.Next(0, Math.Min(4, scaleSize)),// 倾向于较低音区
            /// <summary>
            /// 活力：跳跃动机，较大音程
            /// </summary>
            Emotion.Energetic => _random.Next(Math.Max(0, scaleSize - 4), scaleSize),// 倾向于较高音区
            /// <summary>
            /// 平静：中等音程，不那么活跃
            /// </summary>
            Emotion.Calm => _random.Next(1, Math.Min(5, scaleSize)),// 中等音区
            /// <summary>
            /// 神秘：复杂的旋律结构，可能包含隐藏的含义
            /// </summary>
            Emotion.Mysterious => _random.Next(0, scaleSize),
            /// <summary>
            /// 浪漫：流畅的旋律，丰富的和声
            /// </summary>
            Emotion.Romantic => _random.Next(0, scaleSize),
            /// <summary>
            /// 未知情绪：默认使用标准情绪
            /// </summary>
            _ => _random.Next(0, scaleSize),
        };
    }


    /// <summary>
    /// 创建旋律动机
    /// </summary>
    /// <param name="parameters">旋律生成参数</param>
    /// <param name="scaleSize">音阶大小</param>
    /// <param name="phraseLength">乐句长度</param>
    /// <remarks>
    /// 此方法根据旋律生成参数、音阶大小和乐句长度创建不同类型的旋律动机。
    /// 动机长度通常为2-4个音符，根据情绪和风格选择不同的动机类型。
    /// </remarks>
    private void CreateMelodyMotif(MelodyParameters parameters, int scaleSize, int phraseLength)
    {
        _melodyMotif.Clear();

        // 动机长度通常为2-4个音符
        int motifLength = 2 + _random.Next(3); // 2-4个音符

        // 根据情绪和风格创建不同类型的动机
        switch (parameters.Emotion)
        {
            /// <summary>
            /// 标准：符合传统音乐理论的进行
            /// </summary>
            case Emotion.Standard:
                // 标准动机，小音程
                CreateStepwiseMotif(scaleSize, motifLength);
                break;
                /// <summary>
                /// 快乐：有活力的上行动
                /// </summary>
            case Emotion.Happy:
                // 上行动机，有活力
                CreateUpwardMotif(scaleSize, motifLength);
                break;
            case Emotion.Sad:
                // 下行动机，略带起伏
                CreateDownwardMotif(scaleSize, motifLength);
                break;
                /// <summary>
                /// 活力：跳跃动机，较大音程
                /// </summary>
            case Emotion.Energetic:
                // 跳跃动机，较大音程
                CreateLeapingMotif(scaleSize, motifLength);
                break;
                /// <summary>
                /// 平静：平稳动机，小音程
                /// </summary>
            case Emotion.Calm:
                // 平稳动机，小音程
                CreateStepwiseMotif(scaleSize, motifLength);
                break;
                /// <summary>
                /// 神秘：不协和动机
                /// </summary>
            case Emotion.Mysterious:
                // 不协和动机
                CreateDissonantMotif(scaleSize, motifLength);
                break;
                /// <summary>
                /// 浪漫：流畅动机，优美曲线
                /// </summary>
            case Emotion.Romantic:
                // 流畅动机，优美曲线
                CreateFlowingMotif(scaleSize, motifLength);
                break;
                /// <summary>
                /// 标准：符合传统音乐理论的进行
                /// </summary>
            default:
                // 默认动机
                CreateStepwiseMotif(scaleSize, motifLength);
                break;
        }

        _hasEstablishedMotif = true;
    }

    /// <summary>
    /// 创建上行动机
    /// </summary>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <param name="motifLength">动机长度</param>
    /// <remarks>
    /// 此方法创建一个上行动的旋律，通过主要向上移动来表达情感。
    /// 不同音乐风格和情绪下，可能会有不同的上行动偏好。
    /// 例如，古典音乐在快乐或浪漫情绪下更倾向于使用上行动的旋律，而在悲伤或平静情绪下更保守。
    /// </remarks>
    private void CreateUpwardMotif(int scaleSize, int motifLength)
    {
        int currentDegree = 0; // 从主音开始
        _melodyMotif.Add(currentDegree);

        for (int i = 1; i < motifLength; i++)
        {
            // 主要向上，但可以有小的起伏
            int step = _random.Next(100) < 80 ? 1 : -1; // 80%概率向上
            currentDegree = Math.Min(currentDegree + step, scaleSize - 1);
            _melodyMotif.Add(currentDegree);
        }
    }

    /// <summary>
    /// 创建下行动机
    /// </summary>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <param name="motifLength">动机长度</param>
    /// <remarks>
    /// 此方法创建一个下行动的旋律，通过主要向下移动来表达情感。
    /// 不同音乐风格和情绪下，可能会有不同的下行动偏好。
    /// 例如，古典音乐在悲伤或平静情绪下更倾向于使用下行动的旋律，而在快乐或浪漫情绪下更保守。
    /// </remarks>
    private void CreateDownwardMotif(int scaleSize, int motifLength)
    {
        int currentDegree = 4; // 从五音开始
        _melodyMotif.Add(currentDegree);

        for (int i = 1; i < motifLength; i++)
        {
            // 主要向下，但可以有小的起伏
            int step = _random.Next(100) < 70 ? -1 : 1; // 70%概率向下
            currentDegree = Math.Max(currentDegree + step, 0);
            _melodyMotif.Add(currentDegree);
        }
    }

    /// <summary>
    /// 创建跳跃动机
    /// </summary>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <param name="motifLength">动机长度</param>
    /// <remarks>
    /// 此方法创建一个跳跃的旋律，通过较大的音程变化来表达情感。
    /// 不同音乐风格和情绪下，可能会有不同的跳跃偏好。
    /// 例如，古典音乐在快乐或浪漫情绪下更倾向于使用跳跃的旋律，而在悲伤或平静情绪下更保守。
    /// </remarks>
    private void CreateLeapingMotif(int scaleSize, int motifLength)
    {
        int currentDegree = 0;
        _melodyMotif.Add(currentDegree);

        for (int i = 1; i < motifLength; i++)
        {
            // 较大的音程跳跃
            int[] leaps = [2, 3, -2, -3]; // 大二度、小三度及其下行
            int leap = leaps[_random.Next(leaps.Length)];
            currentDegree = Math.Max(0, Math.Min(currentDegree + leap, scaleSize - 1));
            _melodyMotif.Add(currentDegree);
        }
    }

    /// <summary>
    /// 创建平稳动机
    /// </summary>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <param name="motifLength">动机长度</param>
    /// <remarks>
    /// 此方法创建一个平稳的旋律，通过小的音程变化来表达情感。
    /// 不同音乐风格和情绪下，可能会有不同的平稳偏好。
    /// 例如，古典音乐在快乐或浪漫情绪下更倾向于使用平稳的旋律，而在悲伤或平静情绪下更保守。
    /// </remarks>
    private void CreateStepwiseMotif(int scaleSize, int motifLength)
    {
        int currentDegree = 0;
        _melodyMotif.Add(currentDegree);

        for (int i = 1; i < motifLength; i++)
        {
            // 级进，主要是小二度或大二度
            int[] steps = [1, -1, 0]; // 上、下、保持
            int step = steps[_random.Next(steps.Length)];
            currentDegree = Math.Max(0, Math.Min(currentDegree + step, scaleSize - 1));
            _melodyMotif.Add(currentDegree);
        }
    }

    /// <summary>
    /// 创建不协和动机
    /// </summary>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <param name="motifLength">动机长度</param>
    /// <remarks>
    /// 此方法创建一个不协和的旋律，通过不和谐的音程来表达情感。
    /// 不同音乐风格和情绪下，可能会有不同的不协和偏好。
    /// 例如，古典音乐在快乐或浪漫情绪下更倾向于使用不协和的旋律，而在悲伤或平静情绪下更保守。
    /// </remarks>
    private void CreateDissonantMotif(int scaleSize, int motifLength)
    {
        int currentDegree = 0;
        _melodyMotif.Add(currentDegree);

        for (int i = 1; i < motifLength; i++)
        {
            // 倾向于创建不协和音程（如三全音）
            int[] dissonantIntervals = [1, 4, -1, -4]; // 小二度、三全音及其下行
            int interval = dissonantIntervals[_random.Next(dissonantIntervals.Length)];
            currentDegree = Math.Max(0, Math.Min(currentDegree + interval, scaleSize - 1));
            _melodyMotif.Add(currentDegree);
        }
    }

    /// <summary>
    /// 创建流畅动机
    /// </summary>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <param name="motifLength">动机长度</param>
    /// <remarks>
    /// 此方法创建一个流畅的旋律，通过优美的曲线轮廓来表达情感。
    /// 不同音乐风格和情绪下，可能会有不同的流畅度和曲线形状。
    /// 例如，古典音乐在快乐或浪漫情绪下更倾向于使用流畅的旋律，而在悲伤或平静情绪下更保守。
    /// </remarks>
    private void CreateFlowingMotif(int scaleSize, int motifLength)
    {
        int currentDegree = 0;
        _melodyMotif.Add(currentDegree);

        // 创建优美的曲线轮廓
        int[] contourPattern = [0, 2, 1, 3, 1, 2, 0];
        for (int i = 1; i < motifLength; i++)
        {
            int patternIndex = i % contourPattern.Length;
            currentDegree = Math.Min(contourPattern[patternIndex], scaleSize - 1);
            _melodyMotif.Add(currentDegree);
        }
    }

    /// <summary>
    /// 基于动机获取音阶度数
    /// </summary>
    /// <param name="parameters">旋律参数</param>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <param name="melodicContour">旋律轮廓</param>
    /// <param name="phrasePosition">当前乐句位置</param>
    /// <param name="phraseLength">乐句长度</param>
    /// <returns>变化后的音阶度数</returns>
    /// <remarks>
    /// 此方法根据音乐风格和情绪调整基于动机的音阶度数。
    /// 不同音乐风格和情绪下，可能会有不同的变化策略。
    /// 例如，古典音乐在快乐或浪漫情绪下更倾向于使用原始动机，而在悲伤或平静情绪下更保守。
    /// </remarks>
    private int GetMotifBasedScaleDegree(MelodyParameters parameters, int scaleSize,
                                       List<int> melodicContour, int phrasePosition, int phraseLength)
    {
        int scaleDegree;

        // 尝试使用动机
        if (_melodyMotif.Count > 0)
        {
            // 找到在动机中的对应位置
            int motifIndex = _motifPosition % _melodyMotif.Count;
            int baseDegree = _melodyMotif[motifIndex];

            // 根据音乐风格和情绪调整动机使用的概率
            double useOriginalMotifProbability = GetMotifVariationProbability(parameters.Style, parameters.Emotion);

            // 根据乐句位置调整动机使用策略
            if (phrasePosition == 0 || phrasePosition == phraseLength - 1) // 乐句开始或结束
            {
                // 在乐句开始和结束处，更倾向于使用原始动机以保持结构感
                if (_random.NextDouble() < (useOriginalMotifProbability + 0.2))
                {
                    scaleDegree = baseDegree;
                }
                else
                {
                    // 对动机进行变化，但保持足够的稳定性
                    scaleDegree = VaryMotifDegreeWithPosition(baseDegree, scaleSize, parameters, phrasePosition, phraseLength);
                }
            }
            else if (phrasePosition == phraseLength / 2) // 乐句中点
            {
                // 在乐句中点，可以有更多变化
                if (_random.NextDouble() < (useOriginalMotifProbability - 0.1))
                {
                    scaleDegree = baseDegree;
                }
                else
                {
                    // 对动机进行更自由的变化
                    scaleDegree = VaryMotifDegreeWithPosition(baseDegree, scaleSize, parameters, phrasePosition, phraseLength);
                }
            }
            else // 其他位置
            {
                // 常规位置使用标准概率
                if (_random.NextDouble() < useOriginalMotifProbability)
                {
                    scaleDegree = baseDegree;
                }
                else
                {
                    // 对动机进行变化
                    scaleDegree = VaryMotifDegreeWithPosition(baseDegree, scaleSize, parameters, phrasePosition, phraseLength);
                }
            }
        }
        else
        {
            // 如果没有动机，使用旋律轮廓，但根据音乐风格调整
            int contourIndex = CalculateContourIndex(parameters, phrasePosition, phraseLength, melodicContour.Count);
            scaleDegree = melodicContour[contourIndex % melodicContour.Count];
        }

        // 根据乐句长度和位置进行微调
        if (phraseLength > 8 && phrasePosition % 4 == 0) // 长句中的强拍位置
        {
            // 在长句的强拍位置，使用更稳定的音
            scaleDegree = Math.Min(scaleDegree, scaleSize - 1);
            if (_random.NextDouble() < 0.3)
            {
                scaleDegree = Math.Max(0, scaleDegree - 1);
            }
        }

        return scaleDegree;
    }

    /// <summary>
    /// 根据音乐风格和情绪获取动机变化概率
    /// </summary>
    /// <param name="style">音乐风格</param>
    /// <param name="emotion">情绪</param>
    /// <returns>动机变化概率</returns>
    /// <remarks>
    /// 此方法根据音乐风格和情绪，返回动机变化的概率。
    /// 不同音乐风格和情绪下，可能会有不同的变化偏好。
    /// 例如，古典音乐在快乐或浪漫情绪下更倾向于使用原始动机，而在悲伤或平静情绪下更保守。
    /// </remarks>
    private double GetMotifVariationProbability(MusicStyle style, Emotion emotion)
    {
        // 不同音乐风格和情绪对动机变化的偏好不同
        return style switch
        {
            MusicStyle.Classical => emotion switch
            {
                Emotion.Happy or Emotion.Romantic => 0.6, // 古典音乐在快乐或浪漫情绪下更倾向于使用原始动机
                Emotion.Sad or Emotion.Calm => 0.7, // 悲伤或平静情绪下更保守
                Emotion.Mysterious => 0.4, // 神秘情绪下更多变化
                _ => 0.5, // 默认值
            },
            MusicStyle.Jazz => emotion switch
            {
                Emotion.Happy or Emotion.Energetic => 0.3, // 爵士乐在活跃情绪下变化更多
                _ => 0.4, // 爵士乐通常喜欢即兴变化
            },
            MusicStyle.Rock or MusicStyle.Pop => emotion switch
            {
                Emotion.Energetic => 0.4, // 有活力的流行/摇滚音乐变化稍多
                _ => 0.6, // 流行音乐通常保持动机的一致性
            },
            MusicStyle.Electronic => emotion switch
            {
                Emotion.Mysterious or Emotion.Energetic => 0.3, // 电子音乐在这些情绪下变化很多
                _ => 0.5, // 其他情绪
            },
            _ => 0.5,// 默认值
        };
    }

    /// <summary>
    /// 根据乐句位置和参数变化动机的音阶度数
    /// </summary>
    /// <param name="baseDegree">基础音阶度数</param>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <param name="parameters">旋律参数</param>
    /// <param name="phrasePosition">当前乐句位置</param>
    /// <param name="phraseLength">乐句长度</param>
    /// <returns>变化后的音阶度数</returns>
    /// <remarks>
    /// 此方法根据音乐风格和情绪调整基础音阶度数的变化方式。
    /// 不同音乐风格下，可能会有不同的变化策略。
    /// 例如，古典音乐在快乐或浪漫情绪下更倾向于使用原始动机，而在悲伤或平静情绪下更保守。
    /// </remarks>
    private int VaryMotifDegreeWithPosition(int baseDegree, int scaleSize, MelodyParameters parameters,
                                         int phrasePosition, int phraseLength)
    {
        // 使用parameters中的style和emotion来调整变化方式
        switch (parameters.Style)
        {
            case MusicStyle.Classical:
                // 古典音乐风格下的动机变化
                if (_random.NextDouble() < 0.7)
                {
                    // 更倾向于使用Emotion参数来指导变化
                    return VaryMotifDegree(baseDegree, scaleSize, parameters.Emotion);
                }
                else
                {
                    // 根据乐句位置进行变化
                    if (phrasePosition == phraseLength / 2) // 乐句中点
                    {
                        return Math.Min(baseDegree + 2, scaleSize - 1); // 向上进行变化
                    }
                    break;
                }
            case MusicStyle.Jazz:
                // 爵士风格下更多的即兴变化
                return JazzifyMotif(baseDegree, scaleSize);
            case MusicStyle.Rock or MusicStyle.Pop:
                // 流行/摇滚风格下的动机变化
                return PopRockifyMotif(baseDegree, scaleSize, parameters.Emotion);
            case MusicStyle.Electronic:
                // 电子风格下的动机变化
                return ElectronifyMotif(baseDegree, scaleSize, phrasePosition);
        }

        // 默认情况下使用基本的动机变化
        return VaryMotifDegree(baseDegree, scaleSize, parameters.Emotion);
    }

    /// <summary>
    /// 计算旋律轮廓索引，根据参数进行调整
    /// </summary>
    /// <param name="parameters">旋律参数</param>
    /// <param name="phrasePosition">当前乐句位置</param>
    /// <param name="phraseLength">乐句长度</param>
    /// <param name="contourLength">旋律轮廓长度</param>
    /// <returns>变化后的旋律轮廓索引</returns>
    /// <remarks>
    /// 此方法根据音乐风格和情绪调整旋律轮廓索引的计算方式。
    /// 不同音乐风格下，可能会有不同的轮廓选择策略。
    /// 例如，爵士风格下更自由的轮廓选择，而古典风格下更结构化的轮廓选择。
    /// </remarks>
    private int CalculateContourIndex(MelodyParameters parameters, int phrasePosition, int phraseLength, int contourLength)
    {
        // 根据音乐风格和情绪调整轮廓索引的计算方式
        return parameters.Style switch
        {
            MusicStyle.Jazz => phrasePosition % contourLength,// 爵士风格下更自由的轮廓选择
            MusicStyle.Classical => (phrasePosition * contourLength) / phraseLength,// 古典风格下更结构化的轮廓选择
            _ => (phrasePosition * contourLength) / phraseLength,// 其他风格使用标准计算
        };
    }

    /// <summary>
    /// 爵士风格的动机变化
    /// </summary>
    /// <param name="baseDegree">基础音阶度数</param>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <returns>变化后的音阶度数</returns>
    /// <remarks>
    /// 此方法根据基础音阶度数和音阶大小，使用 jazzVariations 数组中的随机变化来调整动机。
    /// jazzVariations 包含了常见的 jazz 变化，例如大三度、小三度、大三度等。
    /// 变化后的音阶度数确保在音阶范围内，不会超出范围。
    /// </remarks>
    private int JazzifyMotif(int baseDegree, int scaleSize)
    {
        // 爵士乐风格的变化，可能包含更自由的音程
        int[] jazzVariations = [-2, -1, 1, 2, 3, -3]; // 包括大三度等变化
        int variation = jazzVariations[_random.Next(jazzVariations.Length)];
        return Math.Max(0, Math.Min(baseDegree + variation, scaleSize - 1));
    }

    /// <summary>
    /// 流行/摇滚风格的动机变化
    /// </summary>
    /// <param name="baseDegree">基础音阶度数</param>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <param name="emotion">当前情绪类型</param>
    /// <returns>变化后的音阶度数</returns>
    /// <remarks>
    /// 此方法根据当前情绪类型对基础音阶度数进行微调，可能会调整音高、力度或其他属性。
    /// 不同情绪类型可能会导致不同的变化效果，例如快乐可能会增加音高，悲伤可能会降低音高。
    /// </remarks>
    private int PopRockifyMotif(int baseDegree, int scaleSize, Emotion emotion)
    {
        // 根据情绪调整流行/摇滚风格的动机变化
        if (emotion == Emotion.Energetic)
        {
            // 有活力时使用更大的跳跃
            int[] energeticVariations = [-3, 3];
            int variation = energeticVariations[_random.Next(energeticVariations.Length)];
            return Math.Max(0, Math.Min(baseDegree + variation, scaleSize - 1));
        }
        else
        {
            // 其他情绪使用更常规的变化
            int[] variations = [-1, 1];
            int variation = variations[_random.Next(variations.Length)];
            return Math.Max(0, Math.Min(baseDegree + variation, scaleSize - 1));
        }
    }

    /// <summary>
    /// 电子风格的动机变化
    /// </summary>
    /// <param name="baseDegree">基础音阶度数</param>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <param name="phrasePosition">当前乐句位置</param>
    /// <returns>变化后的音阶度数</returns>
    /// <remarks>
    /// 此方法根据当前乐句位置和基础音阶度数，使用不同的变化模式来调整动机。
    /// 在强拍（phrasePosition % 4 == 0）时，有60%的概率保持原始动机，30%的概率进行小变化（例如+1或-1）。
    /// 在弱拍时，使用更大的变化范围（例如+2、-2等）。
    /// 这种变化模式模拟了电子音乐中强拍和弱拍的不同变化方式。
    /// </remarks>
    private int ElectronifyMotif(int baseDegree, int scaleSize, int phrasePosition)
    {
        // 电子风格下，根据位置使用不同的变化模式
        if (phrasePosition % 4 == 0) // 在小节强拍
        {
            // 在强拍使用原始动机或小变化
            return _random.NextDouble() < 0.6 ? baseDegree : baseDegree + (_random.Next(2) * 2 - 1);
        }
        else
        {
            // 在弱拍使用更大的变化
            int[] variations = [-2, -1, 1, 2, 4, -4];
            int variation = variations[_random.Next(variations.Length)];
            return Math.Max(0, Math.Min(baseDegree + variation, scaleSize - 1));
        }
    }

    /// <summary>
    /// 变化动机的音阶度数
    /// </summary>
    /// <param name="baseDegree">基础音阶度数</param>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <param name="emotion">当前情绪类型</param>
    /// <returns>变化后的音阶度数</returns>
    /// <remarks>
    /// 此方法根据当前情绪类型对基础音阶度数进行微调，可能会调整音高、力度或其他属性。
    /// 不同情绪类型可能会导致不同的变化效果，例如快乐可能会增加音高，悲伤可能会降低音高。
    /// </remarks>
    private int VaryMotifDegree(int baseDegree, int scaleSize, Emotion emotion)
    {
        // 根据情绪选择变化类型
        switch (emotion)
        {
            case Emotion.Happy:
                // 向上变化
                return Math.Min(baseDegree + _random.Next(1, 3), scaleSize - 1);
            case Emotion.Sad:
                // 向下变化
                return Math.Max(baseDegree - _random.Next(1, 3), 0);
            case Emotion.Energetic:
                // 大跳变化
                int[] energeticVariations = [-2, -1, 1, 2];
                return Math.Max(0, Math.Min(baseDegree + energeticVariations[_random.Next(energeticVariations.Length)], scaleSize - 1));
            case Emotion.Mysterious:
                // 不协和变化
                int[] mysteriousVariations = [-1, 1, -3, 3];
                return Math.Max(0, Math.Min(baseDegree + mysteriousVariations[_random.Next(mysteriousVariations.Length)], scaleSize - 1));
            default:
                // 轻微变化
                int[] variations = [-1, 0, 1];
                return Math.Max(0, Math.Min(baseDegree + variations[_random.Next(variations.Length)], scaleSize - 1));
        }
    }

    /// <summary>
    /// 添加音乐化的装饰
    /// </summary>
    /// <param name="scaleDegree">当前音阶度数</param>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <param name="phrasePosition">当前短语位置（从0开始）</param>
    /// <param name="phraseLength">当前短语总长度</param>
    /// <param name="parameters">旋律参数（可选）</param>
    /// <returns>装饰后的音阶度数</returns>
    /// <remarks>
    /// 此方法根据当前音阶度数、音阶大小、短语位置和总长度，以及可选的旋律参数，添加音乐化的装饰音。
    /// 装饰音的选择基于音乐风格、情绪参数和随机概率，确保旋律的多样性和变化。
    /// 默认情况下，60%的概率添加装饰音。
    /// </remarks>
    private int AddMusicalOrnament(int scaleDegree, int scaleSize, int phrasePosition, int phraseLength, MelodyParameters? parameters = null)
    {
        // 优先使用传入的parameters，如果没有则使用_currentParameters
        var usedParameters = parameters ?? _currentParameters ?? new MelodyParameters(); // 确保不会为null

        // 根据音乐风格选择基础装饰音
        int decoratedDegree = scaleDegree;

        // 根据装饰音密度决定是否添加装饰音
        if (ShouldAddOrnament(usedParameters, phrasePosition, phraseLength))
        {
            MusicStyle style = usedParameters?.Style ?? MusicStyle.Pop;
            decoratedDegree = style switch
            {
                MusicStyle.Classical => AddClassicalOrnament(scaleDegree, scaleSize, phrasePosition, phraseLength),
                MusicStyle.Jazz => AddJazzOrnament(scaleDegree, scaleSize, phrasePosition, phraseLength),
                MusicStyle.Rock or MusicStyle.Pop => AddPopRockOrnament(scaleDegree, scaleSize, phrasePosition, phraseLength),
                MusicStyle.Electronic => AddElectronicOrnament(scaleDegree, scaleSize, phrasePosition, phraseLength),
                _ => AddGenericOrnament(scaleDegree, scaleSize, phrasePosition, phraseLength),// 默认装饰音
            };
        }

        // 根据情绪参数进行额外的装饰音调整
        return AdjustOrnamentByEmotion(decoratedDegree, scaleSize, usedParameters?.Emotion ?? Emotion.Happy);
    }

    /// <summary>
    /// 根据参数决定是否应该添加装饰音
    /// </summary>
    /// <param name="parameters">旋律参数（可选）</param>
    /// <param name="phrasePosition">当前短语位置（从0开始）</param>
    /// <param name="phraseLength">当前短语总长度</param>
    /// <returns>是否添加装饰音</returns>
    /// <remarks>
    /// 此方法根据旋律参数和当前短语位置判断是否应该添加装饰音。
    /// 考虑到风格、情绪和位置等因素，动态调整装饰音的添加概率。
    /// 默认情况下，60%的概率添加装饰音。
    /// </remarks>
    private bool ShouldAddOrnament(MelodyParameters? parameters, int phrasePosition, int phraseLength)
    {
        if (parameters == null)
            return _random.NextDouble() < 0.6; // 默认60%的概率添加装饰音

        // 计算基础装饰音概率
        double ornamentProbability; // 基础概率

        // 根据风格调整装饰音概率
        ornamentProbability = parameters.Style switch
        {
            MusicStyle.Classical => 0.7,// 古典音乐通常有较多装饰音
            MusicStyle.Jazz => 0.8,// 爵士乐有丰富的即兴装饰
            MusicStyle.Rock => 0.4,// 摇滚乐装饰音相对较少
            MusicStyle.Pop => 0.5,// 流行音乐适中的装饰音
            MusicStyle.Electronic => 0.3,// 电子音乐装饰音较少
            _ => 0.6,// 默认概率
        };

        // 根据情绪调整装饰音概率
        switch (parameters.Emotion)
        {
            case Emotion.Happy:
                ornamentProbability *= 1.1; // 略微增加装饰音
                break;
            case Emotion.Sad:
                ornamentProbability *= 0.8; // 略微减少装饰音
                break;
            case Emotion.Energetic:
                ornamentProbability *= 1.2; // 较多的装饰音
                break;
            case Emotion.Calm:
                ornamentProbability *= 0.7; // 较少的装饰音
                break;
            case Emotion.Mysterious:
                ornamentProbability *= 0.9; // 略微减少装饰音
                break;
        }

        // 根据位置进一步调整概率
        if (phrasePosition == 0 || phrasePosition == phraseLength - 1)
        {
            // 开头和结尾可能使用不同的装饰音概率
            ornamentProbability *= 1.2;
        }
        else if (phrasePosition % 4 == 0 && phraseLength > 4)
        {
            // 强拍位置可能使用更多的装饰音
            ornamentProbability *= 1.1;
        }

        // 确保概率在合理范围内
        ornamentProbability = Math.Max(0.1, Math.Min(0.95, ornamentProbability));

        return _random.NextDouble() < ornamentProbability;
    }

    /// <summary>
    /// 根据情绪参数对装饰音进行额外调整
    /// </summary>
    /// <param name="decoratedDegree">当前装饰音的音阶度数</param>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <param name="emotion">当前情绪类型</param>
    /// <returns>调整后的装饰音音阶度数</returns>
    /// <remarks>
    /// 此方法根据当前情绪类型对装饰音进行微调，可能会调整装饰音的音高、力度或其他属性。
    /// 不同情绪类型可能会导致不同的装饰音效果，例如快乐可能会增加音高，悲伤可能会降低音高。
    /// </remarks>
    private int AdjustOrnamentByEmotion(int decoratedDegree, int scaleSize, Emotion emotion)
    {
        // 根据情绪类型对装饰音进行微调
        switch (emotion)
        {
            case Emotion.Happy:
                // 可能略微升高音高
                if (_random.NextDouble() < 0.3)
                    return Math.Min(decoratedDegree + 1, scaleSize - 1);
                break;
            case Emotion.Sad:
                // 可能略微降低音高
                if (_random.NextDouble() < 0.3)
                    return Math.Max(decoratedDegree - 1, 0);
                break;
            case Emotion.Energetic:
                // 可能产生更大的变化
                if (_random.NextDouble() < 0.2)
                {
                    int variation = _random.Next(1, 3) * (_random.Next(2) == 0 ? 1 : -1);
                    return Math.Max(0, Math.Min(decoratedDegree + variation, scaleSize - 1));
                }
                break;
        }

        return decoratedDegree; // 默认返回原始装饰音
    }

    /// <summary>
    /// 添加古典音乐风格的装饰音
    /// </summary>
    /// <param name="scaleDegree">当前音阶度数</param>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <param name="phrasePosition">当前短语位置（从0开始）</param>
    /// <param name="phraseLength">当前短语总长度</param>
    /// <returns>添加的古典音乐风格装饰音的音阶度数</returns>
    /// <remarks>
    /// 此方法用于添加古典音乐风格的装饰音，包括颤音、 Mordent、_appoggiatura、经过音、邻音和辅助音。
    /// 装饰音的选择基于随机概率和当前短语位置，以确保旋律的多样性和变化。
    /// </remarks>
    private int AddClassicalOrnament(int scaleDegree, int scaleSize, int phrasePosition, int phraseLength)
    {
        // 根据乐句位置和长度调整装饰音概率
        double trillProb = 0.25;
        double mordentProb = 0.20;
        double appoggiaturaProb = 0.15;
        double passingToneProb = 0.15;
        double auxiliaryToneProb = 0.10;
        double noOrnamentProb = 0.15;

        // 根据乐句位置调整装饰音概率
        if (phrasePosition == 0) // 乐句开始位置
        {
            // 开始位置通常使用更简洁的装饰或不使用装饰
            trillProb = 0.10;
            mordentProb = 0.10;
            appoggiaturaProb = 0.05;
            passingToneProb = 0.25;
            auxiliaryToneProb = 0.10;
            noOrnamentProb = 0.40;
        }
        else if (phrasePosition == phraseLength - 1) // 乐句结尾位置
        {
            // 结尾位置可能使用颤音或波音来增强终止感
            trillProb = 0.35;
            mordentProb = 0.25;
            appoggiaturaProb = 0.10;
            passingToneProb = 0.05;
            auxiliaryToneProb = 0.05;
            noOrnamentProb = 0.20;
        }
        else if (phrasePosition == phraseLength / 2) // 乐句中点位置
        {
            // 中点位置可能使用倚音或颤音来增强表现力
            trillProb = 0.20;
            mordentProb = 0.15;
            appoggiaturaProb = 0.30;
            passingToneProb = 0.10;
            auxiliaryToneProb = 0.10;
            noOrnamentProb = 0.15;
        }
        else if (phrasePosition % 4 == 0 && phraseLength > 4) // 强拍位置
        {
            // 强拍位置可能使用更明显的装饰
            trillProb = 0.25;
            mordentProb = 0.20;
            appoggiaturaProb = 0.20;
            passingToneProb = 0.10;
            auxiliaryToneProb = 0.05;
            noOrnamentProb = 0.20;
        }
        else if (phrasePosition % 2 == 1) // 弱拍位置
        {
            // 弱拍位置可能使用辅助音或经过音
            trillProb = 0.15;
            mordentProb = 0.15;
            appoggiaturaProb = 0.10;
            passingToneProb = 0.20;
            auxiliaryToneProb = 0.25;
            noOrnamentProb = 0.15;
        }

        // 归一化概率，确保总和为1
        double totalProb = trillProb + mordentProb + appoggiaturaProb + passingToneProb + auxiliaryToneProb + noOrnamentProb;
        trillProb /= totalProb;
        mordentProb /= totalProb;
        appoggiaturaProb /= totalProb;
        passingToneProb /= totalProb;
        auxiliaryToneProb /= totalProb;
        noOrnamentProb /= totalProb;

        double rand = _random.NextDouble();

        // 根据调整后的概率选择装饰音
        if (rand < trillProb) // 颤音
            return AddTrill(scaleDegree, scaleSize);
        else if (rand < trillProb + mordentProb) // 波音
            return AddMordent(scaleDegree, scaleSize);
        else if (rand < trillProb + mordentProb + appoggiaturaProb) // 倚音
            return AddAppoggiatura(scaleDegree, scaleSize);
        else if (rand < trillProb + mordentProb + appoggiaturaProb + passingToneProb) // 经过音
            return AddPassingTone(scaleDegree, scaleSize);
        else if (rand < trillProb + mordentProb + appoggiaturaProb + passingToneProb + auxiliaryToneProb) // 辅助音
            return AddAuxiliaryTone(scaleDegree, scaleSize);
        else // 保持原音符
            return scaleDegree;
    }

    /// <summary>
    /// 添加爵士音乐风格的装饰音
    /// </summary>
    /// <param name="scaleDegree">当前音阶度数</param>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <param name="phrasePosition">当前短语位置（从0开始）</param>
    /// <param name="phraseLength">当前短语总长度</param>
    /// <returns>添加的爵士音乐风格装饰音的音阶度数</returns>
    /// <remarks>
    /// 此方法用于添加爵士音乐风格的装饰音，包括蓝调音、滑音、装饰性经过音和爵士颤音。
    /// 装饰音的选择基于随机概率和当前短语位置，以确保旋律的多样性和变化。
    /// </remarks>
    private int AddJazzOrnament(int scaleDegree, int scaleSize, int phrasePosition, int phraseLength)
    {
        // 根据乐句位置和长度调整装饰音概率
        double blueNoteProb = 0.25;
        double glissandoProb = 0.20;
        double jazzPassingToneProb = 0.20;
        double jazzTrillProb = 0.15;
        double noOrnamentProb = 0.20;

        // 根据乐句位置调整装饰音概率
        if (phrasePosition == 0) // 乐句开始位置
        {
            // 开始位置使用蓝调音或不使用装饰
            blueNoteProb = 0.30;
            glissandoProb = 0.15;
            jazzPassingToneProb = 0.10;
            jazzTrillProb = 0.10;
            noOrnamentProb = 0.35;
        }
        else if (phrasePosition == phraseLength - 1) // 乐句结尾位置
        {
            // 结尾位置可能使用滑音准备或蓝调音
            blueNoteProb = 0.20;
            glissandoProb = 0.30;
            jazzPassingToneProb = 0.15;
            jazzTrillProb = 0.15;
            noOrnamentProb = 0.20;
        }
        else if (phrasePosition == phraseLength / 2) // 乐句中点位置
        {
            // 中点位置可能使用装饰性经过音和爵士颤音
            blueNoteProb = 0.20;
            glissandoProb = 0.15;
            jazzPassingToneProb = 0.30;
            jazzTrillProb = 0.20;
            noOrnamentProb = 0.15;
        }
        else if (phrasePosition % 4 == 0 && phraseLength > 4) // 强拍位置
        {
            // 强拍位置可能使用蓝调音
            blueNoteProb = 0.30;
            glissandoProb = 0.15;
            jazzPassingToneProb = 0.15;
            jazzTrillProb = 0.15;
            noOrnamentProb = 0.25;
        }
        else if (phrasePosition % 2 == 1) // 弱拍位置
        {
            // 弱拍位置可能使用经过音和爵士颤音
            blueNoteProb = 0.15;
            glissandoProb = 0.15;
            jazzPassingToneProb = 0.25;
            jazzTrillProb = 0.25;
            noOrnamentProb = 0.20;
        }

        // 归一化概率，确保总和为1
        double totalProb = blueNoteProb + glissandoProb + jazzPassingToneProb + jazzTrillProb + noOrnamentProb;
        blueNoteProb /= totalProb;
        glissandoProb /= totalProb;
        jazzPassingToneProb /= totalProb;
        jazzTrillProb /= totalProb;
        noOrnamentProb /= totalProb;

        double rand = _random.NextDouble();

        // 根据调整后的概率选择装饰音
        if (rand < blueNoteProb) // 蓝色音符
            return AddBlueNote(scaleDegree, scaleSize);
        else if (rand < blueNoteProb + glissandoProb) // 滑音准备
            return AddGlissandoPreparation(scaleDegree, scaleSize);
        else if (rand < blueNoteProb + glissandoProb + jazzPassingToneProb) // 装饰性经过音
            return AddJazzPassingTone(scaleDegree, scaleSize);
        else if (rand < blueNoteProb + glissandoProb + jazzPassingToneProb + jazzTrillProb) // 颤音（爵士风格）
            return AddJazzTrill(scaleDegree, scaleSize);
        else // 保持原音符
            return scaleDegree;
    }

    /// <summary>
    /// 添加流行/摇滚风格的装饰音
    /// </summary>
    /// <param name="scaleDegree">当前音阶度数</param>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <param name="phrasePosition">当前短语位置（从0开始）</param>
    /// <param name="phraseLength">当前短语总长度</param>
    /// <returns>添加的流行/摇滚风格装饰音的音阶度数</returns>
    /// <remarks>
    /// 此方法用于添加流行/摇滚风格的装饰音，包括滑音、力量和弦、敲击音效果、快速经过音和高八度重复。
    /// 装饰音的选择基于随机概率和当前短语位置，以确保旋律的多样性和变化。
    /// </remarks>
    private int AddPopRockOrnament(int scaleDegree, int scaleSize, int phrasePosition, int phraseLength)
    {
        // 根据乐句位置和长度调整装饰音概率
        double slideProb = 0.20;
        double powerChordProb = 0.15;
        double percussiveProb = 0.15;
        double fastPassingToneProb = 0.15;
        double octaveRepeatProb = 0.10;
        double noOrnamentProb = 0.25;

        // 根据乐句位置调整装饰音概率
        if (phrasePosition == 0) // 乐句开始位置
        {
            // 开始位置可能使用力量和弦装饰或高八度重复
            slideProb = 0.15;
            powerChordProb = 0.25;
            percussiveProb = 0.10;
            fastPassingToneProb = 0.10;
            octaveRepeatProb = 0.15;
            noOrnamentProb = 0.25;
        }
        else if (phrasePosition == phraseLength - 1) // 乐句结尾位置
        {
            // 结尾位置可能使用滑音或高八度重复
            slideProb = 0.30;
            powerChordProb = 0.10;
            percussiveProb = 0.10;
            fastPassingToneProb = 0.10;
            octaveRepeatProb = 0.20;
            noOrnamentProb = 0.20;
        }
        else if (phrasePosition == phraseLength / 2) // 乐句中点位置
        {
            // 中点位置可能使用敲击音效果
            slideProb = 0.15;
            powerChordProb = 0.15;
            percussiveProb = 0.25;
            fastPassingToneProb = 0.15;
            octaveRepeatProb = 0.10;
            noOrnamentProb = 0.20;
        }
        else if (phrasePosition % 4 == 0 && phraseLength > 4) // 强拍位置
        {
            // 强拍位置可能使用力量和弦装饰
            slideProb = 0.15;
            powerChordProb = 0.25;
            percussiveProb = 0.15;
            fastPassingToneProb = 0.10;
            octaveRepeatProb = 0.10;
            noOrnamentProb = 0.25;
        }
        else if (phrasePosition % 2 == 1) // 弱拍位置
        {
            // 弱拍位置可能使用快速经过音或滑音
            slideProb = 0.20;
            powerChordProb = 0.10;
            percussiveProb = 0.15;
            fastPassingToneProb = 0.25;
            octaveRepeatProb = 0.10;
            noOrnamentProb = 0.20;
        }

        // 归一化概率，确保总和为1
        double totalProb = slideProb + powerChordProb + percussiveProb + fastPassingToneProb + octaveRepeatProb + noOrnamentProb;
        slideProb /= totalProb;
        powerChordProb /= totalProb;
        percussiveProb /= totalProb;
        fastPassingToneProb /= totalProb;
        octaveRepeatProb /= totalProb;
        noOrnamentProb /= totalProb;

        double rand = _random.NextDouble();

        // 根据调整后的概率选择装饰音
        if (rand < slideProb) // 滑音
            return AddSlide(scaleDegree, scaleSize);
        else if (rand < slideProb + powerChordProb) // 力量和弦装饰
            return AddPowerChordOrnament(scaleDegree, scaleSize);
        else if (rand < slideProb + powerChordProb + percussiveProb) // 敲击音效果
            return AddPercussiveOrnament(scaleDegree, scaleSize);
        else if (rand < slideProb + powerChordProb + percussiveProb + fastPassingToneProb) // 快速经过音
            return AddFastPassingTone(scaleDegree, scaleSize);
        else if (rand < slideProb + powerChordProb + percussiveProb + fastPassingToneProb + octaveRepeatProb) // 高八度重复
            return AddOctaveRepeat(scaleDegree, scaleSize);
        else // 保持原音符
            return scaleDegree;
    }

    /// <summary>
    /// 添加电子音乐风格的装饰音
    /// </summary>
    /// <param name="scaleDegree">当前音阶度数</param>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <param name="phrasePosition">当前短语位置（从0开始）</param>
    /// <param name="phraseLength">当前短语总长度</param>
    /// <returns>添加的电子音乐风格装饰音的音阶度数</returns>
    /// <remarks>
    /// 此方法用于添加电子音乐风格的装饰音，包括琶音器效果、合成器风格装饰、跳跃音和重复装饰。
    /// 装饰音的选择基于随机概率和当前短语位置，以确保旋律的多样性和变化。
    /// </remarks>
    private int AddElectronicOrnament(int scaleDegree, int scaleSize, int phrasePosition, int phraseLength)
    {
        // 根据乐句位置和长度调整装饰音概率
        double arpeggiatorProb = 0.25;
        double synthProb = 0.20;
        double jumpProb = 0.15;
        double repeatProb = 0.15;
        double noOrnamentProb = 0.25;

        // 根据乐句位置调整装饰音概率
        if (phrasePosition == 0) // 乐句开始位置
        {
            // 开始位置可能使用琶音器效果或合成器风格装饰
            arpeggiatorProb = 0.30;
            synthProb = 0.25;
            jumpProb = 0.10;
            repeatProb = 0.10;
            noOrnamentProb = 0.25;
        }
        else if (phrasePosition == phraseLength - 1) // 乐句结尾位置
        {
            // 结尾位置可能使用重复装饰或跳跃音
            arpeggiatorProb = 0.15;
            synthProb = 0.15;
            jumpProb = 0.25;
            repeatProb = 0.25;
            noOrnamentProb = 0.20;
        }
        else if (phrasePosition == phraseLength / 2) // 乐句中点位置
        {
            // 中点位置可能使用跳跃音或合成器风格装饰
            arpeggiatorProb = 0.20;
            synthProb = 0.25;
            jumpProb = 0.25;
            repeatProb = 0.15;
            noOrnamentProb = 0.15;
        }
        else if (phrasePosition % 4 == 0 && phraseLength > 4) // 强拍位置
        {
            // 强拍位置可能使用琶音器效果
            arpeggiatorProb = 0.30;
            synthProb = 0.20;
            jumpProb = 0.15;
            repeatProb = 0.10;
            noOrnamentProb = 0.25;
        }
        else if (phrasePosition % 2 == 1) // 弱拍位置
        {
            // 弱拍位置可能使用跳跃音或重复装饰
            arpeggiatorProb = 0.15;
            synthProb = 0.15;
            jumpProb = 0.20;
            repeatProb = 0.25;
            noOrnamentProb = 0.25;
        }

        // 归一化概率，确保总和为1
        double totalProb = arpeggiatorProb + synthProb + jumpProb + repeatProb + noOrnamentProb;
        arpeggiatorProb /= totalProb;
        synthProb /= totalProb;
        jumpProb /= totalProb;
        repeatProb /= totalProb;
        noOrnamentProb /= totalProb;

        double rand = _random.NextDouble();

        // 根据调整后的概率选择装饰音
        if (rand < arpeggiatorProb) // 琶音器效果准备
            return AddArpeggiatorEffect(scaleDegree, scaleSize);
        else if (rand < arpeggiatorProb + synthProb) // 合成器风格装饰
            return AddSynthOrnament(scaleDegree, scaleSize);
        else if (rand < arpeggiatorProb + synthProb + jumpProb) // 跳跃音（电子风格）
            return AddJumpOrnament(scaleDegree, scaleSize);
        else if (rand < arpeggiatorProb + synthProb + jumpProb + repeatProb) // 重复装饰
            return AddRepeatOrnament(scaleDegree, scaleSize);
        else // 保持原音符
            return scaleDegree;
    }

    /// <summary>
    /// 添加通用装饰音
    /// </summary>
    /// <param name="scaleDegree">当前音阶度数</param>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <param name="phrasePosition">当前短语位置（从0开始）</param>
    /// <param name="phraseLength">当前短语总长度</param>
    /// <returns>添加的通用装饰音的音阶度数</returns>
    /// <remarks>
    /// 此方法用于添加通用装饰音，包括经过音、邻音、辅助音和保持原音符。
    /// 装饰音的选择基于随机概率和当前短语位置，以确保旋律的多样性和变化。
    /// </remarks>
    private int AddGenericOrnament(int scaleDegree, int scaleSize, int phrasePosition, int phraseLength)
    {
        // 决定使用哪种装饰
        int ornamentType = _random.Next(4); // 0: 经过音, 1: 邻音, 2: 辅助音, 3: 保持

        switch (ornamentType)
        {
            case 0: // 经过音
                return AddPassingTone(scaleDegree, scaleSize);
            case 1: // 邻音（上方或下方）
                return AddNeighborTone(scaleDegree, scaleSize);
            case 2: // 辅助音（通常在强拍后）
                if (phrasePosition % 2 == 1) // 弱拍位置
                    return AddAuxiliaryTone(scaleDegree, scaleSize);
                break;
        }

        return scaleDegree; // 保持原音符
    }

    // 古典音乐装饰音方法

    /// <summary>
    /// 添加颤音（古典音乐）
    /// </summary>
    /// <param name="scaleDegree">当前音阶度数</param>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <returns>添加的颤音（古典音乐）的音阶度数</returns>
    /// <remarks>
    /// 此方法用于添加颤音，通常是与上方大二度或小三度音快速交替。
    /// 这在古典音乐中常用于创建颤音的旋律过渡。
    /// </remarks>
    private int AddTrill(int scaleDegree, int scaleSize)
    {
        // 颤音通常是与上方大二度或小三度音快速交替
        int trillDegree = scaleDegree + (_random.NextDouble() < 0.7 ? 1 : 2); // 70%是大二度

        if (trillDegree < scaleSize)
        {
            // 记录颤音信息，供后续音符生成使用
            _ornamentType = OrnamentType.Trill;
            _trillTargetDegree = trillDegree;
        }

        return scaleDegree; // 返回基础音，颤音效果将在音符生成时处理
    }

    /// <summary>
    /// 添加波音（古典音乐）
    /// </summary>
    /// <param name="scaleDegree">当前音阶度数</param>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <returns>添加的波音（古典音乐）的音阶度数</returns>
    /// <remarks>
    /// 此方法用于添加波音，通常是先到邻音再回到原音。
    /// 这在古典音乐中常用于创建波音的旋律过渡。
    /// </remarks>
    private int AddMordent(int scaleDegree, int scaleSize)
    {
        // 波音是先到邻音再回到原音
        int mordentDegree = scaleDegree + (_random.NextDouble() < 0.7 ? 1 : -1); // 70%是上波音

        if (mordentDegree >= 0 && mordentDegree < scaleSize)
        {
            _ornamentType = OrnamentType.Mordent;
            _mordentTargetDegree = mordentDegree;
        }

        return scaleDegree;
    }

    /// <summary>
    /// 添加倚音（古典音乐）
    /// </summary>
    /// <param name="scaleDegree">当前音阶度数</param>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <returns>添加的倚音（古典音乐）的音阶度数</returns>
    /// <remarks>
    /// 此方法用于添加倚音，通常是上方小二度或大二度。
    /// 这在古典音乐中常用于创建倚音的旋律过渡。
    /// </remarks>
    private int AddAppoggiatura(int scaleDegree, int scaleSize)
    {
        // 倚音通常是上方小二度或大二度
        int appoggiaturaDegree = scaleDegree + (_random.NextDouble() < 0.6 ? 1 : 2); // 60%是大二度

        if (appoggiaturaDegree < scaleSize)
        {
            _ornamentType = OrnamentType.Appoggiatura;
            _appoggiaturaTargetDegree = appoggiaturaDegree;
        }

        return appoggiaturaDegree; // 倚音先发声，然后解决到原音
    }

    // 爵士音乐装饰音方法

    /// <summary>
    /// 添加蓝色音符（爵士音乐特色）
    /// </summary>
    /// <param name="scaleDegree">当前音阶度数</param>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <returns>添加的蓝色音符（爵士音乐特色）的音阶度数</returns>
    /// <remarks>
    /// 此方法用于添加蓝色音符，通常是降低的3rd、5th或7th音。
    /// 这在爵士音乐中常用于创建蓝色音符的模式或循环结构。
    /// </remarks>
    private int AddBlueNote(int scaleDegree, int scaleSize)
    {
        // 蓝色音符通常是降低的3rd、5th或7th音
        int[] blueNotePositions = [2, 4, 6]; // 通常是3rd, 5th, 7th音阶度数

        if (blueNotePositions.Contains(scaleDegree % 7))
        {
            _ornamentType = OrnamentType.BlueNote;
            // 蓝色音符效果将在音符生成时通过微调音高实现
        }

        return scaleDegree;
    }

    /// <summary>
    /// 添加滑音准备（爵士风格）
    /// </summary>
    /// <param name="scaleDegree">当前音阶度数</param>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <returns>添加的滑音准备（爵士风格）的音阶度数</returns>
    /// <remarks>
    /// 此方法用于为滑音做准备，选择起始音。
    /// 滑音通常是从当前音符滑到上方或下方的音符。
    /// </remarks>
    private int AddGlissandoPreparation(int scaleDegree, int scaleSize)
    {
        // 为滑音做准备，选择起始音
        int glissStartDegree = scaleDegree + (_random.NextDouble() < 0.5 ? -2 : 2); // 向上或向下滑音

        if (glissStartDegree >= 0 && glissStartDegree < scaleSize)
        {
            _ornamentType = OrnamentType.Glissando;
            _glissandoStartDegree = glissStartDegree;
            return glissStartDegree;
        }

        return scaleDegree;
    }

    /// <summary>
    /// 添加爵士风格的经过音
    /// </summary>
    /// <param name="scaleDegree">当前音阶度数</param>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <returns>添加的爵士风格的经过音的音阶度数</returns>
    /// <remarks>
    /// 此方法用于添加爵士风格的经过音，通常是从当前音符跳转到上方或下方的音符。
    /// 这在爵士音乐中常用于创建快速的旋律过渡。
    /// </remarks>
    private int AddJazzPassingTone(int scaleDegree, int scaleSize)
    {
        // 爵士经过音可以包含和弦外音和半音
        int direction = _random.NextDouble() < 0.5 ? 1 : -1;
        int passingDegree = scaleDegree + direction;

        if (passingDegree >= 0 && passingDegree < scaleSize)
            return passingDegree;

        return scaleDegree;
    }

    /// <summary>
    /// 添加爵士风格的颤音
    /// </summary>
    /// <param name="scaleDegree">当前音阶度数</param>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <returns>添加的爵士风格的颤音的音阶度数</returns>
    /// <remarks>
    /// 此方法用于添加爵士风格的颤音，通常是在当前音符基础上跳跃一定的音程。
    /// 这在爵士音乐中常用于创建颤音的旋律过渡。
    /// </remarks>
    private int AddJazzTrill(int scaleDegree, int scaleSize)
    {
        // 爵士颤音可以使用更自由的音程
        int[] jazzTrillIntervals = [1, 2, 3]; // 大二度、小三度、纯四度
        int interval = jazzTrillIntervals[_random.Next(jazzTrillIntervals.Length)];
        int direction = _random.NextDouble() < 0.7 ? 1 : -1;

        int trillDegree = scaleDegree + (interval * direction);

        if (trillDegree >= 0 && trillDegree < scaleSize)
        {
            _ornamentType = OrnamentType.JazzTrill;
            _trillTargetDegree = trillDegree;
        }

        return scaleDegree;
    }

    // 流行/摇滚装饰音方法

    /// <summary>
    /// 添加滑音（摇滚/流行）
    /// </summary>
    /// <param name="scaleDegree">当前音阶度数</param>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <returns>添加的滑音（摇滚/流行）的音阶度数</returns>
    /// <remarks>
    /// 此方法用于添加滑音，通常是从较低音滑到目标音。
    /// 这在摇滚音乐中常用于创建滑音的旋律过渡。
    /// </remarks>
    private int AddSlide(int scaleDegree, int scaleSize)
    {
        // 摇滚滑音通常是从较低音滑到目标音
        int slideStartDegree = Math.Max(0, scaleDegree - (_random.Next(3) + 1)); // 向下1-3个音

        _ornamentType = OrnamentType.Slide;
        _glissandoStartDegree = slideStartDegree;

        return slideStartDegree;
    }

    /// <summary>
    /// 添加力量和弦装饰（摇滚特色）
    /// </summary>
    /// <param name="scaleDegree">当前音阶度数</param>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <returns>添加的力量和弦装饰（摇滚特色）的音阶度数</returns>
    /// <remarks>
    /// 此方法用于添加力量和弦装饰，通常包含根音和五度音。
    /// 这在摇滚音乐中常用于创建力量和弦的模式或循环结构。
    /// </remarks>
    private int AddPowerChordOrnament(int scaleDegree, int scaleSize)
    {
        // 力量和弦通常包含根音和五度音
        int fifthDegree = scaleDegree + 4; // 纯五度

        if (fifthDegree < scaleSize)
        {
            _ornamentType = OrnamentType.PowerChord;
            _powerChordFifthDegree = fifthDegree;
        }

        return scaleDegree;
    }

    /// <summary>
    /// 添加敲击音效果（摇滚特色）
    /// </summary>
    /// <param name="scaleDegree">当前音阶度数</param>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <returns>添加的敲击音效果（摇滚特色）的音阶度数</returns>
    /// <remarks>
    /// 此方法用于添加敲击音效果，通常通过力度变化实现。
    /// 这在摇滚音乐中常用于创建敲击的旋律过渡。
    /// </remarks>
    private int AddPercussiveOrnament(int scaleDegree, int scaleSize)
    {
        // 敲击音效果通常通过力度变化实现
        _ornamentType = OrnamentType.Percussive;
        return scaleDegree;
    }

    /// <summary>
    /// 添加快速经过音（摇滚风格）
    /// </summary>
    /// <param name="scaleDegree">当前音阶度数</param>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <returns>添加的快速经过音（摇滚风格）的音阶度数</returns>
    /// <remarks>
    /// 此方法用于添加快速经过音，通常是级进或级退。
    /// 这在摇滚音乐中常用于创建快速的旋律过渡。
    /// </remarks>
    private int AddFastPassingTone(int scaleDegree, int scaleSize)
    {
        // 快速经过音，通常是级进
        int direction = _random.NextDouble() < 0.5 ? 1 : -1;
        int passingDegree = scaleDegree + direction;

        if (passingDegree >= 0 && passingDegree < scaleSize)
            return passingDegree;

        return scaleDegree;
    }

    /// <summary>
    /// 添加高八度重复（流行效果）
    /// </summary>
    /// <param name="scaleDegree">当前音阶度数</param>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <returns>添加的高八度重复（流行效果）的音阶度数</returns>
    /// <remarks>
    /// 此方法用于添加高八度重复，通常是在当前音符基础上跳跃一定的音程。
    /// 这在流行音乐中常用于创建高八度重复的模式或循环结构。
    /// </remarks>
    private int AddOctaveRepeat(int scaleDegree, int scaleSize)
    {
        _ornamentType = OrnamentType.OctaveRepeat;
        return scaleDegree; // 效果将在音符生成时处理
    }

    // 电子音乐装饰音方法

    /// <summary>
    /// 添加琶音器效果准备（电子音乐）
    /// </summary>
    /// <param name="scaleDegree">当前音阶度数</param>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <returns>添加的琶音器效果准备（电子音乐）的音阶度数</returns>
    /// <remarks>
    /// 此方法用于添加琶音器效果准备，通常是在当前音符基础上跳跃一定的音程。
    /// 这在电子音乐中常用于创建琶音器效果的模式或循环结构。
    /// </remarks>
    private int AddArpeggiatorEffect(int scaleDegree, int scaleSize)
    {
        _ornamentType = OrnamentType.Arpeggiator;
        _arpeggiatorStartDegree = scaleDegree;
        return scaleDegree;
    }

    /// <summary>
    /// 添加合成器风格装饰音（电子音乐）
    /// </summary>
    /// <param name="scaleDegree">当前音阶度数</param>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <returns>添加的合成器风格装饰音（电子音乐）的音阶度数</returns>
    /// <remarks>
    /// 此方法用于添加合成器风格的装饰音，通常是在当前音符基础上跳跃一定的音程。
    /// 这在电子音乐中常用于创建合成器效果的模式或循环结构。
    /// </remarks>
    private int AddSynthOrnament(int scaleDegree, int scaleSize)
    {
        // 合成器装饰音可以是跳跃的、不寻常的音程
        int[] synthIntervals = [3, 4, 5, 7]; // 纯四度、纯五度、大六度等
        int interval = synthIntervals[_random.Next(synthIntervals.Length)];
        int direction = _random.NextDouble() < 0.5 ? 1 : -1;

        int synthDegree = scaleDegree + (interval * direction);

        if (synthDegree >= 0 && synthDegree < scaleSize)
        {
            _ornamentType = OrnamentType.Synth;
            return synthDegree;
        }

        return scaleDegree;
    }

    /// <summary>
    /// 添加跳跃装饰音（电子风格）
    /// </summary>
    /// <param name="scaleDegree">当前音阶度数</param>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <returns>添加的跳跃装饰音（电子风格）的音阶度数</returns>
    /// <remarks>
    /// 此方法用于添加跳跃装饰音，通常是在当前音符基础上跳跃一定的音程。
    /// 这在电子音乐中常用于创建跳跃的模式或循环结构。
    /// </remarks>
    private int AddJumpOrnament(int scaleDegree, int scaleSize)
    {
        // 大幅度跳跃的装饰音
        int jumpInterval = _random.Next(2) + 3; // 3-4度跳跃
        int direction = _random.NextDouble() < 0.5 ? 1 : -1;

        int jumpDegree = scaleDegree + (jumpInterval * direction);

        if (jumpDegree >= 0 && jumpDegree < scaleSize)
            return jumpDegree;

        return scaleDegree;
    }

    /// <summary>
    /// 添加重复装饰（电子风格）
    /// </summary>
    /// <param name="scaleDegree">当前音阶度数</param>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <returns>添加的重复装饰（电子风格）的音阶度数</returns>
    /// <remarks>
    /// 此方法用于添加重复装饰音，通常是在当前音符基础上重复播放相同的音符。
    /// 这在电子音乐中常用于创建重复的模式或循环结构。
    /// </remarks>
    private int AddRepeatOrnament(int scaleDegree, int scaleSize)
    {
        _ornamentType = OrnamentType.Repeat;
        return scaleDegree;
    }

    // 装饰音类型枚举
    /// <summary>
    /// 装饰音类型枚举
    /// </summary>
    private enum OrnamentType
    {
        /// <summary>
        /// 无装饰音
        /// </summary>
        None,
        /// <summary>
        /// 颤音（重复音）
        /// </summary>
        Trill,
        /// <summary>
        ///  Mordent（上斜音）
        /// </summary>
        Mordent,
        /// <summary>
        ///  Appoggiatura（下斜音）
        /// </summary>
        Appoggiatura,
        /// <summary>
        ///  BlueNote（蓝色音符）
        /// </summary>
        BlueNote,
        /// <summary>
        ///  Glissando（滑动音）
        /// </summary>
        Glissando,
        /// <summary>
        ///  Slide（滑动音）
        /// </summary>
        Slide,
        /// <summary>
        ///  PowerChord（功率和弦）
        /// </summary>
        PowerChord,
        /// <summary>
        ///  Percussive（ Percussive（打击音））
        /// </summary>
        Percussive,
        /// <summary>
        ///  OctaveRepeat（高八度重复）
        /// </summary>
        OctaveRepeat,
        /// <summary>
        ///  Arpeggiator（琶音器）
        /// </summary>
        Arpeggiator,
        /// <summary>
        ///  Synth（合成器）
        /// </summary>
        Synth,
        /// <summary>
        ///  Repeat（重复）
        /// </summary>
        Repeat,
        /// <summary>
        ///  JazzTrill（爵士颤音）
        /// </summary>
        JazzTrill
    }

    // 装饰音相关字段
    /// <summary>
    /// 颤音目标度数
    /// </summary>
    private int _trillTargetDegree = -1;
    /// <summary>
    /// Mordent目标度数
    /// </summary>
    private int _mordentTargetDegree = -1;
    /// <summary>
    /// Appoggiatura目标度数
    /// </summary>
    private int _appoggiaturaTargetDegree = -1;
    /// <summary>
    /// Glissando起始度数
    /// </summary>
    private int _glissandoStartDegree = -1;
    /// <summary>
    /// PowerChord第五度
    /// </summary>
    private int _powerChordFifthDegree = -1;
    /// <summary>
    /// Arpeggiator起始度数
    /// </summary>
    private int _arpeggiatorStartDegree = -1;

    /// <summary>
    /// 添加邻音
    /// </summary>
    /// <param name="currentDegree">当前音阶度数</param>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <returns>添加的邻音的音阶度数</returns>
    /// <remarks>
    /// 此方法用于添加当前音阶度数的邻音，确保在音阶范围内。
    /// 邻音可以是当前度数的上一个或下一个度数，具体取决于随机选择的方向。
    /// </remarks>
    private int AddNeighborTone(int currentDegree, int scaleSize)
    {
        int[] neighborDirections = [-1, 1]; // 上邻音或下邻音
        int direction = neighborDirections[_random.Next(neighborDirections.Length)];
        int newDegree = currentDegree + direction;

        // 确保在音阶范围内
        if (newDegree >= 0 && newDegree < scaleSize)
            return newDegree;

        return currentDegree;
    }

    /// <summary>
    /// 添加辅助音
    /// </summary>
    /// <param name="currentDegree">当前音阶度数</param>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <returns>添加的辅助音的音阶度数</returns>
    /// <remarks>
    /// 此方法用于添加当前音阶度数的辅助音，确保在音阶范围内。
    /// 辅助音可以是当前度数的上方或下方的装饰音，具体取决于随机选择的方向。
    /// 辅助音的音程通常是大二度或大三度，以增加音乐的丰富性。
    /// </remarks>
    private int AddAuxiliaryTone(int currentDegree, int scaleSize)
    {
        // 通常是上方或下方的装饰音
        int[] auxiliaryDirections = [-1, 1];
        int direction = auxiliaryDirections[_random.Next(auxiliaryDirections.Length)];

        // 可以是小音程或较大音程
        int interval = _random.Next(100) < 70 ? 1 : 2; // 70%概率是大二度
        int newDegree = currentDegree + (direction * interval);

        // 确保在音阶范围内
        if (newDegree >= 0 && newDegree < scaleSize)
            return newDegree;

        return currentDegree;
    }

    /// <summary>
    /// 确保旋律连贯性，避免过大的跳跃，实现平滑过渡
    /// </summary>
    /// <param name="scaleDegree">当前音阶度数</param>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <param name="parameters">旋律参数，包含音乐风格等信息</param>
    /// <returns>确保旋律连贯性后的音阶度数</returns>
    /// <remarks>
    /// 此方法用于确保生成的旋律在音乐理论上是连贯的，避免过大的跳跃音程。
    /// 它根据音乐风格和上一个音符的音高，计算当前音符的音程。
    /// 如果音程超过最大允许跳跃，根据音乐理论的原则，调整音程为更平滑的过渡。
    /// </remarks>    
    private int EnsureMelodicCoherence(int scaleDegree, int scaleSize, MelodyParameters parameters)
    {
        if (_previousNotes.Count == 0)
            return scaleDegree;

        var lastNote = _previousNotes.Last();
        int lastScaleDegree = GetScaleDegreeFromNote(lastNote.Note, parameters.Scale);

        // 计算音程
        int interval = Math.Abs(scaleDegree - lastScaleDegree);
        int direction = scaleDegree > lastScaleDegree ? 1 : -1;

        // 根据音乐风格确定最大允许跳跃
        int maxAllowedInterval = GetMaxAllowedInterval(parameters.Style);

        // 如果音程太大，调整为更连贯的音程
        if (interval > maxAllowedInterval)
        {
            // 90%概率缩小音程
            if (_random.NextDouble() < 0.9)
            {
                // 基于音乐理论的平滑过渡策略，优化了计算性能
        scaleDegree = ApplySmoothTransitionOptimized(lastScaleDegree, scaleDegree, scaleSize, parameters, direction);

                // 重新计算音程
                interval = Math.Abs(scaleDegree - lastScaleDegree);
            }
        }

        // 优化的不和谐跳跃检测，减少计算量
        if (interval == 1 && ((lastScaleDegree & 1) != (scaleDegree & 1)))
        {
            // 小二度跳跃通常在强拍上不和谐
            if (_random.NextDouble() < 0.6)
            {
                scaleDegree = lastScaleDegree + (direction * 2); // 改为大二度
            }
        }

        // 确保音符在范围内
        return scaleDegree < 0 ? 0 : (scaleDegree >= scaleSize ? scaleSize - 1 : scaleDegree);
    }
    
    /// <summary>
    /// 优化版的平滑过渡算法，提高性能
    /// </summary>
    /// <param name="lastDegree">上一个音阶度数</param>
    /// <param name="targetDegree">目标音阶度数</param>
    /// <param name="scaleSize">音阶大小</param>
    /// <param name="parameters">旋律参数</param>
    /// <param name="direction">方向（1=上升，-1=下降）</param>
    /// <returns>优化后的音阶度数</returns>
    private int ApplySmoothTransitionOptimized(int lastDegree, int targetDegree, int scaleSize, MelodyParameters parameters, int direction)
    {
        // 快速路径：如果音程已经很小，直接返回
        int rawInterval = Math.Abs(targetDegree - lastDegree);
        if (rawInterval <= 2) // 大二度及以下直接接受
            return targetDegree;
        
        // 使用查表法快速确定最优过渡
        // 基于音乐理论的常用过渡模式缓存
        var interval = rawInterval % scaleSize;
        int optimizedDegree = targetDegree;
        
        // 优化的跳跃处理逻辑
        if (interval > 4 && parameters.Complexity < 0.7) // 使用 Complexity 属性，0.7 作为 High 复杂度的阈值
        {
            // 对于中等复杂度，减少大跳跃
            int halfJump = interval / 2;
            optimizedDegree = lastDegree + (direction * halfJump);
        }
        
        return optimizedDegree;
    }

    /// <summary>
    /// 根据音乐风格获取最大允许跳跃音程
    /// </summary>
    /// <param name="style">音乐风格</param>
    /// <returns>最大允许跳跃音程</returns>    
    /// <remarks>
    /// 此方法根据音乐风格确定最大允许跳跃音程，用于确保生成的旋律在音乐理论上是连贯的。
    /// 不同的音乐风格可能有不同的跳跃限制，以符合其独特的音乐特征。
    /// </remarks>
    private int GetMaxAllowedInterval(MusicStyle style)
    {
        // 根据不同音乐风格设置不同的跳跃限制
        return style switch
        {
            // 根据情绪调整跳跃限制
            /// <summary>
            /// 神秘音乐：复杂的旋律和和声结构
            /// </summary>
            MusicStyle.Mysterious => 4, // 神秘音乐适度跳跃
            /// <summary>
            /// 浪漫音乐：流畅的旋律，丰富的和声
            /// </summary>
            MusicStyle.Romantic => 3,   // 浪漫音乐通常避免大跳跃
            /// <summary>
            /// 标准音乐：符合传统音乐理论的进行
            /// </summary>
            MusicStyle.Standard => 4,   // 标准音乐适度跳跃
            /// <summary>
            /// 古典音乐：传统的音符结构和节奏
            /// </summary>
            MusicStyle.Classical => 3,  // 古典音乐通常避免大跳跃
            /// <summary>
            /// 爵士乐：复杂的旋律和丰富的和声
            /// </summary>
            MusicStyle.Jazz => 5,       // 爵士乐可以有更大的跳跃
            /// <summary>
            /// 流行音乐：简单的旋律和丰富的和声
            /// </summary>
            MusicStyle.Pop => 4,        // 流行音乐适度跳跃
            /// <summary>
            /// 摇滚乐：复杂的旋律和丰富的和声
            /// </summary>
            MusicStyle.Rock => 4,       // 摇滚乐适度跳跃
            /// <summary>
            /// 电子音乐：快速的节奏和复杂的音符结构
            /// </summary>
            MusicStyle.Electronic => 5, // 电子音乐可以有更大的跳跃
            /// <summary>
            /// 民间音乐：简单的旋律和和声结构
            /// </summary>
            MusicStyle.Folk => 4,       // 民间音乐适度跳跃
            _ => 4                     // 默认值
        };
    }

    /// <summary>
    /// 应用平滑过渡策略
    /// </summary>
    /// <param name="fromScaleDegree">过渡前的音阶度数</param>
    /// <param name="toScaleDegree">过渡后的音阶度数</param>
    /// <param name="scaleSize">音阶大小（度数数量）</param>
    /// <param name="parameters">旋律参数，包含音乐风格等信息</param>
    /// <param name="direction">过渡方向（1为升，-1为降）</param>
    /// <returns>应用平滑过渡后的音阶度数</returns>
    /// <remarks>
    /// 此方法根据过渡前的音阶度数、过渡后的音阶度数、音阶大小、旋律参数和过渡方向，
    /// 应用平滑过渡策略，返回应用平滑过渡后的音阶度数。
    /// 策略1：使用和弦音作为过渡，40%概率。
    /// 策略2：使用级进或小跳跃，50%概率。
    /// </remarks>
    private int ApplySmoothTransition(int fromScaleDegree, int toScaleDegree, int scaleSize,
                                    MelodyParameters parameters, int direction)
    {
        // 策略1：使用和弦音作为过渡
        if (_currentChord.Count > 0 && _random.NextDouble() < 0.4)
        {
            // 在当前和弦中寻找合适的音作为过渡
            int closestChordTone = FindClosestChordTone(fromScaleDegree, direction);
            if (closestChordTone != -1)
                return closestChordTone;
        }

        // 策略2：使用级进或小跳跃
        if (_random.NextDouble() < 0.5)
        {
            // 使用大二度或小三度
            return fromScaleDegree + (direction * 2);
        }
        else if (_random.NextDouble() < 0.7)
        {
            // 使用纯四度（和声上稳定）
            int targetDegree = fromScaleDegree + (direction * 3);
            // 检查是否在范围内
            if (targetDegree >= 0 && targetDegree < scaleSize)
                return targetDegree;
            else
                return fromScaleDegree + (direction * 2);
        }
        else
        {
            // 使用级进（大二度）
            return fromScaleDegree + direction;
        }
    }

    /// <summary>
    /// 寻找最近的和弦音
    /// </summary>
    /// <param name="fromScaleDegree">过渡前的音阶度数</param>
    /// <param name="direction">过渡方向（1为升，-1为降）</param>
    /// <returns>最近的和弦音的音阶度数，如果没有和弦音则返回-1</returns>
    /// <remarks>
    /// 此方法根据过渡前的音阶度数和过渡方向，寻找当前和弦中最近的和弦音。
    /// 优先考虑与过渡方向同向的和弦音，若不存在则返回最近的和弦音。
    /// 如果当前和弦为空，则返回-1。
    /// </remarks>
    private int FindClosestChordTone(int fromScaleDegree, int direction)
    {
        if (_currentChord.Count == 0)
            return -1;

        // 优先考虑同向的和弦音
        List<int> sortedChordTones = direction > 0 ?
            [.. _currentChord.Where(t => t > fromScaleDegree).OrderBy(t => t)] :
            [.. _currentChord.Where(t => t < fromScaleDegree).OrderByDescending(t => t)];

        if (sortedChordTones.Count > 0)
            return sortedChordTones[0];

        // 如果没有同向的和弦音，返回最近的和弦音
        int closestTone = -1;
        int minDistance = int.MaxValue;

        foreach (int tone in _currentChord)
        {
            int distance = Math.Abs(tone - fromScaleDegree);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestTone = tone;
            }
        }

        return closestTone;
    }

    /// <summary>
    /// 从音符获取音阶度数
    /// </summary>
    /// <param name="note">音符名称</param>
    /// <param name="scale">音阶类型</param>
    /// <returns>音符在音阶中的度数，0表示主音</returns>
    /// <remarks>
    /// 此方法根据音符名称和音阶类型，返回音符在该音阶中的度数。
    /// 例如，C4在Major音阶中是主音（度数为0），而D4在Minor音阶中是三音（度数为2）。
    /// 如果音符不在音阶中，默认返回主音（度数为0）。
    /// </remarks>
    private int GetScaleDegreeFromNote(NoteName note, Scale scale)
    {
        var scaleNoteNames = GetScaleNoteNames(scale);
        for (int i = 0; i < scaleNoteNames.Count; i++)
        {
            if (scaleNoteNames[i] == note)
                return i;
        }
        return 0; // 默认返回主音
    }

    /// <summary>
    /// 获取解决音
    /// </summary>
    /// <param name="emotion">情绪类型</param>
    /// <returns>音阶度数，对应解决音的位置</returns>
    /// <remarks>
    /// 此方法根据情绪类型，返回对应的解决音的音阶度数。
    /// 不同情绪类型会有不同的解决音，例如快乐情绪的解决音通常是主音，而悲伤情绪的解决音通常是三音。
    /// 确保返回的音阶度数在音阶范围内。
    /// </remarks>
    private int GetResolutionTone(Emotion emotion)
    {
        // 根据情绪选择合适的解决音
        // 通常是主音或属音，但可以有变化
        return emotion switch
        {
            /// <summary>
            /// 快乐情绪的解决音为主音
            /// </summary>
            Emotion.Happy => 0,    // 主音
            /// <summary>
            /// 悲伤情绪的解决音为主音（小调中解决到主音）
            /// </summary>
            Emotion.Sad => 0,      // 主音（小调中解决到主音）
            /// <summary>
            /// 能量情绪的解决音为五音
            /// </summary>
            Emotion.Energetic => 4, // 属音（有结束感但仍有动力）
            /// <summary>
            /// 平静情绪的解决音为主音
            /// </summary>
            Emotion.Calm => 0,     // 主音
            /// <summary>
            /// 神秘情绪的解决音为上主音（略有神秘感的解决）
            /// </summary>
            Emotion.Mysterious => 1, // 上主音（略有神秘感的解决）
            /// <summary>
            /// 浪漫情绪的解决音为主音
            /// </summary>
            Emotion.Romantic => 0, // 主音
            _ => 0
        };
    }

    /// <summary>
    /// 获取稳定音（乐句开始音）
    /// </summary>
    /// <param name="emotion">情绪类型</param>
    /// <returns>音阶度数，对应稳定音的位置</returns>
    /// <remarks>
    /// 此方法根据情绪类型，返回对应的稳定音的音阶度数。
    /// 不同情绪类型会有不同的稳定音，例如快乐情绪的稳定音通常是主音，而悲伤情绪的稳定音通常是三音。
    /// 确保返回的音阶度数在音阶范围内。
    /// </remarks>
    private int GetStableTone(Emotion emotion)
    {
        return emotion switch
        {
            /// <summary>
            /// 标准情绪的稳定音为主音
            /// </summary>
            Emotion.Standard => 0, // 主音
            /// <summary>
            /// 快乐情绪的稳定音为主音
            /// </summary>
            Emotion.Happy => 0,    // 主音
            /// <summary>
            /// 悲伤情绪的稳定音为三音
            /// </summary>  
            Emotion.Sad => 2,      // 三音
            /// <summary>
            /// 能量情绪的稳定音为五音
            /// </summary>
            Emotion.Energetic => 4, // 五音
            /// <summary>
            /// 平静情绪的稳定音为主音
            /// </summary>
            Emotion.Calm => 0,     // 主音
            /// <summary>
            /// 神秘情绪的稳定音为四音
            /// </summary>
            Emotion.Mysterious => 3, // 四音
            /// <summary>
            /// 浪漫情绪的稳定音为三音
            /// </summary>
            Emotion.Romantic => 2, // 三音
            /// <summary>
            /// 默认情绪的稳定音为主音
            /// </summary>
            _ => 0
        };
    }

    /// <summary>
    /// 获取高潮音（乐句最高点）
    /// </summary>
    /// <param name="emotion">情绪类型</param>
    /// <returns>音阶度数，对应高潮音的位置</returns>
    /// <remarks>
    /// 此方法根据情绪类型，返回对应的高潮音的音阶度数。
    /// 不同情绪类型会有不同的高潮音，例如快乐情绪的高潮音通常是七音，而悲伤情绪的高潮音通常是五音。
    /// 确保返回的音阶度数在音阶范围内。
    /// </remarks>
    private int GetClimaxTone(Emotion emotion)
    {
        return emotion switch
        {
            /// <summary>
            /// 快乐情绪的高潮音为七音
            /// </summary>
            Emotion.Happy => 6,    // 七音
            /// <summary>
            /// 悲伤情绪的高潮音为五音
            /// </summary>
            Emotion.Sad => 4,      // 五音
            /// <summary>
            /// 能量情绪的高潮音为七音
            /// </summary>
            Emotion.Energetic => 6, // 七音
            /// <summary>
            /// 平静情绪的高潮音为五音
            /// </summary>
            Emotion.Calm => 4,     // 五音
            /// <summary>
            /// 神秘情绪的高潮音为六音
            /// </summary>
            Emotion.Mysterious => 5, // 六音
            /// <summary>
            /// 浪漫情绪的高潮音为六音
            /// </summary>
            Emotion.Romantic => 5, // 六音
            _ => 4
        };
    }

    /// <summary>
    /// 添加经过音增加旋律流动性
    /// </summary>
    /// <param name="currentDegree">当前音阶度数</param>
    /// <param name="scaleLength">音阶长度</param>
    /// <returns>调整后的音阶度数</returns>
    /// <remarks>
    /// 此方法根据当前音阶度数和音阶长度，添加一个经过音。
    /// 经过音可以是升音或降音，增加旋律的流动性和变化性。
    /// 确保新的音阶度数在音阶范围内。
    /// </remarks>
    private int AddPassingTone(int currentDegree, int scaleLength)
    {
        int[] passingDirections = [-1, 1]; // 可以向上或向下
        int direction = passingDirections[_random.Next(passingDirections.Length)];
        int newDegree = currentDegree + direction;

        // 确保在音阶范围内
        if (newDegree >= 0 && newDegree < scaleLength)
            return newDegree;

        return currentDegree;
    }

    /// <summary>
    /// 确定音乐化的八度
    /// </summary>
    /// <param name="parameters">旋律生成参数</param>
    /// <param name="scaleDegree">音阶度数</param>
    /// <param name="phrasePosition">在乐句中的位置</param>
    /// <returns>确定的八度</returns>
    /// <remarks>
    /// 此方法根据音乐风格、音阶度数和乐句位置，确定音符的八度。
    /// 高音区音符通常会在乐句后半段升高八度，以突出其重要性。
    /// 避免频繁的八度跳跃，保持音符之间的八度变化平滑。
    /// </remarks>
    private int DetermineMusicalOctave(MelodyParameters parameters, int scaleDegree, int phrasePosition)
    {
        int baseOctave = parameters.Octave;

        // 根据音阶度数和乐句位置调整八度
        if (scaleDegree >= 5) // 高音区音符
        {
            if (phrasePosition > 0.7 * GetPhraseLength(parameters.Style)) // 乐句后半段
                return baseOctave + 1;
        }

        // 避免频繁的八度跳跃
        if (_previousNotes.Count > 0)
        {
            var lastNote = _previousNotes.Last();
            if (Math.Abs(baseOctave - lastNote.Octave) > 1)
            {
                return lastNote.Octave; // 保持相同八度
            }
        }

        return baseOctave;
    }

    /// <summary>
    /// 获取音乐化的时值
    /// </summary>
    /// <param name="parameters">旋律生成参数</param>
    /// <param name="phrasePosition">在乐句中的位置</param>
    /// <param name="isPhraseEnd">是否是乐句结束</param>
    /// <returns>音符时值（ticks）</returns>
    /// <remarks>
    /// 此方法根据音乐风格、乐句位置和是否是乐句结束，计算音符的时值。
    /// 不同的风格会有不同的时值模式，例如古典风格会有较长的时值，而爵士风格会有较短的时值。
    /// 奇数位置通常会有较短的时值，而偶数位置通常会有较长的时值。
    /// 最后一个音符（isPhraseEnd为true）通常会有较长的时值，以突出结束。
    /// </remarks>
    // 时值缓存
    private static readonly Dictionary<string, long> _durationCache = [];

    // 预定义常用时值常量
    /// <summary>
    /// 十六分音符时值（ticks）
    /// </summary>
    private const long SIXTEENTH_NOTE = 60;
    /// <summary>
    /// 八分音符时值（ticks）
    /// </summary>
    private const long EIGHTH_NOTE = 120;
    /// <summary>
    /// 四分音符时值（ticks）
    /// </summary>
    private const long QUARTER_NOTE = 240;
    /// <summary>
    /// 半音符时值（ticks）
    /// </summary>
    private const long HALF_NOTE = 480;
    /// <summary>
    ///  dotted quarter note duration（ticks）
    /// </summary>
    private const long DOTTED_QUARTER_NOTE = 360;
    /// <summary>
    /// 获取音乐化的时值
    /// </summary>
    /// <param name="parameters">旋律生成参数</param>
    /// <param name="phrasePosition">在乐句中的位置</param>
    /// <param name="isPhraseEnd">是否是乐句结束</param>
    /// <returns>音符时值（ticks）</returns>
    /// <remarks>
    /// 此方法根据音乐风格、乐句位置和是否是乐句结束，计算音符的时值。
    /// 不同的风格会有不同的时值模式，例如古典风格会有较长的时值，而爵士风格会有较短的时值。
    /// 奇数位置通常会有较短的时值，而偶数位置通常会有较长的时值。
    /// 最后一个音符（isPhraseEnd为true）通常会有较长的时值，以突出结束。
    /// </remarks>
    private long GetMusicalDuration(MelodyParameters parameters, int phrasePosition, bool isPhraseEnd)
    {
        // 生成缓存键
        string cacheKey = $"{parameters.Style}_{phrasePosition % 4}_{isPhraseEnd}";

        // 检查缓存
        if (_durationCache.TryGetValue(cacheKey, out var cachedDuration))
        {
            return cachedDuration;
        }

        long duration;

        if (isPhraseEnd)
        {
            // 乐句结束使用较长时值
            duration = HALF_NOTE; // 四分音符
        }
        else
        {
            // 基于风格和位置的时值模式
            // 优化：使用简单的条件判断代替switch表达式，可能更高效
            int positionMod = phrasePosition % 4;

            /// <summary>
            /// 流行风格：偶数位置为四分音符，奇数位置为八分音符
            /// </summary>
            duration = parameters.Style switch
            {
                /// <summary>
                /// 古典风格：偶数位置为半音符，奇数位置为四分音符
                /// </summary>
                MusicStyle.Classical => (positionMod == 0) ? HALF_NOTE : QUARTER_NOTE,
                /// <summary>
                /// 爵士风格：所有位置为八分音符
                /// </summary>
                MusicStyle.Jazz => EIGHTH_NOTE,// 爵士多用短时值
                /// <summary>
                /// 摇滚风格：偶数位置为长四分音符，奇数位置为八分音符
                /// </summary>
                MusicStyle.Rock => (positionMod == 0) ? DOTTED_QUARTER_NOTE : EIGHTH_NOTE,
                /// <summary>
                /// 默认风格：偶数位置为四分音符，奇数位置为八分音符
                /// </summary>
                _ => QUARTER_NOTE,
            };
        }

        // 缓存结果
        _durationCache[cacheKey] = duration;

        return duration;
    }

    /// <summary>
    /// 获取音乐化的力度
    /// </summary>
    /// <param name="parameters">旋律生成参数</param>
    /// <param name="phrasePosition">在乐句中的位置</param>
    /// <param name="phraseLength">乐句长度</param>
    /// <returns>音符力度值（0-127）</returns>
    /// <remarks>
    /// 此方法根据音乐情绪、风格和速度设置，计算音符的力度值。
    /// 不同的情绪会导致音符的力度有显著的变化，符合音乐理论的预期情感。
    /// 例如，快乐情绪会有强烈的 crescendo（ crescendo），而悲伤情绪会有明显的降音。
    /// 风格会影响音符的力度变化，例如古典风格会有更细致的变化。
    /// 速度设置会影响音符的整体力度，例如快速度会有更强的音符。
    /// </remarks>
    private int GetMusicalVelocity(MelodyParameters parameters, int phrasePosition, int phraseLength)
    {
        // 基于乐句位置创建力度轮廓
        double positionRatio = (double)phrasePosition / phraseLength;

        // 根据情绪、风格和速度设置基础力度
        int baseVelocity = CalculateBaseVelocity(parameters);

        // 应用基于音乐理论的力度轮廓
        double velocityMultiplier = ApplyMusicalVelocityCurve(positionRatio, parameters.Emotion);

        // 根据音符上下文调整力度
        double contextAdjustment = GetVelocityContextAdjustment();

        // 应用表情记号效果
        double expressionEffect = GetExpressionEffect(phrasePosition, phraseLength, parameters);

        // 计算最终力度
        int finalVelocity = (int)(baseVelocity * velocityMultiplier * contextAdjustment * expressionEffect);

        // 确保力度在有效范围内
        return Math.Max(10, Math.Min(127, finalVelocity));
    }

    /// <summary>
    /// 根据情绪、风格和速度计算基础力度
    /// </summary>
    /// <param name="parameters">旋律生成参数</param>
    /// <returns>基础音符力度值（0-127）</returns>
    /// <remarks>
    /// 此方法根据音乐情绪、风格和速度设置，计算基础音符的力度值。
    /// 不同的情绪会导致音符的力度有显著的变化，符合音乐理论的预期情感。
    /// 例如，快乐情绪会有强烈的 crescendo（ crescendo），而悲伤情绪会有明显的降音。
    /// 风格会影响音符的力度变化，例如古典风格会有更细致的变化。
    /// 速度设置会影响音符的整体力度，例如快速度会有更强的音符。
    /// </remarks>
    private int CalculateBaseVelocity(MelodyParameters parameters)
    {
        int baseVelocity = parameters.Emotion switch
        {
            /// <summary>
            /// 快乐情绪：基础力度为90
            /// </summary>
            Emotion.Happy => 90,
            /// <summary>
            /// 悲伤情绪：基础力度为70
            /// </summary>
            Emotion.Sad => 70,
            /// <summary>
            /// 能量情绪：基础力度为110
            /// </summary>
            Emotion.Energetic => 110,
            /// <summary>
            /// 冷静情绪：基础力度为60
            /// </summary>
            Emotion.Calm => 60,
            /// <summary>
            /// 神秘情绪：基础力度为80
            /// </summary>
            Emotion.Mysterious => 80,
            /// <summary>
            /// 浪漫情绪：基础力度为85
            /// </summary>
            Emotion.Romantic => 85,
            /// <summary>
            /// 默认情绪：基础力度为80
            /// </summary>
            _ => 80
        };

        // 根据风格调整力度
        switch (parameters.Style)
        {
            /// <summary>
            /// 古典风格：基础力度稍弱，变化更细致
            /// </summary>
            case MusicStyle.Classical:
                baseVelocity = (int)(baseVelocity * 0.9); // 古典音乐力度稍弱，变化更细致
                break;
            /// <summary>
            /// 摇滚风格和电子风格：基础力度更强
            /// </summary>  
            case MusicStyle.Rock:
            case MusicStyle.Electronic:
                baseVelocity = (int)(baseVelocity * 1.1); // 摇滚和电子音乐力度更强
                break;
            /// <summary>
            /// 爵士风格：基础力度适中，变化更细致
            /// </summary>
            case MusicStyle.Jazz:
                baseVelocity = (int)(baseVelocity * 0.95); // 爵士音乐力度适中
                break;
            /// <summary>
            /// 流行风格：基础力度与默认相同
            /// </summary>
            case MusicStyle.Pop:
                baseVelocity = (int)(baseVelocity * 1.0); // 流行音乐保持默认
                break;
        }

        // 根据速度调整力度
        if (parameters.BPM > 120)
            baseVelocity = (int)(baseVelocity * 1.05); // 快速音乐力度稍强
        else if (parameters.BPM < 80)
            baseVelocity = (int)(baseVelocity * 0.95); // 慢速音乐力度稍弱

        return baseVelocity;
    }

    /// <summary>
    /// 应用基于音乐理论的力度曲线
    /// </summary>
    /// <param name="positionRatio">在乐句中的位置比例（0-1）</param>
    /// <param name="emotion">音乐情绪</param>
    /// <returns>基于位置比例和情绪的力度 multiplier</returns>
    /// <remarks>
    /// 此方法根据音符在乐句中的位置比例和音乐情绪，应用不同的力度曲线。
    /// 不同的情绪会导致音符的力度有显著的变化，符合音乐理论的预期情感。
    /// 例如，快乐音乐会有强烈的 crescendo（ crescendo），而悲伤音乐会有明显的降音。
    /// </remarks>
    private double ApplyMusicalVelocityCurve(double positionRatio, Emotion emotion)
    {
        // 根据情绪应用不同的力度曲线
        switch (emotion)
        {
            /// <summary>
            /// 快乐情绪：开始适中，中间有起伏，结束强
            /// </summary>
            case Emotion.Happy:
                // 快乐的音乐：开始适中，中间有起伏，结束强
                if (positionRatio < 0.2) return 0.9; // 开始
                else if (positionRatio < 0.5) return 1.1; // 上升
                else if (positionRatio < 0.8) return 0.95; // 回落
                else return 1.2; // 高潮
            /// <summary>
            /// 悲伤情绪：开始弱，中间起伏，结束弱
            /// </summary>
            case Emotion.Sad:
                // 悲伤的音乐：开始弱，中间起伏，结束弱
                if (positionRatio < 0.3) return 0.8; // 开始
                else if (positionRatio < 0.6) return 1.1; // 情感释放
                else return 0.7; // 减弱
            /// <summary>
            /// 能量情绪：整体强，有明显的强弱对比
            /// </summary>
            case Emotion.Energetic:
                // 充满活力的音乐：整体强，有明显的强弱对比
                if (positionRatio < 0.2) return 1.1; // 开始强
                else if (positionRatio < 0.4) return 0.9; // 稍弱
                else if (positionRatio < 0.7) return 1.2; // 高潮
                else return 1.1; // 保持强度
            /// <summary>
            /// 冷静情绪：整体弱，变化较小
            /// </summary>
            case Emotion.Calm:
                // 平静的音乐：整体弱，变化较小
                if (positionRatio < 0.4) return 0.9; // 开始
                else if (positionRatio < 0.7) return 1.0; // 稍强
                else return 0.9; // 结束
            /// <summary>
            /// 神秘情绪：有不规则的强弱变化
            /// </summary>
            case Emotion.Mysterious:
                // 神秘的音乐：有不规则的强弱变化
                if (_random.NextDouble() < 0.6)
                    return 0.9 + (_random.NextDouble() * 0.3); // 随机变化
                else
                    return 0.8 + (positionRatio * 0.4); // 缓慢增强
            /// <summary>
            /// 浪漫情绪：渐进式的力度变化
            /// </summary>
            case Emotion.Romantic:
                // 浪漫的音乐：渐进式的力度变化
                return 0.8 + (Math.Sin(positionRatio * Math.PI) * 0.3); // 正弦曲线变化
            /// <summary>
            /// 默认曲线：开始适中，中间强，结束适中
            /// </summary>
            default:
                if (positionRatio < 0.2) return 0.9;
                else if (positionRatio < 0.7) return 1.1;
                else return 1.0;
        }
    }

    /// <summary>
    /// 根据音符上下文调整力度
    /// </summary>
    /// <returns>基于音符上下文的力度调整 multiplier</returns>
    /// <remarks>
    /// 此方法根据当前音符和上一个音符的上下文，调整音符的力度。
    /// 考虑音符的方向（上行或下行）、时值（长或短）和演奏法（连音或断音）。
    /// 调整因子根据音乐理论原理设计，确保生成的旋律符合预期的情感和节奏。
    /// </remarks>
    private double GetVelocityContextAdjustment()
    {
        if (_previousNotes.Count == 0)
            return 1.0;

        double adjustment = 1.0;
        var lastNote = _previousNotes.Last();

        // 根据音符方向调整（上行渐强，下行渐弱）
        if (_previousNotes.Count >= 2)
        {
            var secondLastNote = _previousNotes[^2];
            bool isAscending = lastNote.Note < _lastSelectedNote;
            bool isDescending = lastNote.Note > _lastSelectedNote;

            if (isAscending)
                adjustment = 1.05; // 上行渐强
            else if (isDescending)
                adjustment = 0.95; // 下行渐弱
        }

        // 长音符通常更强
        if (_currentDuration > 480) // 四分音符以上
            adjustment *= 1.1;

        // 断音更短，力度稍强
        if (_currentArticulation == Articulation.Staccato)
            adjustment *= 1.15;
        // 连音更连贯，力度稍弱
        else if (_currentArticulation == Articulation.Legato)
            adjustment *= 0.95;

        return adjustment;
    }

    // 存储当前上下文信息的私有字段
    /// <summary>
    /// 上次选择的音符
    /// </summary>
    private NoteName _lastSelectedNote = NoteName.C; // 上次选择的音符
    /// <summary>
    /// 当前音符时值
    /// </summary>
    private int _currentDuration = 480; // 当前音符时值
    /// <summary>
    /// 当前音符演奏法
    /// </summary>
    private Articulation _currentArticulation = Articulation.Normal; // 当前演奏法

    /// <summary>
    /// 音符演奏法枚举
    /// </summary>
    /// <remarks>
    /// 此枚举定义了音符的不同演奏法，用于调整音符的强弱和连续性。
    /// 每个值都有对应的音乐理论解释，影响音符的演奏效果。
    /// </remarks>
    private enum Articulation
    {
        /// <summary>
        /// 普通音符
        /// </summary>
        Normal,
        /// <summary>
        /// 断音
        /// </summary>
        Staccato,
        /// <summary>
        /// 连音
        /// </summary>
        Legato,
        /// <summary>
        /// 强调音
        /// </summary>
        Marcato,
        /// <summary>
        /// 保持音
        /// </summary>
        Tenuto
    }

    /// <summary>
    /// 获取表情记号效果
    /// </summary>
    /// <param name="phrasePosition">当前短语位置</param>
    /// <param name="phraseLength">当前短语长度</param>
    /// <param name="parameters">旋律生成参数</param>
    /// <returns>表情记号效果值（1.0为无效果）</returns>
    /// <remarks>
    /// 此方法根据当前短语位置和长度，根据情绪和风格随机添加表情记号效果。
    /// 支持的表情记号包括渐强、渐弱、突强、弱奏和强奏。
    /// 每个表情记号都有不同的应用范围和效果。
    /// </remarks>
    private double GetExpressionEffect(int phrasePosition, int phraseLength, MelodyParameters parameters)
    {
        double effect = 1.0;

        // 随机添加表情记号效果
        if (_random.NextDouble() < 0.15) // 15%几率添加表情记号
        {
            int expressionType = _random.Next(5);

            switch (expressionType)
            {
                /// <summary>
                /// 渐强 (Crescendo)
                /// </summary>
                case 0: // 渐强 (Crescendo)
                    if (phrasePosition > phraseLength * 0.2 && phrasePosition < phraseLength * 0.7)
                        effect = 1.0 + ((double)phrasePosition / phraseLength) * 0.3;
                    break;

                /// <summary>
                /// 渐弱 (Decrescendo)
                /// </summary>
                case 1: // 渐弱 (Decrescendo)
                    if (phrasePosition > phraseLength * 0.3)
                        effect = 1.2 - ((double)phrasePosition / phraseLength) * 0.3;
                    break;

                /// <summary>
                /// 突强 (Sforzando)
                /// </summary>
                case 2: // 突强 (Sforzando)
                    if (_random.NextDouble() < 0.3)
                        effect = 1.3; // 突然很强
                    break;

                /// <summary>
                /// 弱奏 (Piano)
                /// </summary>
                case 3: // 弱奏 (Piano)
                    effect = 0.8;
                    break;

                /// <summary>
                /// 强奏 (Forte)
                /// </summary>
                case 4: // 强奏 (Forte)
                    effect = 1.2;
                    break;
            }
        }

        // 添加细微的随机波动，使力度更自然
        effect *= 0.95 + (_random.NextDouble() * 0.1);

        return effect;
    }

    /// <summary>
    /// 生成和声进行
    /// </summary>
    /// <param name="parameters">旋律生成参数</param>
    /// <remarks>
    /// 此方法根据提供的旋律生成参数，根据情绪和风格选择合适的和声进行。
    /// 支持的情绪包括标准、欢快、悲伤、充满活力、平静、神秘和浪漫。
    /// 每个情绪都有对应的和声进行生成方法。
    /// </remarks>
    private void GenerateChordProgression(MelodyParameters parameters)
    {
        _chordProgression.Clear();

        // 根据情绪和风格选择和声进行
        switch (parameters.Emotion)
        {
            /// <summary>
            /// 标准和声进行
            /// </summary>
            case Emotion.Standard:
                GenerateStandardChordProgression(parameters.Scale);
                break;
            /// <summary>
            /// 欢快的和声进行
            /// </summary>
            case Emotion.Happy:
                GenerateHappyChordProgression(parameters.Scale);
                break;
            /// <summary>
            /// 悲伤的和声进行
            /// </summary>
            case Emotion.Sad:
                GenerateSadChordProgression(parameters.Scale);
                break;
            /// <summary>
            ///  energetic的和声进行
            /// </summary>
            case Emotion.Energetic:
                GenerateEnergeticChordProgression(parameters.Scale);
                break;
            /// <summary>
            ///  calm的和声进行
            /// </summary>
            case Emotion.Calm:
                GenerateCalmChordProgression(parameters.Scale);
                break;
            /// <summary>
            /// 神秘的和声进行
            /// </summary>
            case Emotion.Mysterious:
                GenerateMysteriousChordProgression(parameters.Scale);
                break;
            /// <summary>
            /// 浪漫的和声进行
            /// </summary>
            case Emotion.Romantic:
                GenerateRomanticChordProgression(parameters.Scale);
                break;
            /// <summary>
            /// 默认：标准和声进行
            /// </summary>
            default:
                GenerateStandardChordProgression(parameters.Scale);
                break;
        }

        // 初始化当前和弦
        if (_chordProgression.Count > 0)
        {
            _currentChord = _chordProgression[0];
            _chordPosition = 0;
        }
    }

    /// <summary>
    /// 生成欢快的和声进行
    /// </summary>
    /// /// <remarks>
    /// 此方法根据提供的音阶生成欢快的和声进行。
    /// 对于大调，进行 I-IV-V-I 模式；
    /// 对于小调，进行 i-iv-VI-VII 模式。
    /// </remarks>
    private void GenerateHappyChordProgression(Scale scale)
    {
        if (IsMajorScale(scale))
        {
            // 大调欢快和声进行: I-IV-V-I
            _chordProgression.Add([0, 2, 4]); // I 和弦
            _chordProgression.Add([3, 5, 7]); // IV 和弦
            _chordProgression.Add([4, 6, 8]); // V 和弦
            _chordProgression.Add([0, 2, 4]); // I 和弦
        }
        else
        {
            // 小调欢快和声进行: i-iv-VI-VII
            _chordProgression.Add([0, 2, 4]); // i 和弦
            _chordProgression.Add([3, 5, 7]); // iv 和弦
            _chordProgression.Add([5, 7, 9]); // VI 和弦
            _chordProgression.Add([6, 8, 10]); // VII 和弦
        }
    }

    /// <summary>
    /// 生成悲伤的和声进行
    /// </summary>
    /// /// <remarks>
    /// 此方法根据提供的音阶生成悲伤的和声进行。
    /// 对于大调，进行 I-iii-IV-vi 模式；
    /// 对于小调，进行 i-VI-VII-V 模式。
    /// </remarks>
    private void GenerateSadChordProgression(Scale scale)
    {
        if (IsMajorScale(scale))
        {
            // 大调悲伤和声进行: I-iii-IV-vi
            _chordProgression.Add([0, 2, 4]); // I 和弦
            _chordProgression.Add([2, 4, 6]); // iii 和弦
            _chordProgression.Add([3, 5, 7]); // IV 和弦
            _chordProgression.Add([5, 7, 9]); // vi 和弦
        }
        else
        {
            // 小调悲伤和声进行: i-VI-VII-V
            _chordProgression.Add([0, 2, 4]); // i 和弦
            _chordProgression.Add([5, 7, 9]); // VI 和弦
            _chordProgression.Add([6, 8, 10]); // VII 和弦
            _chordProgression.Add([4, 6, 8]); // V 和弦
        }
    }

    /// <summary>
    /// 生成充满活力的和声进行
    /// </summary>
    /// /// <remarks>
    /// 此方法根据提供的音阶生成充满活力的和声进行。
    /// 对于大调，进行 I-V-vi-IV 模式；
    /// 对于小调，进行 i-III-VI-iv 模式。
    /// </remarks>
    private void GenerateEnergeticChordProgression(Scale scale)
    {
        if (IsMajorScale(scale))
        {
            // 大调活力和声进行: I-V-vi-IV
            _chordProgression.Add([0, 2, 4]); // I 和弦
            _chordProgression.Add([4, 6, 8]); // V 和弦
            _chordProgression.Add([5, 7, 9]); // vi 和弦
            _chordProgression.Add([3, 5, 7]); // IV 和弦
        }
        else
        {
            // 小调活力和声进行: i-III-VI-iv
            _chordProgression.Add([0, 2, 4]); // i 和弦
            _chordProgression.Add([2, 4, 6]); // III 和弦
            _chordProgression.Add([5, 7, 9]); // VI 和弦
            _chordProgression.Add([3, 5, 7]); // iv 和弦
        }
    }

    /// <summary>
    /// 生成平静的和声进行
    /// </summary>
    /// /// <remarks>
    /// 此方法根据提供的音阶生成平静的和声进行。
    /// 对于大调，进行 I-vi-IV-V 模式；
    /// 对于小调，进行 i-iv-VII-VI 模式。
    /// </remarks>
    private void GenerateCalmChordProgression(Scale scale)
    {
        if (IsMajorScale(scale))
        {
            // 大调平静和声进行: I-vi-IV-V
            _chordProgression.Add([0, 2, 4]); // I 和弦
            _chordProgression.Add([5, 7, 9]); // vi 和弦
            _chordProgression.Add([3, 5, 7]); // IV 和弦
            _chordProgression.Add([4, 6, 8]); // V 和弦
        }
        else
        {
            // 小调平静和声进行: i-iv-VII-VI
            _chordProgression.Add([0, 2, 4]); // i 和弦
            _chordProgression.Add([3, 5, 7]); // iv 和弦
            _chordProgression.Add([6, 8, 10]); // VII 和弦
            _chordProgression.Add([5, 7, 9]); // VI 和弦
        }
    }

    /// <summary>
    /// 生成神秘的和声进行
    /// </summary>
    /// <remarks>
    /// 此方法根据提供的音阶生成神秘的和声进行。
    /// 对于大调，进行 I-II-iii-viio 模式；
    /// 对于小调，进行 i-iiio-VI-VII 模式。
    /// </remarks>
    private void GenerateMysteriousChordProgression(Scale scale)
    {
        if (IsMajorScale(scale))
        {
            // 大调神秘和声进行: I-II-iii-viio
            _chordProgression.Add([0, 2, 4]); // I 和弦
            _chordProgression.Add([1, 3, 5]); // II 和弦
            _chordProgression.Add([2, 4, 6]); // iii 和弦
            _chordProgression.Add([6, 1, 3]); // viio 和弦
        }
        else
        {
            // 小调神秘和声进行: i-iiio-VI-VII
            _chordProgression.Add([0, 2, 4]); // i 和弦
            _chordProgression.Add([2, 4, 6]); // iiio 和弦
            _chordProgression.Add([5, 7, 9]); // VI 和弦
            _chordProgression.Add([6, 8, 10]); // VII 和弦
        }
    }

    /// <summary>
    /// 生成浪漫的和声进行
    /// </summary>
    private void GenerateRomanticChordProgression(Scale scale)
    {
        if (IsMajorScale(scale))
        {
            // 大调浪漫和声进行: I-vi-ii-V
            _chordProgression.Add([0, 2, 4]); // I 和弦
            _chordProgression.Add([5, 7, 9]); // vi 和弦
            _chordProgression.Add([1, 3, 5]); // ii 和弦
            _chordProgression.Add([4, 6, 8]); // V 和弦
        }
        else
        {
            // 小调浪漫和声进行: i-VI-III-V
            _chordProgression.Add([0, 2, 4]); // i 和弦
            _chordProgression.Add([5, 7, 9]); // VI 和弦
            _chordProgression.Add([2, 4, 6]); // III 和弦
            _chordProgression.Add([4, 6, 8]); // V 和弦
        }
    }

    /// <summary>
    /// 生成标准和声进行
    /// </summary>
    private void GenerateStandardChordProgression(Scale scale)
    {
        if (IsMajorScale(scale))
        {
            // 标准大调进行: I-V-vi-IV
            _chordProgression.Add([0, 2, 4]); // I 和弦
            _chordProgression.Add([4, 6, 8]); // V 和弦
            _chordProgression.Add([5, 7, 9]); // vi 和弦
            _chordProgression.Add([3, 5, 7]); // IV 和弦
        }
        else
        {
            // 标准小调进行: i-VI-VII-V
            _chordProgression.Add([0, 2, 4]); // i 和弦
            _chordProgression.Add([5, 7, 9]); // VI 和弦
            _chordProgression.Add([6, 8, 10]); // VII 和弦
            _chordProgression.Add([4, 6, 8]); // V 和弦
        }
    }

    /// <summary>
    /// 更新当前和弦
    /// </summary>
    private void UpdateCurrentChord(int phrasePosition, int phraseLength)
    {
        if (_chordProgression.Count == 0)
            return;

        // 计算应该切换和弦的位置
        int beatsPerChord = phraseLength / _chordsPerPhrase;

        // 如果到达新的和弦位置，切换和弦
        if (phrasePosition % beatsPerChord == 0)
        {
            _chordPosition = (phrasePosition / beatsPerChord) % _chordProgression.Count;
            _currentChord = _chordProgression[_chordPosition];
        }
    }

    /// <summary>
    /// 调整音符以适应当前和弦
    /// </summary>
    private int AdaptToCurrentChord(int scaleDegree, int scaleSize, int phrasePosition, int phraseLength)
    {
        // 如果当前没有和弦，返回原音阶度数
        if (_currentChord.Count == 0)
            return scaleDegree;

        // 在强拍上更倾向于使用和弦音
        bool isStrongBeat = phrasePosition % 2 == 0;

        if (isStrongBeat && _random.NextDouble() < 0.7) // 70%概率在强拍上使用和弦音
        {
            // 检查当前音符是否在和弦中
            if (!_currentChord.Contains(scaleDegree % scaleSize))
            {
                // 如果不在和弦中，选择最近的和弦音
                return FindNearestChordTone(scaleDegree, scaleSize);
            }
        }
        else if (!isStrongBeat && _random.NextDouble() < 0.4) // 40%概率在弱拍上使用和弦音
        {
            // 检查当前音符是否在和弦中
            if (!_currentChord.Contains(scaleDegree % scaleSize))
            {
                // 如果不在和弦中，可能选择和弦音或保留经过音
                if (_random.NextDouble() < 0.5)
                {
                    return FindNearestChordTone(scaleDegree, scaleSize);
                }
            }
        }

        return scaleDegree;
    }

    /// <summary>
    /// 寻找最近的和弦音
    /// </summary>
    private int FindNearestChordTone(int currentDegree, int scaleSize)
    {
        int nearestTone = currentDegree;
        int minDistance = int.MaxValue;

        foreach (int chordTone in _currentChord)
        {
            // 计算距离
            int directDistance = Math.Abs(chordTone - currentDegree);
            int wrappedDistance = scaleSize - directDistance;
            int distance = Math.Min(directDistance, wrappedDistance);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearestTone = chordTone;
            }
        }

        return nearestTone;
    }

    /// <summary>
    /// 获取旋律轮廓模式
    /// </summary>
    /// <param name="emotion">情绪类型</param>
    /// <returns>旋律轮廓模式，包含音阶度数序列</returns>
    private List<int> GetMelodicContour(Emotion emotion)
    {
        return emotion switch
        {
            Emotion.Happy => [0, 2, 4, 2, 4, 6, 4, 2], // 上行然后下行
            Emotion.Sad => [2, 1, 0, 1, 0, 1, 2, 1],   // 波浪形
            Emotion.Energetic => [0, 4, 2, 6, 4, 2, 0, 4], // 大跳跃
            Emotion.Calm => [0, 1, 2, 1, 0, 1, 2, 1],   // 小波浪
            Emotion.Mysterious => [3, 2, 1, 4, 3, 2, 3, 2], // 不规则
            Emotion.Romantic => [0, 2, 3, 5, 3, 2, 0, 2], // 平滑曲线
            _ => [0, 2, 4, 2, 0, 1, 0, 2]
        };
    }

    /// <summary>
    /// 获取乐句长度
    /// </summary>
    /// <param name="style">音乐风格</param>
    /// <returns>乐句长度（拍数）</returns>
    private int GetPhraseLength(MusicStyle style)
    {
        return style switch
        {
            MusicStyle.Pop => 8,      // 流行：8拍乐句
            MusicStyle.Classical => 16, // 古典：16拍乐句
            MusicStyle.Jazz => 12,     // 爵士：12拍乐句
            MusicStyle.Rock => 8,      // 摇滚：8拍乐句
            MusicStyle.Electronic => 4, // 电子：4拍乐句
            MusicStyle.Blues => 12,    // 布鲁斯：12拍乐句
            _ => 8
        };
    }

    /// <summary>
    /// 决定是否在当前拍子添加音符
    /// </summary>
    /// <param name="beat">当前拍子位置</param>
    /// <param name="rhythmPattern">节奏模式</param>
    /// <param name="style">音乐风格</param>
    /// <returns>如果应该添加音符则返回true</returns>
    private bool ShouldAddNoteAtBeat(int beat, List<int> rhythmPattern, MusicStyle style)
    {
        int patternPosition = beat % rhythmPattern.Count;
        return rhythmPattern[patternPosition] == 1;
    }

    /// <summary>
    /// 获取旋律节奏模式
    /// </summary>
    /// <param name="style">音乐风格</param>
    /// <param name="emotion">情绪类型</param>
    /// <returns>节奏模式，1表示添加音符，0表示不添加</returns>
    private List<int> GetMelodicRhythmPattern(MusicStyle style, Emotion emotion)
    {
        // 获取基础节奏模式
        var basePattern = GetBaseRhythmPattern(style, emotion);

        // 添加变化和切分
        return AddRhythmVariations(basePattern, style, emotion);
    }

    /// <summary>
    /// 获取基础节奏模式
    /// </summary>
    /// <param name="style">音乐风格</param>
    /// <param name="emotion">情绪类型</param>
    /// <returns>基础节奏模式</returns>
    private List<int> GetBaseRhythmPattern(MusicStyle style, Emotion emotion)
    {
        return (style, emotion) switch
        {
            // 流行音乐：风格化的节奏模式
            (MusicStyle.Pop, Emotion.Happy) => [1, 0, 1, 0, 1, 1, 1, 0],  // 带附点的节奏
            (MusicStyle.Pop, Emotion.Sad) => [1, 0, 0, 0, 1, 0, 1, 0],     // 稀疏节奏
            (MusicStyle.Pop, Emotion.Energetic) => [1, 1, 1, 0, 1, 1, 1, 1], // 密集节奏
            (MusicStyle.Pop, _) => [1, 0, 1, 1, 0, 1, 1, 0],               // 基础流行节奏

            // 古典音乐：更复杂的节奏
            (MusicStyle.Classical, Emotion.Calm) => [1, 0, 0, 0, 1, 0, 1, 0, 0, 0, 1, 0],
            (MusicStyle.Classical, _) => [1, 0, 0, 1, 0, 1, 1, 0, 1, 0, 0, 1, 0, 1, 0, 0],

            // 爵士音乐：摇摆和切分节奏
            (MusicStyle.Jazz, _) => [1, 0, 1, 1, 1, 0, 1, 0],

            // 摇滚音乐：强节奏
            (MusicStyle.Rock, Emotion.Energetic) => [1, 1, 0, 1, 1, 0, 1, 1],
            (MusicStyle.Rock, _) => [1, 0, 0, 1, 1, 0, 1, 0],

            // 电子音乐：精确节奏
            (MusicStyle.Electronic, _) => [1, 1, 0, 0, 1, 1, 1, 1],

            // 布鲁斯音乐：12小节蓝调节奏
            (MusicStyle.Blues, _) => [1, 0, 1, 0, 1, 1, 0, 1, 0, 1, 0, 1],

            // 神秘情绪：不规则节奏
            (_, Emotion.Mysterious) => [1, 0, 1, 0, 0, 1, 0, 0, 1, 0],

            // 浪漫情绪：流畅节奏
            (_, Emotion.Romantic) => [1, 0, 1, 0, 0, 1, 0, 1, 0, 1],

            _ => [1, 0, 1, 0, 1, 0, 1, 0]
        };
    }

    /// <summary>
    /// 添加节奏变化和切分
    /// </summary>
    /// <param name="basePattern">基础节奏模式</param>
    /// <param name="style">音乐风格</param>
    /// <param name="emotion">情绪类型</param>
    /// <returns>带变化的节奏模式</returns>
    private List<int> AddRhythmVariations(List<int> basePattern, MusicStyle style, Emotion emotion)
    {
        var pattern = new List<int>(basePattern);
        int variationChance = 0;

        // 根据风格和情绪确定变化概率
        variationChance = style switch
        {
            MusicStyle.Jazz or MusicStyle.Blues => 40,// 40%概率变化
            MusicStyle.Classical => 35,
            MusicStyle.Electronic => 20,// 电子音乐更规则
            _ => 30,
        };

        // 情绪影响变化
        if (emotion == Emotion.Sad || emotion == Emotion.Calm)
        {
            variationChance -= 10;
        }
        else if (emotion == Emotion.Energetic || emotion == Emotion.Mysterious)
        {
            variationChance += 10;
        }

        // 避免过度变化，保持节奏的可识别性
        variationChance = Math.Max(20, Math.Min(45, variationChance));

        // 添加切分节奏
        if (_random.Next(100) < variationChance && pattern.Count >= 4)
        {
            AddSyncopation(pattern);
        }

        // 添加节奏填充
        if (_random.Next(100) < variationChance - 10 && pattern.Count >= 8)
        {
            AddRhythmicFill(pattern);
        }

        return pattern;
    }

    /// <summary>
    /// 添加切分节奏
    /// </summary>
    /// <param name="pattern">节奏模式</param>
    private void AddSyncopation(List<int> pattern)
    {
        // 选择切分位置（通常在强拍之间）
        int[] syncopationPositions = GetSyncopationPositions(pattern.Count);

        if (syncopationPositions.Length > 0)
        {
            int position = syncopationPositions[_random.Next(syncopationPositions.Length)];

            // 在弱拍上添加音符，创建切分效果
            if (position >= 0 && position < pattern.Count)
            {
                pattern[position] = 1;

                // 有时移除前一个强拍的音符，增强切分效果
                if (_random.Next(100) < 50 && position > 0)
                {
                    pattern[position - 1] = 0;
                }
            }
        }
    }

    /// <summary>
    /// 获取可能的切分位置
    /// </summary>
    /// <param name="patternLength">模式长度</param>
    /// <returns>可能的切分位置数组</returns>
    private int[] GetSyncopationPositions(int patternLength)
    {
        var positions = new List<int>();

        // 找到可能的切分位置（通常是弱拍）
        for (int i = 0; i < patternLength; i++)
        {
            // 避免在强拍位置切分（如第0、4、8拍）
            if (i % 4 != 0 && i % 4 != 2) // 16分音符的切分位置
            {
                positions.Add(i);
            }
        }

        return [.. positions];
    }

    /// <summary>
    /// 添加节奏填充
    /// </summary>
    /// <param name="pattern">节奏模式</param>
    private void AddRhythmicFill(List<int> pattern)
    {
        // 在模式的后半部分添加填充
        int startPos = pattern.Count / 2;
        int fillLength = Math.Min(4, pattern.Count - startPos);

        // 创建填充节奏（更密集的音符）
        for (int i = startPos; i < startPos + fillLength; i++)
        {
            if (_random.Next(100) < 70)
            {
                pattern[i] = 1;
            }
        }
    }
}