# Ho Hema Loans - Database Schema Documentation

**Version:** 1.0  
**Date:** November 7, 2025  
**Database:** Microsoft SQL Server 2022 / Azure SQL Database  
**Compatibility:** SQL Server 2019+  

---

## Table of Contents

1. [Database Overview](#database-overview)
2. [Schema Structure](#schema-structure)
3. [Core Tables](#core-tables)
4. [Indexes and Performance](#indexes-and-performance)
5. [Stored Procedures](#stored-procedures)
6. [Views and Functions](#views-and-functions)
7. [Security and Permissions](#security-and-permissions)
8. [Data Migration Scripts](#data-migration-scripts)
9. [Backup and Recovery](#backup-and-recovery)

---

## Database Overview

### Database Name: `HoHemaLoans`

### Schema Organization
- `dbo` - Core application tables
- `audit` - Audit trail and compliance tables
- `integration` - External system integration tables
- `reporting` - Views and tables for business intelligence

### Naming Conventions
- **Tables:** PascalCase (e.g., `LoanApplications`)
- **Columns:** PascalCase (e.g., `EmployeeId`)
- **Indexes:** `IX_TableName_ColumnName`
- **Primary Keys:** `PK_TableName`
- **Foreign Keys:** `FK_TableName_ReferencedTable`
- **Check Constraints:** `CK_TableName_ColumnName`

---

## Schema Structure

```sql
-- Create Database and Schemas
CREATE DATABASE HoHemaLoans
COLLATE SQL_Latin1_General_CP1_CI_AS;

USE HoHemaLoans;

-- Create custom schemas
CREATE SCHEMA audit;
CREATE SCHEMA integration;
CREATE SCHEMA reporting;
```

---

## Core Tables

### 1. Users Table

```sql
CREATE TABLE dbo.Users (
    UserId uniqueidentifier PRIMARY KEY DEFAULT NEWID(),
    PhoneNumber nvarchar(20) NOT NULL,
    Email nvarchar(255) NULL,
    PasswordHash nvarchar(255) NOT NULL,
    Salt nvarchar(255) NOT NULL,
    Role nvarchar(50) NOT NULL DEFAULT 'Employee',
    IsActive bit NOT NULL DEFAULT 1,
    LastLoginAt datetime2(7) NULL,
    CreatedAt datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT CK_Users_Role CHECK (Role IN ('Employee', 'Administrator', 'Agent', 'SystemIntegrator')),
    CONSTRAINT UQ_Users_PhoneNumber UNIQUE (PhoneNumber),
    CONSTRAINT UQ_Users_Email UNIQUE (Email)
);

-- Indexes
CREATE INDEX IX_Users_PhoneNumber ON dbo.Users (PhoneNumber);
CREATE INDEX IX_Users_Role ON dbo.Users (Role);
```

### 2. Employers Table

```sql
CREATE TABLE dbo.Employers (
    EmployerId uniqueidentifier PRIMARY KEY DEFAULT NEWID(),
    CompanyName nvarchar(255) NOT NULL,
    CompanyRegistrationNumber nvarchar(50) NULL,
    ContactPerson nvarchar(255) NOT NULL,
    ContactEmail nvarchar(255) NOT NULL,
    ContactPhone nvarchar(20) NOT NULL,
    Address nvarchar(500) NULL,
    IsActive bit NOT NULL DEFAULT 1,
    ContractStartDate date NOT NULL,
    ContractEndDate date NULL,
    CreatedAt datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT UQ_Employers_CompanyName UNIQUE (CompanyName),
    CONSTRAINT UQ_Employers_RegistrationNumber UNIQUE (CompanyRegistrationNumber)
);
```

### 3. Employees Table

```sql
CREATE TABLE dbo.Employees (
    EmployeeId uniqueidentifier PRIMARY KEY DEFAULT NEWID(),
    UserId uniqueidentifier NOT NULL,
    EmployerId uniqueidentifier NOT NULL,
    EmployeeNumber nvarchar(50) NOT NULL,
    IdNumber nvarchar(13) NOT NULL,
    FirstName nvarchar(100) NOT NULL,
    LastName nvarchar(100) NOT NULL,
    DateOfBirth date NOT NULL,
    Gender char(1) NULL,
    Position nvarchar(100) NULL,
    Department nvarchar(100) NULL,
    EmploymentStartDate date NOT NULL,
    EmploymentEndDate date NULL,
    MonthlyRate decimal(18,2) NOT NULL,
    PayrollFrequency nvarchar(20) NOT NULL DEFAULT 'Monthly',
    BankName nvarchar(100) NOT NULL,
    AccountNumber nvarchar(20) NOT NULL,
    BranchCode nvarchar(10) NOT NULL,
    AccountHolder nvarchar(255) NOT NULL,
    IsActive bit NOT NULL DEFAULT 1,
    CreatedAt datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_Employees_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_Employees_Employers FOREIGN KEY (EmployerId) REFERENCES dbo.Employers(EmployerId),
    CONSTRAINT CK_Employees_Gender CHECK (Gender IN ('M', 'F', 'O')),
    CONSTRAINT CK_Employees_PayrollFrequency CHECK (PayrollFrequency IN ('Weekly', 'Bi-Weekly', 'Monthly')),
    CONSTRAINT UQ_Employees_EmployeeNumber_Employer UNIQUE (EmployeeNumber, EmployerId),
    CONSTRAINT UQ_Employees_IdNumber UNIQUE (IdNumber)
);

-- Indexes
CREATE INDEX IX_Employees_EmployerId ON dbo.Employees (EmployerId);
CREATE INDEX IX_Employees_EmployeeNumber ON dbo.Employees (EmployeeNumber);
CREATE INDEX IX_Employees_IdNumber ON dbo.Employees (IdNumber);
```

### 4. EmployerParameters Table

```sql
CREATE TABLE dbo.EmployerParameters (
    ParameterId uniqueidentifier PRIMARY KEY DEFAULT NEWID(),
    EmployerId uniqueidentifier NOT NULL,
    ProductType nvarchar(20) NOT NULL,
    ParameterName nvarchar(100) NOT NULL,
    ParameterValue nvarchar(255) NOT NULL,
    DataType nvarchar(20) NOT NULL DEFAULT 'string',
    IsActive bit NOT NULL DEFAULT 1,
    EffectiveFromDate date NOT NULL DEFAULT CAST(GETDATE() AS date),
    EffectiveToDate date NULL,
    CreatedAt datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_EmployerParameters_Employers FOREIGN KEY (EmployerId) REFERENCES dbo.Employers(EmployerId),
    CONSTRAINT CK_EmployerParameters_ProductType CHECK (ProductType IN ('Advance', 'ShortTermLoan')),
    CONSTRAINT CK_EmployerParameters_DataType CHECK (DataType IN ('string', 'decimal', 'int', 'boolean', 'date'))
);

-- Indexes
CREATE INDEX IX_EmployerParameters_EmployerId ON dbo.EmployerParameters (EmployerId);
CREATE INDEX IX_EmployerParameters_ProductType ON dbo.EmployerParameters (ProductType);
```

### 5. LoanApplications Table

```sql
CREATE TABLE dbo.LoanApplications (
    ApplicationId uniqueidentifier PRIMARY KEY DEFAULT NEWID(),
    EmployeeId uniqueidentifier NOT NULL,
    ProductType nvarchar(20) NOT NULL,
    RequestedAmount decimal(18,2) NOT NULL,
    ApprovedAmount decimal(18,2) NULL,
    Status nvarchar(30) NOT NULL DEFAULT 'Submitted',
    Channel nvarchar(20) NOT NULL,
    ReferenceNumber AS ('HOHA' + FORMAT(DATEPART(year, CreatedAt), '0000') + FORMAT(DATEPART(dayofyear, CreatedAt), '000') + RIGHT('000000' + CAST(ABS(CHECKSUM(ApplicationId)) % 1000000 AS VARCHAR), 6)) PERSISTED,
    
    -- Financial Details
    MonthlyIncome decimal(18,2) NULL,
    DeclaredExpenses decimal(18,2) NULL,
    CalculatedExpenses decimal(18,2) NULL,
    DisposableIncome decimal(18,2) NULL,
    CreditScore int NULL,
    RiskCategory nvarchar(20) NULL,
    
    -- Terms and Conditions
    LoanTerm int NULL, -- months
    InterestRate decimal(5,2) NULL,
    InitiationFee decimal(18,2) NULL,
    AdminFee decimal(18,2) NULL,
    TotalRepaymentAmount decimal(18,2) NULL,
    
    -- Tracking
    SubmittedAt datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
    ProcessedAt datetime2(7) NULL,
    ApprovedAt datetime2(7) NULL,
    DisbursedAt datetime2(7) NULL,
    ProcessingTimeMinutes AS (DATEDIFF(minute, SubmittedAt, ISNULL(ProcessedAt, GETUTCDATE()))) PERSISTED,
    
    CreatedAt datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_LoanApplications_Employees FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(EmployeeId),
    CONSTRAINT CK_LoanApplications_ProductType CHECK (ProductType IN ('Advance', 'ShortTermLoan')),
    CONSTRAINT CK_LoanApplications_Status CHECK (Status IN (
        'Submitted', 'PendingValidation', 'ValidatingIdentity', 'ValidatingEmployment', 
        'AffordabilityAssessment', 'Approved', 'ContractSigning', 'PaymentProcessing', 
        'Disbursed', 'Declined', 'Cancelled', 'Expired'
    )),
    CONSTRAINT CK_LoanApplications_Channel CHECK (Channel IN ('Web', 'WhatsApp', 'Mobile', 'Phone')),
    CONSTRAINT CK_LoanApplications_RiskCategory CHECK (RiskCategory IN ('Low', 'Medium', 'High') OR RiskCategory IS NULL)
);

-- Indexes
CREATE INDEX IX_LoanApplications_EmployeeId ON dbo.LoanApplications (EmployeeId);
CREATE INDEX IX_LoanApplications_Status ON dbo.LoanApplications (Status);
CREATE INDEX IX_LoanApplications_ProductType ON dbo.LoanApplications (ProductType);
CREATE INDEX IX_LoanApplications_SubmittedAt ON dbo.LoanApplications (SubmittedAt);
CREATE INDEX IX_LoanApplications_ReferenceNumber ON dbo.LoanApplications (ReferenceNumber);
```

### 6. ApplicationDocuments Table

```sql
CREATE TABLE dbo.ApplicationDocuments (
    DocumentId uniqueidentifier PRIMARY KEY DEFAULT NEWID(),
    ApplicationId uniqueidentifier NOT NULL,
    DocumentType nvarchar(50) NOT NULL,
    FileName nvarchar(255) NOT NULL,
    ContentType nvarchar(100) NOT NULL,
    FileSize bigint NOT NULL,
    StoragePath nvarchar(500) NOT NULL,
    UploadedBy uniqueidentifier NOT NULL,
    VerificationStatus nvarchar(20) NOT NULL DEFAULT 'Pending',
    VerifiedBy uniqueidentifier NULL,
    VerifiedAt datetime2(7) NULL,
    ExtractionData nvarchar(max) NULL, -- JSON data extracted from document
    IsDeleted bit NOT NULL DEFAULT 0,
    CreatedAt datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_ApplicationDocuments_Applications FOREIGN KEY (ApplicationId) REFERENCES dbo.LoanApplications(ApplicationId),
    CONSTRAINT FK_ApplicationDocuments_UploadedBy FOREIGN KEY (UploadedBy) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_ApplicationDocuments_VerifiedBy FOREIGN KEY (VerifiedBy) REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_ApplicationDocuments_DocumentType CHECK (DocumentType IN (
        'IdDocument', 'Payslip', 'BankStatement', 'ProofOfEmployment', 'Contract', 'Other'
    )),
    CONSTRAINT CK_ApplicationDocuments_VerificationStatus CHECK (VerificationStatus IN (
        'Pending', 'Verified', 'Rejected', 'RequiresReview'
    ))
);

-- Indexes
CREATE INDEX IX_ApplicationDocuments_ApplicationId ON dbo.ApplicationDocuments (ApplicationId);
CREATE INDEX IX_ApplicationDocuments_DocumentType ON dbo.ApplicationDocuments (DocumentType);
```

### 7. LoanAgreements Table

```sql
CREATE TABLE dbo.LoanAgreements (
    AgreementId uniqueidentifier PRIMARY KEY DEFAULT NEWID(),
    ApplicationId uniqueidentifier NOT NULL,
    AgreementNumber AS ('AGR' + FORMAT(DATEPART(year, CreatedAt), '0000') + '-' + RIGHT('000000' + CAST(ABS(CHECKSUM(AgreementId)) % 1000000 AS VARCHAR), 6)) PERSISTED,
    
    -- Agreement Details
    PrincipalAmount decimal(18,2) NOT NULL,
    InterestRate decimal(5,2) NOT NULL,
    InitiationFee decimal(18,2) NOT NULL,
    AdminFee decimal(18,2) NOT NULL,
    TotalAmount decimal(18,2) NOT NULL,
    RepaymentTerm int NOT NULL, -- months
    
    -- AOD (Acknowledgment of Debt) Details
    AODAmount decimal(18,2) NOT NULL,
    AODDescription nvarchar(500) NOT NULL,
    
    -- Digital Signature
    SignedBy uniqueidentifier NOT NULL,
    SignatureMethod nvarchar(20) NOT NULL,
    SignedAt datetime2(7) NOT NULL,
    IPAddress nvarchar(45) NULL,
    UserAgent nvarchar(500) NULL,
    OTPUsed nvarchar(10) NULL, -- Encrypted OTP for audit
    
    -- Document Storage
    ContractDocumentPath nvarchar(500) NOT NULL,
    AODDocumentPath nvarchar(500) NOT NULL,
    
    CreatedAt datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_LoanAgreements_Applications FOREIGN KEY (ApplicationId) REFERENCES dbo.LoanApplications(ApplicationId),
    CONSTRAINT FK_LoanAgreements_SignedBy FOREIGN KEY (SignedBy) REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_LoanAgreements_SignatureMethod CHECK (SignatureMethod IN ('OTP', 'Biometric', 'Digital', 'Wet')),
    CONSTRAINT UQ_LoanAgreements_ApplicationId UNIQUE (ApplicationId)
);

-- Indexes
CREATE INDEX IX_LoanAgreements_SignedAt ON dbo.LoanAgreements (SignedAt);
CREATE INDEX IX_LoanAgreements_AgreementNumber ON dbo.LoanAgreements (AgreementNumber);
```

### 8. PaymentSchedules Table

```sql
CREATE TABLE dbo.PaymentSchedules (
    ScheduleId uniqueidentifier PRIMARY KEY DEFAULT NEWID(),
    AgreementId uniqueidentifier NOT NULL,
    InstallmentNumber int NOT NULL,
    DueDate date NOT NULL,
    PrincipalAmount decimal(18,2) NOT NULL,
    InterestAmount decimal(18,2) NOT NULL,
    TotalAmount decimal(18,2) NOT NULL,
    Status nvarchar(20) NOT NULL DEFAULT 'Scheduled',
    PayrollPeriod nvarchar(7) NULL, -- Format: YYYY-MM
    ProcessedAt datetime2(7) NULL,
    CreatedAt datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_PaymentSchedules_Agreements FOREIGN KEY (AgreementId) REFERENCES dbo.LoanAgreements(AgreementId),
    CONSTRAINT CK_PaymentSchedules_Status CHECK (Status IN (
        'Scheduled', 'SentToPayroll', 'Deducted', 'Failed', 'Reversed'
    )),
    CONSTRAINT UQ_PaymentSchedules_Agreement_Installment UNIQUE (AgreementId, InstallmentNumber)
);

-- Indexes
CREATE INDEX IX_PaymentSchedules_DueDate ON dbo.PaymentSchedules (DueDate);
CREATE INDEX IX_PaymentSchedules_Status ON dbo.PaymentSchedules (Status);
CREATE INDEX IX_PaymentSchedules_PayrollPeriod ON dbo.PaymentSchedules (PayrollPeriod);
```

### 9. Payments Table

```sql
CREATE TABLE dbo.Payments (
    PaymentId uniqueidentifier PRIMARY KEY DEFAULT NEWID(),
    ApplicationId uniqueidentifier NOT NULL,
    PaymentType nvarchar(20) NOT NULL,
    Amount decimal(18,2) NOT NULL,
    Status nvarchar(20) NOT NULL DEFAULT 'Pending',
    PaymentReference nvarchar(50) NOT NULL,
    BankReference nvarchar(50) NULL,
    
    -- Recipient Details
    RecipientName nvarchar(255) NOT NULL,
    RecipientAccount nvarchar(20) NOT NULL,
    RecipientBank nvarchar(100) NOT NULL,
    BranchCode nvarchar(10) NOT NULL,
    
    -- Processing Details
    BatchId uniqueidentifier NULL,
    Priority nvarchar(10) NOT NULL DEFAULT 'Standard',
    RequestedAt datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
    ProcessedAt datetime2(7) NULL,
    CompletedAt datetime2(7) NULL,
    FailureReason nvarchar(500) NULL,
    
    CreatedAt datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_Payments_Applications FOREIGN KEY (ApplicationId) REFERENCES dbo.LoanApplications(ApplicationId),
    CONSTRAINT CK_Payments_PaymentType CHECK (PaymentType IN ('LoanDisbursement', 'Refund')),
    CONSTRAINT CK_Payments_Status CHECK (Status IN (
        'Pending', 'Queued', 'Processing', 'Completed', 'Failed', 'Cancelled'
    )),
    CONSTRAINT CK_Payments_Priority CHECK (Priority IN ('Standard', 'Urgent', 'Immediate')),
    CONSTRAINT UQ_Payments_PaymentReference UNIQUE (PaymentReference)
);

-- Indexes
CREATE INDEX IX_Payments_Status ON dbo.Payments (Status);
CREATE INDEX IX_Payments_BatchId ON dbo.Payments (BatchId);
CREATE INDEX IX_Payments_RequestedAt ON dbo.Payments (RequestedAt);
```

### 10. EmployeeShifts Table

```sql
CREATE TABLE dbo.EmployeeShifts (
    ShiftId uniqueidentifier PRIMARY KEY DEFAULT NEWID(),
    EmployeeId uniqueidentifier NOT NULL,
    ShiftDate date NOT NULL,
    StartTime time(7) NOT NULL,
    EndTime time(7) NOT NULL,
    BreakMinutes int NOT NULL DEFAULT 0,
    RegularHours decimal(4,2) NOT NULL,
    OvertimeHours decimal(4,2) NOT NULL DEFAULT 0,
    HourlyRate decimal(10,2) NOT NULL,
    RegularEarnings decimal(18,2) NOT NULL,
    OvertimeEarnings decimal(18,2) NOT NULL DEFAULT 0,
    TotalEarnings AS (RegularEarnings + OvertimeEarnings) PERSISTED,
    PayrollPeriod nvarchar(7) NOT NULL, -- Format: YYYY-MM
    SyncedFromSystem nvarchar(50) NULL, -- Source system identifier
    LastSyncAt datetime2(7) NULL,
    CreatedAt datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_EmployeeShifts_Employees FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(EmployeeId),
    CONSTRAINT UQ_EmployeeShifts_Employee_Date UNIQUE (EmployeeId, ShiftDate)
);

-- Indexes
CREATE INDEX IX_EmployeeShifts_EmployeeId_Date ON dbo.EmployeeShifts (EmployeeId, ShiftDate);
CREATE INDEX IX_EmployeeShifts_PayrollPeriod ON dbo.EmployeeShifts (PayrollPeriod);
```

### 11. WhatsAppSessions Table

```sql
CREATE TABLE dbo.WhatsAppSessions (
    SessionId uniqueidentifier PRIMARY KEY DEFAULT NEWID(),
    PhoneNumber nvarchar(20) NOT NULL,
    EmployeeId uniqueidentifier NULL,
    SessionState nvarchar(50) NOT NULL,
    CurrentFlow nvarchar(50) NULL,
    CurrentStep nvarchar(50) NULL,
    SessionData nvarchar(max) NULL, -- JSON data
    LastMessageAt datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
    ExpiresAt datetime2(7) NOT NULL DEFAULT DATEADD(hour, 24, GETUTCDATE()),
    IsActive bit NOT NULL DEFAULT 1,
    CreatedAt datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_WhatsAppSessions_Employees FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(EmployeeId)
);

-- Indexes
CREATE INDEX IX_WhatsAppSessions_PhoneNumber ON dbo.WhatsAppSessions (PhoneNumber);
CREATE INDEX IX_WhatsAppSessions_IsActive ON dbo.WhatsAppSessions (IsActive);
CREATE INDEX IX_WhatsAppSessions_ExpiresAt ON dbo.WhatsAppSessions (ExpiresAt);
```

---

## Audit Tables

### 1. AuditLog Table

```sql
CREATE TABLE audit.AuditLog (
    AuditId uniqueidentifier PRIMARY KEY DEFAULT NEWID(),
    TableName nvarchar(128) NOT NULL,
    RecordId nvarchar(50) NOT NULL,
    Operation nvarchar(10) NOT NULL,
    UserId uniqueidentifier NULL,
    OldValues nvarchar(max) NULL, -- JSON
    NewValues nvarchar(max) NULL, -- JSON
    ChangedColumns nvarchar(max) NULL, -- JSON array
    IPAddress nvarchar(45) NULL,
    UserAgent nvarchar(500) NULL,
    Timestamp datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT CK_AuditLog_Operation CHECK (Operation IN ('INSERT', 'UPDATE', 'DELETE'))
);

-- Indexes
CREATE INDEX IX_AuditLog_TableName ON audit.AuditLog (TableName);
CREATE INDEX IX_AuditLog_RecordId ON audit.AuditLog (RecordId);
CREATE INDEX IX_AuditLog_Timestamp ON audit.AuditLog (Timestamp);
CREATE INDEX IX_AuditLog_UserId ON audit.AuditLog (UserId);
```

### 2. ComplianceLog Table

```sql
CREATE TABLE audit.ComplianceLog (
    ComplianceId uniqueidentifier PRIMARY KEY DEFAULT NEWID(),
    ApplicationId uniqueidentifier NULL,
    ComplianceType nvarchar(50) NOT NULL,
    CheckName nvarchar(100) NOT NULL,
    Status nvarchar(20) NOT NULL,
    Details nvarchar(max) NULL, -- JSON
    RegulatoryReference nvarchar(100) NULL,
    Timestamp datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_ComplianceLog_Applications FOREIGN KEY (ApplicationId) REFERENCES dbo.LoanApplications(ApplicationId),
    CONSTRAINT CK_ComplianceLog_ComplianceType CHECK (ComplianceType IN ('NCR', 'POPIA', 'Internal')),
    CONSTRAINT CK_ComplianceLog_Status CHECK (Status IN ('Pass', 'Fail', 'Warning', 'Review'))
);

-- Indexes
CREATE INDEX IX_ComplianceLog_ApplicationId ON audit.ComplianceLog (ApplicationId);
CREATE INDEX IX_ComplianceLog_ComplianceType ON audit.ComplianceLog (ComplianceType);
CREATE INDEX IX_ComplianceLog_Timestamp ON audit.ComplianceLog (Timestamp);
```

---

## Integration Tables

### 1. IntegrationLogs Table

```sql
CREATE TABLE integration.IntegrationLogs (
    LogId uniqueidentifier PRIMARY KEY DEFAULT NEWID(),
    SystemName nvarchar(50) NOT NULL,
    Operation nvarchar(100) NOT NULL,
    RequestData nvarchar(max) NULL,
    ResponseData nvarchar(max) NULL,
    Status nvarchar(20) NOT NULL,
    ErrorMessage nvarchar(1000) NULL,
    Duration int NULL, -- milliseconds
    RequestId nvarchar(100) NULL,
    Timestamp datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT CK_IntegrationLogs_Status CHECK (Status IN ('Success', 'Error', 'Timeout', 'Cancelled'))
);

-- Indexes
CREATE INDEX IX_IntegrationLogs_SystemName ON integration.IntegrationLogs (SystemName);
CREATE INDEX IX_IntegrationLogs_Status ON integration.IntegrationLogs (Status);
CREATE INDEX IX_IntegrationLogs_Timestamp ON integration.IntegrationLogs (Timestamp);
```

### 2. ExternalSystemStatus Table

```sql
CREATE TABLE integration.ExternalSystemStatus (
    SystemId uniqueidentifier PRIMARY KEY DEFAULT NEWID(),
    SystemName nvarchar(50) NOT NULL,
    Status nvarchar(20) NOT NULL,
    LastHealthCheck datetime2(7) NOT NULL,
    ResponseTime int NULL, -- milliseconds
    ErrorCount int NOT NULL DEFAULT 0,
    LastError nvarchar(1000) NULL,
    UpdatedAt datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT CK_ExternalSystemStatus_Status CHECK (Status IN ('Healthy', 'Degraded', 'Unavailable')),
    CONSTRAINT UQ_ExternalSystemStatus_SystemName UNIQUE (SystemName)
);
```

---

## Indexes and Performance

### Composite Indexes for Common Queries

```sql
-- Application search and filtering
CREATE INDEX IX_LoanApplications_Composite_Status_Date 
ON dbo.LoanApplications (Status, SubmittedAt) 
INCLUDE (EmployeeId, ProductType, RequestedAmount);

-- Employee loan history
CREATE INDEX IX_LoanApplications_Employee_Date 
ON dbo.LoanApplications (EmployeeId, SubmittedAt DESC) 
INCLUDE (Status, ProductType, ApprovedAmount);

-- Payment processing queries
CREATE INDEX IX_Payments_Status_Priority_Date 
ON dbo.Payments (Status, Priority, RequestedAt) 
INCLUDE (PaymentId, Amount, ApplicationId);

-- Shift earnings calculations
CREATE INDEX IX_EmployeeShifts_Employee_Period 
ON dbo.EmployeeShifts (EmployeeId, PayrollPeriod) 
INCLUDE (TotalEarnings, RegularHours, OvertimeHours);

-- Repayment schedule processing
CREATE INDEX IX_PaymentSchedules_DueDate_Status 
ON dbo.PaymentSchedules (DueDate, Status) 
INCLUDE (AgreementId, TotalAmount, PayrollPeriod);
```

### Columnstore Indexes for Reporting

```sql
-- Reporting columnstore index
CREATE NONCLUSTERED COLUMNSTORE INDEX IX_LoanApplications_Reporting
ON dbo.LoanApplications (
    EmployeeId, ProductType, Status, RequestedAmount, ApprovedAmount, 
    SubmittedAt, ProcessedAt, Channel, RiskCategory
);

CREATE NONCLUSTERED COLUMNSTORE INDEX IX_Payments_Reporting
ON dbo.Payments (
    ApplicationId, PaymentType, Amount, Status, RequestedAt, 
    CompletedAt, Priority
);
```

---

## Stored Procedures

### 1. Employee Affordability Assessment

```sql
CREATE PROCEDURE dbo.sp_CalculateAffordability
    @EmployeeId uniqueidentifier,
    @RequestedAmount decimal(18,2),
    @ProductType nvarchar(20)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @MonthlyIncome decimal(18,2);
    DECLARE @AvailableAmount decimal(18,2);
    DECLARE @MaxAdvanceAmount decimal(18,2);
    DECLARE @MaxLoanAmount decimal(18,2);
    
    -- Calculate monthly income from shifts
    SELECT @MonthlyIncome = AVG(MonthlyEarnings)
    FROM (
        SELECT SUM(TotalEarnings) as MonthlyEarnings
        FROM dbo.EmployeeShifts s
        WHERE s.EmployeeId = @EmployeeId
        AND s.ShiftDate >= DATEADD(month, -3, GETDATE())
        GROUP BY s.PayrollPeriod
    ) t;
    
    -- Calculate available advance amount (current month earnings)
    SELECT @AvailableAmount = ISNULL(SUM(TotalEarnings), 0)
    FROM dbo.EmployeeShifts s
    WHERE s.EmployeeId = @EmployeeId
    AND s.PayrollPeriod = FORMAT(GETDATE(), 'yyyy-MM');
    
    -- Get employer parameters
    SELECT 
        @MaxAdvanceAmount = @AvailableAmount * (CAST(p1.ParameterValue AS decimal) / 100),
        @MaxLoanAmount = @MonthlyIncome * (CAST(p2.ParameterValue AS decimal) / 100)
    FROM dbo.Employees e
    INNER JOIN dbo.EmployerParameters p1 ON e.EmployerId = p1.EmployerId 
        AND p1.ProductType = 'Advance' 
        AND p1.ParameterName = 'MaxPercentage'
    INNER JOIN dbo.EmployerParameters p2 ON e.EmployerId = p2.EmployerId 
        AND p2.ProductType = 'ShortTermLoan' 
        AND p2.ParameterName = 'MaxShiftPercentage'
    WHERE e.EmployeeId = @EmployeeId;
    
    -- Return results
    SELECT 
        @MonthlyIncome as MonthlyIncome,
        @AvailableAmount as CurrentMonthEarnings,
        CASE 
            WHEN @ProductType = 'Advance' THEN @MaxAdvanceAmount
            WHEN @ProductType = 'ShortTermLoan' THEN @MaxLoanAmount
            ELSE 0
        END as MaxQualifiedAmount,
        CASE 
            WHEN @ProductType = 'Advance' AND @RequestedAmount <= @MaxAdvanceAmount THEN 1
            WHEN @ProductType = 'ShortTermLoan' AND @RequestedAmount <= @MaxLoanAmount THEN 1
            ELSE 0
        END as IsQualified;
END;
```

### 2. Create Repayment Schedule

```sql
CREATE PROCEDURE dbo.sp_CreateRepaymentSchedule
    @AgreementId uniqueidentifier,
    @PrincipalAmount decimal(18,2),
    @InterestRate decimal(5,2),
    @Term int
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @MonthlyPayment decimal(18,2);
    DECLARE @InstallmentNumber int = 1;
    DECLARE @RemainingBalance decimal(18,2) = @PrincipalAmount;
    DECLARE @CurrentDate date = DATEADD(month, 1, CAST(GETDATE() AS date));
    
    -- Calculate monthly payment using loan formula
    SET @MonthlyPayment = @PrincipalAmount * 
        (@InterestRate/100/12) * POWER(1 + (@InterestRate/100/12), @Term) /
        (POWER(1 + (@InterestRate/100/12), @Term) - 1);
    
    WHILE @InstallmentNumber <= @Term
    BEGIN
        DECLARE @InterestPortion decimal(18,2) = @RemainingBalance * (@InterestRate/100/12);
        DECLARE @PrincipalPortion decimal(18,2) = @MonthlyPayment - @InterestPortion;
        
        -- Handle final payment rounding
        IF @InstallmentNumber = @Term
        BEGIN
            SET @PrincipalPortion = @RemainingBalance;
            SET @MonthlyPayment = @PrincipalPortion + @InterestPortion;
        END;
        
        INSERT INTO dbo.PaymentSchedules (
            AgreementId, InstallmentNumber, DueDate, 
            PrincipalAmount, InterestAmount, TotalAmount
        )
        VALUES (
            @AgreementId, @InstallmentNumber, @CurrentDate,
            @PrincipalPortion, @InterestPortion, @MonthlyPayment
        );
        
        SET @RemainingBalance = @RemainingBalance - @PrincipalPortion;
        SET @CurrentDate = DATEADD(month, 1, @CurrentDate);
        SET @InstallmentNumber = @InstallmentNumber + 1;
    END;
END;
```

### 3. Process Batch Payments

```sql
CREATE PROCEDURE dbo.sp_ProcessBatchPayments
    @BatchSize int = 100
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @BatchId uniqueidentifier = NEWID();
    
    -- Update pending payments to processing
    UPDATE TOP (@BatchSize) dbo.Payments 
    SET 
        Status = 'Processing',
        BatchId = @BatchId,
        ProcessedAt = GETUTCDATE()
    WHERE Status = 'Pending' 
    AND Priority IN ('Standard', 'Urgent')
    ORDER BY 
        CASE Priority 
            WHEN 'Urgent' THEN 1 
            WHEN 'Standard' THEN 2 
            ELSE 3 
        END,
        RequestedAt;
    
    -- Return batch for external processing
    SELECT 
        p.PaymentId,
        p.PaymentReference,
        p.Amount,
        p.RecipientName,
        p.RecipientAccount,
        p.BranchCode,
        p.RecipientBank
    FROM dbo.Payments p
    WHERE p.BatchId = @BatchId;
    
    -- Log batch processing
    INSERT INTO integration.IntegrationLogs (
        SystemName, Operation, RequestData, Status
    )
    VALUES (
        'BankingSystem', 'BatchPaymentProcessing', 
        '{"BatchId":"' + CAST(@BatchId AS nvarchar(50)) + '","Count":' + CAST(@@ROWCOUNT AS nvarchar) + '}',
        'Success'
    );
END;
```

---

## Views and Functions

### 1. Employee Dashboard View

```sql
CREATE VIEW reporting.vw_EmployeeDashboard
AS
SELECT 
    e.EmployeeId,
    e.EmployeeNumber,
    e.FirstName + ' ' + e.LastName as FullName,
    e.Position,
    emp.CompanyName,
    
    -- Current month earnings
    ISNULL(shifts.CurrentMonthEarnings, 0) as CurrentMonthEarnings,
    ISNULL(shifts.CurrentMonthHours, 0) as CurrentMonthHours,
    
    -- Loan summary
    ISNULL(loans.ActiveLoansCount, 0) as ActiveLoansCount,
    ISNULL(loans.TotalOutstanding, 0) as TotalOutstanding,
    loans.LastApplicationDate,
    loans.LastApplicationStatus,
    
    -- Qualification amounts
    af.MaxAdvanceAmount,
    af.MaxLoanAmount
    
FROM dbo.Employees e
INNER JOIN dbo.Employers emp ON e.EmployerId = emp.EmployerId
LEFT JOIN (
    SELECT 
        EmployeeId,
        SUM(CASE WHEN PayrollPeriod = FORMAT(GETDATE(), 'yyyy-MM') THEN TotalEarnings ELSE 0 END) as CurrentMonthEarnings,
        SUM(CASE WHEN PayrollPeriod = FORMAT(GETDATE(), 'yyyy-MM') THEN RegularHours + OvertimeHours ELSE 0 END) as CurrentMonthHours
    FROM dbo.EmployeeShifts
    GROUP BY EmployeeId
) shifts ON e.EmployeeId = shifts.EmployeeId
LEFT JOIN (
    SELECT 
        la.EmployeeId,
        COUNT(*) as ActiveLoansCount,
        SUM(ps.TotalAmount) as TotalOutstanding,
        MAX(la.SubmittedAt) as LastApplicationDate,
        MAX(la.Status) as LastApplicationStatus
    FROM dbo.LoanApplications la
    INNER JOIN dbo.LoanAgreements lag ON la.ApplicationId = lag.ApplicationId
    INNER JOIN dbo.PaymentSchedules ps ON lag.AgreementId = ps.AgreementId
    WHERE la.Status IN ('Disbursed', 'Active')
    AND ps.Status IN ('Scheduled', 'SentToPayroll')
    GROUP BY la.EmployeeId
) loans ON e.EmployeeId = loans.EmployeeId
OUTER APPLY (
    SELECT 
        shifts.CurrentMonthEarnings * 0.8 as MaxAdvanceAmount,
        e.MonthlyRate * 0.25 as MaxLoanAmount
) af
WHERE e.IsActive = 1;
```

### 2. Compliance Reporting View

```sql
CREATE VIEW reporting.vw_NCRCompliance
AS
SELECT 
    FORMAT(la.SubmittedAt, 'yyyy-MM') as ReportingMonth,
    la.ProductType,
    COUNT(*) as ApplicationCount,
    COUNT(CASE WHEN la.Status IN ('Approved', 'Disbursed') THEN 1 END) as ApprovedCount,
    SUM(la.ApprovedAmount) as TotalApprovedAmount,
    AVG(la.ApprovedAmount) as AverageAmount,
    AVG(la.ProcessingTimeMinutes) as AverageProcessingMinutes,
    
    -- Interest rate compliance
    AVG(CASE WHEN la.ProductType = 'ShortTermLoan' THEN la.InterestRate END) as AverageInterestRate,
    MAX(CASE WHEN la.ProductType = 'ShortTermLoan' THEN la.InterestRate END) as MaxInterestRate,
    
    -- Fee compliance
    AVG(la.InitiationFee) as AverageInitiationFee,
    MAX(la.InitiationFee) as MaxInitiationFee
    
FROM dbo.LoanApplications la
WHERE la.SubmittedAt >= DATEADD(month, -12, GETDATE())
GROUP BY FORMAT(la.SubmittedAt, 'yyyy-MM'), la.ProductType;
```

### 3. Utility Functions

```sql
-- Calculate business days between dates
CREATE FUNCTION dbo.fn_BusinessDaysBetween(
    @StartDate date,
    @EndDate date
)
RETURNS int
AS
BEGIN
    DECLARE @BusinessDays int;
    
    SET @BusinessDays = (
        SELECT COUNT(*)
        FROM (
            SELECT DATEADD(day, number, @StartDate) as CalcDate
            FROM master.dbo.spt_values
            WHERE type = 'P' 
            AND DATEADD(day, number, @StartDate) <= @EndDate
        ) t
        WHERE DATEPART(weekday, t.CalcDate) NOT IN (1, 7) -- Exclude weekends
    );
    
    RETURN @BusinessDays;
END;
```

---

## Security and Permissions

### 1. Database Roles

```sql
-- Create custom database roles
CREATE ROLE db_application_user;
CREATE ROLE db_integration_user;
CREATE ROLE db_reporting_user;
CREATE ROLE db_admin_user;

-- Application user permissions
GRANT SELECT, INSERT, UPDATE ON dbo.Users TO db_application_user;
GRANT SELECT, INSERT, UPDATE ON dbo.Employees TO db_application_user;
GRANT SELECT, INSERT, UPDATE ON dbo.LoanApplications TO db_application_user;
GRANT SELECT, INSERT, UPDATE ON dbo.ApplicationDocuments TO db_application_user;
GRANT SELECT, INSERT, UPDATE ON dbo.LoanAgreements TO db_application_user;
GRANT SELECT, INSERT, UPDATE ON dbo.Payments TO db_application_user;
GRANT SELECT, INSERT ON audit.AuditLog TO db_application_user;
GRANT SELECT, INSERT ON audit.ComplianceLog TO db_application_user;

-- Integration user permissions
GRANT SELECT, INSERT, UPDATE ON dbo.EmployeeShifts TO db_integration_user;
GRANT SELECT, INSERT ON integration.IntegrationLogs TO db_integration_user;
GRANT SELECT, UPDATE ON integration.ExternalSystemStatus TO db_integration_user;

-- Reporting user permissions (read-only)
GRANT SELECT ON ALL OBJECTS IN SCHEMA dbo TO db_reporting_user;
GRANT SELECT ON ALL OBJECTS IN SCHEMA reporting TO db_reporting_user;
GRANT SELECT ON ALL OBJECTS IN SCHEMA audit TO db_reporting_user;
```

### 2. Row Level Security

```sql
-- Enable RLS for multi-tenant isolation
ALTER TABLE dbo.Employees ENABLE ROW LEVEL SECURITY;

-- Create security policy for employer isolation
CREATE FUNCTION dbo.fn_EmployerSecurityPredicate(@EmployerId uniqueidentifier)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN SELECT 1 as AccessResult
WHERE @EmployerId = CAST(SESSION_CONTEXT(N'EmployerId') AS uniqueidentifier)
OR IS_MEMBER('db_admin_user') = 1;

CREATE SECURITY POLICY dbo.EmployerSecurityPolicy
ADD FILTER PREDICATE dbo.fn_EmployerSecurityPredicate(EmployerId) ON dbo.Employees,
ADD FILTER PREDICATE dbo.fn_EmployerSecurityPredicate(
    (SELECT EmployerId FROM dbo.Employees WHERE EmployeeId = dbo.LoanApplications.EmployeeId)
) ON dbo.LoanApplications;
```

### 3. Data Encryption

```sql
-- Create master key and certificates for encryption
CREATE MASTER KEY ENCRYPTION BY PASSWORD = 'StrongPassword123!';

CREATE CERTIFICATE HoHemaEncryption
WITH SUBJECT = 'Ho Hema Loans Data Encryption';

CREATE SYMMETRIC KEY HoHemaSymmetricKey
WITH ALGORITHM = AES_256
ENCRYPTION BY CERTIFICATE HoHemaEncryption;

-- Encrypt sensitive columns
ALTER TABLE dbo.Employees 
ADD IdNumberEncrypted varbinary(256);

-- Update procedure to encrypt ID numbers
CREATE PROCEDURE dbo.sp_EncryptEmployeeData
AS
BEGIN
    OPEN SYMMETRIC KEY HoHemaSymmetricKey 
    DECRYPTION BY CERTIFICATE HoHemaEncryption;
    
    UPDATE dbo.Employees 
    SET IdNumberEncrypted = ENCRYPTBYKEY(KEY_GUID('HoHemaSymmetricKey'), IdNumber);
    
    CLOSE SYMMETRIC KEY HoHemaSymmetricKey;
END;
```

---

## Data Migration Scripts

### Initial Data Setup

```sql
-- Insert default system parameters
INSERT INTO dbo.EmployerParameters (EmployerId, ProductType, ParameterName, ParameterValue, DataType)
SELECT 
    e.EmployerId,
    'Advance',
    'MaxPercentage',
    '80',
    'decimal'
FROM dbo.Employers e;

INSERT INTO dbo.EmployerParameters (EmployerId, ProductType, ParameterName, ParameterValue, DataType)
SELECT 
    e.EmployerId,
    'Advance', 
    'TransactionFee',
    '60.00',
    'decimal'
FROM dbo.Employers e;

-- Create system admin user
INSERT INTO dbo.Users (UserId, PhoneNumber, Email, PasswordHash, Salt, Role)
VALUES (
    NEWID(),
    '+27100000000',
    'admin@hohema.co.za',
    'hashed_password_here',
    'salt_here',
    'Administrator'
);
```

---

## Backup and Recovery

### Backup Strategy

```sql
-- Full backup schedule (daily)
BACKUP DATABASE HoHemaLoans 
TO DISK = 'C:\Backups\HoHemaLoans_Full_' + FORMAT(GETDATE(), 'yyyyMMdd_HHmmss') + '.bak'
WITH COMPRESSION, CHECKSUM, STATS = 10;

-- Transaction log backup (every 15 minutes)
BACKUP LOG HoHemaLoans 
TO DISK = 'C:\Backups\HoHemaLoans_Log_' + FORMAT(GETDATE(), 'yyyyMMdd_HHmmss') + '.trn'
WITH COMPRESSION, CHECKSUM, STATS = 10;

-- Differential backup (every 6 hours)
BACKUP DATABASE HoHemaLoans 
TO DISK = 'C:\Backups\HoHemaLoans_Diff_' + FORMAT(GETDATE(), 'yyyyMMdd_HHmmss') + '.bak'
WITH DIFFERENTIAL, COMPRESSION, CHECKSUM, STATS = 10;
```

### Recovery Procedures

```sql
-- Point-in-time recovery example
RESTORE DATABASE HoHemaLoans_Recovery 
FROM DISK = 'C:\Backups\HoHemaLoans_Full_20251107_000000.bak'
WITH REPLACE, NORECOVERY, STATS = 10;

RESTORE LOG HoHemaLoans_Recovery 
FROM DISK = 'C:\Backups\HoHemaLoans_Log_20251107_120000.trn'
WITH STOPAT = '2025-11-07 12:30:00', NORECOVERY, STATS = 10;

RESTORE DATABASE HoHemaLoans_Recovery WITH RECOVERY;
```

---

## Database Maintenance

### Index Maintenance

```sql
-- Rebuild/reorganize indexes based on fragmentation
WITH IndexMaintenance AS (
    SELECT 
        OBJECT_NAME(ips.object_id) as TableName,
        i.name as IndexName,
        ips.avg_fragmentation_in_percent,
        CASE 
            WHEN ips.avg_fragmentation_in_percent > 30 THEN 'REBUILD'
            WHEN ips.avg_fragmentation_in_percent > 10 THEN 'REORGANIZE'
            ELSE 'NONE'
        END as MaintenanceAction
    FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
    INNER JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
    WHERE ips.avg_fragmentation_in_percent > 10
)
SELECT 
    'ALTER INDEX [' + IndexName + '] ON [' + TableName + '] ' + 
    CASE MaintenanceAction
        WHEN 'REBUILD' THEN 'REBUILD WITH (ONLINE = ON, SORT_IN_TEMPDB = ON)'
        WHEN 'REORGANIZE' THEN 'REORGANIZE'
    END as MaintenanceCommand
FROM IndexMaintenance
WHERE MaintenanceAction != 'NONE';
```

### Statistics Update

```sql
-- Update statistics for all tables
EXEC sp_MSforeachtable 'UPDATE STATISTICS ? WITH FULLSCAN';
```

This comprehensive database schema provides a robust foundation for the Ho Hema Loans platform, ensuring data integrity, performance, security, and compliance with South African regulations.