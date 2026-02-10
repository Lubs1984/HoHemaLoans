import { useState, useEffect, useCallback } from 'react';
import { apiService } from '../../services/api';

interface DeductionEntry {
  id: string;
  loanApplicationId: string;
  installmentNumber: number;
  dueDate: string;
  principalAmount: number;
  interestAmount: number;
  adminFeeAmount: number;
  totalAmount: number;
  status: string;
  paidDate: string | null;
  paidAmount: number | null;
  paymentReference: string | null;
  bankTransactionId: string | null;
  notes: string | null;
  borrower: string;
  borrowerEmail: string | null;
  loanAmount: number;
}

interface UnscheduledLoan {
  id: string;
  amount: number;
  totalAmount: number;
  monthlyPayment: number;
  termMonths: number;
  interestRate: number;
  repaymentDay: number | null;
  applicationDate: string;
  approvalDate: string | null;
  borrower: string;
}

interface Summary {
  totalScheduled: number;
  totalPaid: number;
  totalOverdue: number;
  totalFailed: number;
  amountExpected: number;
  amountCollected: number;
}

export default function AdminDeductionSchedule() {
  const [entries, setEntries] = useState<DeductionEntry[]>([]);
  const [unscheduledLoans, setUnscheduledLoans] = useState<UnscheduledLoan[]>([]);
  const [summary, setSummary] = useState<Summary | null>(null);
  const [loading, setLoading] = useState(true);
  const [statusFilter, setStatusFilter] = useState('');
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [showPayModal, setShowPayModal] = useState<DeductionEntry | null>(null);
  const [payForm, setPayForm] = useState({ amount: '', reference: '', notes: '' });
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [tab, setTab] = useState<'schedule' | 'unscheduled'>('schedule');

  const fetchDeductions = useCallback(async () => {
    try {
      setLoading(true);
      const params = new URLSearchParams();
      if (statusFilter) params.set('status', statusFilter);
      params.set('page', page.toString());
      params.set('pageSize', '50');
      const resp = await apiService.get<any>(`/admin/deductions?${params}`);
      setEntries(resp.data.entries);
      setSummary(resp.data.summary);
      setTotalCount(resp.data.totalCount);
    } catch (err: any) {
      setError(err.message || 'Failed to load deductions');
    } finally {
      setLoading(false);
    }
  }, [statusFilter, page]);

  const fetchUnscheduled = useCallback(async () => {
    try {
      const resp = await apiService.get<any>('/admin/deductions/unscheduled-loans');
      setUnscheduledLoans(resp.data);
    } catch (err: any) {
      console.error('Failed to load unscheduled loans', err);
    }
  }, []);

  useEffect(() => {
    fetchDeductions();
    fetchUnscheduled();
  }, [fetchDeductions, fetchUnscheduled]);

  const generateSchedule = async (loanId: string) => {
    try {
      setError('');
      const result = await apiService.post<any>(`/admin/deductions/generate/${loanId}`, {});
      setSuccess(result.data.message);
      fetchDeductions();
      fetchUnscheduled();
    } catch (err: any) {
      setError(err.message || 'Failed to generate schedule');
    }
  };

  const markPaid = async () => {
    if (!showPayModal) return;
    try {
      setError('');
      await apiService.post(`/admin/deductions/${showPayModal.id}/mark-paid`, {
        amount: payForm.amount ? parseFloat(payForm.amount) : null,
        paymentReference: payForm.reference || null,
        notes: payForm.notes || null,
      });
      setSuccess('Deduction marked as paid');
      setShowPayModal(null);
      setPayForm({ amount: '', reference: '', notes: '' });
      fetchDeductions();
    } catch (err: any) {
      setError(err.message || 'Failed to mark as paid');
    }
  };

  const updateStatus = async (id: string, status: string) => {
    try {
      setError('');
      await apiService.put(`/admin/deductions/${id}/status`, { status });
      setSuccess(`Status updated to ${status}`);
      fetchDeductions();
    } catch (err: any) {
      setError(err.message || 'Failed to update status');
    }
  };

  const formatCurrency = (amount: number) => `R ${amount.toLocaleString('en-ZA', { minimumFractionDigits: 2 })}`;
  const formatDate = (date: string) => new Date(date).toLocaleDateString('en-ZA');

  const statusColors: Record<string, string> = {
    Scheduled: 'bg-blue-100 text-blue-800',
    Paid: 'bg-green-100 text-green-800',
    Overdue: 'bg-red-100 text-red-800',
    Failed: 'bg-orange-100 text-orange-800',
    Reversed: 'bg-gray-100 text-gray-800',
    PartiallyPaid: 'bg-yellow-100 text-yellow-800',
  };

  return (
    <div className="p-6">
      <h1 className="text-2xl font-bold mb-6">Deduction Schedule</h1>

      {error && (
        <div className="mb-4 p-3 bg-red-50 border border-red-200 text-red-700 rounded-lg">
          {error}
          <button onClick={() => setError('')} className="ml-2 font-bold">&times;</button>
        </div>
      )}
      {success && (
        <div className="mb-4 p-3 bg-green-50 border border-green-200 text-green-700 rounded-lg">
          {success}
          <button onClick={() => setSuccess('')} className="ml-2 font-bold">&times;</button>
        </div>
      )}

      {/* Summary Cards */}
      {summary && (
        <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-4 mb-6">
          <div className="bg-blue-50 rounded-lg p-4">
            <p className="text-sm text-blue-600">Scheduled</p>
            <p className="text-2xl font-bold text-blue-800">{summary.totalScheduled}</p>
          </div>
          <div className="bg-green-50 rounded-lg p-4">
            <p className="text-sm text-green-600">Paid</p>
            <p className="text-2xl font-bold text-green-800">{summary.totalPaid}</p>
          </div>
          <div className="bg-red-50 rounded-lg p-4">
            <p className="text-sm text-red-600">Overdue</p>
            <p className="text-2xl font-bold text-red-800">{summary.totalOverdue}</p>
          </div>
          <div className="bg-orange-50 rounded-lg p-4">
            <p className="text-sm text-orange-600">Failed</p>
            <p className="text-2xl font-bold text-orange-800">{summary.totalFailed}</p>
          </div>
          <div className="bg-indigo-50 rounded-lg p-4">
            <p className="text-sm text-indigo-600">Expected</p>
            <p className="text-xl font-bold text-indigo-800">{formatCurrency(summary.amountExpected)}</p>
          </div>
          <div className="bg-emerald-50 rounded-lg p-4">
            <p className="text-sm text-emerald-600">Collected</p>
            <p className="text-xl font-bold text-emerald-800">{formatCurrency(summary.amountCollected)}</p>
          </div>
        </div>
      )}

      {/* Tabs */}
      <div className="flex border-b mb-4">
        <button
          onClick={() => setTab('schedule')}
          className={`px-4 py-2 font-medium ${tab === 'schedule' ? 'border-b-2 border-blue-500 text-blue-600' : 'text-gray-500'}`}
        >
          Deduction Schedule
        </button>
        <button
          onClick={() => setTab('unscheduled')}
          className={`px-4 py-2 font-medium ${tab === 'unscheduled' ? 'border-b-2 border-blue-500 text-blue-600' : 'text-gray-500'}`}
        >
          Unscheduled Loans ({unscheduledLoans.length})
        </button>
      </div>

      {tab === 'schedule' && (
        <>
          {/* Filter */}
          <div className="flex items-center gap-4 mb-4">
            <select
              value={statusFilter}
              onChange={(e) => { setStatusFilter(e.target.value); setPage(1); }}
              className="border rounded-lg px-3 py-2"
            >
              <option value="">All Statuses</option>
              <option value="Scheduled">Scheduled</option>
              <option value="Paid">Paid</option>
              <option value="Overdue">Overdue</option>
              <option value="Failed">Failed</option>
              <option value="Reversed">Reversed</option>
            </select>
            <span className="text-sm text-gray-500">{totalCount} total entries</span>
          </div>

          {/* Table */}
          {loading ? (
            <div className="text-center py-8 text-gray-500">Loading...</div>
          ) : (
            <div className="overflow-x-auto">
              <table className="min-w-full bg-white rounded-lg shadow">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">#</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Borrower</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Due Date</th>
                    <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Amount</th>
                    <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase">Status</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Paid</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Reference</th>
                    <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200">
                  {entries.map((entry) => (
                    <tr key={entry.id} className="hover:bg-gray-50">
                      <td className="px-4 py-3 text-sm">{entry.installmentNumber}</td>
                      <td className="px-4 py-3 text-sm">
                        <div className="font-medium">{entry.borrower}</div>
                        <div className="text-xs text-gray-400">{entry.borrowerEmail}</div>
                      </td>
                      <td className="px-4 py-3 text-sm">{formatDate(entry.dueDate)}</td>
                      <td className="px-4 py-3 text-sm text-right font-mono">{formatCurrency(entry.totalAmount)}</td>
                      <td className="px-4 py-3 text-center">
                        <span className={`px-2 py-1 rounded-full text-xs font-medium ${statusColors[entry.status] || 'bg-gray-100'}`}>
                          {entry.status}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-sm">
                        {entry.paidDate && (
                          <>
                            <div>{formatDate(entry.paidDate)}</div>
                            <div className="text-xs text-gray-400">{entry.paidAmount != null ? formatCurrency(entry.paidAmount) : ''}</div>
                          </>
                        )}
                      </td>
                      <td className="px-4 py-3 text-sm text-gray-500">{entry.paymentReference || '-'}</td>
                      <td className="px-4 py-3 text-center">
                        {(entry.status === 'Scheduled' || entry.status === 'Overdue') && (
                          <div className="flex gap-1 justify-center">
                            <button
                              onClick={() => {
                                setShowPayModal(entry);
                                setPayForm({ amount: entry.totalAmount.toString(), reference: '', notes: '' });
                              }}
                              className="px-2 py-1 text-xs bg-green-600 text-white rounded hover:bg-green-700"
                            >
                              Mark Paid
                            </button>
                            <button
                              onClick={() => updateStatus(entry.id, 'Failed')}
                              className="px-2 py-1 text-xs bg-red-600 text-white rounded hover:bg-red-700"
                            >
                              Failed
                            </button>
                          </div>
                        )}
                        {entry.status === 'Paid' && (
                          <button
                            onClick={() => updateStatus(entry.id, 'Reversed')}
                            className="px-2 py-1 text-xs bg-gray-600 text-white rounded hover:bg-gray-700"
                          >
                            Reverse
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {/* Pagination */}
          {totalCount > 50 && (
            <div className="flex justify-center gap-2 mt-4">
              <button
                onClick={() => setPage(p => Math.max(1, p - 1))}
                disabled={page === 1}
                className="px-3 py-1 border rounded disabled:opacity-50"
              >
                Previous
              </button>
              <span className="px-3 py-1">Page {page}</span>
              <button
                onClick={() => setPage(p => p + 1)}
                disabled={entries.length < 50}
                className="px-3 py-1 border rounded disabled:opacity-50"
              >
                Next
              </button>
            </div>
          )}
        </>
      )}

      {tab === 'unscheduled' && (
        <div>
          {unscheduledLoans.length === 0 ? (
            <div className="text-center py-8 text-gray-500">
              All disbursed loans have deduction schedules.
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="min-w-full bg-white rounded-lg shadow">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Borrower</th>
                    <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Loan Amount</th>
                    <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Monthly Payment</th>
                    <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase">Term</th>
                    <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase">Repayment Day</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Approved</th>
                    <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200">
                  {unscheduledLoans.map((loan) => (
                    <tr key={loan.id} className="hover:bg-gray-50">
                      <td className="px-4 py-3 text-sm font-medium">{loan.borrower}</td>
                      <td className="px-4 py-3 text-sm text-right font-mono">{formatCurrency(loan.amount)}</td>
                      <td className="px-4 py-3 text-sm text-right font-mono">{formatCurrency(loan.monthlyPayment)}</td>
                      <td className="px-4 py-3 text-sm text-center">{loan.termMonths} months</td>
                      <td className="px-4 py-3 text-sm text-center">{loan.repaymentDay || 25}</td>
                      <td className="px-4 py-3 text-sm">{loan.approvalDate ? formatDate(loan.approvalDate) : '-'}</td>
                      <td className="px-4 py-3 text-center">
                        <button
                          onClick={() => generateSchedule(loan.id)}
                          className="px-3 py-1 text-sm bg-blue-600 text-white rounded hover:bg-blue-700"
                        >
                          Generate Schedule
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {/* Mark Paid Modal */}
      {showPayModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 w-full max-w-md">
            <h2 className="text-lg font-bold mb-4">Mark Deduction as Paid</h2>
            <p className="text-sm text-gray-600 mb-4">
              Installment #{showPayModal.installmentNumber} - {showPayModal.borrower}<br />
              Due: {formatDate(showPayModal.dueDate)} | Expected: {formatCurrency(showPayModal.totalAmount)}
            </p>
            <div className="space-y-3">
              <div>
                <label className="block text-sm font-medium text-gray-700">Amount Paid (R)</label>
                <input
                  type="number"
                  value={payForm.amount}
                  onChange={(e) => setPayForm(f => ({ ...f, amount: e.target.value }))}
                  className="mt-1 w-full border rounded-lg px-3 py-2"
                  step="0.01"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Payment Reference</label>
                <input
                  type="text"
                  value={payForm.reference}
                  onChange={(e) => setPayForm(f => ({ ...f, reference: e.target.value }))}
                  className="mt-1 w-full border rounded-lg px-3 py-2"
                  placeholder="Bank reference or EFT number"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Notes</label>
                <textarea
                  value={payForm.notes}
                  onChange={(e) => setPayForm(f => ({ ...f, notes: e.target.value }))}
                  className="mt-1 w-full border rounded-lg px-3 py-2"
                  rows={2}
                />
              </div>
            </div>
            <div className="flex justify-end gap-2 mt-4">
              <button
                onClick={() => setShowPayModal(null)}
                className="px-4 py-2 border rounded-lg hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={markPaid}
                className="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700"
              >
                Confirm Payment
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
