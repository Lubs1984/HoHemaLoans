# Authentication Testing Checklist

## Prerequisites
- [ ] Backend running on `http://localhost:5149`
- [ ] Frontend running on `http://localhost:5173`
- [ ] Database migrations applied (Entity Framework)
- [ ] Dependencies installed (`npm install` in frontend folder)

## Registration Flow Tests

### Test 1: Successful Registration
- [ ] Navigate to `/register`
- [ ] Fill in all required fields with valid data
- [ ] Accept terms and conditions
- [ ] Click "Create Account"
- **Expected**: Redirect to dashboard (`/`), token in localStorage
- **Verify**: `localStorage.getItem('auth-store')` contains token

### Test 2: Validation Errors
- [ ] Try submitting with invalid email format
- [ ] Try submitting with passwords that don't match
- [ ] Try submitting with missing required fields
- [ ] Try submitting without accepting terms
- **Expected**: Error messages appear below each field, form not submitted

### Test 3: Email Validation
- [ ] Try registering with already-used email
- **Expected**: "Email already in use" error from backend

### Test 4: Phone Number Validation
- [ ] Try registering with invalid phone number format
- **Expected**: "Please enter a valid phone number" error

### Test 5: Password Strength
- [ ] Try registering with password < 6 characters
- **Expected**: "Password must be at least 6 characters" error

### Test 6: Pre-filled Form
- [ ] Register successfully
- [ ] Logout (button in dashboard navigation)
- [ ] Go back to `/register`
- **Expected**: Form should be empty (PublicRoute redirects to dashboard after logout)

## Login Flow Tests

### Test 7: Successful Login
- [ ] Navigate to `/login`
- [ ] Enter registered email
- [ ] Enter correct password
- [ ] Click "Sign in"
- **Expected**: Redirect to dashboard, token in localStorage

### Test 8: Invalid Credentials
- [ ] Try login with correct email but wrong password
- **Expected**: "Invalid credentials" or "Login failed" error message

### Test 9: Non-existent User
- [ ] Try login with email that was never registered
- **Expected**: "User not found" or "Invalid credentials" error

### Test 10: Email Format Validation
- [ ] Try submitting login with invalid email format
- **Expected**: "Please enter a valid email address" error

### Test 11: Empty Fields
- [ ] Try submitting login with empty email
- [ ] Try submitting login with empty password
- **Expected**: "Email is required" / "Password is required" errors

### Test 12: Loading State
- [ ] Click "Sign in" button
- [ ] Quickly check button text changes to "Signing in..."
- [ ] Verify form inputs are disabled during submission
- **Expected**: All disabled until response received

## Token & Session Tests

### Test 13: Token Persistence
- [ ] Login successfully
- [ ] Check browser localStorage for `auth-store` key
- [ ] Refresh browser page (F5)
- **Expected**: Still logged in, dashboard loads, token still present

### Test 14: Token in API Calls
- [ ] Login successfully
- [ ] Open DevTools Network tab
- [ ] Navigate to `/loans`
- [ ] Check HTTP request headers
- **Expected**: `Authorization: Bearer {token}` header present

### Test 15: Token Expiry (manual)
- [ ] Extract token from localStorage
- [ ] Manually delete/corrupt it in DevTools
- [ ] Refresh page
- **Expected**: Redirect to `/login`

## Protected Routes Tests

### Test 16: Direct Access to Protected Route
- [ ] Clear localStorage or logout
- [ ] Try direct access to `/loans`
- **Expected**: Redirect to `/login`

### Test 17: Direct Access to Dashboard
- [ ] Clear localStorage or logout
- [ ] Try direct access to `/`
- **Expected**: Redirect to `/login`

### Test 18: Direct Access to Profile
- [ ] Clear localStorage or logout
- [ ] Try direct access to `/profile`
- **Expected**: Redirect to `/login`

## Public Routes Tests

### Test 19: Authenticated Access to Login
- [ ] Login successfully
- [ ] Navigate to `/login`
- **Expected**: Redirect to `/` (dashboard)

