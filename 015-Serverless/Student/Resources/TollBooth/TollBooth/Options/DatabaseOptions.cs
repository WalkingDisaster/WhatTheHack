namespace TollBooth.Options;

public record DatabaseOptions(string DatabaseId, string ContainerId, string AccountEndpoint)
{
    public const string Section = "Database";
};