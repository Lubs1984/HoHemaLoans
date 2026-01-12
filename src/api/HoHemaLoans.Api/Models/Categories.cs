namespace HoHemaLoans.Api.Models;

/// <summary>
/// Income source categories for worker-based lending
/// </summary>
public static class IncomeCategories
{
    public const string Employment = "Employment";
    public const string SelfEmployment = "SelfEmployment";
    public const string GovernmentGrants = "GovernmentGrants";
    public const string Other = "Other";

    public static readonly string[] All = 
    {
        Employment,
        SelfEmployment,
        GovernmentGrants,
        Other
    };

    public static readonly Dictionary<string, string> DisplayNames = new()
    {
        { Employment, "Employment (Salary/Wages)" },
        { SelfEmployment, "Self-Employment/Business" },
        { GovernmentGrants, "Government Grants" },
        { Other, "Other Income" }
    };
}

/// <summary>
/// Expense categories for affordability assessment
/// </summary>
public static class ExpenseCategories
{
    public const string Housing = "Housing";
    public const string Utilities = "Utilities";
    public const string Transport = "Transport";
    public const string Food = "Food";
    public const string Debt = "Debt";
    public const string Communication = "Communication";
    public const string Insurance = "Insurance";
    public const string Dependents = "Dependents";
    public const string Medical = "Medical";
    public const string Personal = "Personal";
    public const string Other = "Other";

    public static readonly string[] All = 
    {
        Housing,
        Utilities,
        Transport,
        Food,
        Debt,
        Communication,
        Insurance,
        Dependents,
        Medical,
        Personal,
        Other
    };

    public static readonly Dictionary<string, string> DisplayNames = new()
    {
        { Housing, "Housing (Rent/Bond)" },
        { Utilities, "Utilities (Water/Electricity)" },
        { Transport, "Transport (Fuel/Public)" },
        { Food, "Food & Groceries" },
        { Debt, "Debt Repayments" },
        { Communication, "Communication (Phone/Internet)" },
        { Insurance, "Insurance" },
        { Dependents, "Dependents (Childcare/Support)" },
        { Medical, "Medical & Healthcare" },
        { Personal, "Personal & Lifestyle" },
        { Other, "Other Expenses" }
    };

    public static readonly Dictionary<string, bool> EssentialByDefault = new()
    {
        { Housing, true },
        { Utilities, true },
        { Transport, true },
        { Food, true },
        { Debt, true },
        { Communication, true },
        { Insurance, true },
        { Dependents, true },
        { Medical, true },
        { Personal, false },
        { Other, false }
    };
}
