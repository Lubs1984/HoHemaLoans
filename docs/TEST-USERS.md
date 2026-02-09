# 🧪 Test Users - Ho Hema Loans

## Quick Reference

Use these test accounts to login and test the application. All test users have the same password.

### Test Credentials

| Name | Email | Password | ID Number | Income |
|------|-------|----------|-----------|--------|
| John Doe | `john.doe@example.com` | `TestPassword123!` | 9001010001234 | R25,000 |
| Jane Smith | `jane.smith@example.com` | `TestPassword123!` | 8512055678901 | R18,500 |
| Demo User | `demo@example.com` | `TestPassword123!` | 7603015678901 | R22,000 |
| Test Account | `test@example.com` | `TestPassword123!` | 9504156789012 | R20,000 |

---

## 📝 Detailed Test User Information

### User 1: John Doe (Established Employee)
- **Email**: john.doe@example.com
- **Password**: TestPassword123!
- **Name**: John Doe
- **ID**: 9001010001234
- **DOB**: 1990-01-01
- **Phone**: +27812345678
- **Address**: 123 Main Street, Cape Town, 8000
- **Monthly Income**: R25,000
- **Status**: Verified ✅
- **Use Case**: Established employee with good income, likely to qualify for loans

### User 2: Jane Smith (Mid-Income Casual)
- **Email**: jane.smith@example.com
- **Password**: TestPassword123!
- **Name**: Jane Smith
- **ID**: 8512055678901
- **DOB**: 1985-12-05
- **Phone**: +27821234567
- **Address**: 456 Oak Avenue, Johannesburg, 2000
- **Monthly Income**: R18,500
- **Status**: Verified ✅
- **Use Case**: Casual employee with moderate income

### User 3: Demo User (Testing User)
- **Email**: demo@example.com
- **Password**: TestPassword123!
- **Name**: Demo User
- **ID**: 7603015678901
- **DOB**: 1976-03-01
- **Phone**: +27831234567
- **Address**: 789 Test Lane, Durban, 4000
- **Monthly Income**: R22,000
- **Status**: Verified ✅
- **Use Case**: Demonstration purposes

### User 4: Test Account (Quick Testing)
- **Email**: test@example.com
- **Password**: TestPassword123!
- **Name**: Test Account
- **ID**: 9504156789012
- **DOB**: 1995-04-15
- **Phone**: +27809876543
- **Address**: 321 Demo Road, Pretoria, 0001
- **Monthly Income**: R20,000
- **Status**: Verified ✅
- **Use Case**: Quick testing and validation

---

## 🚀 How to Login

### Via Web Browser
1. Navigate to `http://localhost:5173/register` (or `/login`)
2. Enter email: `test@example.com` (or any test user email)
3. Enter password: `TestPassword123!`
4. Click "Sign in"
5. Should redirect to dashboard

### Via API (cURL)
```bash
curl -X POST http://localhost:5149/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "TestPassword123!"
  }'
```

**Response**:
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIs...",
    "user": {
      "id": "...",
      "email": "test@example.com",
      "firstName": "Test",
      "lastName": "Account",
      "phoneNumber": "+27809876543",
      "idNumber": "9504156789012",
      "dateOfBirth": "1995-04-15T00:00:00Z",
      "address": "321 Demo Road, Pretoria, 0001",
      "monthlyIncome": 20000,
      "isVerified": true
    }
  }
}
```

### Via Postman
1. Create new POST request
2. URL: `http://localhost:5149/api/auth/login`
3. Headers:
   - `Content-Type: application/json`
4. Body (JSON):
   ```json
   {
     "email": "test@example.com",
     "password": "TestPassword123!"
   }
   ```
5. Click Send
6. Copy the `token` from response

---

## 🔒 Using the JWT Token

Once logged in, use the token in subsequent API requests:

```bash
curl -X GET http://localhost:5149/api/loanapplications \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

Or in Postman:
1. Go to "Authorization" tab
2. Type: Select "Bearer Token"
3. Token: Paste your token from login response
4. Requests will automatically include the Authorization header

---

## 🧪 Testing Scenarios

### Scenario 1: First-Time User Testing
```
1. Open http://localhost:5173/register
2. Try registering with any of the test user emails (they won't be in system yet on first run)
3. Fill in details and submit
4. Should create account and redirect to dashboard
```

### Scenario 2: Existing User Login
```
1. Open http://localhost:5173/login
2. Use: test@example.com / TestPassword123!
3. Should login successfully and redirect to dashboard
```

### Scenario 3: Multiple Users
```
1. Login with test@example.com
2. Check dashboard shows Test Account
3. Logout
4. Login with john.doe@example.com
5. Check dashboard shows John Doe
6. Verify each user has their own session
```

### Scenario 4: Protected Routes
```
1. Clear browser localStorage (DevTools > Application > Storage > Clear)
2. Try accessing http://localhost:5173/loans directly
3. Should redirect to /login (not authenticated)
4. Login with any test user
5. Try /loans again - should show page (authenticated)
```

---

## 📋 Test Data Summary

### All Test Users Share:
- **Password**: `TestPassword123!`
- **Email Verified**: ✅ Yes
- **Account Status**: ✅ Verified
- **Created At**: Application startup

### Monthly Income Range:
- Minimum: R18,500 (Jane Smith)
- Maximum: R25,000 (John Doe)
- Average: R21,375

### Geographic Distribution:
- Cape Town (1)
- Johannesburg (1)
- Durban (1)
- Pretoria (1)

---

## 🔄 Creating Additional Test Users

To add more test users, edit `/src/api/HoHemaLoans.Api/Data/DbInitializer.cs`:

1. Add new `ApplicationUser` object in `SeedTestUsersAsync` method
2. Add tuple with user and password to `testUsers` array
3. Restart backend application
4. New user will be created on startup

---

## 🚨 Important Notes

- ⚠️ **Test users are created on application startup** if they don't already exist
- ⚠️ **Passwords are hashed** with bcrypt before storage
- ⚠️ **Emails are unique** - if user exists, account won't be duplicated
- ⚠️ **All test users are pre-verified** to skip email confirmation flow
- ⚠️ **Income values are for testing** - not connected to actual payroll systems

---

## 💡 Tips for Testing

### Test Registration Flow
- Use a new email not in the test users list
- Password must be at least 6 characters
- Confirm password must match
- Must accept terms & conditions

### Test Login Flow
- Incorrect email → "Invalid credentials" error
- Incorrect password → "Invalid credentials" error
- Correct credentials → Redirect to dashboard with token

### Test Protected Routes
- Without token → Redirects to `/login`
- With valid token → Shows protected page
- With expired token → Redirects to `/login`

### Test Token Persistence
- Login with test user
- Refresh browser (F5)
- Should still be logged in (token in localStorage)
- Clear localStorage
- Refresh browser
- Should be logged out

---

## 📞 Troubleshooting

### Can't login with test users?
1. Check backend is running: `dotnet run` in `src/api/HoHemaLoans.Api`
2. Check frontend is running: `npm run dev` in `src/frontend`
3. Check CORS is enabled for your frontend URL
4. Check test users were created - look for "✅ Created test user" logs

### Test users don't appear on startup?
1. Ensure DbInitializer is called in Program.cs
2. Check database connection is working
3. Look for errors in console output
4. Manually create users via `/api/auth/register` endpoint

### Password not working?
1. Test users have password: `TestPassword123!`
2. Ensure you typed it correctly (case-sensitive)
3. Try resetting password or creating new account

### Token expiration?
1. Test tokens expire after 7 days
2. Login again to get fresh token
3. Or adjust token expiry in `appsettings.json` `JwtSettings.ExpiryMinutes`

---

## 🔐 Security Notes

- 🔒 **Test users are for development only** - remove from production
- 🔒 **Default password should be changed** for any production environment
- 🔒 **Passwords are hashed with bcrypt** - not stored in plaintext
- 🔒 **Emails are unique** - prevents multiple accounts per email
- 🔒 **Tokens are JWT** - stateless and expire after 7 days

---

## ✅ Verification Checklist

After application startup, verify test users are created:

```bash
# Check logs for:
✅ Created test user: john.doe@example.com
✅ Created test user: jane.smith@example.com
✅ Created test user: demo@example.com
✅ Created test user: test@example.com

# Or run query against database:
SELECT Email, FirstName, LastName, IsVerified FROM AspNetUsers;
```

Expected output:
```
john.doe@example.com  | John   | Doe     | true
jane.smith@example.com | Jane  | Smith   | true
demo@example.com      | Demo   | User    | true
test@example.com      | Test   | Account | true
```

---

*Test users are automatically created on application startup via DbInitializer.cs*
