using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace VillageShop.Infrastructure.Uploader;

public record UploadWorkItem(Guid BatchId, long TenantId, long UserId);

public class UploadProcessingChannel
{
    private readonly Channel<UploadWorkItem> _channel = Channel.CreateBounded<UploadWorkItem>(new BoundedChannelOptions(500)
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = true
    });

    public async ValueTask QueueAsync(UploadWorkItem item, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(item, cancellationToken);
    }

    public IAsyncEnumerable<UploadWorkItem> ReadAllAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
