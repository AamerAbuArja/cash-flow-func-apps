
namespace TransactionFuncApp.API.Enums.TransactionEnums;

public enum InstallmentMode
{
    None,
    Manual,
    Auto,
    Percentage,
    Recurring
}

public enum TransactionType
{
    Income,
    Expense
}

public enum RecurringFrequency
{
    Weekly,
    Monthly,
    Quarterly,
    Yearly
}