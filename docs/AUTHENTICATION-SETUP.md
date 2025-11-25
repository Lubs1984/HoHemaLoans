# Authentication Flow Setup - Complete

## Overview
The Ho Hema Loans application now has a complete end-to-end authentication system with JWT-based authentication, allowing employees to register, login, and access protected resources.

## Architecture

### Authentication Flow
```
User Registration → User Login → JWT Token → Protected Routes → Dashboard
```

### Technology Stack
- **Backend**: ASP.NET Core 8 with JWT authentication
- **Frontend**: React 18 with TypeScript, Zustand state management
- **Storage**: SQLite (local development), JWT stored in localStorage
- **Token Expiry**: 7 days (configurable in backend)

## Backend Implementation

### 1. Authentication Controller
**Location**: `src/api/HoHemaLoans.Api/Controllers/AuthController.cs`

**Endpoints**:
- `POST /api/auth/login` - Login with email/password
- `POST /api/auth/register` - Register new employee account
- `POST /api/auth/refresh` - Refresh JWT token (future implementation)
- `POST /api/auth/logout` - Logout and invalidate token (optional)

**JWT Configuration**:
- Located in `appsettings.json` under `JwtSettings`
- Secret Key: Configure in production
- Token Expiry: 7 days
- Issuer: "HoHemaLoans"
- Audience: "HoHemaLoansUsers"

### 2. Database Models
**Location**: `src/api/HoHemaLoans.Api/Models/User.cs`

**User Model Fields**:
```csharp
- Id (GUID)
- Email (required, unique)
- PasswordHash (hashed with bcrypt)
- FirstName
- LastName
- PhoneNumber
- IdNumber
- DateOfBirth
- Address
- MonthlyIncome
- IsVerified (default: false)
- Role (default: "Employee")
- CreatedAt (timestamp)
- UpdatedAt (timestamp)
```

### 3. Entity Framework Configuration
**Location**: `src/api/HoHemaLoans.Api/Data/ApplicationDbContext.cs`

- Configured with SQLite for local development
- User entity mapped with constraints
- Email field has unique constraint
- CreatedAt/UpdatedAt auto-managed

## Frontend Implementation

### 1. State Management (Zustand)
**Location**: `src/frontend/src/store/authStore.ts`

**State Structure**:
```typescript
{
  user: User | null
  token: string | null
  refreshToken: string | null
  isAuthenticated: boolean
  isLoading: boolean
  error: string | null
}
```

**Key Methods**:
- `login(user, token, refreshToken)` - Store auth state + localStorage
- `logout()` - Clear auth state + localStorage
- `setUser(user)` - Update user profile
- `setLoading(bool)` - Loading state
- `setError(error)` - Error state
- `clearError()` - Clear error messages

**Persistence**: Stores auth data in localStorage with key `auth-store`

### 2. API Service
**Location**: `src/frontend/src/services/api.ts`

**Key Features**:
- Automatic JWT injection in Authorization header: `Bearer {token}`
- Automatic token retrieval from auth store
- Error handling with fallback messages
- Support for JSON request/response bodies

**Methods**:
- `login(credentials)` - POST /auth/login
- `register(data)` - POST /auth/register
- `refreshToken()` - POST /auth/refresh (future)

### 3. Login Page
**Location**: `src/frontend/src/pages/auth/Login.tsx`

**Features**:
- Email and password input fields
- Client-side validation (email format, password length)
- Real-time error messages
- Remember me functionality (can be expanded)
- Password visibility toggle
- Responsive gradient design
- Disabled state during submission
- Error banner display
- Links to registration and forgot password

**Validation Rules**:
- Email: Valid email format required
- Password: Minimum 6 characters

**On Success**:
- Stores token in auth store
- Redirects to `/dashboard` (protected route)
- Token persists across browser sessions

### 4. Register Page
**Location**: `src/frontend/src/pages/auth/Register.tsx`

**Form Fields** (9 total):
1. First Name
2. Last Name
3. Email Address
4. Phone Number
5. ID Number
6. Date of Birth
7. Address
8. Monthly Income
9. Password & Confirm Password
10. Terms & Conditions checkbox

**Validation**:
- Individual field validation with error messages
- Password confirmation matching
- Terms & conditions must be accepted
- Email format validation
- Phone number format validation
- All required fields must be filled

**On Success**:
- Stores returned token in auth store
- Creates user profile
- Redirects to `/dashboard`
- Token persists across browser sessions

### 5. Type Definitions
**Location**: `src/frontend/src/types/`

**`index.ts`**:
```typescript
interface LoginRequest {
  email: string
  password: string
}

interface LoginResponse {
  token: string
  refreshToken?: string
  user: User
}

interface RegisterRequest {
  email: string
  password: string
  firstName: string
  lastName: string
  phoneNumber: string
  idNumber: string
  dateOfBirth: Date
  address: string
  monthlyIncome: number
}
```

**`user.ts`**:
```typescript
interface User {
  id?: string
  email?: string
  firstName?: string
  lastName?: string
  phoneNumber?: string
  idNumber?: string
  dateOfBirth?: string | Date
  address?: string
  monthlyIncome?: number
  isVerified?: boolean
  role?: string
  createdAt?: string
  updatedAt?: string
}
```

### 6. Routing & Protected Routes
**Location**: `src/frontend/src/App.tsx`

**Route Structure**:
```
Public Routes:
  /login - Login page (redirects to / if authenticated)
  /register - Registration page (redirects to / if authenticated)

Protected Routes (require authentication):
  / - Dashboard (Layout wrapper)
    /loans - Loan Applications
    /profile - User Profile
```

