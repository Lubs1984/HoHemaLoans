using System.Globalization;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HoHemaLoans.Api.Data;
using HoHemaLoans.Api.Models;

namespace HoHemaLoans.Api.Services;

public interface IBulkUserImportService
{
    Task<BulkImportValidationResult> ValidateImportDataAsync(Stream fileStream, string fileName);
    Task<BulkImportResult> ImportUsersAsync(List<BulkUserImportDto> users, string adminUserId);
}

public class BulkUserImportService : IBulkUserImportService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<BulkUserImportService> _logger;

    public BulkUserImportService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<BulkUserImportService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<BulkImportValidationResult> ValidateImportDataAsync(Stream fileStream, string fileName)
    {
        var result = new BulkImportValidationResult
        {
            IsValid = true,
            ValidationErrors = new List<BulkImportValidationError>(),
            ValidUsers = new List<BulkUserImportDto>(),
            TotalRows = 0
        };

        try
        {
            var users = new List<BulkUserImportDto>();
            
            if (fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                users = await ParseCsvFileAsync(fileStream);
            }
            else if (fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) || 
                     fileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
            {
                // For now, only support CSV. Excel support would require additional packages like EPPlus or ClosedXML
                result.IsValid = false;
                result.ValidationErrors.Add(new BulkImportValidationError
                {
                    RowNumber = 0,
                    Field = "FileType",
                    Error = "Excel files not supported yet. Please convert to CSV format."
                });
                return result;
            }
            else
            {
                result.IsValid = false;
                result.ValidationErrors.Add(new BulkImportValidationError
                {
                    RowNumber = 0,
                    Field = "FileType",
                    Error = "Unsupported file format. Please use CSV format."
                });
                return result;
            }

            result.TotalRows = users.Count;

            // Get existing emails and phone numbers for duplicate checking
            var existingEmailsList = await _context.Users
                .Where(u => u.Email != null)
                .Select(u => u.Email.ToLower())
                .ToListAsync();
            var existingEmails = existingEmailsList.ToHashSet();
            
            var existingPhonesList = await _context.Users
                .Where(u => !string.IsNullOrEmpty(u.PhoneNumber))
                .Select(u => u.PhoneNumber!)
                .ToListAsync();
            var existingPhones = existingPhonesList.ToHashSet();

            // Validate each user
            for (int i = 0; i < users.Count; i++)
            {
                var user = users[i];
                user.RowNumber = i + 2; // +2 because row 1 is header and arrays are 0-indexed
                
                var userErrors = ValidateUser(user, existingEmails, existingPhones);
                
                if (userErrors.Any())
                {
                    result.ValidationErrors.AddRange(userErrors);
                    result.IsValid = false;
                }
                else
                {
                    result.ValidUsers.Add(user);
                    // Add email and phone to existing sets to check for duplicates within the file
                    existingEmails.Add(user.Email.ToLower());
                    if (!string.IsNullOrEmpty(user.PhoneNumber))
                        existingPhones.Add(user.PhoneNumber);
                }
            }

            _logger.LogInformation("Bulk import validation completed. Total rows: {TotalRows}, Valid: {ValidCount}, Errors: {ErrorCount}",
                result.TotalRows, result.ValidUsers.Count, result.ValidationErrors.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating bulk import file");
            result.IsValid = false;
            result.ValidationErrors.Add(new BulkImportValidationError
            {
                RowNumber = 0,
                Field = "File",
                Error = $"Error processing file: {ex.Message}"
            });
            return result;
        }
    }

    public async Task<BulkImportResult> ImportUsersAsync(List<BulkUserImportDto> users, string adminUserId)
    {
        var result = new BulkImportResult
        {
            TotalUsers = users.Count,
            SuccessCount = 0,
            FailureCount = 0,
            Errors = new List<string>()
        };

        foreach (var userDto in users)
        {
            try
            {
                var user = new ApplicationUser
                {
                    UserName = userDto.Email,
                    Email = userDto.Email,
                    EmailConfirmed = true, // Auto-confirm bulk imported users
                    FirstName = userDto.FirstName,
                    LastName = userDto.LastName,
                    IdNumber = userDto.IdNumber,
                    DateOfBirth = userDto.DateOfBirth,
                    Address = userDto.Address,
                    PhoneNumber = userDto.PhoneNumber,
                    MonthlyIncome = userDto.MonthlyIncome,
                    IsVerified = false, // Admin can verify later
                    CreatedAt = DateTime.UtcNow
                };

                // Generate a temporary password (user should reset on first login)
                var tempPassword = GenerateTemporaryPassword();
                var createResult = await _userManager.CreateAsync(user, tempPassword);

                if (createResult.Succeeded)
                {
                    // Assign default User role
                    await _userManager.AddToRoleAsync(user, "User");
                    result.SuccessCount++;
                    
                    _logger.LogInformation("Successfully imported user {Email} (Row {RowNumber})",
                        userDto.Email, userDto.RowNumber);
                }
                else
                {
                    result.FailureCount++;
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    result.Errors.Add($"Row {userDto.RowNumber} ({userDto.Email}): {errors}");
                    
                    _logger.LogError("Failed to import user {Email} (Row {RowNumber}): {Errors}",
                        userDto.Email, userDto.RowNumber, errors);
                }
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.Errors.Add($"Row {userDto.RowNumber} ({userDto.Email}): {ex.Message}");
                
                _logger.LogError(ex, "Exception importing user {Email} (Row {RowNumber})",
                    userDto.Email, userDto.RowNumber);
            }
        }

        _logger.LogInformation("Bulk import completed by admin {AdminUserId}. Success: {SuccessCount}, Failures: {FailureCount}",
            adminUserId, result.SuccessCount, result.FailureCount);

        return result;
    }

    private async Task<List<BulkUserImportDto>> ParseCsvFileAsync(Stream fileStream)
    {
        using var reader = new StreamReader(fileStream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        
        // Configure CSV reader to be case-insensitive for headers
        csv.Context.RegisterClassMap<BulkUserImportMap>();
        
        var users = new List<BulkUserImportDto>();
        await foreach (var user in csv.GetRecordsAsync<BulkUserImportDto>())
        {
            users.Add(user);
        }
        
        return users;
    }

    private List<BulkImportValidationError> ValidateUser(
        BulkUserImportDto user, 
        HashSet<string> existingEmails, 
        HashSet<string> existingPhones)
    {
        var errors = new List<BulkImportValidationError>();

        // Email validation
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            errors.Add(new BulkImportValidationError
            {
                RowNumber = user.RowNumber,
                Field = "Email",
                Error = "Email is required"
            });
        }
        else if (!IsValidEmail(user.Email))
        {
            errors.Add(new BulkImportValidationError
            {
                RowNumber = user.RowNumber,
                Field = "Email",
                Error = "Invalid email format"
            });
        }
        else if (existingEmails.Contains(user.Email.ToLower()))
        {
            errors.Add(new BulkImportValidationError
            {
                RowNumber = user.RowNumber,
                Field = "Email",
                Error = "Email already exists in system or duplicate in file"
            });
        }

        // Name validation
        if (string.IsNullOrWhiteSpace(user.FirstName))
        {
            errors.Add(new BulkImportValidationError
            {
                RowNumber = user.RowNumber,
                Field = "FirstName",
                Error = "First name is required"
            });
        }

        if (string.IsNullOrWhiteSpace(user.LastName))
        {
            errors.Add(new BulkImportValidationError
            {
                RowNumber = user.RowNumber,
                Field = "LastName",
                Error = "Last name is required"
            });
        }

        // ID Number validation
        if (string.IsNullOrWhiteSpace(user.IdNumber))
        {
            errors.Add(new BulkImportValidationError
            {
                RowNumber = user.RowNumber,
                Field = "IdNumber",
                Error = "ID number is required"
            });
        }
        else if (!IsValidSouthAfricanId(user.IdNumber))
        {
            errors.Add(new BulkImportValidationError
            {
                RowNumber = user.RowNumber,
                Field = "IdNumber",
                Error = "Invalid South African ID number format"
            });
        }

        // Date of Birth validation
        if (user.DateOfBirth == default)
        {
            errors.Add(new BulkImportValidationError
            {
                RowNumber = user.RowNumber,
                Field = "DateOfBirth",
                Error = "Date of birth is required"
            });
        }
        else if (user.DateOfBirth > DateTime.Today.AddYears(-18))
        {
            errors.Add(new BulkImportValidationError
            {
                RowNumber = user.RowNumber,
                Field = "DateOfBirth",
                Error = "User must be at least 18 years old"
            });
        }

        // Phone number validation
        if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            if (!IsValidPhoneNumber(user.PhoneNumber))
            {
                errors.Add(new BulkImportValidationError
                {
                    RowNumber = user.RowNumber,
                    Field = "PhoneNumber",
                    Error = "Invalid phone number format"
                });
            }
            else if (existingPhones.Contains(user.PhoneNumber))
            {
                errors.Add(new BulkImportValidationError
                {
                    RowNumber = user.RowNumber,
                    Field = "PhoneNumber",
                    Error = "Phone number already exists in system or duplicate in file"
                });
            }
        }

        // Monthly income validation
        if (user.MonthlyIncome <= 0)
        {
            errors.Add(new BulkImportValidationError
            {
                RowNumber = user.RowNumber,
                Field = "MonthlyIncome",
                Error = "Monthly income must be greater than 0"
            });
        }

        // Address validation
        if (string.IsNullOrWhiteSpace(user.Address))
        {
            errors.Add(new BulkImportValidationError
            {
                RowNumber = user.RowNumber,
                Field = "Address",
                Error = "Address is required"
            });
        }

        return errors;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidSouthAfricanId(string idNumber)
    {
        if (string.IsNullOrWhiteSpace(idNumber) || idNumber.Length != 13)
            return false;

        return idNumber.All(char.IsDigit);
    }

    private static bool IsValidPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        // Remove spaces, dashes, brackets
        var cleaned = Regex.Replace(phoneNumber, @"[\s\-\(\)]", "");
        
        // Check if it starts with + and has digits
        if (cleaned.StartsWith("+"))
        {
            cleaned = cleaned[1..];
        }

        return cleaned.All(char.IsDigit) && cleaned.Length >= 10 && cleaned.Length <= 15;
    }

    private static string GenerateTemporaryPassword()
    {
        // Generate a secure temporary password
        const string chars = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}

// CSV Mapping class
public class BulkUserImportMap : ClassMap<BulkUserImportDto>
{
    public BulkUserImportMap()
    {
        Map(m => m.Email).Name("Email", "email", "Email Address", "email_address");
        Map(m => m.FirstName).Name("FirstName", "firstname", "First Name", "first_name");
        Map(m => m.LastName).Name("LastName", "lastname", "Last Name", "last_name");
        Map(m => m.IdNumber).Name("IdNumber", "idnumber", "ID Number", "id_number", "IdentityNumber");
        Map(m => m.DateOfBirth).Name("DateOfBirth", "dateofbirth", "Date of Birth", "date_of_birth", "DOB", "dob");
        Map(m => m.Address).Name("Address", "address");
        Map(m => m.PhoneNumber).Name("PhoneNumber", "phonenumber", "Phone Number", "phone_number", "Phone", "phone");
        Map(m => m.MonthlyIncome).Name("MonthlyIncome", "monthlyincome", "Monthly Income", "monthly_income", "Income", "income");
    }
}