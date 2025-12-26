namespace AggregationFunction.Models;

public class AggregateDocument
{
    public string Id { get; set; }
    public string TenantId { get; set; }
    public string CompanyId { get; set; }
    public decimal TotalInflow { get; set; }
    public decimal TotalOutflow { get; set; }
    public DateTime Period { get; set; }
    public string PeriodString { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
