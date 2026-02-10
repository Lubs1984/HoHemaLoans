using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HoHemaLoans.Api.Migrations
{
    /// <inheritdoc />
    public partial class NCRCompliance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FinancialInformationId",
                table: "LoanApplications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PersonalInformationId",
                table: "LoanApplications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SignedAt",
                table: "LoanApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "LoanApplications",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "ConsumerComplaints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoanApplicationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignedToUserId = table.Column<string>(type: "text", nullable: true),
                    Resolution = table.Column<string>(type: "text", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedBy = table.Column<string>(type: "text", nullable: true),
                    EscalatedToNCR = table.Column<bool>(type: "boolean", nullable: false),
                    EscalatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NCRReferenceNumber = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumerComplaints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsumerComplaints_AspNetUsers_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ConsumerComplaints_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConsumerComplaints_LoanApplications_LoanApplicationId",
                        column: x => x.LoanApplicationId,
                        principalTable: "LoanApplications",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FinancialInformation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    EmploymentStatus = table.Column<int>(type: "integer", nullable: false),
                    EmployerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EmployerAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    JobTitle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EmploymentStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MonthlyIncome = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    OtherIncome = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    OtherIncomeSource = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TotalMonthlyIncome = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MonthlyExpenses = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ExistingDebtPayments = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    RentMortgagePayment = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    UtilitiesPayment = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    FoodTransportExpenses = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    OtherExpenses = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    BankName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AccountType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AccountNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BranchCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    HasCreditCheck = table.Column<bool>(type: "boolean", nullable: false),
                    LastCreditCheckDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialInformation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialInformation_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoanCalculations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LoanApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoanAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    InterestRate = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    TermInMonths = table.Column<int>(type: "integer", nullable: false),
                    InitiationFee = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    MonthlyServiceFee = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    InsuranceFee = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    OtherFees = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    MonthlyInstallment = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalInterest = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalFees = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalAmountPayable = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    EffectiveInterestRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    DebtServiceRatio = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    DisposableIncomeRequired = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CalculatedBy = table.Column<string>(type: "text", nullable: false),
                    CalculationMethod = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsNCRCompliant = table.Column<bool>(type: "boolean", nullable: false),
                    ComplianceNotes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanCalculations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanCalculations_LoanApplications_LoanApplicationId",
                        column: x => x.LoanApplicationId,
                        principalTable: "LoanApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoanCancellations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LoanApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CancellationReason = table.Column<string>(type: "text", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsWithinCoolingOffPeriod = table.Column<bool>(type: "boolean", nullable: false),
                    RefundAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    RefundReference = table.Column<string>(type: "text", nullable: true),
                    RefundProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanCancellations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanCancellations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LoanCancellations_LoanApplications_LoanApplicationId",
                        column: x => x.LoanApplicationId,
                        principalTable: "LoanApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NCRAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Details = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IPAddress = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    ComplianceRelated = table.Column<bool>(type: "boolean", nullable: false),
                    ComplianceNotes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NCRAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NCRAuditLogs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NCRConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaxInterestRatePerAnnum = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    DefaultInterestRatePerAnnum = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    MaxInitiationFee = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    InitiationFeePercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    MaxMonthlyServiceFee = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    DefaultMonthlyServiceFee = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    MaxDebtToIncomeRatio = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    MinSafetyBuffer = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    MinLoanAmount = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    MaxLoanAmount = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    MinLoanTermMonths = table.Column<int>(type: "integer", nullable: false),
                    MaxLoanTermMonths = table.Column<int>(type: "integer", nullable: false),
                    CoolingOffPeriodDays = table.Column<int>(type: "integer", nullable: false),
                    NCRCPRegistrationNumber = table.Column<string>(type: "text", nullable: true),
                    ComplianceOfficerName = table.Column<string>(type: "text", nullable: true),
                    ComplianceOfficerEmail = table.Column<string>(type: "text", nullable: true),
                    DocumentRetentionYears = table.Column<int>(type: "integer", nullable: false),
                    EnforceNCRCompliance = table.Column<bool>(type: "boolean", nullable: false),
                    AllowCoolingOffCancellation = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NCRConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PersonalInformation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    IdNumber = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MaritalStatus = table.Column<int>(type: "integer", nullable: false),
                    Dependents = table.Column<int>(type: "integer", nullable: false),
                    HomeAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PostalCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Province = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    HomePhone = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    WorkPhone = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalInformation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonalInformation_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComplaintNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ComplaintId = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplaintNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplaintNotes_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ComplaintNotes_ConsumerComplaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalTable: "ConsumerComplaints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoanApplications_FinancialInformationId",
                table: "LoanApplications",
                column: "FinancialInformationId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanApplications_PersonalInformationId",
                table: "LoanApplications",
                column: "PersonalInformationId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplaintNotes_ComplaintId",
                table: "ComplaintNotes",
                column: "ComplaintId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplaintNotes_CreatedByUserId",
                table: "ComplaintNotes",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumerComplaints_AssignedToUserId",
                table: "ConsumerComplaints",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumerComplaints_LoanApplicationId",
                table: "ConsumerComplaints",
                column: "LoanApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumerComplaints_UserId",
                table: "ConsumerComplaints",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialInformation_UserId",
                table: "FinancialInformation",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanCalculations_LoanApplicationId",
                table: "LoanCalculations",
                column: "LoanApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoanCancellations_LoanApplicationId",
                table: "LoanCancellations",
                column: "LoanApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoanCancellations_UserId",
                table: "LoanCancellations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_NCRAuditLogs_UserId",
                table: "NCRAuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalInformation_UserId",
                table: "PersonalInformation",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_LoanApplications_FinancialInformation_FinancialInformationId",
                table: "LoanApplications",
                column: "FinancialInformationId",
                principalTable: "FinancialInformation",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LoanApplications_PersonalInformation_PersonalInformationId",
                table: "LoanApplications",
                column: "PersonalInformationId",
                principalTable: "PersonalInformation",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LoanApplications_FinancialInformation_FinancialInformationId",
                table: "LoanApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_LoanApplications_PersonalInformation_PersonalInformationId",
                table: "LoanApplications");

            migrationBuilder.DropTable(
                name: "ComplaintNotes");

            migrationBuilder.DropTable(
                name: "FinancialInformation");

            migrationBuilder.DropTable(
                name: "LoanCalculations");

            migrationBuilder.DropTable(
                name: "LoanCancellations");

            migrationBuilder.DropTable(
                name: "NCRAuditLogs");

            migrationBuilder.DropTable(
                name: "NCRConfigurations");

            migrationBuilder.DropTable(
                name: "PersonalInformation");

            migrationBuilder.DropTable(
                name: "ConsumerComplaints");

            migrationBuilder.DropIndex(
                name: "IX_LoanApplications_FinancialInformationId",
                table: "LoanApplications");

            migrationBuilder.DropIndex(
                name: "IX_LoanApplications_PersonalInformationId",
                table: "LoanApplications");

            migrationBuilder.DropColumn(
                name: "FinancialInformationId",
                table: "LoanApplications");

            migrationBuilder.DropColumn(
                name: "PersonalInformationId",
                table: "LoanApplications");

            migrationBuilder.DropColumn(
                name: "SignedAt",
                table: "LoanApplications");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "LoanApplications");
        }
    }
}
