// Core user types
export interface User {
  id: string;
  email?: string;
  firstName?: string;
  lastName?: string;
  phoneNumber?: string;
  idNumber?: string;
  dateOfBirth?: Date;
  address?: string; // Legacy field - kept for backward compatibility
  monthlyIncome?: number;
  isVerified?: boolean;
  role?: UserRole;
  roles?: string[];
  isActive?: boolean;
  lastLoginAt?: Date;
  createdAt?: Date;
  updatedAt?: Date;
  
  // NCR-required South African Address fields
  streetAddress?: string;
  suburb?: string;
  city?: string;
  province?: string;
  postalCode?: string;
  
  // NCR-required Employment fields
  employerName?: string;
  employeeNumber?: string;
  payrollReference?: string;
  employmentType?: string;
  
  // NCR-required Banking fields
  bankName?: string;
  accountType?: string;
  accountNumber?: string;
  branchCode?: string;
  
  // NCR-required Next of Kin fields
  nextOfKinName?: string;
  nextOfKinRelationship?: string;
  nextOfKinPhone?: string;
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