using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace VillageShop.Infrastructure.Uploader;

public class UploadProcessingBackgroundService : BackgroundService
{
    private readonly UploadProcessingChannel _channel;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UploadProcessingBackgroundService> _logger;

    public UploadProcessingBackgroundService(
        UploadProcessingChannel channel,
        IServiceProvider serviceProvider,
        ILogger<UploadProcessingBackgroundService> logger)
    {
        _channel = channel;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("UploadProcessingBackgroundService started.");

        await foreach (var item in _channel.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var uploaderService = scope.ServiceProvider.GetRequiredService<IExcelUploaderService>();
                await uploaderService.ProcessBatchAsync(item.BatchId, item.TenantId, item.UserId, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal background exception while processing BatchId {BatchId}", item.BatchId);
            }
        }
    }
}
