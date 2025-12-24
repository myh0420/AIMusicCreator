using AIMusicCreator.Web;
using AIMusicCreator.Web.Components;
using AIMusicCreator.Web.Services;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
// ���ӷ���
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor(options =>
{
    //options.DetailedErrors = true;
    //options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(2);
    //options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
    options.DetailedErrors = true;
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(5);
    options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(2); // 
    options.MaxBufferedUnacknowledgedRenderBatches = 10;
});
builder.Services.AddScoped<ConnectionStateService>();
builder.Services.AddScoped<IAudioPlayerService, AudioPlayerService>();
builder.Services.AddScoped<CircuitHandler, AppCircuitHandler>();
//builder.Services.AddSingleton<CircuitHandler, CircuitHandlerService>();
builder.Services.AddOutputCache();
builder.Services.AddScoped<AIMusicCreator.Web.Services.JsInteropService>();
builder.Services.AddScoped<ApiService>();
// ���� HTTP �ͻ��˳�ʱ
builder.Services.AddHttpClient("default", client =>
{
    client.Timeout = TimeSpan.FromMinutes(2);
});


builder.Services.AddHttpClient();
//builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7000") });
builder.Services.AddHttpClient<ApiService>(client =>
{
    // This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
    // Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
    client.BaseAddress = new("https://apiservice");
    client.Timeout = TimeSpan.FromMinutes(2);
});
// ���Ӵ������Բ��Ե� HttpClient
builder.Services.AddHttpClient<AudioPlayerService>()
    .AddPolicyHandler(GetRetryPolicy())
    .SetHandlerLifetime(TimeSpan.FromMinutes(5));
builder.Services.AddHttpClient<WeatherApiClient>(client =>
    {
        // This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
        // Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
        client.BaseAddress = new("https+http://apiservice");
    });
// ���ӽ������
builder.Services.AddHealthChecks()
    .AddCheck<AudioPlayerHealthCheck>("audio_player");

// �����ļ��ϴ���С����
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 500 * 1024 * 1024; // 500MB
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBoundaryLengthLimit = int.MaxValue;
    options.MultipartHeadersCountLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

// �����Ҫ������Kestrel
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 500 * 1024 * 1024; // 500MB
    serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
});

var app = builder.Build();
// ���ӽ������˵�
//app.MapHealthChecks("/health");
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
if (app.Environment.IsDevelopment())
{
    // �ڿ�����������ʾ��ϸ��·����Ϣ
    app.Use(async (context, next) =>
    {
        var endpoint = context.GetEndpoint();
        if (endpoint != null)
        {
            Console.WriteLine($"Endpoint: {endpoint.DisplayName}");
        }
        await next();
    });
}

app.UseHttpsRedirection();
//app.UseStaticFiles();
//app.UseRouting();
//app.MapBlazorHub();
//app.MapFallbackToPage("/_Host");

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();
//Configure(app);
app.Run();

// ����� .NET 5 ʹ�� Startup.cs
//static void ConfigureServices(IServiceCollection services)
//{
//    services.Configure<FormOptions>(options =>
//    {
//        options.MultipartBodyLengthLimit = 500 * 1024 * 1024; // 500MB
//    });

//    services.AddServerSideBlazor(options =>
//    {
//        options.DetailedErrors = true;
//        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(5);
//        options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(2); // ���� JS ��������ʱ
//        options.MaxBufferedUnacknowledgedRenderBatches = 10;
//    });
//}

//static void Configure(IApplicationBuilder app)
//{
//    app.UseStaticFiles();
//    app.UseRouting();
//    app.UseEndpoints(endpoints =>
//    {
//        endpoints.MapBlazorHub();
//        endpoints.MapFallbackToPage("/_Host");
//    });
//}
// ���Բ���
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => !msg.IsSuccessStatusCode)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Console.WriteLine($"���� {retryCount} �Σ��ȴ� {timespan} ������");
            });
}
// �������ʵ��
public class AudioPlayerHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // ʵ����Ľ�������߼�
        return Task.FromResult(HealthCheckResult.Healthy("��Ƶ��������������"));
    }
}
