import React, { useEffect, useState } from 'react';
import { apiService } from '../../services/api';
import { useToast } from '../../contexts/ToastContext';
import { BanknotesIcon, CheckCircleIcon, ClockIcon } from '@heroicons/react/24/outline';

interface LoanApplication {
  id: string;
  amount: number;
  interestRate: number;
  termMonths: number;
  monthlyPayment: number;
  totalAmount: number;
  status: string;
  applicationDate: string;
  approvalDate?: string;
  bankName?: string;
  accountNumber?: string;
  accountHolderName?: string;
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

interface PayoutResponse {
  totalCount: number;
  pageCount: number;
  currentPage: number;
  pageSize: number;
  data: LoanApplication[];
}

const AdminPayouts: React.FC = () => {
  const [loans, setLoans] = useState<LoanApplication[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [selectedLoan, setSelectedLoan] = useState<LoanApplication | null>(null);
  const [totalAmount, setTotalAmount] = useState(0);

  const fetchPayouts = async (page: number = 1) => {
    try {
      setLoading(true);
      const params = new URLSearchParams({
        page: page.toString(),
        pageSize: '20',
      });

      const response = await apiService.request<PayoutResponse>(`/admin/loans/ready-for-payout?${params}`);
      setLoans(response.data);
      setTotalPages(response.pageCount);
      setCurrentPage(response.currentPage);
      
      // Calculate total amount to be disbursed
      const total = response.data.reduce((sum, loan) => sum + loan.amount, 0);
      setTotalAmount(total);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load payouts');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchPayouts(1);
  }, []);

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
        <h1 className="text-3xl font-bold text-gray-900">Loan Payouts</h1>
        <p className="text-gray-600 mt-2">Manage and process approved loan disbursements</p>
      </div>

      {/* Summary Card */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center">
            <div className="p-3 rounded-full bg-blue-100 text-blue-600">
              <BanknotesIcon className="h-8 w-8" />
            </div>
            <div className="ml-4">
              <p className="text-sm text-gray-600">Ready for Payout</p>
              <p className="text-2xl font-bold text-gray-900">{loans.length}</p>
            </div>
          </div>
        </div>

        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center">
            <div className="p-3 rounded-full bg-green-100 text-green-600">
              <BanknotesIcon className="h-8 w-8" />
            </div>
            <div className="ml-4">
              <p className="text-sm text-gray-600">Total Amount</p>
              <p className="text-2xl font-bold text-gray-900">R {totalAmount.toLocaleString()}</p>
            </div>
          </div>
        </div>

        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center">
            <div className="p-3 rounded-full bg-purple-100 text-purple-600">
              <ClockIcon className="h-8 w-8" />
            </div>
            <div className="ml-4">
              <p className="text-sm text-gray-600">Status</p>
              <p className="text-lg font-semibold text-gray-900">Awaiting Disbursement</p>
            </div>
          </div>
        </div>
      </div>

      {/* Payouts Table */}
      <div className="bg-white rounded-lg shadow overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">
                  Borrower
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">
                  Loan Amount
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">
                  Bank Details
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">
                  Approved Date
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">
                  Term
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
                  <td colSpan={6} className="px-6 py-8 text-center">
                    <CheckCircleIcon className="h-12 w-12 text-gray-400 mx-auto mb-2" />
                    <p className="text-gray-500 font-medium">No loans ready for payout</p>
                    <p className="text-sm text-gray-400 mt-1">All approved loans have been disbursed</p>
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
                        <p className="text-sm text-gray-500">{loan.user.phoneNumber}</p>
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <p className="text-lg font-bold text-green-600">R {loan.amount.toLocaleString()}</p>
                      <p className="text-xs text-gray-500">
                        Total: R {loan.totalAmount.toLocaleString()}
                      </p>
                    </td>
                    <td className="px-6 py-4">
                      {loan.bankName && loan.accountNumber ? (
                        <div className="text-sm">
                          <p className="font-medium text-gray-900">{loan.bankName}</p>
                          <p className="text-gray-600">{loan.accountNumber}</p>
                          {loan.accountHolderName && (
                            <p className="text-gray-500">{loan.accountHolderName}</p>
                          )}
                        </div>
                      ) : (
                        <span className="text-sm text-red-600 font-medium">Missing bank details</span>
                      )}
                    </td>
                    <td className="px-6 py-4 text-sm text-gray-600">
                      {loan.approvalDate ? new Date(loan.approvalDate).toLocaleDateString() : 'N/A'}
                    </td>
                    <td className="px-6 py-4">
                      <p className="text-sm font-medium text-gray-900">{loan.termMonths} months</p>
                      <p className="text-xs text-gray-500">
                        {loan.interestRate}% interest
                      </p>
                    </td>
                    <td className="px-6 py-4 text-right">
                      <button
                        onClick={() => setSelectedLoan(loan)}
                        disabled={!loan.bankName || !loan.accountNumber}
                        className="inline-flex items-center px-4 py-2 bg-blue-600 hover:bg-blue-700 disabled:bg-gray-300 disabled:cursor-not-allowed text-white rounded-lg font-medium transition"
                      >
                        <BanknotesIcon className="h-4 w-4 mr-2" />
                        Disburse
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
                onClick={() => fetchPayouts(Math.max(1, currentPage - 1))}
                disabled={currentPage === 1}
                className="px-4 py-2 border border-gray-300 rounded-lg text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50"
              >
                Previous
              </button>
              <button
                onClick={() => fetchPayouts(Math.min(totalPages, currentPage + 1))}
                disabled={currentPage === totalPages}
                className="px-4 py-2 border border-gray-300 rounded-lg text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50"
              >
                Next
              </button>
            </div>
          </div>
        )}
      </div>

      {/* Disbursement Modal */}
      {selectedLoan && (
        <DisbursementModal
          loan={selectedLoan}
          onClose={() => setSelectedLoan(null)}
          onSuccess={() => {
            setSelectedLoan(null);
            fetchPayouts(currentPage);
          }}
        />
      )}
    </div>
  );
};