### Test 20: Authenticated Access to Register
- [ ] Login successfully
- [ ] Navigate to `/register`
- **Expected**: Redirect to `/` (dashboard)

## Logout Tests

### Test 21: Logout Functionality
- [ ] Login successfully
- [ ] Find logout button (should be in header/navigation)
- [ ] Click logout
- **Expected**: Redirect to `/login`, localStorage cleared, auth store reset

### Test 22: Session Isolation
- [ ] Register with User A
- [ ] Logout
- [ ] Register/Login with User B
- [ ] Verify dashboard shows User B info
- **Expected**: No User A data visible, clean session isolation

## Error Handling Tests

### Test 23: Server Error Handling
- [ ] Stop backend server
- [ ] Try to login
- **Expected**: Connection error message displayed

### Test 24: Backend Validation Error
- [ ] Register with email of existing user
- **Expected**: Backend error message displayed in error banner

### Test 25: Network Timeout
- [ ] Throttle network (DevTools > Network tab > slow 3G)
- [ ] Try to login
- **Expected**: Loading state shown, appropriate timeout error handling

## Browser Compatibility Tests

- [ ] Chrome/Chromium (Latest)
- [ ] Firefox (Latest)
- [ ] Safari (Latest)
- [ ] Edge (Latest)

## Responsive Design Tests

- [ ] Desktop (1920px)
- [ ] Laptop (1366px)
- [ ] Tablet (768px)
- [ ] Mobile (375px)
- [ ] **Expected**: Forms should be readable and functional at all sizes

## Cross-Origin Tests

### Test 26: CORS Headers
- [ ] Frontend on `localhost:5173`
- [ ] Backend on `localhost:5149`
- [ ] Perform login
- **Expected**: CORS headers properly configured, no CORS errors in console

## Security Tests

### Test 27: XSS Prevention
- [ ] Try registering with name containing `<script>alert('xss')</script>`
- **Expected**: Script should be escaped/sanitized, not executed

### Test 28: Password Not in Local Storage
- [ ] Login successfully
- [ ] Check localStorage
- **Expected**: Only token stored, never the password

### Test 29: Token in URL
- [ ] Login successfully
- [ ] Verify token NOT in URL (only in Authorization header and localStorage)
- **Expected**: Token not exposed in URL

## Performance Tests

### Test 30: Login Response Time
- [ ] Login successfully
- [ ] Open DevTools > Network
- [ ] Check POST `/api/auth/login` response time
- **Expected**: < 2 seconds typically

### Test 31: Page Load After Login
- [ ] Login successfully
- [ ] Navigate to `/loans`
- **Expected**: Page loads smoothly, no lag or flicker

## Documentation Tests

- [ ] AUTHENTICATION-SETUP.md accurately describes the flow
- [ ] All endpoints documented
- [ ] All fields documented
- [ ] All error cases documented
- [ ] Troubleshooting section helpful

## Known Issues to Track

- [ ] Issue: __________________ | Status: _________ | Fix: __________
- [ ] Issue: __________________ | Status: _________ | Fix: __________
- [ ] Issue: __________________ | Status: _________ | Fix: __________

## Test Results Summary

**Date**: ___________
**Tester**: ___________
**Total Tests**: 31
**Passed**: ___ 
**Failed**: ___
**Notes**: 

___________________________________________________________________________

___________________________________________________________________________

## Sign-off

- [ ] QA Lead Approval
- [ ] Backend Developer Approval
- [ ] Frontend Developer Approval

**Ready for Phase 2**: Yes / No

---

## Quick Test Sequence (5 minutes)

1. ✅ Register new account
2. ✅ Logout
3. ✅ Login with registered credentials
4. ✅ Navigate to /loans (protected route)
5. ✅ Refresh page and verify still authenticated
6. ✅ Try accessing /register (should redirect to dashboard)
7. ✅ Logout and try accessing /loans (should redirect to login)

**All Pass** = Authentication system working correctly ✅
