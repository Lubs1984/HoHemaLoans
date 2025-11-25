import type { LoginRequest, LoginResponse, RegisterRequest } from '../types';

// Use runtime config if available, otherwise fall back to env var or default
const API_BASE_URL = (window as any).__API_URL__ || import.meta.env.VITE_API_URL || 'http://localhost:8080/api';

class ApiService {
  private baseUrl = API_BASE_URL;

  private async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> {
    const url = `${this.baseUrl}${endpoint}`;
    
    // Get token from auth store
    const authStore = JSON.parse(localStorage.getItem('auth-store') || '{}');
    const token = authStore.state?.token;

    const headers = {
      'Content-Type': 'application/json',
      ...(token && { Authorization: `Bearer ${token}` }),
      ...options.headers,
    };

    try {
      const response = await fetch(url, {
        ...options,
        headers,
        credentials: 'include',
      });

      if (!response.ok) {
        const errorText = await response.text();
        let errorMessage = 'An error occurred';
        
        try {
          const errorData = JSON.parse(errorText);
          errorMessage = errorData.message || errorData.title || errorMessage;
        } catch {
          errorMessage = errorText || errorMessage;
        }
        
        throw new Error(errorMessage);
      }

      // Handle empty responses
      const contentType = response.headers.get('content-type');
      if (contentType && contentType.includes('application/json')) {
        return await response.json();
      }
      
      return {} as T;
    } catch (error) {
      console.error('API request failed:', error);
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
    return this.request<any>('/users/profile');
  }

  async updateProfile(data: any): Promise<any> {
    return this.request<any>('/users/profile', {
      method: 'PUT',
      body: JSON.stringify(data),
    });
  }

  // Loan application endpoints
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
}

export const apiService = new ApiService();