interface DisbursementModalProps {
  loan: LoanApplication;
  onClose: () => void;
  onSuccess: () => void;
}

const DisbursementModal: React.FC<DisbursementModalProps> = ({ loan, onClose, onSuccess }) => {
  const { success, error: showError, warning } = useToast();
  const [notes, setNotes] = useState('');
  const [loading, setLoading] = useState(false);
  const [confirmed, setConfirmed] = useState(false);

  const handleDisburse = async () => {
    if (!confirmed) {
      warning('Please confirm the bank details before disbursing');
      return;
    }

    try {
      setLoading(true);
      await apiService.request(`/admin/loans/${loan.id}/disburse`, {
        method: 'POST',
        body: JSON.stringify({ notes }),
      });
      success('Loan disbursed successfully!');
      onSuccess();
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to disburse loan';
      showError(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 bg-black bg-opacity-50 flex items-center justify-center p-4">
      <div className="bg-white rounded-lg max-w-2xl w-full max-h-[90vh] overflow-y-auto">
        <div className="sticky top-0 bg-gradient-to-r from-green-500 to-green-600 text-white px-6 py-4 flex items-center justify-between">
          <h2 className="text-xl font-bold">Confirm Loan Disbursement</h2>
          <button onClick={onClose} className="text-2xl hover:bg-green-500 p-1 rounded">×</button>
        </div>

        <div className="p-6 space-y-6">
          {/* Warning Banner */}
          <div className="bg-yellow-50 border-l-4 border-yellow-400 p-4">
            <div className="flex">
              <div className="flex-shrink-0">
                <svg className="h-5 w-5 text-yellow-400" viewBox="0 0 20 20" fill="currentColor">
                  <path fillRule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
                </svg>
              </div>
              <div className="ml-3">
                <p className="text-sm text-yellow-700 font-medium">
                  Please verify all details before confirming disbursement. This action cannot be undone.
                </p>
              </div>
            </div>
          </div>

          {/* Borrower Info */}
          <div>
            <h3 className="font-semibold text-gray-900 mb-3 text-lg">Borrower Information</h3>
            <div className="grid grid-cols-2 gap-4 bg-gray-50 p-4 rounded-lg">
              <div>
                <p className="text-sm text-gray-600">Name</p>
                <p className="font-medium text-gray-900">{loan.user.firstName} {loan.user.lastName}</p>
              </div>
              <div>
                <p className="text-sm text-gray-600">Email</p>
                <p className="font-medium text-gray-900">{loan.user.email}</p>
              </div>
              <div>
                <p className="text-sm text-gray-600">Phone</p>
                <p className="font-medium text-gray-900">{loan.user.phoneNumber}</p>
              </div>
              <div>
                <p className="text-sm text-gray-600">Monthly Income</p>
                <p className="font-medium text-gray-900">R {loan.user.monthlyIncome.toLocaleString()}</p>
              </div>
            </div>
          </div>

          {/* Loan Details */}
          <div>
            <h3 className="font-semibold text-gray-900 mb-3 text-lg">Loan Details</h3>
            <div className="grid grid-cols-2 gap-4 bg-blue-50 p-4 rounded-lg border border-blue-200">
              <div>
                <p className="text-sm text-blue-700">Disbursement Amount</p>
                <p className="text-2xl font-bold text-blue-900">R {loan.amount.toLocaleString()}</p>
              </div>
              <div>
                <p className="text-sm text-blue-700">Total Repayment</p>
                <p className="text-xl font-bold text-blue-900">R {loan.totalAmount.toLocaleString()}</p>
              </div>
              <div>
                <p className="text-sm text-blue-700">Interest Rate</p>
                <p className="font-medium text-blue-900">{loan.interestRate}% per annum</p>
              </div>
              <div>
                <p className="text-sm text-blue-700">Term</p>
                <p className="font-medium text-blue-900">{loan.termMonths} months</p>
              </div>
              <div>
                <p className="text-sm text-blue-700">Monthly Payment</p>
                <p className="font-medium text-blue-900">R {loan.monthlyPayment.toLocaleString()}</p>
              </div>
              <div>
                <p className="text-sm text-blue-700">Approval Date</p>
                <p className="font-medium text-blue-900">
                  {loan.approvalDate ? new Date(loan.approvalDate).toLocaleDateString() : 'N/A'}
                </p>
              </div>
            </div>
          </div>

          {/* Bank Details */}
          <div>
            <h3 className="font-semibold text-gray-900 mb-3 text-lg">Bank Account Details</h3>
            <div className="bg-green-50 p-4 rounded-lg border border-green-200">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <p className="text-sm text-green-700">Bank Name</p>
                  <p className="font-medium text-green-900 text-lg">{loan.bankName}</p>
                </div>
                <div>
                  <p className="text-sm text-green-700">Account Number</p>
                  <p className="font-medium text-green-900 text-lg">{loan.accountNumber}</p>
                </div>
                {loan.accountHolderName && (
                  <div className="col-span-2">
                    <p className="text-sm text-green-700">Account Holder Name</p>
                    <p className="font-medium text-green-900 text-lg">{loan.accountHolderName}</p>
                  </div>
                )}
              </div>
            </div>
          </div>

          {/* Notes */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Disbursement Notes (Optional)
            </label>
            <textarea
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 resize-none"
              rows={3}
              placeholder="Add any notes about this disbursement (e.g., transaction reference, payment method)..."
            />
          </div>

          {/* Confirmation Checkbox */}
          <div className="bg-gray-50 p-4 rounded-lg border border-gray-200">
            <label className="flex items-start cursor-pointer">
              <input
                type="checkbox"
                checked={confirmed}
                onChange={(e) => setConfirmed(e.target.checked)}
                className="mt-1 mr-3 h-5 w-5 text-blue-600"
              />
              <span className="text-sm text-gray-700">
                <span className="font-medium">I confirm that:</span>
                <ul className="list-disc ml-5 mt-2 space-y-1">
                  <li>I have verified the bank account details are correct</li>
                  <li>The loan amount of <strong>R {loan.amount.toLocaleString()}</strong> will be transferred</li>
                  <li>This action will mark the loan as disbursed and cannot be reversed</li>
                </ul>
              </span>
            </label>
          </div>

          {/* Action Buttons */}
          <div className="flex space-x-3 pt-4 border-t">
            <button
              onClick={onClose}
              className="flex-1 bg-gray-200 hover:bg-gray-300 text-gray-800 px-6 py-3 rounded-lg font-medium transition"
            >
              Cancel
            </button>
            <button
              onClick={handleDisburse}
              disabled={loading || !confirmed}
              className="flex-1 bg-green-600 hover:bg-green-700 disabled:bg-gray-300 disabled:cursor-not-allowed text-white px-6 py-3 rounded-lg font-medium transition"
            >
              {loading ? 'Processing...' : `Disburse R ${loan.amount.toLocaleString()}`}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default AdminPayouts;
