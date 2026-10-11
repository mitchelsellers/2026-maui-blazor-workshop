using System.ComponentModel.DataAnnotations;

namespace FieldOps.Core.Models;

public sealed class UpdateWorkOrderRequest
{
    [Required]
    [StringLength(200, MinimumLength = 5)]
    public string Summary { get; set; } = "";
    [EnumDataType(typeof(WorkOrderStatus))]
    public WorkOrderStatus Status { get; set; }
}