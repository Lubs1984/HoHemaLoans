using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HoHemaLoans.Api.Services;

/// <summary>
/// Service for generating PDF documents for Form 39 Credit Agreements
/// and Pre-Agreement Statements using QuestPDF.
/// </summary>
public interface IPdfGenerationService
{
    byte[] GenerateForm39Pdf(Form39Data data);
    byte[] GeneratePreAgreementPdf(PreAgreementStatementData data);
}

public class PdfGenerationService : IPdfGenerationService
{
    private readonly ILogger<PdfGenerationService> _logger;

    // Brand colours
    private static readonly string PrimaryColor = "#1a365d";   // Dark navy
    private static readonly string AccentColor = "#2563eb";    // Blue
    private static readonly string LightBg = "#f8fafc";        // Light grey
    private static readonly string WarningBg = "#fef3c7";      // Amber light
    private static readonly string InfoBg = "#dbeafe";         // Blue light
    private static readonly string BorderColor = "#cbd5e1";    // Slate border

    public PdfGenerationService(ILogger<PdfGenerationService> logger)
    {
        _logger = logger;
    }

    public byte[] GenerateForm39Pdf(Form39Data data)
    {
        _logger.LogInformation("Generating Form 39 PDF for loan application {LoanApplicationId}", data.LoanApplicationId);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginTop(1.5f, Unit.Centimetre);
                page.MarginBottom(1.5f, Unit.Centimetre);
                page.MarginHorizontal(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Black));

                page.Header().Element(header => ComposeForm39Header(header));
                page.Content().Element(content => ComposeForm39Content(content, data));
                page.Footer().Element(footer => ComposeForm39Footer(footer, data));
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GeneratePreAgreementPdf(PreAgreementStatementData data)
    {
        _logger.LogInformation("Generating Pre-Agreement Statement PDF");

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginTop(1.5f, Unit.Centimetre);
                page.MarginBottom(1.5f, Unit.Centimetre);
                page.MarginHorizontal(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Black));

