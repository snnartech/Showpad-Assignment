using System.Text.Json;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Lumen.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lumen.Infrastructure.Messaging;

/// <summary>
/// Creates the "quiz-attempts" queue + dead-letter queue (mirrors lumen-infra/stacks/queue.ts) and
/// exposes the queue URL. Works against LocalStack locally, no AWS account needed.
/// </summary>
public sealed class SqsQueueProvisioner
{
    private readonly IAmazonSQS _sqs;
    private readonly SqsOptions _options;
    private readonly ILogger<SqsQueueProvisioner> _log;
    private string? _queueUrl;

    public SqsQueueProvisioner(IAmazonSQS sqs, IOptions<SqsOptions> options, ILogger<SqsQueueProvisioner> log)
    {
        _sqs = sqs;
        _options = options.Value;
        _log = log;
    }

    public string QueueUrl => _queueUrl ?? throw new InvalidOperationException("Queues are not provisioned yet.");

    public async Task EnsureQueuesAsync(CancellationToken ct = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var dlq = await _sqs.CreateQueueAsync(new CreateQueueRequest { QueueName = _options.DeadLetterQueueName }, ct);
                var dlqAttributes = await _sqs.GetQueueAttributesAsync(dlq.QueueUrl, ["QueueArn"], ct);

                var redrive = JsonSerializer.Serialize(new
                {
                    deadLetterTargetArn = dlqAttributes.Attributes["QueueArn"],
                    maxReceiveCount = _options.MaxReceiveCount
                });

                var main = await _sqs.CreateQueueAsync(new CreateQueueRequest
                {
                    QueueName = _options.QueueName,
                    Attributes = new Dictionary<string, string>
                    {
                        ["VisibilityTimeout"] = _options.VisibilityTimeoutSeconds.ToString(),
                        ["ReceiveMessageWaitTimeSeconds"] = _options.ReceiveWaitTimeSeconds.ToString(),
                        ["RedrivePolicy"] = redrive
                    }
                }, ct);

                _queueUrl = main.QueueUrl;
                _log.LogInformation("SQS queue {Queue} ready at {Url}", _options.QueueName, _queueUrl);
                return;
            }
            catch (Exception ex) when (attempt < 30 && ex is AmazonServiceException or HttpRequestException or TaskCanceledException)
            {
                _log.LogWarning("SQS endpoint not ready ({Message}); retry {Attempt}/30", ex.Message, attempt);
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
            }
        }
    }

    internal static IAmazonSQS CreateClient(SqsOptions o)
    {
        var config = new AmazonSQSConfig();
        if (!string.IsNullOrWhiteSpace(o.ServiceUrl))
        {
            config.ServiceURL = o.ServiceUrl;
            config.AuthenticationRegion = o.Region;
            return new AmazonSQSClient(new BasicAWSCredentials(o.AccessKey, o.SecretKey), config);
        }

        config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(o.Region);
        return new AmazonSQSClient(config);
    }
}
