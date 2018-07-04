using System;

namespace AsyncWorkshop.UsagePatterns.Helpers
{
    public enum NotifcationType
    {
        Clear,
        Append,
        Update
    }

    public class Notification
    {
        public static Notification Clear { get; } = new Notification(NotifcationType.Clear);

        public static Func<string, Notification> Append = message => new Notification(NotifcationType.Append, message);

        public static Func<string, string, Notification> Update = (identifier, update) => new Notification(NotifcationType.Update, update, identifier);

        public NotifcationType Type { get; }

        public string Id { get; }

        public string Message { get; }

        private Notification(NotifcationType type, string message = null, string id = null)
        {
            Type = type;
            Id = id;
            Message = message;
        }
    }
}
