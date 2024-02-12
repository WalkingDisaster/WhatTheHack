namespace TollBooth.Options;

public record EventGridTopicOptions(string AccountEndpoint, string AccessKey)
{
    public const string Section = "EventGridTopic";
}