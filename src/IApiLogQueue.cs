namespace ApiLogger;

public interface IApiLogQueue<TPayload>
{
    void Enqueue(ApiLogItem<TPayload> item);
}

