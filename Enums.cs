namespace Omi.Education.Services.Notification.Enums
{
    public enum ConnectionType
    {
        Service = 1,
        Client = 2,
        Hub = 4,
        Self = 8,
        Base = 16,
        Cache = 32,
        Group = 64,
        SubHub = 128
    }

    public enum MessageType
    {
        Command = 1,
        Message = 2
    }

    public enum ReplyMethodName
    {
        Connection = 1,
        Subscribe = 2,
        DeSubscribe = 4
    }
    public enum ReplyStatus
    {
        Success = 1,
        Error = 2,
    }
}