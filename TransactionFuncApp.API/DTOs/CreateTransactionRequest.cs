namespace TransactionFuncApp.API.DTOs;

public class CreateTransactionRequest
{
    public string type { get; set; } = default!;
    public string category { get; set; } = default!;
    public string description { get; set; } = default!;
    public string? relatedTo { get; set; }

    public decimal amount { get; set; }
    public string currency { get; set; } = default!;

    public string installmentMode { get; set; } = "None";
    public int? installmentCount { get; set; }
    public int? installmentInterval { get; set; } // days

    public string? dueDate { get; set; } // optional ISO date
}
