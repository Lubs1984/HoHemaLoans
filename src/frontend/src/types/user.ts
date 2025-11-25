// Core user types
export interface User {
  id: string;
  email?: string;
  firstName?: string;
  lastName?: string;
  phoneNumber?: string;
  idNumber?: string;
  dateOfBirth?: Date;
  address?: string;
  monthlyIncome?: number;
  isVerified?: boolean;
  role?: UserRole;
  isActive?: boolean;
  lastLoginAt?: Date;
  createdAt?: Date;
  updatedAt?: Date;
}

export interface Employee extends User {
  employeeId: string;
  employerId: string;
  employeeNumber: string;
  idNumber: string;
  firstName: string;
  lastName: string;
  dateOfBirth: Date;
  gender?: 'M' | 'F' | 'O';
  position?: string;
  department?: string;
  employmentStartDate: Date;
  employmentEndDate?: Date;
  monthlyRate: number;
  payrollFrequency: PayrollFrequency;
  bankDetails: BankDetails;
}

export interface Employer {
  id: string;
  companyName: string;
  companyRegistrationNumber?: string;
  contactPerson: string;
  contactEmail: string;
  contactPhone: string;
  address?: string;
  isActive: boolean;
  contractStartDate: Date;
  contractEndDate?: Date;
}

export interface BankDetails {
  bankName: string;
  accountNumber: string;
  branchCode: string;
  accountHolder: string;
}

export type UserRole = 'Employee' | 'Administrator' | 'Agent' | 'SystemIntegrator';
export type PayrollFrequency = 'Weekly' | 'Bi-Weekly' | 'Monthly';