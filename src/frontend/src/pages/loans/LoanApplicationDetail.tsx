import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeftIcon, CheckCircleIcon, ClockIcon, XCircleIcon } from '@heroicons/react/24/outline';
import { apiService } from '../../services/api';
import { useAuthStore } from '../../store/authStore';
import { useToast } from '../../contexts/ToastContext';

interface LoanApplication {
  id: string;
  amount: number;
  termMonths: number;
  purpose: string;
  status: string;
  interestRate: number;
  monthlyPayment: number;
  totalAmount: number;
  applicationDate: string;
  approvalDate?: string;
  channelOrigin: string;
  currentStep: number;
  notes?: string;
  bankName?: string;
  accountNumber?: string;
  accountHolderName?: string;
}

const LoanApplicationDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user } = useAuthStore();
  const { success, error: showError } = useToast();
  const [application, setApplication] = useState<LoanApplication | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showApproveModal, setShowApproveModal] = useState(false);
  const [showRejectModal, setShowRejectModal] = useState(false);
  const [actionLoading, setActionLoading] = useState(false);
  
  // Admin approval form state
  const [interestRate, setInterestRate] = useState(12);
  const [repaymentMonths, setRepaymentMonths] = useState(12);
  const [notes, setNotes] = useState('');
  const [rejectionReason, setRejectionReason] = useState('');
  
  const isAdmin = user?.roles?.includes('Admin');

  useEffect(() => {
    if (id) {
      loadApplication(id);
    }
  }, [id]);

  const loadApplication = async (applicationId: string) => {
    try {
      setIsLoading(true);
      setError(null);
      const data = await apiService.getLoanApplication(applicationId);
      setApplication(data);
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to load application';
      showError(errorMessage);
      setError(errorMessage);
    } finally {
      setIsLoading(false);
    }
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-ZA', {
      style: 'currency',
      currency: 'ZAR',
      minimumFractionDigits: 0,
      maximumFractionDigits: 0,
    }).format(amount);
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-ZA', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  const formatPercentage = (rate: number) => {
    return `${(rate * 100).toFixed(2)}%`;
  };

  const handleApprove = async () => {
    if (!id) return;
    
    try {
      setActionLoading(true);
      await apiService.request(`/admin/loans/${id}/approve`, {
        method: 'POST',
        body: JSON.stringify({ 
          interestRate: interestRate / 100, // Convert percentage to decimal
          repaymentMonths, 
          notes 
        }),
      });
      setShowApproveModal(false);
      await loadApplication(id);
      success('Loan approved successfully!');
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to approve loan';
      showError(errorMessage);
    } finally {
      setActionLoading(false);
    }
  };

  const handleReject = async () => {
    if (!id) return;
    
    try {
      setActionLoading(true);
      await apiService.request(`/admin/loans/${id}/reject`, {
        method: 'POST',
        body: JSON.stringify({ reason: rejectionReason }),
      });
      setShowRejectModal(false);
      await loadApplication(id);
      success('Loan rejected successfully!');
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to reject loan';
      showError(errorMessage);
    } finally {
      setActionLoading(false);
    }
  };

  const getStatusBadge = (status: string) => {
    const statusConfig: Record<string, { color: string; icon: any; text: string }> = {
      Draft: { color: 'bg-gray-100 text-gray-800', icon: ClockIcon, text: 'Draft' },
      Pending: { color: 'bg-yellow-100 text-yellow-800', icon: ClockIcon, text: 'Pending Review' },
      UnderReview: { color: 'bg-blue-100 text-blue-800', icon: ClockIcon, text: 'Under Review' },
      Approved: { color: 'bg-green-100 text-green-800', icon: CheckCircleIcon, text: 'Approved' },
      Rejected: { color: 'bg-red-100 text-red-800', icon: XCircleIcon, text: 'Rejected' },
      Disbursed: { color: 'bg-purple-100 text-purple-800', icon: CheckCircleIcon, text: 'Disbursed' },
      Closed: { color: 'bg-gray-100 text-gray-800', icon: CheckCircleIcon, text: 'Closed' },
    };

    const config = statusConfig[status] || statusConfig.Pending;
    const Icon = config.icon;

    return (
      <span className={`inline-flex items-center px-3 py-1 rounded-full text-sm font-medium ${config.color}`}>
        <Icon className="w-5 h-5 mr-2" />
        {config.text}
      </span>
    );
  };

  if (isLoading) {
    return (
      <div>
        <div className="mb-8">
          <button
            onClick={() => navigate('/loans')}
            className="inline-flex items-center text-sm text-gray-600 hover:text-gray-900 mb-4"
          >
            <ArrowLeftIcon className="w-4 h-4 mr-1" />
            Back to Applications
          </button>
          <h1 className="text-2xl font-bold text-gray-900">Loan Application Details</h1>
        </div>
        <div className="card">
          <div className="flex items-center justify-center py-12">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
          </div>
        </div>
      </div>
    );
  }

  if (error || !application) {
    return (
      <div>
        <div className="mb-8">
          <button
            onClick={() => navigate('/loans')}
            className="inline-flex items-center text-sm text-gray-600 hover:text-gray-900 mb-4"
          >
            <ArrowLeftIcon className="w-4 h-4 mr-1" />
            Back to Applications
          </button>
          <h1 className="text-2xl font-bold text-gray-900">Loan Application Details</h1>
        </div>
        <div className="card bg-red-50 border border-red-200">
          <p className="text-sm text-red-700">{error || 'Application not found'}</p>
        </div>
      </div>
    );
  }

  return (
    <div>
      <div className="mb-8">
        <button
          onClick={() => navigate('/loans')}
          className="inline-flex items-center text-sm text-gray-600 hover:text-gray-900 mb-4"
        >
          <ArrowLeftIcon className="w-4 h-4 mr-1" />
          Back to Applications
        </button>
        <div className="flex items-center justify-between">
          <h1 className="text-2xl font-bold text-gray-900">Loan Application Details</h1>
          <div className="flex items-center space-x-4">
            {getStatusBadge(application.status)}
            {isAdmin && application.status === 'Pending' && (
              <div className="flex space-x-2">
                <button
                  onClick={() => setShowApproveModal(true)}
                  className="bg-green-600 hover:bg-green-700 text-white px-4 py-2 rounded-lg font-medium text-sm"
                >
                  ✓ Approve
                </button>
                <button
                  onClick={() => setShowRejectModal(true)}
                  className="bg-red-600 hover:bg-red-700 text-white px-4 py-2 rounded-lg font-medium text-sm"
                >
                  ✕ Reject
                </button>
              </div>
            )}
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Main Details */}
        <div className="lg:col-span-2 space-y-6">
          {/* Application Summary */}
          <div className="card">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">Application Summary</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div>
                <label className="text-sm font-medium text-gray-500">Loan Amount</label>
                <p className="mt-1 text-2xl font-bold text-gray-900">{formatCurrency(application.amount)}</p>
              </div>
              <div>
                <label className="text-sm font-medium text-gray-500">Loan Term</label>
                <p className="mt-1 text-2xl font-bold text-gray-900">{application.termMonths} months</p>
              </div>
              <div>
                <label className="text-sm font-medium text-gray-500">Interest Rate</label>
                <p className="mt-1 text-xl font-semibold text-gray-900">{formatPercentage(application.interestRate)}</p>
              </div>
              <div>
                <label className="text-sm font-medium text-gray-500">Monthly Payment</label>
                <p className="mt-1 text-xl font-semibold text-gray-900">{formatCurrency(application.monthlyPayment)}</p>
              </div>
              <div>
                <label className="text-sm font-medium text-gray-500">Total Amount Payable</label>
                <p className="mt-1 text-xl font-semibold text-gray-900">{formatCurrency(application.totalAmount)}</p>
              </div>
              <div>
                <label className="text-sm font-medium text-gray-500">Purpose</label>
                <p className="mt-1 text-lg text-gray-900">{application.purpose || 'Not specified'}</p>
              </div>
            </div>
          </div>

          {/* Bank Details */}
          {(application.bankName || application.accountNumber) && (
            <div className="card">
              <h2 className="text-lg font-semibold text-gray-900 mb-4">Bank Details</h2>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                {application.bankName && (
                  <div>
                    <label className="text-sm font-medium text-gray-500">Bank Name</label>
                    <p className="mt-1 text-gray-900">{application.bankName}</p>
                  </div>
                )}
                {application.accountNumber && (
                  <div>
                    <label className="text-sm font-medium text-gray-500">Account Number</label>
                    <p className="mt-1 text-gray-900">{application.accountNumber}</p>
                  </div>
                )}
                {application.accountHolderName && (
                  <div>
                    <label className="text-sm font-medium text-gray-500">Account Holder</label>
                    <p className="mt-1 text-gray-900">{application.accountHolderName}</p>
                  </div>
                )}
              </div>
            </div>
          )}

          {/* Notes */}
          {application.notes && (
            <div className="card">
              <h2 className="text-lg font-semibold text-gray-900 mb-4">Notes</h2>
              <p className="text-gray-700 whitespace-pre-wrap">{application.notes}</p>
            </div>
          )}
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Status Timeline */}
          <div className="card">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">Application Timeline</h2>
            <div className="space-y-4">
              <div className="flex items-start">
                <div className="flex-shrink-0">
                  <div className="w-8 h-8 bg-blue-100 rounded-full flex items-center justify-center">
                    <CheckCircleIcon className="w-5 h-5 text-blue-600" />
                  </div>
                </div>
                <div className="ml-3">
                  <p className="text-sm font-medium text-gray-900">Application Submitted</p>
                  <p className="text-sm text-gray-500">{formatDate(application.applicationDate)}</p>
                </div>
              </div>

              {application.approvalDate && (
                <div className="flex items-start">
                  <div className="flex-shrink-0">
                    <div className="w-8 h-8 bg-green-100 rounded-full flex items-center justify-center">
                      <CheckCircleIcon className="w-5 h-5 text-green-600" />
                    </div>
                  </div>
                  <div className="ml-3">
                    <p className="text-sm font-medium text-gray-900">Application Approved</p>
                    <p className="text-sm text-gray-500">{formatDate(application.approvalDate)}</p>
                  </div>
                </div>
              )}
            </div>
          </div>

          {/* Application Info */}
          <div className="card">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">Application Info</h2>
            <div className="space-y-3">
              <div>
                <label className="text-sm font-medium text-gray-500">Application ID</label>
                <p className="mt-1 text-sm text-gray-900 font-mono">{application.id}</p>
              </div>
              <div>
                <label className="text-sm font-medium text-gray-500">Channel</label>
                <p className="mt-1">
                  <span className={`px-2 py-1 text-xs rounded ${
                    application.channelOrigin === 'WhatsApp' 
                      ? 'bg-green-100 text-green-800' 
                      : 'bg-blue-100 text-blue-800'
                  }`}>
                    {application.channelOrigin}
                  </span>
                </p>
              </div>
              <div>
                <label className="text-sm font-medium text-gray-500">Current Step</label>
                <p className="mt-1 text-sm text-gray-900">Step {application.currentStep + 1}</p>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Approve Modal */}
      {showApproveModal && (
        <div className="fixed inset-0 z-50 bg-black bg-opacity-50 flex items-center justify-center p-4">
          <div className="bg-white rounded-lg max-w-md w-full">
            <div className="bg-gradient-to-r from-green-500 to-green-600 text-white px-6 py-4 flex items-center justify-between rounded-t-lg">
              <h2 className="text-xl font-bold">Approve Loan Application</h2>
              <button onClick={() => setShowApproveModal(false)} className="text-2xl hover:bg-green-500 p-1 rounded">×</button>
            </div>

            <div className="p-6 space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Interest Rate (% per annum)
                </label>
                <input
                  type="number"
                  step="0.1"
                  value={interestRate}
                  onChange={(e) => setInterestRate(parseFloat(e.target.value))}
                  className="w-full border border-gray-300 rounded-lg px-4 py-2"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Repayment Period (months)
                </label>
                <select
                  value={repaymentMonths}
                  onChange={(e) => setRepaymentMonths(parseInt(e.target.value))}
                  className="w-full border border-gray-300 rounded-lg px-4 py-2"
                >
                  <option value="6">6 months</option>
                  <option value="12">12 months</option>
                  <option value="24">24 months</option>
                  <option value="36">36 months</option>
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Notes (optional)
                </label>
                <textarea
                  value={notes}
                  onChange={(e) => setNotes(e.target.value)}
                  className="w-full border border-gray-300 rounded-lg px-4 py-2 resize-none"
                  rows={3}
                  placeholder="Add any notes about this approval..."
                />
              </div>

              <div className="flex space-x-3 pt-4">
                <button
                  onClick={handleApprove}
                  disabled={actionLoading}
                  className="flex-1 bg-green-600 hover:bg-green-700 text-white px-4 py-3 rounded-lg font-medium disabled:opacity-50"
                >
                  {actionLoading ? 'Approving...' : 'Confirm Approval'}
                </button>
                <button
                  onClick={() => setShowApproveModal(false)}
                  disabled={actionLoading}
                  className="flex-1 bg-gray-300 hover:bg-gray-400 text-gray-800 px-4 py-3 rounded-lg font-medium"
                >
                  Cancel
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Reject Modal */}
      {showRejectModal && (
        <div className="fixed inset-0 z-50 bg-black bg-opacity-50 flex items-center justify-center p-4">
          <div className="bg-white rounded-lg max-w-md w-full">
            <div className="bg-gradient-to-r from-red-500 to-red-600 text-white px-6 py-4 flex items-center justify-between rounded-t-lg">
              <h2 className="text-xl font-bold">Reject Loan Application</h2>
              <button onClick={() => setShowRejectModal(false)} className="text-2xl hover:bg-red-500 p-1 rounded">×</button>
            </div>

            <div className="p-6 space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Reason for Rejection <span className="text-red-500">*</span>
                </label>
                <textarea
                  value={rejectionReason}
                  onChange={(e) => setRejectionReason(e.target.value)}
                  className="w-full border border-gray-300 rounded-lg px-4 py-2 resize-none"
                  rows={4}
                  placeholder="Explain why this application is being rejected..."
                  required
                />
              </div>

              <div className="flex space-x-3 pt-4">
                <button
                  onClick={handleReject}
                  disabled={actionLoading || !rejectionReason.trim()}
                  className="flex-1 bg-red-600 hover:bg-red-700 text-white px-4 py-3 rounded-lg font-medium disabled:opacity-50"
                >
                  {actionLoading ? 'Rejecting...' : 'Confirm Rejection'}
                </button>
                <button
                  onClick={() => setShowRejectModal(false)}
                  disabled={actionLoading}
                  className="flex-1 bg-gray-300 hover:bg-gray-400 text-gray-800 px-4 py-3 rounded-lg font-medium"
                >
                  Cancel
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default LoanApplicationDetail;
