using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Lumen.Application.Leaderboards;
using Lumen.Application.Messages;
using Lumen.Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lumen.Infrastructure.Messaging;

/// <summary>
/// Long-polls the quiz-attempts queue and runs <see cref="RecalculateLeaderboardTask"/>. The message is
/// deleted when the handler returns (as in production, see log extract A5).
/// </summary>
internal sealed class SqsQuizAttemptConsumer : BackgroundService
{
    private readonly IAmazonSQS _sqs;
    private readonly SqsQueueProvisioner _queues;
    private readonly SqsOptions _options;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<SqsQuizAttemptConsumer> _log;

    public SqsQuizAttemptConsumer(
        IAmazonSQS sqs,
        SqsQueueProvisioner queues,
        IOptions<SqsOptions> options,
        IServiceScopeFactory scopes,
        ILogger<SqsQuizAttemptConsumer> log)
    {
        _sqs = sqs;
        _queues = queues;
        _options = options.Value;
        _scopes = scopes;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield so host start-up is never blocked by the polling loop.
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var response = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = _queues.QueueUrl,
                    MaxNumberOfMessages = 1,
                    WaitTimeSeconds = _options.ReceiveWaitTimeSeconds,
                    MessageSystemAttributeNames = ["ApproximateReceiveCount"]
                }, stoppingToken);

                foreach (var message in response.Messages ?? [])
                    await HandleAsync(message, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "SQS receive loop failed; retrying in 5s");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task HandleAsync(Message message, CancellationToken ct)
    {
        var msg = JsonSerializer.Deserialize<QuizAttemptCompleted>(message.Body);
        if (msg is null)
        {
            _log.LogError("Message {MessageId} has an unreadable body; leaving it for the DLQ", message.MessageId);
            return;
        }

        message.Attributes.TryGetValue("ApproximateReceiveCount", out var receiveCount);
        _log.LogInformation("RecalculateLeaderboardTask start messageId={MessageId} attemptKey={AttemptKey} receiveCount={ReceiveCount}",
            message.MessageId, msg.AttemptKey, receiveCount);

        using var scope = _scopes.CreateScope();
        await scope.ServiceProvider.GetRequiredService<RecalculateLeaderboardTask>().Execute(msg);

        await _sqs.DeleteMessageAsync(_queues.QueueUrl, message.ReceiptHandle, ct);
        _log.LogInformation("sqs.delete messageId={MessageId} (handler returned)", message.MessageId);
    }
}
