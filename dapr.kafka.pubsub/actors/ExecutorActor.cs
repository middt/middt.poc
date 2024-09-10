using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapr.Actors.Runtime;
using Dapr.Client;

public class ExecutorActor : Actor, IExecutorActor
{
    protected virtual string RecordStateName => "statestore";
    protected virtual int MaxRetries => 3;
    protected virtual int RetryDelayMs => 1000;
    protected virtual string PubSubName => "pubsub";
    protected virtual string DeadLetterTopic => "deadletter";

    private readonly DaprClient _client;

    public ExecutorActor(ActorHost host, DaprClient client) : base(host)
    {
        _client = client;
    }

    private async Task<bool> CheckRecordExistsAsync()
    {
        return await StateManager.ContainsStateAsync(RecordStateName);
    }

    public async Task<bool> ExecuteAndAddRecordAsync(Func<Task<bool>> action)
    {
        if (!await CheckRecordExistsAsync())
        {
            for (int retry = 0; retry < MaxRetries; retry++)
            {
                bool actionResult = await action();
                if (actionResult)
                {
                    // await StateManager.SaveStateAsync(RecordStateName, id, id); .SetStateAsync(RecordStateName, "Record initialized",);
                    return true;
                }

                if (retry < MaxRetries - 1)
                {
                    await Task.Delay(RetryDelayMs);
                }
            }

            // If all retries fail, publish to dead letter queue
            await PublishToDeadLetterQueueAsync();
            return false;
        }
        return false;
    }

    private async Task PublishToDeadLetterQueueAsync()
    {
        var deadLetterMessage = new
        {
            ActorId = this.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            Message = "Failed to execute action after multiple retries"
        };

        await _client.PublishEventAsync(PubSubName, DeadLetterTopic, deadLetterMessage);
    }

}
