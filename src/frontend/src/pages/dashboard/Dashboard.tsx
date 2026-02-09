import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { apiService } from '../../services/api';

interface LoanApplication {
  id: string;
  status: string;
}

interface PrerequisiteStatus {
  profileComplete: boolean;
  documentsUploaded: boolean;
  affordabilityComplete: boolean;
}

interface DocumentStatus {
  totalDocuments: number;
  approvedDocuments: number;
  pendingDocuments: number;
  missingDocuments: string[];
}

interface DashboardStats {
  availableAdvance: number;
  currentBalance: number;
  hoursThisMonth: number;
  earningsThisMonth: number;
}

const Dashboard: React.FC = () => {
  const [draftApplication, setDraftApplication] = useState<LoanApplication | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [prerequisiteStatus, setPrerequisiteStatus] = useState<PrerequisiteStatus>({
    profileComplete: false,
    documentsUploaded: false,
    affordabilityComplete: false,
  });
  const [documentStatus, setDocumentStatus] = useState<DocumentStatus | null>(null);
  const [dashboardStats, setDashboardStats] = useState<DashboardStats>({
    availableAdvance: 0,
    currentBalance: 0,
    hoursThisMonth: 0,
    earningsThisMonth: 0,
  });

  useEffect(() => {
    loadDashboardData();
  }, []);

  const loadDashboardData = async () => {
    try {
      setIsLoading(true);
      await Promise.all([
        loadDraftApplication(),
        loadPrerequisiteStatus(),
        loadDashboardStats(),
      ]);
    } catch (err) {
      console.error('Failed to load dashboard data:', err);
    } finally {
      setIsLoading(false);
    }
  };

  const loadDraftApplication = async () => {
    try {
      const applications = await apiService.getLoanApplications();
      const draft = applications.find((app: LoanApplication) => app.status === 'Draft');
      setDraftApplication(draft || null);
    } catch (err) {
      console.error('Failed to load applications:', err);
    }
  };

  const loadPrerequisiteStatus = async () => {
    try {
      // Check profile completion - includes NCR-required fields
      const profile = await apiService.getProfile();
      const profileComplete = !!(
        // Basic info
        profile.firstName &&
        profile.lastName &&
        profile.idNumber &&
        profile.phoneNumber &&
        profile.dateOfBirth &&
        // Address (NCR required)
        profile.streetAddress &&
        profile.city &&
        profile.province &&
        profile.postalCode &&
        // Employment (NCR required)
        profile.employerName &&
        profile.employmentType &&
        // Banking (NCR required)
        profile.bankName &&
        profile.accountType &&
        profile.accountNumber &&
        profile.branchCode &&
        // Next of Kin (NCR recommended)
        profile.nextOfKinName &&
        profile.nextOfKinRelationship &&
        profile.nextOfKinPhone
      );

      // Check document status
      const verificationStatus = await apiService.getVerificationStatus();
      setDocumentStatus(verificationStatus);
      const documentsUploaded = verificationStatus.missingDocuments?.length === 0 || false;

      // Check affordability assessment
      const affordability = await apiService.getAffordability();
      const affordabilityComplete = affordability && affordability.totalIncome > 0;

      setPrerequisiteStatus({
        profileComplete,
        documentsUploaded,
        affordabilityComplete,
      });
    } catch (err) {
      console.error('Failed to load prerequisite status:', err);
    }
  };

  const loadDashboardStats = async () => {
    try {
      // Get affordability data for available advance
      const affordability = await apiService.getAffordability();
      const maxLoan = await apiService.getMaxLoanAmount();
      
      // Get active loan applications to calculate current balance
      const applications = await apiService.getLoanApplications();
      const activeLoans = applications.filter(
        (app: any) => app.status === 'Approved' || app.status === 'Disbursed'
      );
      const currentBalance = activeLoans.reduce((sum: number, loan: any) => 
        sum + (loan.outstandingAmount || loan.amount || 0), 0
      );

      // Calculate hours and earnings from income data
      const incomes = await apiService.getIncomes();
      const currentMonth = new Date().getMonth();
      const currentYear = new Date().getFullYear();
      
      let totalHours = 0;
      let totalEarnings = 0;
      
      incomes.forEach((income: any) => {
        const incomeDate = new Date(income.date || income.createdAt);
        if (incomeDate.getMonth() === currentMonth && incomeDate.getFullYear() === currentYear) {
          totalHours += income.hoursWorked || 0;
          totalEarnings += (income.hoursWorked || 0) * (income.hourlyRate || 0);
        }
      });

      setDashboardStats({
        availableAdvance: maxLoan?.maxLoanAmount || affordability?.maxLoanAmount || 0,
        currentBalance,
        hoursThisMonth: totalHours,
        earningsThisMonth: totalEarnings,
      });
    } catch (err) {
      console.error('Failed to load dashboard stats:', err);
    }
  };

  return (
    <div>
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>
        <p className="text-gray-600">Welcome to your Ho Hema Loans dashboard</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
        <div className="card">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <div className="w-8 h-8 bg-primary-100 rounded-lg flex items-center justify-center">
                <span className="text-primary-600 font-semibold">R</span>
              </div>
            </div>
            <div className="ml-4">
              <h3 className="text-sm font-medium text-gray-500">Available Advance</h3>
              <p className="text-2xl font-bold text-gray-900">
                {isLoading ? '...' : `R ${dashboardStats.availableAdvance.toLocaleString('en-ZA', { minimumFractionDigits: 0, maximumFractionDigits: 0 })}`}
              </p>
            </div>
          </div>
        </div>

        <div className="card">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <div className="w-8 h-8 bg-success-100 rounded-lg flex items-center justify-center">
                <span className="text-success-600 font-semibold">R</span>
              </div>
            </div>
            <div className="ml-4">
              <h3 className="text-sm font-medium text-gray-500">Current Balance</h3>
              <p className="text-2xl font-bold text-gray-900">
                {isLoading ? '...' : `R ${dashboardStats.currentBalance.toLocaleString('en-ZA', { minimumFractionDigits: 0, maximumFractionDigits: 0 })}`}
              </p>
            </div>
          </div>
        </div>

        <div className="card">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <div className="w-8 h-8 bg-warning-100 rounded-lg flex items-center justify-center">
                <span className="text-warning-600 font-semibold">H</span>
              </div>
            </div>
            <div className="ml-4">
              <h3 className="text-sm font-medium text-gray-500">Hours This Month</h3>
              <p className="text-2xl font-bold text-gray-900">
                {isLoading ? '...' : dashboardStats.hoursThisMonth}
              </p>
            </div>
          </div>
        </div>

        <div className="card">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <div className="w-8 h-8 bg-secondary-100 rounded-lg flex items-center justify-center">
                <span className="text-secondary-600 font-semibold">E</span>
              </div>
            </div>
            <div className="ml-4">
              <h3 className="text-sm font-medium text-gray-500">Earnings This Month</h3>
              <p className="text-2xl font-bold text-gray-900">
                {isLoading ? '...' : `R ${dashboardStats.earningsThisMonth.toLocaleString('en-ZA', { minimumFractionDigits: 0, maximumFractionDigits: 0 })}`}
              </p>
            </div>
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
        {/* Loan Application Prerequisites */}
        <div className="card">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Before You Apply</h2>
          <p className="text-sm text-gray-600 mb-4">Complete these steps to apply for a loan:</p>
          
          {isLoading ? (
            <div className="flex items-center justify-center py-4">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
            </div>
          ) : (
            <div className="space-y-3">
              {/* Profile Completion */}
              <Link
                to="/profile"
                className={`flex items-center justify-between p-4 rounded-lg border-2 transition-all hover:shadow-md ${
                  prerequisiteStatus.profileComplete
                    ? 'border-success-200 bg-success-50'
                    : 'border-warning-200 bg-warning-50'
                }`}
              >
                <div className="flex items-center">
                  <div className={`w-10 h-10 rounded-full flex items-center justify-center ${
                    prerequisiteStatus.profileComplete
                      ? 'bg-success-100'
                      : 'bg-warning-100'
                  }`}>
                    {prerequisiteStatus.profileComplete ? (
                      <svg className="w-6 h-6 text-success-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                      </svg>
                    ) : (
                      <svg className="w-6 h-6 text-warning-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                      </svg>
                    )}
                  </div>
                  <div className="ml-4">
                    <h3 className="text-sm font-semibold text-gray-900">Complete Your Profile</h3>
                    <p className="text-xs text-gray-600">Personal details & biographical information</p>
                  </div>
                </div>
                <svg className="w-5 h-5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                </svg>
              </Link>

              {/* Document Upload */}
              <Link
                to="/documents"
                className={`flex items-center justify-between p-4 rounded-lg border-2 transition-all hover:shadow-md ${
                  prerequisiteStatus.documentsUploaded
                    ? 'border-success-200 bg-success-50'
                    : 'border-warning-200 bg-warning-50'
                }`}
              >
                <div className="flex items-center">
                  <div className={`w-10 h-10 rounded-full flex items-center justify-center ${
                    prerequisiteStatus.documentsUploaded
                      ? 'bg-success-100'
                      : 'bg-warning-100'
                  }`}>
                    {prerequisiteStatus.documentsUploaded ? (
                      <svg className="w-6 h-6 text-success-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                      </svg>
                    ) : (
                      <svg className="w-6 h-6 text-warning-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                      </svg>
                    )}
                  </div>
                  <div className="ml-4">
                    <h3 className="text-sm font-semibold text-gray-900">Upload Documents</h3>
                    <p className="text-xs text-gray-600">
                      {documentStatus?.missingDocuments && documentStatus.missingDocuments.length > 0
                        ? `Missing: ${documentStatus.missingDocuments.join(', ')}`
                        : 'ID document & proof of address'}
                    </p>
                  </div>
                </div>
                <svg className="w-5 h-5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                </svg>
              </Link>

              {/* Affordability Assessment */}
              <Link
                to="/loans/affordability"
                className={`flex items-center justify-between p-4 rounded-lg border-2 transition-all hover:shadow-md ${
                  prerequisiteStatus.affordabilityComplete
                    ? 'border-success-200 bg-success-50'
                    : 'border-warning-200 bg-warning-50'
                }`}
              >
                <div className="flex items-center">
                  <div className={`w-10 h-10 rounded-full flex items-center justify-center ${
                    prerequisiteStatus.affordabilityComplete
                      ? 'bg-success-100'
                      : 'bg-warning-100'
                  }`}>
                    {prerequisiteStatus.affordabilityComplete ? (
                      <svg className="w-6 h-6 text-success-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                      </svg>
                    ) : (
                      <svg className="w-6 h-6 text-warning-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                      </svg>
                    )}
                  </div>
                  <div className="ml-4">
                    <h3 className="text-sm font-semibold text-gray-900">Complete Affordability Assessment</h3>
                    <p className="text-xs text-gray-600">Income & expenses verification</p>
                  </div>
                </div>
                <svg className="w-5 h-5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                </svg>
              </Link>
            </div>
          )}
        </div>

        <div className="card">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Quick Actions</h2>
          <div className="space-y-3">
            {isLoading ? (
              <div className="flex items-center justify-center py-4">
                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
              </div>
            ) : draftApplication ? (
              <Link
                to={`/loans/new?id=${draftApplication.id}`}
                className="w-full btn-primary block text-center"
              >
                Resume Application
              </Link>
            ) : (
              <Link
                to="/loans/new"
                className="w-full btn-primary block text-center"
              >
                New Loan Application
              </Link>
            )}
          </div>
        </div>

        <div className="card">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Recent Activity</h2>
          <div className="space-y-3">
            <div className="flex items-center justify-between py-2 border-b border-gray-200">
              <span className="text-sm text-gray-600">No recent activity</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Dashboard;