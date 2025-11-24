// Loan application types
export interface LoanApplication {
  id: string;
  employeeId: string;
  productType: ProductType;
  requestedAmount: number;
  approvedAmount?: number;
  status: ApplicationStatus;
  channel: ApplicationChannel;
  referenceNumber: string;
  
  // Financial details
  monthlyIncome?: number;
  declaredExpenses?: number;
  calculatedExpenses?: number;
  disposableIncome?: number;
  creditScore?: number;
  riskCategory?: RiskCategory;
  
  // Terms and conditions
  loanTerm?: number; // months
  interestRate?: number;
  initiationFee?: number;
  adminFee?: number;
  totalRepaymentAmount?: number;
  
  // Tracking
  submittedAt: Date;
  processedAt?: Date;
  approvedAt?: Date;
  disbursedAt?: Date;
  processingTimeMinutes?: number;
  
  createdAt: Date;
  updatedAt: Date;
}

export interface LoanAgreement {
  id: string;
  applicationId: string;
  agreementNumber: string;
  principalAmount: number;
  interestRate: number;
  initiationFee: number;
  adminFee: number;
  totalAmount: number;
  repaymentTerm: number;
  aodAmount: number;
  aodDescription: string;
  signedBy: string;
  signatureMethod: SignatureMethod;
  signedAt: Date;
  ipAddress?: string;
  userAgent?: string;
  contractDocumentPath: string;
  aodDocumentPath: string;
}

export interface PaymentSchedule {
  id: string;
  agreementId: string;
  installmentNumber: number;
  dueDate: Date;
  principalAmount: number;
  interestAmount: number;
  totalAmount: number;
  status: PaymentStatus;
  payrollPeriod?: string;
  processedAt?: Date;
}

export interface Payment {
  id: string;
  applicationId: string;
  paymentType: PaymentType;
  amount: number;
  status: PaymentProcessingStatus;
  paymentReference: string;
  bankReference?: string;
  recipientName: string;
  recipientAccount: string;
  recipientBank: string;
  branchCode: string;
  batchId?: string;
  priority: PaymentPriority;
  requestedAt: Date;
  processedAt?: Date;
  completedAt?: Date;
  failureReason?: string;
}

export type ProductType = 'Advance' | 'ShortTermLoan';

export type ApplicationStatus = 
  | 'Submitted' 
  | 'PendingValidation' 
  | 'ValidatingIdentity' 
  | 'ValidatingEmployment'
  | 'AffordabilityAssessment' 
  | 'Approved' 
  | 'ContractSigning' 
  | 'PaymentProcessing'
  | 'Disbursed' 
  | 'Declined' 
  | 'Cancelled' 
  | 'Expired';

export type ApplicationChannel = 'Web' | 'WhatsApp' | 'Mobile' | 'Phone';

export type RiskCategory = 'Low' | 'Medium' | 'High';

export type SignatureMethod = 'OTP' | 'Biometric' | 'Digital' | 'Wet';

export type PaymentStatus = 'Scheduled' | 'SentToPayroll' | 'Deducted' | 'Failed' | 'Reversed';

export type PaymentType = 'LoanDisbursement' | 'Refund';

export type PaymentProcessingStatus = 'Pending' | 'Queued' | 'Processing' | 'Completed' | 'Failed' | 'Cancelled';

export type PaymentPriority = 'Standard' | 'Urgent' | 'Immediate';

// Form types for UI
export interface LoanApplicationForm {
  productType: ProductType;
  requestedAmount: string;
  declaredExpenses?: string;
  loanPurpose?: string;
  employmentConfirmed: boolean;
  termsAccepted: boolean;
}