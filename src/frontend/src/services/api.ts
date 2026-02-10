import type { LoginRequest, LoginResponse, RegisterRequest } from '../types';
import type { PhoneLoginRequest, PhoneVerifyRequest } from '../types';

// Determine API URL based on environment
function getApiUrl(): string {
  // 1. Check for runtime config (set by docker-entrypoint.sh)
  if ((window as any).__API_URL__) {
    console.log('[API] Using runtime config API URL:', (window as any).__API_URL__);
    return (window as any).__API_URL__;
  }
  
  // 2. Check for build-time env var
  if (import.meta.env.VITE_API_URL) {
    console.log('[API] Using VITE_API_URL:', import.meta.env.VITE_API_URL);
    return import.meta.env.VITE_API_URL;
  }
  
  // 3. Railway detection - if on Railway frontend, use Railway API
  if (typeof window !== 'undefined') {
    const hostname = window.location.hostname;
    
    // Check if we're on Railway
    if (hostname.includes('hohemaweb-development.up.railway.app')) {
      const apiUrl = 'https://hohemaapi-development.up.railway.app/api';
      console.log('[API] Detected Railway environment, using:', apiUrl);
      return apiUrl;
    }
    
    if (hostname.includes('hohemaweb') && hostname.includes('railway.app')) {
      // For other Railway deployments, try to infer API URL
      const apiUrl = hostname.replace('hohemaweb', 'hohemaapi');
      console.log('[API] Detected Railway environment, using:', `https://${apiUrl}`);
      return `https://${apiUrl}`;
    }
  }
  
  // 4. Docker development: use service name
  if (typeof window !== 'undefined' && window.location.hostname === 'hohema-frontend') {
    console.log('[API] Using Docker service name: http://hohema-api:5000');
    return 'http://hohema-api:5000';
  }
  
  // 5. Smart local development fallback
  // If accessed via localhost, try different API ports
  if (typeof window !== 'undefined' && window.location.hostname === 'localhost') {
    console.log('[API] Using localhost: http://localhost:5214/api');
    return 'http://localhost:5214/api';
  }
  
  // 6. Docker localhost detection - if port is 5174 (frontend port)
  if (typeof window !== 'undefined' && window.location.port === '5174') {
    console.log('[API] Using localhost (port 5174): http://localhost:5214/api');
    return 'http://localhost:5214/api';
  }
  
  // 7. Default fallback for production
  console.log('[API] Using default relative path: /api');
  return '/api';
}

const API_BASE_URL = getApiUrl();
console.log('[API] Final API base URL:', API_BASE_URL);

class ApiService {
  private baseUrl = API_BASE_URL;

  async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> {
    const url = `${this.baseUrl}${endpoint}`;
    
    console.log(`[API] Making ${options.method || 'GET'} request to:`, url);
    
    // Get token from auth store
    const authStore = JSON.parse(localStorage.getItem('auth-store') || '{}');
    const token = authStore.state?.token;

    // Don't set Content-Type for FormData — browser sets it with boundary automatically
    const isFormData = options.body instanceof FormData;

    const headers: Record<string, string> = {
      ...(isFormData ? {} : { 'Content-Type': 'application/json' }),
      ...(token && { Authorization: `Bearer ${token}` }),
      ...(options.headers as Record<string, string>),
    };

    try {
      const response = await fetch(url, {
        ...options,
        headers,
        credentials: 'include',
        mode: 'cors',
      });

      console.log(`[API] Response status:`, response.status);

      if (!response.ok) {
        const errorText = await response.text();
        let errorMessage = `Request failed (${response.status})`;
        
        try {
          const errorData = JSON.parse(errorText);
          errorMessage = errorData.message || errorData.title || errorData.detail || errorMessage;
        } catch {
          if (errorText && errorText.length > 0 && errorText.length < 500) {
            errorMessage = errorText;
          }
        }
        
        console.error(`[API] Error response (${response.status}):`, errorMessage, errorText?.substring(0, 200));
        throw new Error(errorMessage);
      }

      // Handle empty responses
      const contentType = response.headers.get('content-type');
      if (contentType && contentType.includes('application/json')) {
        const data = await response.json();
        console.log(`[API] Success response received`);
        return data;
      }
      
      return {} as T;
    } catch (error) {
      console.error('[API] Request failed:', error);
      if (error instanceof TypeError && error.message === 'Failed to fetch') {
        throw new Error('Unable to connect to server. Please check your internet connection and try again.');
      }
      throw error;
    }
  }

  // Authentication endpoints
  async login(credentials: LoginRequest): Promise<LoginResponse> {
    return this.request<LoginResponse>('/auth/login', {
      method: 'POST',
      body: JSON.stringify(credentials),
    });
  }

