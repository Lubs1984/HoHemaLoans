import React, { useEffect, useState } from 'react';
import { apiService } from '../../services/api';
import { CheckCircleIcon, XCircleIcon, ClockIcon, EyeIcon } from '@heroicons/react/24/outline';

interface LoanApplication {
  id: string;
  amount: number;
  status: string;
  interestRate: number;
  repaymentMonths: number;
  applicationDate: string;
  processedAt?: string;
  notes?: string;
  user: {
    id: string;
    firstName: string;
    lastName: string;
    email: string;
    phoneNumber: string;
    monthlyIncome: number;
  };
}

interface LoanResponse {
  totalCount: number;
  pageCount: number;
  currentPage: number;
  pageSize: number;
  data: LoanApplication[];
}

const AdminLoans: React.FC = () => {
  const [loans, setLoans] = useState<LoanApplication[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [status, setStatus] = useState<string>('');
  const [search, setSearch] = useState<string>('');
  const [selectedLoan, setSelectedLoan] = useState<LoanApplication | null>(null);

  const fetchLoans = async (page: number = 1, statusFilter: string = '', searchTerm: string = '') => {
    try {
      setLoading(true);
      const params = new URLSearchParams({
        page: page.toString(),
        pageSize: '20',
      });

      if (statusFilter) params.append('status', statusFilter);
      if (searchTerm) params.append('search', searchTerm);

      const response = await apiService.request<LoanResponse>(`/admin/loans?${params}`);
      setLoans(response.data);
      setTotalPages(response.pageCount);
      setCurrentPage(response.currentPage);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load loans');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchLoans(1, status, search);
  }, [status, search]);

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Draft':
        return 'bg-gray-100 text-gray-800';
      case 'Approved':
        return 'bg-green-100 text-green-800';
      case 'Rejected':
        return 'bg-red-100 text-red-800';
      case 'Pending':
        return 'bg-yellow-100 text-yellow-800';
      case 'Disbursed':
        return 'bg-blue-100 text-blue-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'Approved':
        return <CheckCircleIcon className="h-5 w-5" />;
      case 'Rejected':
        return <XCircleIcon className="h-5 w-5" />;
      case 'Pending':
        return <ClockIcon className="h-5 w-5" />;
      default:
        return null;
    }
  };

  if (error) {
    return (
      <div className="bg-red-50 border border-red-200 rounded-lg p-4">
        <p className="text-red-700">{error}</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Loan Applications</h1>
        <p className="text-gray-600 mt-2">Manage and process loan applications</p>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg shadow p-4 space-y-4">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Status
            </label>
            <select
              value={status}
              onChange={(e) => { setStatus(e.target.value); setCurrentPage(1); }}
              className="w-full border border-gray-300 rounded-lg px-3 py-2"
            >
              <option value="">All Status</option>
              <option value="Draft">Draft</option>
              <option value="Pending">Pending</option>
              <option value="Approved">Approved</option>
              <option value="Rejected">Rejected</option>
              <option value="Disbursed">Disbursed</option>
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Search by Name or Email
            </label>
            <input
              type="text"
              placeholder="Search..."
              value={search}
              onChange={(e) => { setSearch(e.target.value); setCurrentPage(1); }}
              className="w-full border border-gray-300 rounded-lg px-3 py-2"
            />
          </div>
        </div>
      </div>

      {/* Loans Table */}
      <div className="bg-white rounded-lg shadow overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">
                  Applicant
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">
                  Amount
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">
                  Status
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">
                  Applied Date
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">
                  Income
                </th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-700 uppercase tracking-wider">
                  Action
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200">
              {loading ? (
                <tr>
                  <td colSpan={6} className="px-6 py-4 text-center">
                    <div className="flex justify-center">
                      <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
                    </div>
                  </td>
                </tr>
              ) : loans.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-6 py-4 text-center text-gray-500">
                    No loan applications found
                  </td>
                </tr>
              ) : (
                loans.map((loan) => (
                  <tr key={loan.id} className="hover:bg-gray-50 transition">
                    <td className="px-6 py-4">
                      <div>
                        <p className="font-medium text-gray-900">
                          {loan.user.firstName} {loan.user.lastName}
                        </p>
                        <p className="text-sm text-gray-600">{loan.user.email}</p>
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <p className="font-semibold text-gray-900">R {loan.amount.toLocaleString()}</p>
                    </td>
                    <td className="px-6 py-4">
                      <span className={`inline-flex items-center space-x-1 px-3 py-1 rounded-full text-sm font-medium ${getStatusColor(loan.status)}`}>
                        {getStatusIcon(loan.status)}
                        <span>{loan.status}</span>
                      </span>
                    </td>
                    <td className="px-6 py-4 text-sm text-gray-600">
                      {new Date(loan.applicationDate).toLocaleDateString()}
                    </td>
                    <td className="px-6 py-4">
                      <p className="font-medium text-gray-900">R {loan.user.monthlyIncome.toLocaleString()}</p>
                    </td>
                    <td className="px-6 py-4 text-right">
                      <button
                        onClick={() => setSelectedLoan(loan)}
                        className="inline-flex items-center space-x-1 text-blue-600 hover:text-blue-800 font-medium"
                      >
                        <EyeIcon className="h-4 w-4" />
                        <span>View</span>
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        {totalPages > 1 && (
          <div className="bg-gray-50 px-6 py-4 flex items-center justify-between">
            <p className="text-sm text-gray-600">
              Page {currentPage} of {totalPages}
            </p>
            <div className="flex space-x-2">
              <button
                onClick={() => fetchLoans(Math.max(1, currentPage - 1), status, search)}
                disabled={currentPage === 1}
                className="px-4 py-2 border border-gray-300 rounded-lg text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50"
              >
                Previous
              </button>
              <button
                onClick={() => fetchLoans(Math.min(totalPages, currentPage + 1), status, search)}
                disabled={currentPage === totalPages}
                className="px-4 py-2 border border-gray-300 rounded-lg text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50"
              >
                Next
              </button>
            </div>
          </div>
        )}
      </div>

      {/* Detail Modal */}
      {selectedLoan && (
        <LoanDetailModal
          loan={selectedLoan}
          onClose={() => setSelectedLoan(null)}
          onUpdate={() => {
            setSelectedLoan(null);
            fetchLoans(currentPage, status, search);
          }}
        />
      )}
    </div>
  );
};

interface LoanDetailModalProps {
  loan: LoanApplication;
  onClose: () => void;
  onUpdate: () => void;
}

const LoanDetailModal: React.FC<LoanDetailModalProps> = ({ loan, onClose, onUpdate }) => {
  const [action, setAction] = useState<'approve' | 'reject' | null>(null);
  const [interestRate, setInterestRate] = useState(loan.interestRate);
  const [repaymentMonths, setRepaymentMonths] = useState(loan.repaymentMonths);
  const [notes, setNotes] = useState(loan.notes || '');
  const [reason, setReason] = useState('');
  const [loading, setLoading] = useState(false);

  const handleApprove = async () => {
    try {
      setLoading(true);
      await apiService.request(`/admin/loans/${loan.id}/approve`, {
        method: 'POST',
        body: JSON.stringify({ interestRate, repaymentMonths, notes }),
      });
      onUpdate();
    } catch (err) {
      alert(err instanceof Error ? err.message : 'Failed to approve loan');
    } finally {
      setLoading(false);
    }
  };

  const handleReject = async () => {
    try {
      setLoading(true);
      await apiService.request(`/admin/loans/${loan.id}/reject`, {
        method: 'POST',
        body: JSON.stringify({ reason }),
      });
      onUpdate();
    } catch (err) {
      alert(err instanceof Error ? err.message : 'Failed to reject loan');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 bg-black bg-opacity-50 flex items-center justify-center p-4">
      <div className="bg-white rounded-lg max-w-5xl w-full max-h-[90vh] overflow-y-auto">
        <div className="sticky top-0 bg-gradient-to-r from-blue-500 to-blue-600 text-white px-6 py-4 flex items-center justify-between z-10">
          <h2 className="text-2xl font-bold">Loan Application Details</h2>
          <button onClick={onClose} className="text-3xl hover:bg-blue-500 p-2 rounded transition">×</button>
        </div>

        <div className="p-8 space-y-6">{/* Applicant Info */}
          <div>
            <h3 className="text-lg font-semibold text-gray-900 mb-3">Applicant Information</h3>
            <div className="grid grid-cols-2 gap-4 bg-gray-50 p-5 rounded-lg border border-gray-200">
              <div>
                <p className="text-sm font-medium text-gray-600">Name</p>
                <p className="text-lg font-semibold text-gray-900 mt-1">{loan.user.firstName} {loan.user.lastName}</p>
              </div>
              <div>
                <p className="text-sm font-medium text-gray-600">Email</p>
                <p className="text-lg font-medium text-gray-900 mt-1">{loan.user.email}</p>
              </div>
              <div>
                <p className="text-sm font-medium text-gray-600">Phone</p>
                <p className="text-lg font-medium text-gray-900 mt-1">{loan.user.phoneNumber}</p>
              </div>
              <div>
                <p className="text-sm font-medium text-gray-600">Monthly Income</p>
                <p className="text-lg font-semibold text-green-600 mt-1">R {loan.user.monthlyIncome.toLocaleString()}</p>
              </div>
            </div>
          </div>

          {/* Loan Info */}
          <div>
            <h3 className="text-lg font-semibold text-gray-900 mb-3">Loan Details</h3>
            <div className="grid grid-cols-3 gap-4 bg-blue-50 p-5 rounded-lg border border-blue-200">
              <div>
                <p className="text-sm font-medium text-gray-600">Requested Amount</p>
                <p className="text-2xl font-bold text-blue-600 mt-1">R {loan.amount.toLocaleString()}</p>
              </div>
              <div>
                <p className="text-sm font-medium text-gray-600">Current Status</p>
                <p className="text-lg font-semibold text-gray-900 mt-1">
                  <span className={`px-3 py-1 rounded-full text-sm ${
                    loan.status === 'Pending' ? 'bg-yellow-100 text-yellow-800' :
                    loan.status === 'Approved' ? 'bg-green-100 text-green-800' :
                    loan.status === 'Rejected' ? 'bg-red-100 text-red-800' :
                    'bg-gray-100 text-gray-800'
                  }`}>
                    {loan.status}
                  </span>
                </p>
              </div>
              <div>
                <p className="text-sm font-medium text-gray-600">Applied Date</p>
                <p className="text-lg font-medium text-gray-900 mt-1">{new Date(loan.applicationDate).toLocaleDateString('en-ZA', { 
                  year: 'numeric', 
                  month: 'short', 
                  day: 'numeric',
                  hour: '2-digit',
                  minute: '2-digit'
                })}</p>
              </div>
            </div>
          </div>

          {/* Actions */}
          {loan.status === 'Pending' && (
            <div className="space-y-4 border-t-2 border-gray-200 pt-6">
              {!action ? (
                <div className="grid grid-cols-2 gap-6">
                  <button
                    onClick={() => setAction('approve')}
                    className="bg-green-600 hover:bg-green-700 text-white px-6 py-4 rounded-lg font-semibold text-lg shadow-md hover:shadow-lg transition"
                  >
                    ✓ Approve Loan
                  </button>
                  <button
                    onClick={() => setAction('reject')}
                    className="bg-red-600 hover:bg-red-700 text-white px-6 py-4 rounded-lg font-semibold text-lg shadow-md hover:shadow-lg transition"
                  >
                    ✕ Reject Loan
                  </button>
                </div>
              ) : action === 'approve' ? (
                <div className="space-y-5 bg-green-50 p-6 rounded-lg border border-green-200">
                  <h4 className="text-lg font-semibold text-gray-900">Approve Loan Application</h4>
                  <div>
                    <label className="block text-base font-medium text-gray-700 mb-2">
                      Interest Rate (%)
                    </label>
                    <input
                      type="number"
                      step="0.1"
                      value={interestRate}
                      onChange={(e) => setInterestRate(parseFloat(e.target.value))}
                      className="w-full border-2 border-gray-300 rounded-lg px-4 py-3 text-lg focus:border-green-500 focus:ring-2 focus:ring-green-200"
                    />
                  </div>
                  <div>
                    <label className="block text-base font-medium text-gray-700 mb-2">
                      Repayment Months
                    </label>
                    <input
                      type="number"
                      value={repaymentMonths}
                      onChange={(e) => setRepaymentMonths(parseInt(e.target.value))}
                      className="w-full border-2 border-gray-300 rounded-lg px-4 py-3 text-lg focus:border-green-500 focus:ring-2 focus:ring-green-200"
                    />
                  </div>
                  <div>
                    <label className="block text-base font-medium text-gray-700 mb-2">
                      Notes (Optional)
                    </label>
                    <textarea
                      value={notes}
                      onChange={(e) => setNotes(e.target.value)}
                      className="w-full border-2 border-gray-300 rounded-lg px-4 py-3 text-base resize-none focus:border-green-500 focus:ring-2 focus:ring-green-200"
                      rows={3}
                      placeholder="Add any additional notes or conditions..."
                    />
                  </div>
                  <div className="flex space-x-4 pt-2">
                    <button
                      onClick={handleApprove}
                      disabled={loading}
                      className="flex-1 bg-green-600 hover:bg-green-700 text-white px-6 py-3 rounded-lg font-semibold text-lg disabled:opacity-50 shadow-md hover:shadow-lg transition"
                    >
                      {loading ? 'Approving...' : 'Confirm Approval'}
                    </button>
                    <button
                      onClick={() => setAction(null)}
                      className="flex-1 bg-gray-300 hover:bg-gray-400 text-gray-800 px-6 py-3 rounded-lg font-semibold text-lg shadow-md hover:shadow-lg transition"
                    >
                      Cancel
                    </button>
                  </div>
                </div>
              ) : (
                <div className="space-y-5 bg-red-50 p-6 rounded-lg border border-red-200">
                  <h4 className="text-lg font-semibold text-gray-900">Reject Loan Application</h4>
                  <div>
                    <label className="block text-base font-medium text-gray-700 mb-2">
                      Reason for Rejection
                    </label>
                    <textarea
                      value={reason}
                      onChange={(e) => setReason(e.target.value)}
                      className="w-full border-2 border-gray-300 rounded-lg px-4 py-3 text-base resize-none focus:border-red-500 focus:ring-2 focus:ring-red-200"
                      rows={4}
                      placeholder="Explain why this application is being rejected..."
                    />
                  </div>
                  <div className="flex space-x-4 pt-2">
                    <button
                      onClick={handleReject}
                      disabled={loading}
                      className="flex-1 bg-red-600 hover:bg-red-700 text-white px-6 py-3 rounded-lg font-semibold text-lg disabled:opacity-50 shadow-md hover:shadow-lg transition"
                    >
                      {loading ? 'Rejecting...' : 'Confirm Rejection'}
                    </button>
                    <button
                      onClick={() => setAction(null)}
                      className="flex-1 bg-gray-300 hover:bg-gray-400 text-gray-800 px-6 py-3 rounded-lg font-semibold text-lg shadow-md hover:shadow-lg transition"
                    >
                      Cancel
                    </button>
                  </div>
                </div>
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default AdminLoans;
