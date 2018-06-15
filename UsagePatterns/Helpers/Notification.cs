using System;

namespace AsyncWorkshop.UsagePatterns.Helpers
{
    public enum NotifcationType
    {
        Clear,
        Append,
        Update
    }

    public class Notifcation
    {
        public static Notifcation Clear { get; } = new Notifcation(NotifcationType.Clear);

        public static Func<string, Notifcation> Append = message => new Notifcation(NotifcationType.Append, message);

        public static Func<string, string, Notifcation> Update = (identifier, update) => new Notifcation(NotifcationType.Update, update, identifier);

        public NotifcationType Type { get; }

        public string Id { get; }

        public string Message { get; }

        private Notifcation(NotifcationType type, string message = null, string id = null)
        {
            Type = type;
            Id = id;
            Message = message;
        }
    }
}