  async loginMobileRequest(data: PhoneLoginRequest): Promise<{ message: string; phoneNumber: string }> {
    return this.request<{ message: string; phoneNumber: string }>('/auth/login-mobile-request', {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  async loginMobileVerify(data: PhoneVerifyRequest): Promise<LoginResponse> {
    return this.request<LoginResponse>('/auth/login-mobile-verify', {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  async register(data: RegisterRequest): Promise<LoginResponse> {
    return this.request<LoginResponse>('/auth/register', {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  async logout(): Promise<void> {
    return this.request<void>('/auth/logout', {
      method: 'POST',
    });
  }

  async refreshToken(refreshToken: string): Promise<LoginResponse> {
    return this.request<LoginResponse>('/auth/refresh', {
      method: 'POST',
      body: JSON.stringify({ refreshToken }),
    });
  }

  // OTP endpoints (not implemented in backend yet)
  async sendOtp(phoneNumber: string, type: string): Promise<void> {
    return this.request<void>('/auth/send-otp', {
      method: 'POST',
      body: JSON.stringify({ phoneNumber, type }),
    });
  }

  async verifyOtp(phoneNumber: string, otp: string, type: string): Promise<void> {
    return this.request<void>('/auth/verify-otp', {
      method: 'POST',
      body: JSON.stringify({ phoneNumber, otp, type }),
    });
  }

  // User profile endpoints (using auth user data)
  async getProfile(): Promise<any> {
    return this.request<any>('/profile');
  }

  async updateProfile(data: any): Promise<any> {
    return this.request<any>('/profile', {
      method: 'PUT',
      body: JSON.stringify(data),
    });
  }

  // Loan application endpoints
  async createLoanApplication(data: any): Promise<any> {
    return this.request<any>('/loanapplications', {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  async submitLoanApplication(data: any): Promise<any> {
    return this.request<any>('/loanapplications', {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  async getLoanApplications(): Promise<any[]> {
    return this.request<any[]>('/loanapplications');
  }

  async getLoanApplication(id: string): Promise<any> {
    return this.request<any>(`/loanapplications/${id}`);
  }

  // Dashboard endpoints (using loan applications for stats)
  async getDashboardStats(): Promise<any> {
    return this.getLoanApplications();
  }

  // Health check
  async healthCheck(): Promise<any> {
    return this.request<any>('/health');
  }

  // Income endpoints
  async getIncomes(): Promise<any[]> {
    return this.request<any[]>('/profile/income');
  }

  async addIncome(data: any): Promise<any> {
    return this.request<any>('/profile/income', {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  async updateIncome(id: string, data: any): Promise<void> {
    return this.request<void>(`/profile/income/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    });
  }

  async deleteIncome(id: string): Promise<void> {
    return this.request<void>(`/profile/income/${id}`, {
      method: 'DELETE',
    });
  }

  // Expense endpoints
  async getExpenses(): Promise<any[]> {
    return this.request<any[]>('/profile/expense');
  }

  async addExpense(data: any): Promise<any> {
    return this.request<any>('/profile/expense', {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  async updateExpense(id: string, data: any): Promise<void> {
    return this.request<void>(`/profile/expense/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    });
  }

  async deleteExpense(id: string): Promise<void> {
    return this.request<void>(`/profile/expense/${id}`, {
      method: 'DELETE',
    });
  }

  // Affordability endpoints
  async getAffordability(): Promise<any> {
    return this.request<any>('/profile/affordability');
  }

  async getMaxLoanAmount(): Promise<any> {
    return this.request<any>('/profile/affordability/max-loan');
  }

  // Document endpoints
  async getUserDocuments(): Promise<any[]> {
    return this.request<any[]>('/documents');
  }

  async uploadDocument(file: File, documentType: string, notes?: string): Promise<any> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('documentType', documentType);
    if (notes) {
      formData.append('notes', notes);
    }

    // Get token from auth store (same method as main request function)
    const authStore = JSON.parse(localStorage.getItem('auth-store') || '{}');
    const token = authStore.state?.token;

    const response = await fetch(`${this.baseUrl}/documents/upload`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
      },
      body: formData,
    });

    if (!response.ok) {
      let errorMessage = 'Upload failed';
      try {
        const contentType = response.headers.get('content-type');
        if (contentType && contentType.includes('application/json')) {
          const errorData = await response.json();
          errorMessage = errorData.message || errorMessage;
        } else {
          errorMessage = await response.text() || errorMessage;
        }
      } catch (e) {
        // Fallback to status text if parsing fails
        errorMessage = response.statusText || errorMessage;
      }
      throw new Error(errorMessage);
    }

    // Check if response has content and is JSON
    const contentType = response.headers.get('content-type');
    if (contentType && contentType.includes('application/json')) {
      return response.json();
    } else {
      // Return a default success response if no JSON content
      return { success: true, message: 'Document uploaded successfully' };
    }
  }

  async getVerificationStatus(): Promise<any> {
    return this.request<any>('/documents/verification-status');
  }

  // Generic HTTP methods for flexible API calls
  async get<T>(endpoint: string): Promise<{ data: T }> {
    const data = await this.request<T>(endpoint, {
      method: 'GET',
    });
    return { data };
  }

  async post<T>(endpoint: string, body: any): Promise<{ data: T }> {
    const data = await this.request<T>(endpoint, {
      method: 'POST',
      body: body instanceof FormData ? body : JSON.stringify(body),
    });
    return { data };
  }

  async put<T>(endpoint: string, body: any): Promise<{ data: T }> {
    const data = await this.request<T>(endpoint, {
      method: 'PUT',
      body: body instanceof FormData ? body : JSON.stringify(body),
    });
    return { data };
  }

  async delete<T>(endpoint: string): Promise<{ data: T }> {
    const data = await this.request<T>(endpoint, {
      method: 'DELETE',
    });
    return { data };
  }
}

export const apiService = new ApiService();