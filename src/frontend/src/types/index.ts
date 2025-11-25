// API response types
export interface ApiResponse<T = any> {
  success: boolean;
  data?: T;
  message?: string;
  errors?: ApiError[];
  pagination?: PaginationInfo;
}

export interface ApiError {
  field?: string;
  code: string;
  message: string;
}

export interface PaginationInfo {
  currentPage: number;
  totalPages: number;
  pageSize: number;
  totalItems: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

// Authentication types
export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  user: User;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  idNumber: string;
  dateOfBirth: Date;
  address: string;
  monthlyIncome: number;
}

export interface OtpRequest {
  phoneNumber: string;
  type: OtpType;
}

export interface OtpVerification {
  phoneNumber: string;
  otp: string;
  type: OtpType;
}

export type OtpType = 'registration' | 'login' | 'password_reset' | 'transaction';

// Common UI types
export interface SelectOption {
  label: string;
  value: string;
  disabled?: boolean;
}

export interface FormError {
  field: string;
  message: string;
}

export interface LoadingState {
  isLoading: boolean;
  error?: string;
}

// WhatsApp types
export interface WhatsAppSession {
  id: string;
  phoneNumber: string;
  employeeId?: string;
  sessionState: string;
  currentFlow?: string;
  currentStep?: string;
  sessionData?: Record<string, any>;
  lastMessageAt: Date;
  expiresAt: Date;
  isActive: boolean;
}

// Dashboard types
export interface DashboardStats {
  totalApplications: number;
  pendingApplications: number;
  approvedApplications: number;
  totalDisbursed: number;
  currentMonthEarnings: number;
  availableAdvanceAmount: number;
  maxLoanAmount: number;
  outstandingBalance: number;
}

// Notification types
export interface Notification {
  id: string;
  type: NotificationType;
  title: string;
  message: string;
  isRead: boolean;
  createdAt: Date;
  actionUrl?: string;
}

export type NotificationType = 'info' | 'success' | 'warning' | 'error';

// Re-export user and loan types
export * from './user';
export * from './loans';

// Import for local use
import type { User } from './user';