import { useState, useEffect, useCallback, useRef } from 'react';
import { apiService } from '../../services/api';

interface BankTransaction {
  id: string;
  transactionDate: string;
  amount: number;
  balance: number | null;
  description: string;
  reference: string | null;
  type: string;
  category: string;
  matchStatus: string;
  matchedDeductionId: string | null;
  matchedLoanId: string | null;
  importBatchId: string | null;
  sourceFileName: string | null;
  notes: string | null;
  createdAt: string;
}

interface DailySummary {
  date: string;
  deductions: {
    expected: number;
    totalExpected: number;
    paid: number;
    outstanding: number;
    overdueTotal: number;
    items: any[];
  };
  bankActivity: {
    totalTransactions: number;
    totalReceived: number;
    totalPaidOut: number;
    netFlow: number;
    matched: number;
    unmatched: number;
  };
  variance: number;
}

export default function AdminBankRecon() {
  const [transactions, setTransactions] = useState<BankTransaction[]>([]);
  const [dailySummary, setDailySummary] = useState<DailySummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [uploading, setUploading] = useState(false);
  const [matchFilter, setMatchFilter] = useState('');
  const [typeFilter, setTypeFilter] = useState('');
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [tab, setTab] = useState<'summary' | 'transactions' | 'upload'>('summary');
  const [summaryDate, setSummaryDate] = useState(new Date().toISOString().split('T')[0]);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const fetchTransactions = useCallback(async () => {
    try {
      setLoading(true);
      const params = new URLSearchParams();
      if (matchFilter) params.set('matchStatus', matchFilter);
      if (typeFilter) params.set('type', typeFilter);
      params.set('page', page.toString());
      params.set('pageSize', '50');
      const data = await apiService.get(`/admin/bank-recon/transactions?${params}`);
      setTransactions(data.transactions);
      setTotalCount(data.totalCount);
    } catch (err: any) {
      setError(err.message || 'Failed to load transactions');
    } finally {
      setLoading(false);
    }
  }, [matchFilter, typeFilter, page]);

  const fetchDailySummary = useCallback(async () => {
    try {
      const data = await apiService.get(`/admin/bank-recon/daily-summary?date=${summaryDate}`);
      setDailySummary(data);
    } catch (err: any) {
      console.error('Failed to load daily summary', err);
    }
  }, [summaryDate]);

  useEffect(() => {
    fetchTransactions();
    fetchDailySummary();
  }, [fetchTransactions, fetchDailySummary]);

  const uploadFile = async (file: File) => {
    try {
      setUploading(true);
      setError('');
      const formData = new FormData();
      formData.append('file', file);
      const result = await apiService.post('/admin/bank-recon/upload', formData);
      setSuccess(`${result.message} (${result.credits} credits, ${result.debits} debits)`);
      fetchTransactions();
      fetchDailySummary();
    } catch (err: any) {
      setError(err.message || 'Failed to upload bank statement');
    } finally {
      setUploading(false);
      if (fileInputRef.current) fileInputRef.current.value = '';
    }
  };

  const autoMatch = async () => {
    try {
      setError('');
      const result = await apiService.post('/admin/bank-recon/auto-match', {});
      setSuccess(result.message);
      fetchTransactions();
      fetchDailySummary();
    } catch (err: any) {
      setError(err.message || 'Auto-match failed');
    }
  };

  const ignoreTransaction = async (id: string) => {
    try {
      setError('');
      await apiService.post(`/admin/bank-recon/${id}/ignore`, {});
      setSuccess('Transaction ignored');
      fetchTransactions();
    } catch (err: any) {
      setError(err.message || 'Failed to ignore transaction');
    }
  };

  const formatCurrency = (amount: number) => `R ${amount.toLocaleString('en-ZA', { minimumFractionDigits: 2 })}`;
  const formatDate = (date: string) => new Date(date).toLocaleDateString('en-ZA');

  const matchColors: Record<string, string> = {
    Unmatched: 'bg-yellow-100 text-yellow-800',
    AutoMatched: 'bg-green-100 text-green-800',
    ManuallyMatched: 'bg-blue-100 text-blue-800',
    Ignored: 'bg-gray-100 text-gray-800',
  };

  return (
    <div className="p-6">
      <h1 className="text-2xl font-bold mb-2">Bank Reconciliation</h1>
      <p className="text-sm text-gray-500 mb-6">FNB Statement Import & Matching</p>

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

      {/* Tabs */}
      <div className="flex border-b mb-6">
        <button
          onClick={() => setTab('summary')}
          className={`px-4 py-2 font-medium ${tab === 'summary' ? 'border-b-2 border-blue-500 text-blue-600' : 'text-gray-500'}`}
        >
          Daily Summary
        </button>
        <button
          onClick={() => setTab('transactions')}
          className={`px-4 py-2 font-medium ${tab === 'transactions' ? 'border-b-2 border-blue-500 text-blue-600' : 'text-gray-500'}`}
        >
          Transactions ({totalCount})
        </button>
        <button
          onClick={() => setTab('upload')}
          className={`px-4 py-2 font-medium ${tab === 'upload' ? 'border-b-2 border-blue-500 text-blue-600' : 'text-gray-500'}`}
        >
          Upload Statement
        </button>
      </div>

      {/* Daily Summary Tab */}
      {tab === 'summary' && (
        <div>
          <div className="flex items-center gap-4 mb-6">
            <label className="text-sm font-medium text-gray-700">Date:</label>
            <input
              type="date"
              value={summaryDate}
              onChange={(e) => setSummaryDate(e.target.value)}
              className="border rounded-lg px-3 py-2"
            />
          </div>

          {dailySummary ? (
            <div className="space-y-6">
              {/* Payments vs Payouts */}
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div className="bg-white rounded-lg shadow p-6 border-l-4 border-green-500">
                  <h3 className="text-sm font-medium text-gray-500 mb-1">Payments Received</h3>
                  <p className="text-3xl font-bold text-green-700">{formatCurrency(dailySummary.bankActivity.totalReceived)}</p>
                  <p className="text-sm text-gray-400 mt-1">{dailySummary.bankActivity.totalTransactions - dailySummary.bankActivity.matched - dailySummary.bankActivity.unmatched + dailySummary.bankActivity.matched} transactions</p>
                </div>
                <div className="bg-white rounded-lg shadow p-6 border-l-4 border-red-500">
                  <h3 className="text-sm font-medium text-gray-500 mb-1">Payouts / Disbursements</h3>
                  <p className="text-3xl font-bold text-red-700">{formatCurrency(dailySummary.bankActivity.totalPaidOut)}</p>
                </div>
                <div className={`bg-white rounded-lg shadow p-6 border-l-4 ${dailySummary.bankActivity.netFlow >= 0 ? 'border-green-500' : 'border-red-500'}`}>
                  <h3 className="text-sm font-medium text-gray-500 mb-1">Net Flow</h3>
                  <p className={`text-3xl font-bold ${dailySummary.bankActivity.netFlow >= 0 ? 'text-green-700' : 'text-red-700'}`}>
                    {formatCurrency(dailySummary.bankActivity.netFlow)}
                  </p>
                </div>
              </div>

              {/* Deduction Summary */}
              <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
                <div className="bg-blue-50 rounded-lg p-4">
                  <p className="text-sm text-blue-600">Expected Deductions</p>
                  <p className="text-2xl font-bold text-blue-800">{dailySummary.deductions.expected}</p>
                  <p className="text-sm text-blue-500">{formatCurrency(dailySummary.deductions.totalExpected)}</p>
                </div>
                <div className="bg-green-50 rounded-lg p-4">
                  <p className="text-sm text-green-600">Paid Today</p>
                  <p className="text-2xl font-bold text-green-800">{dailySummary.deductions.paid}</p>
                </div>
                <div className="bg-orange-50 rounded-lg p-4">
                  <p className="text-sm text-orange-600">Outstanding</p>
                  <p className="text-2xl font-bold text-orange-800">{dailySummary.deductions.outstanding}</p>
                </div>
                <div className="bg-red-50 rounded-lg p-4">
                  <p className="text-sm text-red-600">Overdue (All)</p>
                  <p className="text-2xl font-bold text-red-800">{dailySummary.deductions.overdueTotal}</p>
                </div>
              </div>

              {/* Variance */}
              <div className={`rounded-lg p-6 ${dailySummary.variance >= 0 ? 'bg-green-50 border border-green-200' : 'bg-red-50 border border-red-200'}`}>
                <h3 className="text-lg font-semibold mb-2">
                  {dailySummary.variance >= 0 ? '✓' : '⚠'} End of Day Variance
                </h3>
                <p className={`text-2xl font-bold ${dailySummary.variance >= 0 ? 'text-green-700' : 'text-red-700'}`}>
                  {formatCurrency(dailySummary.variance)}
                </p>
                <p className="text-sm text-gray-600 mt-1">
                  Received ({formatCurrency(dailySummary.bankActivity.totalReceived)}) vs Expected ({formatCurrency(dailySummary.deductions.totalExpected)})
                </p>
              </div>

              {/* Matching Summary */}
              <div className="bg-white rounded-lg shadow p-6">
                <div className="flex justify-between items-center mb-4">
                  <h3 className="text-lg font-semibold">Transaction Matching</h3>
                  <button
                    onClick={autoMatch}
                    className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
                  >
                    Run Auto-Match
                  </button>
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div className="text-center p-3 bg-green-50 rounded">
                    <p className="text-2xl font-bold text-green-700">{dailySummary.bankActivity.matched}</p>
                    <p className="text-sm text-green-600">Matched</p>
                  </div>
                  <div className="text-center p-3 bg-yellow-50 rounded">
                    <p className="text-2xl font-bold text-yellow-700">{dailySummary.bankActivity.unmatched}</p>
                    <p className="text-sm text-yellow-600">Unmatched</p>
                  </div>
                </div>
              </div>

              {/* Expected Deductions Detail */}
              {dailySummary.deductions.items.length > 0 && (
                <div className="bg-white rounded-lg shadow p-6">
                  <h3 className="text-lg font-semibold mb-4">Expected Deductions for {dailySummary.date}</h3>
                  <table className="min-w-full">
                    <thead className="bg-gray-50">
                      <tr>
                        <th className="px-4 py-2 text-left text-xs font-medium text-gray-500">Borrower</th>
                        <th className="px-4 py-2 text-left text-xs font-medium text-gray-500">#</th>
                        <th className="px-4 py-2 text-right text-xs font-medium text-gray-500">Amount</th>
                        <th className="px-4 py-2 text-center text-xs font-medium text-gray-500">Status</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y">
                      {dailySummary.deductions.items.map((item: any) => (
                        <tr key={item.id}>
                          <td className="px-4 py-2 text-sm">{item.borrower}</td>
                          <td className="px-4 py-2 text-sm">{item.installmentNumber}</td>
                          <td className="px-4 py-2 text-sm text-right font-mono">{formatCurrency(item.totalAmount)}</td>
                          <td className="px-4 py-2 text-center">
                            <span className={`px-2 py-1 rounded-full text-xs font-medium ${
                              item.status === 'Paid' ? 'bg-green-100 text-green-800' :
                              item.status === 'Overdue' ? 'bg-red-100 text-red-800' :
                              'bg-blue-100 text-blue-800'
                            }`}>
                              {item.status}
                            </span>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          ) : (
            <div className="text-center py-8 text-gray-500">Loading summary...</div>
          )}
        </div>
      )}

      {/* Transactions Tab */}
      {tab === 'transactions' && (
        <div>
          <div className="flex items-center gap-4 mb-4">
            <select
              value={matchFilter}
              onChange={(e) => { setMatchFilter(e.target.value); setPage(1); }}
              className="border rounded-lg px-3 py-2"
            >
              <option value="">All Match Status</option>
              <option value="Unmatched">Unmatched</option>
              <option value="AutoMatched">Auto-Matched</option>
              <option value="ManuallyMatched">Manually Matched</option>
              <option value="Ignored">Ignored</option>
            </select>
            <select
              value={typeFilter}
              onChange={(e) => { setTypeFilter(e.target.value); setPage(1); }}
              className="border rounded-lg px-3 py-2"
            >
              <option value="">All Types</option>
              <option value="Credit">Credits (Incoming)</option>
              <option value="Debit">Debits (Outgoing)</option>
            </select>
            <button
              onClick={autoMatch}
              className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
            >
              Auto-Match
            </button>
            <span className="text-sm text-gray-500">{totalCount} total</span>
          </div>

          {loading ? (
            <div className="text-center py-8 text-gray-500">Loading...</div>
          ) : transactions.length === 0 ? (
            <div className="text-center py-8 text-gray-500">
              No transactions found. Upload an FNB bank statement to get started.
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="min-w-full bg-white rounded-lg shadow">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Date</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Description</th>
                    <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Amount</th>
                    <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Balance</th>
                    <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase">Type</th>
                    <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase">Match</th>
                    <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200">
                  {transactions.map((tx) => (
                    <tr key={tx.id} className="hover:bg-gray-50">
                      <td className="px-4 py-3 text-sm">{formatDate(tx.transactionDate)}</td>
                      <td className="px-4 py-3 text-sm">
                        <div>{tx.description}</div>
                        {tx.reference && <div className="text-xs text-gray-400">Ref: {tx.reference}</div>}
                      </td>
                      <td className={`px-4 py-3 text-sm text-right font-mono ${tx.type === 'Credit' ? 'text-green-600' : 'text-red-600'}`}>
                        {tx.type === 'Credit' ? '+' : '-'}{formatCurrency(tx.amount)}
                      </td>
                      <td className="px-4 py-3 text-sm text-right font-mono text-gray-500">
                        {tx.balance != null ? formatCurrency(tx.balance) : '-'}
                      </td>
                      <td className="px-4 py-3 text-center">
                        <span className={`px-2 py-1 rounded-full text-xs font-medium ${
                          tx.type === 'Credit' ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'
                        }`}>
                          {tx.type}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-center">
                        <span className={`px-2 py-1 rounded-full text-xs font-medium ${matchColors[tx.matchStatus] || 'bg-gray-100'}`}>
                          {tx.matchStatus}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-center">
                        {tx.matchStatus === 'Unmatched' && (
                          <button
                            onClick={() => ignoreTransaction(tx.id)}
                            className="px-2 py-1 text-xs bg-gray-500 text-white rounded hover:bg-gray-600"
                          >
                            Ignore
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

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
                disabled={transactions.length < 50}
                className="px-3 py-1 border rounded disabled:opacity-50"
              >
                Next
              </button>
            </div>
          )}
        </div>
      )}

      {/* Upload Tab */}
      {tab === 'upload' && (
        <div className="max-w-lg mx-auto">
          <div className="bg-white rounded-lg shadow p-8">
            <h2 className="text-lg font-bold mb-4">Upload FNB Bank Statement</h2>
            <p className="text-sm text-gray-600 mb-6">
              Upload a CSV export from your FNB Business Online banking.<br />
              The system will auto-detect columns for Date, Amount, Balance, Description, and Reference.
            </p>

            <div className="border-2 border-dashed border-gray-300 rounded-lg p-8 text-center hover:border-blue-400 transition-colors">
              <input
                ref={fileInputRef}
                type="file"
                accept=".csv"
                onChange={(e) => {
                  const file = e.target.files?.[0];
                  if (file) uploadFile(file);
                }}
                className="hidden"
                id="csv-upload"
              />
              <label htmlFor="csv-upload" className="cursor-pointer">
                <svg className="mx-auto h-12 w-12 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
                </svg>
                <p className="mt-2 text-sm text-gray-600">
                  {uploading ? 'Uploading...' : 'Click to select FNB CSV file'}
                </p>
                <p className="text-xs text-gray-400 mt-1">CSV files only</p>
              </label>
            </div>

            <div className="mt-6 bg-gray-50 rounded-lg p-4">
              <h3 className="text-sm font-semibold mb-2">Supported FNB CSV Formats:</h3>
              <ul className="text-xs text-gray-600 space-y-1">
                <li>- FNB Business Online statement export</li>
                <li>- Columns: Date, Amount, Balance, Description, Reference</li>
                <li>- Header row is auto-detected</li>
                <li>- South African Rand amounts (R prefix optional)</li>
              </ul>
            </div>

            <div className="mt-4 bg-blue-50 rounded-lg p-4">
              <h3 className="text-sm font-semibold text-blue-800 mb-2">After Upload:</h3>
              <ol className="text-xs text-blue-700 space-y-1 list-decimal list-inside">
                <li>Transactions are imported and categorized</li>
                <li>Run "Auto-Match" to link payments to deductions</li>
                <li>Review unmatched items manually</li>
                <li>Check the Daily Summary for end-of-day reconciliation</li>
              </ol>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
