public class PercentageInstallmentDto
{
    public int     installmentNum { get; set; }
    public decimal percentage     { get; set; } // e.g. 30.5 means 30.5%
    public DateTime?  expectedDate   { get; set; }
}