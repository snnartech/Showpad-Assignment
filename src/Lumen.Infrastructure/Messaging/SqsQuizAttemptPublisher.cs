using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Lumen.Application.Abstractions;
using Lumen.Application.Messages;

namespace Lumen.Infrastructure.Messaging;

internal sealed class SqsQuizAttemptPublisher : IQuizAttemptPublisher
{
    private readonly IAmazonSQS _sqs;
    private readonly SqsQueueProvisioner _queues;

    public SqsQuizAttemptPublisher(IAmazonSQS sqs, SqsQueueProvisioner queues)
    {
        _sqs = sqs;
        _queues = queues;
    }

    public async Task<string> PublishAsync(QuizAttemptCompleted message, CancellationToken ct = default)
    {
        var response = await _sqs.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = _queues.QueueUrl,
            MessageBody = JsonSerializer.Serialize(message),
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["MessageType"] = new() { DataType = "String", StringValue = nameof(QuizAttemptCompleted) }
            }
        }, ct);
        return response.MessageId;
    }
}
