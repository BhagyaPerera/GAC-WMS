using SharedKernal.Interfaces;

namespace Core.Entities.Logging
{
    public class SalesOrderIncomingLog : BaseEntity,IAggregateRoot
    {
        public string? SalesOrderXmlContent { get; set; }
        public bool? IsErrored { get; set; }
        public string? ErrorMessage { get; set; }


        public SalesOrderIncomingLog(string? payload, bool? isError, string? errorMessage)
        {
            SalesOrderXmlContent = payload;
            IsErrored = isError;
            ErrorMessage = errorMessage;
        }

        public SalesOrderIncomingLog() { }
    }
}
