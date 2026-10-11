namespace FieldOps.Core.Models;

public enum WorkOrderStatus { New, Scheduled, InProgress, Completed }

public sealed record WorkOrder(Guid Id, string Number, string CustomerName, string Summary, string Address, DateTimeOffset ScheduledFor, WorkOrderStatus Status);