                page.Header().Element(header => ComposePreAgreementHeader(header));
                page.Content().Element(content => ComposePreAgreementContent(content, data));
                page.Footer().Element(footer => ComposePreAgreementFooter(footer, data));
            });
        });

        return document.GeneratePdf();
    }

    // ─── Form 39 Composition ────────────────────────────────────────────

    private void ComposeForm39Header(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().BorderBottom(2).BorderColor(PrimaryColor).PaddingBottom(8).Column(inner =>
            {
                inner.Item().AlignCenter().Text("FORM 39").FontSize(20).Bold().FontColor(PrimaryColor);
                inner.Item().AlignCenter().Text("CREDIT AGREEMENT").FontSize(14).Bold().FontColor(PrimaryColor);
                inner.Item().AlignCenter().PaddingTop(4).Text("National Credit Act, 2005 (Act No. 34 of 2005)").FontSize(9).Italic();
                inner.Item().AlignCenter().Text("Regulation 24(1)").FontSize(9).Italic();
            });
        });
    }

    private void ComposeForm39Content(IContainer container, Form39Data data)
    {
        container.PaddingTop(10).Column(col =>
        {
            col.Spacing(12);

            // Section 1: Credit Provider Details
            col.Item().Element(c => SectionTitle(c, "1. CREDIT PROVIDER DETAILS"));
            col.Item().Element(c => ComposeProviderDetails(c, data));

            // Section 2: Consumer Details
            col.Item().Element(c => SectionTitle(c, "2. CONSUMER DETAILS"));
            col.Item().Element(c => ComposeConsumerDetails(c, data));

            // Section 3: Credit Agreement Details
            col.Item().Element(c => SectionTitle(c, "3. CREDIT AGREEMENT DETAILS"));
            col.Item().Element(c => ComposeLoanDetailsTable(c, data));

            // Section 4: Repayment Schedule Summary
            col.Item().Element(c => SectionTitle(c, "4. REPAYMENT SCHEDULE"));
            col.Item().Element(c => ComposeRepaymentSchedule(c, data));

            // Section 5: Affordability Assessment
            col.Item().Element(c => SectionTitle(c, "5. AFFORDABILITY ASSESSMENT"));
            col.Item().Element(c => ComposeAffordabilitySection(c, data));

            // Section 6: Consumer Rights
            col.Item().Element(c => SectionTitle(c, "6. IMPORTANT CONSUMER RIGHTS"));
            col.Item().Element(c => ComposeConsumerRights(c, data));

            // Section 7: Terms and Conditions
            col.Item().Element(c => SectionTitle(c, "7. TERMS AND CONDITIONS"));
            col.Item().Element(c => ComposeTermsAndConditions(c));

            // Section 8: Declarations
            col.Item().Element(c => SectionTitle(c, "8. DECLARATIONS"));
            col.Item().Element(c => ComposeDeclarations(c));

            // Section 9: Signatures
            col.Item().Element(c => SectionTitle(c, "9. SIGNATURES"));
            col.Item().Element(c => ComposeSignatures(c));
        });
    }

    private void ComposeProviderDetails(IContainer container, Form39Data data)
    {
        container.Background(LightBg).Border(1).BorderColor(BorderColor).Padding(10).Column(col =>
        {
            col.Spacing(4);
            col.Item().Element(c => FieldRow(c, "Name:", data.CreditProviderName));
            col.Item().Element(c => FieldRow(c, "NCRCP Registration:", data.NCRCPRegistrationNumber));
            col.Item().Element(c => FieldRow(c, "Address:", data.CreditProviderAddress));
        });
    }

    private void ComposeConsumerDetails(IContainer container, Form39Data data)
    {
        container.Background(LightBg).Border(1).BorderColor(BorderColor).Padding(10).Column(col =>
        {
            col.Spacing(4);
            col.Item().Element(c => FieldRow(c, "Full Name:", data.ConsumerName));
            col.Item().Element(c => FieldRow(c, "ID Number:", data.ConsumerIdNumber));
            col.Item().Element(c => FieldRow(c, "Address:", data.ConsumerAddress));
            col.Item().Element(c => FieldRow(c, "Phone:", data.ConsumerPhone));
            col.Item().Element(c => FieldRow(c, "Email:", data.ConsumerEmail));
        });
    }

    private void ComposeLoanDetailsTable(IContainer container, Form39Data data)
    {
        container.Column(col =>
        {
            col.Item().Element(c => FieldRow(c, "Agreement Date:", data.AgreementDate.ToString("dd MMMM yyyy")));
            col.Item().PaddingTop(6);

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Background(PrimaryColor).Padding(6)
                        .Text("Description").FontColor(Colors.White).Bold().FontSize(10);
                    header.Cell().Background(PrimaryColor).Padding(6).AlignRight()
                        .Text("Amount").FontColor(Colors.White).Bold().FontSize(10);
                });

                // Rows
                ResetRowIndex();
                TableRow(table, "Principal Debt", $"R {data.PrincipalDebt:N2}");
                TableRow(table, "Interest Rate (per annum)", $"{data.InterestRate:F2}%");
                TableRow(table, "Initiation Fee", $"R {data.InitiationFee:N2}");
                TableRow(table, "Monthly Service Fee", $"R {data.MonthlyServiceFee:N2}");
                TableRow(table, "Term", $"{data.TermInMonths} months");
                TableRowBold(table, "Monthly Installment", $"R {data.MonthlyInstallment:N2}");
                TableRowBold(table, "Total Amount Payable", $"R {data.TotalAmountPayable:N2}");
            });
        });
    }

    private void ComposeRepaymentSchedule(IContainer container, Form39Data data)
    {
        var firstPaymentDate = data.AgreementDate.AddMonths(1);

        container.Column(col =>
        {
            col.Spacing(4);
            col.Item().Element(c => FieldRow(c, "First Payment Date:", firstPaymentDate.ToString("dd MMMM yyyy")));
            col.Item().Element(c => FieldRow(c, "Payment Frequency:", "Monthly"));
            col.Item().Element(c => FieldRow(c, "Number of Payments:", data.TermInMonths.ToString()));
            col.Item().Element(c => FieldRow(c, "Monthly Amount:", $"R {data.MonthlyInstallment:N2}"));
            col.Item().Element(c => FieldRow(c, "Final Payment Date:", firstPaymentDate.AddMonths(data.TermInMonths - 1).ToString("dd MMMM yyyy")));
        });
    }

    private void ComposeAffordabilitySection(IContainer container, Form39Data data)
    {
        container.Column(col =>
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().Background(PrimaryColor).Padding(6)
                        .Text("Metric").FontColor(Colors.White).Bold().FontSize(10);
                    header.Cell().Background(PrimaryColor).Padding(6).AlignRight()
                        .Text("Value").FontColor(Colors.White).Bold().FontSize(10);
                });

                ResetRowIndex();
                TableRow(table, "Monthly Income", $"R {data.MonthlyIncome:N2}");
                TableRow(table, "Monthly Expenses", $"R {data.MonthlyExpenses:N2}");
                TableRow(table, "Disposable Income", $"R {(data.MonthlyIncome - data.MonthlyExpenses):N2}");
                TableRow(table, "Proposed Installment", $"R {data.MonthlyInstallment:N2}");
                TableRowBold(table, "Debt-to-Income Ratio", $"{data.DebtToIncomeRatio:F2}%");
            });

            col.Item().PaddingTop(6).Text(text =>
            {
                text.Span("NCR Maximum DTI: ").Bold();
                text.Span("35% — ");
                if (data.DebtToIncomeRatio <= 35)
                    text.Span("COMPLIANT").Bold().FontColor("#16a34a");
                else
                    text.Span("NON-COMPLIANT").Bold().FontColor("#dc2626");
            });
        });
    }

    private void ComposeConsumerRights(IContainer container, Form39Data data)
    {
        container.Background(WarningBg).Border(1).BorderColor("#f59e0b").Padding(12).Column(col =>
        {
            col.Spacing(8);

            col.Item().Text(text =>
            {
                text.Span("Cooling-off Period: ").Bold();
                text.Span($"You have {data.CoolingOffPeriodDays} business days from signing this agreement to cancel without penalty, as per Section 121 of the National Credit Act.");
            });

            col.Item().Text(text =>
            {
                text.Span("Right to Information: ").Bold();
                text.Span("You have the right to receive a statement of account within 7 business days of request, as per Section 108 of the NCA.");
            });

            col.Item().Text(text =>
            {
                text.Span("Early Settlement: ").Bold();
                text.Span("You may settle your account in full at any time, with a maximum early termination fee as prescribed in Section 125 of the NCA.");
            });

            col.Item().Text(text =>
            {
                text.Span("Complaints: ").Bold();
                text.Span(data.ComplaintsProcedure);
            });

            col.Item().Text(text =>
            {
                text.Span("National Credit Regulator: ").Bold();
                text.Span("127 – 15th Road, Randjespark, Midrand, 1685. Tel: 0860 627 627. Website: www.ncr.org.za");
            });
        });
    }

    private void ComposeTermsAndConditions(IContainer container)
    {
        container.Column(col =>
        {
            col.Spacing(4);

            var terms = new[]
            {
                "The consumer agrees to repay the total amount payable in the manner set out in this agreement.",
                "Interest will be calculated on the reducing balance of the principal debt at the rate specified above.",
                "All fees charged are in accordance with the National Credit Act fee caps as gazetted.",
                "Late payments may attract penalty interest at the prescribed rate under the NCA.",
                "The credit provider may not unilaterally increase the interest rate or any fee without written notice.",
                "This agreement is governed by the laws of the Republic of South Africa and subject to the jurisdiction of the South African courts.",
                "The consumer acknowledges that all information provided in the application is true and correct.",
                "The credit provider is obligated to conduct an affordability assessment before granting credit, as per Section 81 of the NCA."
            };

            for (int i = 0; i < terms.Length; i++)
            {
                col.Item().Text($"{i + 1}. {terms[i]}").FontSize(9);
            }
        });
    }

    private void ComposeDeclarations(IContainer container)
    {
        container.Column(col =>
        {
            col.Spacing(6);

            col.Item().Text("The consumer hereby declares that:").Bold().FontSize(10);

            var declarations = new[]
            {
                "I have read and understood this credit agreement in full.",
                "I have received a copy of the pre-agreement statement and quotation.",
                "The information I have provided is true and accurate to the best of my knowledge.",
                "I understand that I have 5 business days to cancel this agreement without penalty.",
                "I have been informed of my right to receive a statement of account.",
                "I understand the total cost of credit and my monthly repayment obligations."
            };

            foreach (var decl in declarations)
            {
                col.Item().Row(row =>
                {
                    row.AutoItem().PaddingRight(6).Text("☐").FontSize(12);
                    row.RelativeItem().Text(decl).FontSize(9);
                });
            }
        });
    }

    private void ComposeSignatures(IContainer container)
    {
        container.PaddingTop(10).Column(col =>
        {
            col.Item().Text("By signing below, both parties agree to the terms and conditions of this credit agreement.").FontSize(9).Italic();
            col.Item().PaddingTop(20);

            col.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text("Consumer:").Bold();
                    left.Item().PaddingTop(30).BorderBottom(1).BorderColor(Colors.Black).Width(180);
                    left.Item().PaddingTop(4).Text("Signature").FontSize(8).Italic();
                    left.Item().PaddingTop(15).BorderBottom(1).BorderColor(Colors.Black).Width(180);
                    left.Item().PaddingTop(4).Text("Date").FontSize(8).Italic();
                });

                row.ConstantItem(40); // spacer

                row.RelativeItem().Column(right =>
                {
                    right.Item().Text("Credit Provider:").Bold();
                    right.Item().PaddingTop(30).BorderBottom(1).BorderColor(Colors.Black).Width(180);
                    right.Item().PaddingTop(4).Text("Signature").FontSize(8).Italic();
                    right.Item().PaddingTop(15).BorderBottom(1).BorderColor(Colors.Black).Width(180);
                    right.Item().PaddingTop(4).Text("Date").FontSize(8).Italic();
                });
            });
        });
    }

    private void ComposeForm39Footer(IContainer container, Form39Data data)
    {
        container.BorderTop(1).BorderColor(BorderColor).PaddingTop(6).Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span($"Generated: {data.GeneratedAt:yyyy-MM-dd HH:mm}").FontSize(7).FontColor("#94a3b8");
                });
                row.RelativeItem().AlignCenter().Text(text =>
                {
                    text.Span($"Loan Ref: {data.LoanApplicationId}").FontSize(7).FontColor("#94a3b8");
                });
                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.CurrentPageNumber().FontSize(7).FontColor("#94a3b8");
                    text.Span(" / ").FontSize(7).FontColor("#94a3b8");
                    text.TotalPages().FontSize(7).FontColor("#94a3b8");
                });
            });
            col.Item().AlignCenter().Text("Subject to the National Credit Act, 2005 (Act No. 34 of 2005)")
                .FontSize(7).FontColor("#94a3b8").Italic();
        });
    }

    // ─── Pre-Agreement Statement Composition ────────────────────────────

    private void ComposePreAgreementHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().BorderBottom(2).BorderColor(AccentColor).PaddingBottom(8).Column(inner =>
            {
                inner.Item().AlignCenter().Text("PRE-AGREEMENT STATEMENT").FontSize(20).Bold().FontColor(AccentColor);
                inner.Item().AlignCenter().PaddingTop(2).Text("Ho Hema Loans (Pty) Ltd").FontSize(12).Bold();
                inner.Item().AlignCenter().PaddingTop(4).Text("National Credit Act Disclosure — Section 92").FontSize(9).Italic();
            });
        });
    }

    private void ComposePreAgreementContent(IContainer container, PreAgreementStatementData data)
    {
        container.PaddingTop(10).Column(col =>
        {
            col.Spacing(12);

            // Consumer Info
            col.Item().Element(c => SectionTitle(c, "CONSUMER INFORMATION"));
            col.Item().Background(LightBg).Border(1).BorderColor(BorderColor).Padding(10).Column(inner =>
            {
                inner.Spacing(4);
                inner.Item().Element(c => FieldRow(c, "Name:", data.ConsumerName));
                inner.Item().Element(c => FieldRow(c, "Email:", data.ConsumerEmail));
            });

            // Proposed Loan Terms
            col.Item().Element(c => SectionTitle(c, "PROPOSED LOAN TERMS"));
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().Background(AccentColor).Padding(6)
                        .Text("Description").FontColor(Colors.White).Bold().FontSize(10);
                    header.Cell().Background(AccentColor).Padding(6).AlignRight()
                        .Text("Amount").FontColor(Colors.White).Bold().FontSize(10);
                });

                ResetRowIndex();
                TableRow(table, "Loan Amount", $"R {data.LoanAmount:N2}");
                TableRow(table, "Interest Rate (per annum)", $"{data.InterestRate:F2}%");
                TableRow(table, "Loan Term", $"{data.TermInMonths} months");
                TableRow(table, "Initiation Fee", $"R {data.InitiationFee:N2}");
                TableRow(table, "Monthly Service Fee", $"R {data.MonthlyServiceFee:N2}");
                TableRowBold(table, "Monthly Installment", $"R {data.MonthlyInstallment:N2}");
                TableRowBold(table, "Total Amount Payable", $"R {data.TotalAmountPayable:N2}");
            });

            // Cooling-off Notice
            col.Item().Element(c => SectionTitle(c, "COOLING-OFF PERIOD"));
            col.Item().Background(WarningBg).Border(1).BorderColor("#f59e0b").Padding(12).Column(inner =>
            {
                inner.Spacing(6);
                inner.Item().Text(data.CoolingOffPeriod).Bold();
                inner.Item().Text("This means you can change your mind and cancel the agreement without any fees or penalties within this period. This right is protected under Section 121 of the National Credit Act.");
            });

            // Important Notices
            col.Item().Element(c => SectionTitle(c, "IMPORTANT INFORMATION"));
            col.Item().Background(InfoBg).Border(1).BorderColor("#3b82f6").Padding(12).Column(inner =>
            {
                inner.Spacing(4);
                foreach (var notice in data.ImportantNotices)
                {
                    inner.Item().Row(row =>
                    {
                        row.AutoItem().PaddingRight(6).Text("•").Bold().FontColor(AccentColor);
                        row.RelativeItem().Text(notice).FontSize(9);
                    });
                }
            });

            // Next Steps
            col.Item().Element(c => SectionTitle(c, "NEXT STEPS"));
            col.Item().Column(inner =>
            {
                inner.Spacing(4);
                var steps = new[]
                {
                    "Review all information in this statement carefully.",
                    "Ensure you understand your rights and obligations.",
                    "If you agree, proceed to sign the formal credit agreement (Form 39).",
                    "Remember your cooling-off period rights — you may cancel within 5 business days of signing."
                };

                for (int i = 0; i < steps.Length; i++)
                {
                    inner.Item().Text($"{i + 1}. {steps[i]}").FontSize(9);
                }
            });

            // Disclaimer
            col.Item().PaddingTop(8).AlignCenter()
                .Text("This is not a binding agreement. The formal credit agreement (Form 39) will be provided separately.")
                .FontSize(9).Italic().FontColor("#64748b");
        });
    }

    private void ComposePreAgreementFooter(IContainer container, PreAgreementStatementData data)
    {
        container.BorderTop(1).BorderColor(BorderColor).PaddingTop(6).Row(row =>
        {
            row.RelativeItem().Text($"Generated: {data.GeneratedAt:yyyy-MM-dd HH:mm}")
                .FontSize(7).FontColor("#94a3b8");
            row.RelativeItem().AlignCenter().Text("Ho Hema Loans (Pty) Ltd")
                .FontSize(7).FontColor("#94a3b8");
            row.RelativeItem().AlignRight().Text(text =>
            {
                text.CurrentPageNumber().FontSize(7).FontColor("#94a3b8");
                text.Span(" / ").FontSize(7).FontColor("#94a3b8");
                text.TotalPages().FontSize(7).FontColor("#94a3b8");
            });
        });
    }

    // ─── Shared Helpers ─────────────────────────────────────────────────

    private uint _rowIndex = 0;

    private void ResetRowIndex() => _rowIndex = 0;

    private void SectionTitle(IContainer container, string title)
    {
        container.BorderBottom(1).BorderColor(PrimaryColor).PaddingBottom(4)
            .Text(title).Bold().FontSize(11).FontColor(PrimaryColor);
    }

    private void FieldRow(IContainer container, string label, string value)
    {
        container.Row(row =>
        {
            row.ConstantItem(160).Text(label).Bold().FontSize(9);
            row.RelativeItem().Text(value).FontSize(9);
        });
    }

    private void TableRow(TableDescriptor table, string label, string value)
    {
        var bgColor = (_rowIndex++ % 2 == 0) ? Colors.White : Color.FromHex(LightBg);

        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(BorderColor).Padding(6)
            .Text(label).FontSize(9);
        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(BorderColor).Padding(6).AlignRight()
            .Text(value).FontSize(9);
    }

    private void TableRowBold(TableDescriptor table, string label, string value)
    {
        table.Cell().Background(LightBg).BorderBottom(1).BorderColor(BorderColor).Padding(6)
            .Text(label).Bold().FontSize(9);
        table.Cell().Background(LightBg).BorderBottom(1).BorderColor(BorderColor).Padding(6).AlignRight()
            .Text(value).Bold().FontSize(9).FontColor("#16a34a");
    }
}