**Route Components**:
- `PublicRoute` - Redirects authenticated users to dashboard
- `ProtectedRoute` - Redirects unauthenticated users to login
- `Layout` - Wrapper component for authenticated pages

## Configuration

### Backend Configuration
**`appsettings.json`**:
```json
{
  "JwtSettings": {
    "Secret": "your-super-secret-key-change-in-production",
    "Issuer": "HoHemaLoans",
    "Audience": "HoHemaLoansUsers",
    "ExpiryMinutes": 10080
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173", "http://localhost:5174"]
  }
}
```

### Frontend Environment
**Development Server**: `http://localhost:5173`
**Backend API**: `http://localhost:5149` (or configured value)

## Testing the Authentication Flow

### 1. Manual Testing - Web Browser

**Registration Flow**:
1. Navigate to `http://localhost:5173/register`
2. Fill in all required fields:
   - Name, Email, Phone, ID, DOB, Address, Income
   - Password (minimum 6 characters)
   - Confirm Password
   - Accept Terms & Conditions
3. Click "Create Account"
4. Should redirect to `/` (dashboard) if successful
5. Token should persist in browser localStorage

**Login Flow**:
1. Logout or clear localStorage
2. Navigate to `http://localhost:5173/login`
3. Enter registered email and password
4. Click "Sign in"
5. Should redirect to `/` (dashboard) if successful
6. Dashboard should show user welcome message

**Protected Route Testing**:
1. Clear localStorage or close browser
2. Try accessing `http://localhost:5173/loans` directly
3. Should redirect to `/login`
4. After login, can access `/loans`

### 2. API Testing with Postman/curl

**Register Endpoint**:
```bash
curl -X POST http://localhost:5149/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "employee@example.com",
    "password": "password123",
    "firstName": "John",
    "lastName": "Doe",
    "phoneNumber": "+27812345678",
    "idNumber": "1234567890123",
    "dateOfBirth": "1990-01-15",
    "address": "123 Main St, Cape Town",
    "monthlyIncome": 25000
  }'
```

**Login Endpoint**:
```bash
curl -X POST http://localhost:5149/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "employee@example.com",
    "password": "password123"
  }'
```

**Protected Endpoint** (with JWT):
```bash
curl -X GET http://localhost:5149/api/loanapplications \
  -H "Authorization: Bearer {token_from_login}"
```

## Deployment Considerations

### Security Best Practices

1. **JWT Secret**:
   - Change `JwtSettings.Secret` in production
   - Use environment variables for sensitive data
   - Never commit secrets to repository

2. **HTTPS**:
   - Use HTTPS in production
   - Configure proper CORS origins
   - Set Secure flag on cookies if using them

3. **Password Security**:
   - Passwords are hashed with bcrypt
   - Implement password reset functionality
   - Consider adding password strength requirements UI

4. **Token Management**:
   - Consider implementing refresh tokens
   - Implement token revocation/logout
   - Consider short token expiry (1 hour) with refresh tokens

### Environment-Specific Settings

**Development** (`appsettings.Development.json`):
- Local SQLite database
- Localhost CORS origins
- JWT secret for testing only

**Production**:
- Azure SQL or managed database
- Production domain CORS origins
- Environment variable for JWT secret
- HTTPS enabled
- Security headers configured

## Next Steps

### Phase 2 - Enhanced Authentication
1. **Password Reset Flow**
   - Email verification link
   - Temporary password reset token
   - Password reset page UI

2. **Refresh Token Implementation**
   - Extend session without re-login
   - Store refresh token securely
   - Automatic token refresh on expiry

3. **OAuth/SSO Integration** (optional)
   - Microsoft Entra ID
   - Google Sign-In
   - Multi-factor authentication

### Phase 3 - WhatsApp Integration
- Link authentication with WhatsApp messaging
- Send verification codes via WhatsApp
- Receive loan application updates via WhatsApp

### Phase 4 - User Profile Management
- Edit profile page
- Change password functionality
- Account settings
- Privacy preferences

## Troubleshooting

### Issue: "Unauthorized" on protected routes
**Solution**: Ensure token is present in localStorage with key `auth-store`

### Issue: CORS errors on login/register
**Solution**: Check CORS configuration in `Program.cs` includes your frontend URL

### Issue: "Invalid token" error
**Solution**: Check JWT secret matches between backend and token generation

### Issue: Token persists after logout
**Solution**: Ensure logout clears both auth store and localStorage

## Files Summary

**Backend Files**:
- `Controllers/AuthController.cs` - Authentication endpoints
- `Models/User.cs` - User entity
- `Data/ApplicationDbContext.cs` - Database context
- `appsettings.json` - JWT configuration

**Frontend Files**:
- `pages/auth/Login.tsx` - Login component
- `pages/auth/Register.tsx` - Register component
- `store/authStore.ts` - Zustand auth state
- `services/api.ts` - API client
- `App.tsx` - Route configuration
- `types/index.ts` & `types/user.ts` - TypeScript types

**Configuration Files**:
- `appsettings.json` - Backend config
- `vite.config.ts` - Frontend build config
- `tsconfig.json` - TypeScript config

## Status

✅ **Complete Implementation**:
- Registration form with all required fields
- Login form with validation
- JWT token generation and storage
- Protected routes with redirect
- API integration with auth store
- Type safety with TypeScript
- Error handling and user feedback
- Responsive UI design

⏳ **Next to Implement**:
- Password reset functionality
- Email verification
- Refresh token rotation
- User profile management
- WhatsApp integration with auth
