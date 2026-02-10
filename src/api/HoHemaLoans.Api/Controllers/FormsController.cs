using HoHemaLoans.Api.Services;
using HoHemaLoans.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text;

namespace HoHemaLoans.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FormsController : ControllerBase
{
    private readonly INCRComplianceService _ncrComplianceService;
    private readonly IOmnichannelLoanService _loanService;
    private readonly IPdfGenerationService _pdfService;
    private readonly ILogger<FormsController> _logger;

    public FormsController(
        INCRComplianceService ncrComplianceService,
        IOmnichannelLoanService loanService,
        IPdfGenerationService pdfService,
        ILogger<FormsController> logger)
    {
        _ncrComplianceService = ncrComplianceService;
        _loanService = loanService;
        _pdfService = pdfService;
        _logger = logger;
    }

    /// <summary>
    /// Generate Form 39 (NCR Credit Agreement) as HTML
    /// </summary>
    [HttpGet("form39/{applicationId}")]
    public async Task<IActionResult> GenerateForm39Html(Guid applicationId)
    {
        try
        {
            var form39Data = await _ncrComplianceService.GenerateForm39DataAsync(applicationId);
            var html = GenerateForm39Html(form39Data);
            
            return Ok(new { html });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Form 39 HTML for application {ApplicationId}", applicationId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Download Form 39 (NCR Credit Agreement) as PDF
    /// </summary>
    [HttpGet("form39/{applicationId}/pdf")]
    public async Task<IActionResult> DownloadForm39Pdf(Guid applicationId)
    {
        try
        {
            var form39Data = await _ncrComplianceService.GenerateForm39DataAsync(applicationId);
            var pdfBytes = _pdfService.GenerateForm39Pdf(form39Data);
            
            return File(pdfBytes, "application/pdf", $"Form39_LoanApplication_{applicationId}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Form 39 PDF for application {ApplicationId}", applicationId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Download Pre-Agreement Statement as PDF
    /// </summary>
    [HttpPost("pre-agreement-statement/pdf")]
    public async Task<IActionResult> DownloadPreAgreementPdf([FromBody] PreAgreementRequest request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = new ApplicationUser
            {
                Id = userId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email
            };

            var calculation = new LoanCalculation
            {
                LoanAmount = request.LoanAmount,
                InterestRate = request.InterestRate,
                TermInMonths = request.TermInMonths,
                InitiationFee = request.InitiationFee,
                MonthlyServiceFee = request.MonthlyServiceFee,
                MonthlyInstallment = request.MonthlyInstallment,
                TotalAmountPayable = request.TotalAmountPayable
            };

            var preAgreementData = await _ncrComplianceService.GeneratePreAgreementStatementAsync(calculation, user);
            var pdfBytes = _pdfService.GeneratePreAgreementPdf(preAgreementData);

            return File(pdfBytes, "application/pdf", $"PreAgreement_{request.FirstName}_{request.LastName}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating pre-agreement statement PDF");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Generate Pre-Agreement Statement HTML
    /// </summary>
    [HttpPost("pre-agreement-statement")]
    public async Task<IActionResult> GeneratePreAgreementStatementHtml([FromBody] PreAgreementRequest request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = new ApplicationUser
            {
                Id = userId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email
            };

            var calculation = new LoanCalculation
            {
                LoanAmount = request.LoanAmount,
                InterestRate = request.InterestRate,
                TermInMonths = request.TermInMonths,
                InitiationFee = request.InitiationFee,
                MonthlyServiceFee = request.MonthlyServiceFee,
                MonthlyInstallment = request.MonthlyInstallment,
                TotalAmountPayable = request.TotalAmountPayable
            };

            var preAgreementData = await _ncrComplianceService.GeneratePreAgreementStatementAsync(calculation, user);
            var html = GeneratePreAgreementStatementHtml(preAgreementData);
            
            return Ok(new { html });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating pre-agreement statement HTML");
            return StatusCode(500, "Internal server error");
        }
    }

    private static string GenerateForm39Html(Form39Data data)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Form 39 - Credit Agreement</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; line-height: 1.4; }}
        .header {{ text-align: center; border-bottom: 2px solid #000; padding-bottom: 10px; margin-bottom: 20px; }}
        .section {{ margin: 20px 0; }}
        .section-title {{ font-weight: bold; font-size: 14px; margin-bottom: 10px; border-bottom: 1px solid #ccc; }}
        .field {{ margin: 8px 0; }}
        .field-label {{ font-weight: bold; display: inline-block; width: 150px; }}
        .amount {{ text-align: right; }}
        .signature-section {{ margin-top: 40px; border-top: 1px solid #000; }}
        .signature-line {{ border-bottom: 1px solid #000; width: 200px; display: inline-block; margin: 10px; }}
        .important {{ background: #fff3cd; padding: 10px; border: 1px solid #ffeaa7; margin: 10px 0; }}
        table {{ width: 100%; border-collapse: collapse; margin: 10px 0; }}
        th, td {{ border: 1px solid #ccc; padding: 8px; text-align: left; }}
        th {{ background-color: #f8f9fa; }}
        .amount-cell {{ text-align: right; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>FORM 39</h1>
        <h2>CREDIT AGREEMENT</h2>
        <p><strong>National Credit Act, 2005 (Act No. 34 of 2005)</strong></p>
        <p>Regulation 24(1)</p>
    </div>

    <div class='section'>
        <div class='section-title'>1. CREDIT PROVIDER DETAILS</div>
        <div class='field'><span class='field-label'>Name:</span> {data.CreditProviderName}</div>
        <div class='field'><span class='field-label'>NCRCP Registration:</span> {data.NCRCPRegistrationNumber}</div>
        <div class='field'><span class='field-label'>Address:</span> {data.CreditProviderAddress}</div>
    </div>

    <div class='section'>
        <div class='section-title'>2. CONSUMER DETAILS</div>
        <div class='field'><span class='field-label'>Name:</span> {data.ConsumerName}</div>
        <div class='field'><span class='field-label'>ID Number:</span> {data.ConsumerIdNumber}</div>
        <div class='field'><span class='field-label'>Address:</span> {data.ConsumerAddress}</div>
        <div class='field'><span class='field-label'>Phone:</span> {data.ConsumerPhone}</div>
        <div class='field'><span class='field-label'>Email:</span> {data.ConsumerEmail}</div>
    </div>

    <div class='section'>
        <div class='section-title'>3. CREDIT AGREEMENT DETAILS</div>
        <div class='field'><span class='field-label'>Agreement Date:</span> {data.AgreementDate:yyyy-MM-dd}</div>
        
        <table>
            <tr>
                <th>Description</th>
                <th>Amount</th>
            </tr>
            <tr>
                <td>Principal Debt</td>
                <td class='amount-cell'>R {data.PrincipalDebt:N2}</td>
            </tr>
            <tr>
                <td>Interest Rate (per annum)</td>
                <td class='amount-cell'>{data.InterestRate:F2}%</td>
            </tr>
            <tr>
                <td>Initiation Fee</td>
                <td class='amount-cell'>R {data.InitiationFee:N2}</td>
            </tr>
            <tr>
                <td>Monthly Service Fee</td>
                <td class='amount-cell'>R {data.MonthlyServiceFee:N2}</td>
            </tr>
            <tr>
                <td>Term (months)</td>
                <td class='amount-cell'>{data.TermInMonths}</td>
            </tr>
            <tr>
                <td><strong>Monthly Installment</strong></td>
                <td class='amount-cell'><strong>R {data.MonthlyInstallment:N2}</strong></td>
            </tr>
            <tr>
                <td><strong>Total Amount Payable</strong></td>
                <td class='amount-cell'><strong>R {data.TotalAmountPayable:N2}</strong></td>
            </tr>
        </table>
    </div>

    <div class='section'>
        <div class='section-title'>4. AFFORDABILITY ASSESSMENT</div>
        <div class='field'><span class='field-label'>Monthly Income:</span> R {data.MonthlyIncome:N2}</div>
        <div class='field'><span class='field-label'>Monthly Expenses:</span> R {data.MonthlyExpenses:N2}</div>
        <div class='field'><span class='field-label'>Debt-to-Income Ratio:</span> {data.DebtToIncomeRatio:F2}%</div>
    </div>

    <div class='important'>
        <div class='section-title'>IMPORTANT CONSUMER RIGHTS</div>
        <p><strong>Cooling-off Period:</strong> You have {data.CoolingOffPeriodDays} days from signing this agreement to cancel without penalty.</p>
        <p><strong>Right to Information:</strong> You have the right to receive a statement of account and information about your credit agreement.</p>
        <p><strong>Complaints:</strong> {data.ComplaintsProcedure}</p>
    </div>

    <div class='signature-section'>
        <div class='section-title'>5. SIGNATURES</div>
        <p>By signing below, both parties agree to the terms and conditions of this credit agreement.</p>
        
        <div style='margin-top: 40px;'>
            <div style='float: left; width: 45%;'>
                <p>Consumer Signature:</p>
                <div class='signature-line'></div>
                <p>Date: _____________</p>
            </div>
            <div style='float: right; width: 45%;'>
                <p>Credit Provider Signature:</p>
                <div class='signature-line'></div>
                <p>Date: _____________</p>
            </div>
            <div style='clear: both;'></div>
        </div>
    </div>

    <div class='section' style='margin-top: 50px; font-size: 10px; color: #666;'>
        <p><em>This document was generated on {data.GeneratedAt:yyyy-MM-dd HH:mm:ss} for loan application {data.LoanApplicationId}.</em></p>
        <p><em>This agreement is subject to the National Credit Act, 2005 (Act No. 34 of 2005) and regulations.</em></p>
    </div>
</body>
</html>";
    }

    private static string GeneratePreAgreementStatementHtml(PreAgreementStatementData data)
    {
        var noticesHtml = string.Join("", data.ImportantNotices.Select(notice => $"<li>{notice}</li>"));

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Pre-Agreement Statement</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; line-height: 1.6; }}
        .header {{ text-align: center; border-bottom: 2px solid #007bff; padding-bottom: 15px; margin-bottom: 20px; }}
        .section {{ margin: 20px 0; }}
        .section-title {{ font-weight: bold; font-size: 16px; color: #007bff; margin-bottom: 10px; }}
        .field {{ margin: 8px 0; }}
        .field-label {{ font-weight: bold; display: inline-block; width: 200px; }}
        .important {{ background: #d1ecf1; padding: 15px; border: 1px solid #bee5eb; margin: 15px 0; border-radius: 5px; }}
        .cooling-off {{ background: #fff3cd; padding: 15px; border: 1px solid #ffeaa7; margin: 15px 0; border-radius: 5px; }}
        .amount {{ color: #28a745; font-weight: bold; }}
        ul {{ padding-left: 20px; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>PRE-AGREEMENT STATEMENT</h1>
        <h3>Ho Hema Loans (Pty) Ltd</h3>
        <p><strong>National Credit Act Disclosure</strong></p>
    </div>

    <div class='section'>
        <div class='section-title'>Consumer Information</div>
        <div class='field'><span class='field-label'>Name:</span> {data.ConsumerName}</div>
        <div class='field'><span class='field-label'>Email:</span> {data.ConsumerEmail}</div>
    </div>

    <div class='section'>
        <div class='section-title'>Proposed Loan Terms</div>
        <div class='field'><span class='field-label'>Loan Amount:</span> <span class='amount'>R {data.LoanAmount:N2}</span></div>
        <div class='field'><span class='field-label'>Interest Rate:</span> {data.InterestRate:F2}% per annum</div>
        <div class='field'><span class='field-label'>Loan Term:</span> {data.TermInMonths} months</div>
        <div class='field'><span class='field-label'>Initiation Fee:</span> R {data.InitiationFee:N2}</div>
        <div class='field'><span class='field-label'>Monthly Service Fee:</span> R {data.MonthlyServiceFee:N2}</div>
        <div class='field'><span class='field-label'>Monthly Installment:</span> <span class='amount'>R {data.MonthlyInstallment:N2}</span></div>
        <div class='field'><span class='field-label'>Total Amount Payable:</span> <span class='amount'>R {data.TotalAmountPayable:N2}</span></div>
    </div>

    <div class='cooling-off'>
        <div class='section-title'>⏰ Cooling-off Period</div>
        <p><strong>{data.CoolingOffPeriod}</strong></p>
        <p>This means you can change your mind and cancel the agreement without any fees or penalties within this period.</p>
    </div>

    <div class='important'>
        <div class='section-title'>📋 Important Information</div>
        <ul>
            {noticesHtml}
        </ul>
    </div>

    <div class='section'>
        <div class='section-title'>Next Steps</div>
        <p>If you agree to these terms:</p>
        <ol>
            <li>Review all information carefully</li>
            <li>Ensure you understand your rights and obligations</li>
            <li>Sign the formal credit agreement (Form 39)</li>
            <li>Remember your cooling-off period rights</li>
        </ol>
    </div>

    <div style='margin-top: 40px; font-size: 12px; color: #666; text-align: center;'>
        <p>Generated on {data.GeneratedAt:yyyy-MM-dd HH:mm:ss}</p>
        <p>This is not a binding agreement. The formal credit agreement will be provided separately.</p>
    </div>
</body>
</html>";
    }
}