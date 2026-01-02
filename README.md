# AIMusicCreator

AIMusicCreator 是一个基于 .NET 的 AI 音乐创作平台，提供旋律生成、MIDI 处理、音频合成等功能。

## 功能特性

### 核心功能
- 🎵 **旋律生成**：支持基于风格、情绪和 BPM 参数化生成旋律
- 🎹 **MIDI 处理**：MIDI 文件的创建、编辑、解析和转换
- 🎤 **音频合成**：基于 SoundFont 的 MIDI 音频合成
- 🎼 **和声生成**：自动生成和弦进行
- 🎛️ **音频效果**：支持混响、延迟、失真等音频效果处理

### AI 能力
- 🧠 **ONNX 模型推理**：支持使用预训练模型生成旋律
- 🤖 **降级方案**：当模型不可用时，自动切换到基于音乐理论的旋律生成
- 📊 **参数化控制**：支持通过温度、调号等参数控制生成结果

## 技术栈

### 后端
- **.NET 9**：主要开发框架
- **ASP.NET Core**：Web API 服务
- **ONNX Runtime**：AI 模型推理
- **NAudio**：音频处理和 MIDI 合成
- **DryWetMIDI**：MIDI 文件处理

### 前端
- **Blazor WebAssembly**：交互式前端界面
- **ASP.NET Core SignalR**：实时通信

### 数据处理
- **SoundFont**：MIDI 音色库支持
- **Git LFS**：大文件版本控制

## 项目结构

```
AIMusicCreator/
├── AIMusicCreator.ApiService/      # API 服务
│   ├── Controllers/               # API 控制器
│   ├── Interfaces/                # 服务接口
│   ├── Services/                  # 业务服务
│   │   ├── DryWetMidiGerenteMidi/ # MIDI 处理服务
│   │   ├── MidiService.cs         # MIDI 生成服务
│   │   └── VocalService.cs        # 人声处理服务
│   └── wwwroot/                   # 静态资源
│       ├── models/                # AI 模型
│       └── soundfonts/            # 音色库
├── AIMusicCreator.AppHost/         # 应用主机
├── AIMusicCreator.Entity/          # 实体模型
├── AIMusicCreator.ServiceDefaults/ # 服务默认配置
├── AIMusicCreator.Tests/           # 测试项目
├── AIMusicCreator.Utils/           # 工具类
└── AIMusicCreator.Web/             # Web 前端
```

## 快速开始

### 环境要求
- .NET 9 SDK
- Git (可选，用于克隆仓库)
- Git LFS (可选，用于获取 AI 模型)

### 安装步骤

1. **克隆仓库**

```bash
git clone <repository-url>
cd AIMusicCreator
```

2. **获取 AI 模型**

如果使用 Git LFS：

```bash
git lfs install
git lfs pull
```

或者手动下载模型文件并放置在 `AIMusicCreator.ApiService/wwwroot/models/` 目录下。

3. **构建项目**

```bash
dotnet build
```

4. **运行服务**

```bash
# 运行 API 服务
dotnet run --project AIMusicCreator.ApiService

# 运行 Web 前端
dotnet run --project AIMusicCreator.Web
```

### 访问应用

- API 服务：http://localhost:5591
- Web 前端：http://localhost:5283

## 使用指南

### 1. 生成旋律

#### API 端点
```
POST /api/MusicGenerator/generate-melody
```

#### 请求参数
```json
{
  "style": "classical",   // 音乐风格：classical, electronic, pop
  "mood": "happy",       // 音乐情绪：happy, sad
  "bpm": 120              // 每分钟节拍数：1-200
}
```

#### 响应
返回生成的 MIDI 文件字节流。

### 2. MIDI 分析

#### API 端点
```
POST /api/MidiEditor/analyze
```

#### 请求参数
```json
{
  "midiBytes": "base64-encoded-midi-data"
}
```

#### 响应
返回 MIDI 文件的详细分析报告。

## 配置说明

### 应用配置

应用配置文件位于 `AIMusicCreator.ApiService/appsettings.json` 和 `appsettings.Development.json`。

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "OpenAI": {
    "ApiKey": "your-api-key",
    "ApiEndpoint": "https://api.openai.com/v1/chat/completions"
  },
  "AllowedHosts": "*"
}
```

### 模型配置

- **ONNX 模型**：`sageconv_Opset18.onnx`
  - 位于 `wwwroot/models/` 目录
  - 用于旋律生成
  - 支持 ONNX Runtime 推理

### 音色库配置

- **SoundFont 文件**：位于 `wwwroot/soundfonts/` 目录
- 默认使用：`GeneralUser GS v1.471.sf2`

## 开发说明

### 项目启动

1. **API 服务**
   - 入口：`AIMusicCreator.ApiService/Program.cs`
   - 端口：5591 (Development)

2. **Web 前端**
   - 入口：`AIMusicCreator.Web/Program.cs`
   - 端口：5283 (Development)

### 服务架构

- **MidiService**：核心 MIDI 生成服务
- **AudioService**：音频处理服务
- **VocalService**：人声处理服务
- **OpenAIService**：OpenAI API 集成

### 异常处理

- **模型加载失败**：自动切换到基于音乐理论的降级方案
- **音频处理失败**：提供详细的错误日志和备选方案
- **MIDI 解析失败**：容错处理，支持部分解析

## 贡献指南

1. **Fork 仓库**
2. **创建特性分支**
3. **提交更改**
4. **创建 Pull Request**

### 代码规范

- 遵循 .NET 编码规范
- 使用 C# 12 特性
- 添加详细的 XML 文档注释
- 编写单元测试

## 许可证

MIT License

## 联系方式

如有问题或建议，请通过以下方式联系：

- 项目维护者：[laoma]
- 邮箱：暂无//[your-email@example.com]
- GitHub Issues：暂无//[repository-url]/issues

## 更新日志

### v1.0.0 (2025-11-07)
- 初始版本发布
- 支持旋律生成功能
- 实现 MIDI 处理和音频合成
- 集成 ONNX 模型推理

---

© 2025 AIMusicCreator 团队