using AIMusicCreator.ApiService.Interfaces;
using AIMusicCreator.ApiService.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
// 添加音乐生成相关服务
builder.Services.AddMusicGenerationServices();
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
// 添加HTTP日志记录服务
builder.Services.AddHttpLogging();
// 添加响应压缩服务
builder.Services.AddResponseCompression();
// Configure CORS policy for API access
// 允许所有来源、方法和头信息，同时暴露 Content-Disposition 头用于文件下载
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Content-Disposition"); // Expose content disposition for file downloads
    });
});

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.Services.AddScoped<IAudioService, AudioService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IOpenAIService>(provider => 
{
    var httpClient = provider.GetRequiredService<HttpClient>();
    var logger = provider.GetRequiredService<ILogger<OpenAIService>>();
    var apiKey = builder.Configuration["OpenAI:ApiKey"] ?? "";
    var apiEndpoint = builder.Configuration["OpenAI:ApiEndpoint"] ?? "https://api.openai.com/v1/chat/completions";
    
    return new OpenAIService(httpClient, apiKey, apiEndpoint, logger);
});
builder.Services.AddScoped<IMidiService, MidiService>();
builder.Services.AddScoped<IVocalService, VocalService>();
builder.Services.AddScoped<IAudioEffectService, AudioEffectService>();
builder.Services.AddScoped<IMidiEditorService, MidiEditorService>();
builder.Services.AddScoped<IFacade, Facade>();
builder.Services.AddScoped<IFlacConverter, FlacConverter>();
//builder.Services.AddScoped<MidiFileGenerator>(); // 移除重复注册，已在AddMusicGenerationServices中注册
//builder.Services.AddMvc();
// Add services to the container.
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
        ctx.ProblemDetails.Status = ctx.HttpContext.Response.StatusCode;
        if (ctx.Exception is ArgumentException argumentException)
        {
            ctx.ProblemDetails.Title = "Invalid argument";
            ctx.ProblemDetails.Detail = argumentException.Message;
            ctx.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    };
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();


// Configure middleware for development environment
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Add request logging middleware
app.UseHttpLogging();

// Use CORS middleware (must come before UseRouting)
app.UseCors("AllowAll");

app.UseRouting();

// Add response compression for better performance
app.UseResponseCompression();

// 
app.MapControllers();
//app.MapBlazorHub();
//app.MapFallbackToPage("/_Host");
// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


app.MapDefaultEndpoints();

app.Run();












