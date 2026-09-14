namespace RSSFeedReader.Api.Services;

public sealed class SubscriptionStore
{
    private readonly List<string> subscriptions = [];
    private readonly Lock sync = new();

    public bool Add(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        lock (sync)
        {
            subscriptions.Add(value);
        }

        return true;
    }

    public string[] Snapshot()
    {
        lock (sync)
        {
            return subscriptions.ToArray();
        }
    }
}