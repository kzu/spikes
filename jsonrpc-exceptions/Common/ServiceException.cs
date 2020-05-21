using System;
using System.Runtime.Serialization;

namespace Common
{
    public class ServiceException : Exception
    {
        public ServiceException(string message, Guid eventId, ServiceKind serviceKind) : base(message)
        {
            EventId = eventId;
            ServiceKind = serviceKind;
        }

        public ServiceException(string message, Guid eventId, ServiceKind serviceKind, Exception innerException) : base(message, innerException)
        {
            EventId = eventId;
            ServiceKind = serviceKind;
        }

        public Guid EventId { get; }

        public ServiceKind ServiceKind { get; }

        protected ServiceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            EventId = Guid.Parse(info.GetString(nameof(EventId)));
            ServiceKind = (ServiceKind)info.GetInt32(nameof(ServiceKind));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(EventId), EventId.ToString());
            info.AddValue(nameof(ServiceKind), (int)ServiceKind);
        }
    }
}
