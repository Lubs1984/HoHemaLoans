import React, { useState, useEffect } from 'react';
import { useAuthStore } from '../../store/authStore';
import { apiService } from '../../services/api';
import { PlusIcon, TrashIcon, CheckIcon } from '@heroicons/react/24/outline';

interface Income {
  id: string;
  sourceType: string;
  description: string;
  monthlyAmount: number;
  frequency: string;
  notes?: string;
  isVerified: boolean;
  createdAt: string;
}

interface Expense {
  id: string;
  category: string;
  description: string;
  monthlyAmount: number;
  frequency: string;
  notes?: string;
  isEssential: boolean;
  isFixed: boolean;
  createdAt: string;
}

interface Affordability {
  grossMonthlyIncome: number;
  netMonthlyIncome: number;
  totalMonthlyExpenses: number;
  essentialExpenses: number;
  nonEssentialExpenses: number;
  debtToIncomeRatio: number;
  availableFunds: number;
  expenseToIncomeRatio: number;
  affordabilityStatus: string;
  assessmentNotes: string;
  maxRecommendedLoanAmount: number;
  expiryDate: string;
}

const INCOME_SOURCES = ['Employment', 'Self-Employment', 'Grant', 'Investment', 'Pension', 'Other'];
const EXPENSE_CATEGORIES = ['Rent', 'Bond', 'Groceries', 'Utilities', 'Insurance', 'Debt', 'Transport', 'Childcare', 'Healthcare', 'Education', 'Other'];

const Profile: React.FC = () => {
  const { user } = useAuthStore();
  const [activeTab, setActiveTab] = useState<'personal' | 'income' | 'expenses' | 'affordability'>('personal');
  const [incomes, setIncomes] = useState<Income[]>([]);
  const [expenses, setExpenses] = useState<Expense[]>([]);
  const [affordability, setAffordability] = useState<Affordability | null>(null);
  const [loading, setLoading] = useState(false);

  // Income form state
  const [newIncome, setNewIncome] = useState({ sourceType: '', description: '', monthlyAmount: '', frequency: 'Monthly', notes: '' });
  const [showIncomeForm, setShowIncomeForm] = useState(false);

  // Expense form state
  const [newExpense, setNewExpense] = useState({ category: '', description: '', monthlyAmount: '', frequency: 'Monthly', isEssential: false, isFixed: false, notes: '' });
  const [showExpenseForm, setShowExpenseForm] = useState(false);

  useEffect(() => {
    if (activeTab === 'income') loadIncomes();
    if (activeTab === 'expenses') loadExpenses();
    if (activeTab === 'affordability') loadAffordability();
  }, [activeTab]);

  const loadIncomes = async () => {
    try {
      setLoading(true);
      const response = await apiService.get('/profile/income');
      setIncomes(response.data);
    } catch (error) {
      console.error('Failed to load incomes:', error);
    } finally {
      setLoading(false);
    }
  };

  const loadExpenses = async () => {
    try {
      setLoading(true);
      const response = await apiService.get('/profile/expense');
      setExpenses(response.data);
    } catch (error) {
      console.error('Failed to load expenses:', error);
    } finally {
      setLoading(false);
    }
  };

  const loadAffordability = async () => {
    try {
      setLoading(true);
      const response = await apiService.get('/profile/affordability');
      setAffordability(response.data);
    } catch (error) {
      console.error('Failed to load affordability:', error);
    } finally {
      setLoading(false);
    }
  };

  const addIncome = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await apiService.post('/profile/income', {
        sourceType: newIncome.sourceType,
        description: newIncome.description,
        monthlyAmount: parseFloat(newIncome.monthlyAmount),
        frequency: newIncome.frequency,
        notes: newIncome.notes || null
      });
      setNewIncome({ sourceType: '', description: '', monthlyAmount: '', frequency: 'Monthly', notes: '' });
      setShowIncomeForm(false);
      await loadIncomes();
      await loadAffordability();
    } catch (error) {
      console.error('Failed to add income:', error);
    }
  };

  const deleteIncome = async (id: string) => {
    if (window.confirm('Are you sure you want to delete this income?')) {
      try {
        await apiService.delete(`/profile/income/${id}`);
        await loadIncomes();
        await loadAffordability();
      } catch (error) {
        console.error('Failed to delete income:', error);
      }
    }
  };

  const addExpense = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await apiService.post('/profile/expense', {
        category: newExpense.category,
        description: newExpense.description,
        monthlyAmount: parseFloat(newExpense.monthlyAmount),
        frequency: newExpense.frequency,
        isEssential: newExpense.isEssential,
        isFixed: newExpense.isFixed,
        notes: newExpense.notes || null
      });
      setNewExpense({ category: '', description: '', monthlyAmount: '', frequency: 'Monthly', isEssential: false, isFixed: false, notes: '' });
      setShowExpenseForm(false);
      await loadExpenses();
      await loadAffordability();
    } catch (error) {
      console.error('Failed to add expense:', error);
    }
  };

  const deleteExpense = async (id: string) => {
    if (window.confirm('Are you sure you want to delete this expense?')) {
      try {
        await apiService.delete(`/profile/expense/${id}`);
        await loadExpenses();
        await loadAffordability();
      } catch (error) {
        console.error('Failed to delete expense:', error);
      }
    }
  };

  return (
    <div className="max-w-4xl mx-auto">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900">Profile</h1>
        <p className="text-gray-600">Manage your profile information, income, and expenses</p>
      </div>

      {/* Tabs */}
      <div className="border-b border-gray-200 mb-8">
        <div className="flex space-x-8">
          {(['personal', 'income', 'expenses', 'affordability'] as const).map((tab) => (
            <button
              key={tab}
              onClick={() => setActiveTab(tab)}
              className={`px-4 py-2 border-b-2 font-medium text-sm ${
                activeTab === tab
                  ? 'border-primary-600 text-primary-600'
                  : 'border-transparent text-gray-600 hover:text-gray-900 hover:border-gray-300'
              }`}
            >
              {tab.charAt(0).toUpperCase() + tab.slice(1)}
            </button>
          ))}
        </div>
      </div>

      {/* Personal Information Tab */}
      {activeTab === 'personal' && (
        <div className="bg-white rounded-lg shadow p-6">
          <h2 className="text-lg font-semibold mb-4">Personal Information</h2>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700">First Name</label>
              <input type="text" value={user?.firstName || ''} disabled className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50" />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700">Last Name</label>
              <input type="text" value={user?.lastName || ''} disabled className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50" />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700">Email</label>
              <input type="email" value={user?.email || ''} disabled className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50" />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700">Phone</label>
              <input type="tel" value={user?.phoneNumber || ''} disabled className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50" />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700">ID Number</label>
              <input type="text" value={user?.idNumber || ''} disabled className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50" />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700">Address</label>
              <input type="text" value={user?.address || ''} disabled className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50" />
            </div>
          </div>
        </div>
      )}

      {/* Income Tab */}
      {activeTab === 'income' && (
        <div className="space-y-6">
          <div className="bg-white rounded-lg shadow p-6">
            <div className="flex justify-between items-center mb-4">
              <h2 className="text-lg font-semibold">Monthly Income</h2>
              <button onClick={() => setShowIncomeForm(!showIncomeForm)} className="flex items-center gap-2 px-4 py-2 bg-primary-600 text-white rounded-md hover:bg-primary-700">
                <PlusIcon className="h-5 w-5" />
                Add Income
              </button>
            </div>

            {showIncomeForm && (
              <form onSubmit={addIncome} className="mb-6 p-4 bg-gray-50 rounded-lg space-y-4">
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700">Income Source</label>
                    <select
                      required
                      value={newIncome.sourceType}
                      onChange={(e) => setNewIncome({ ...newIncome, sourceType: e.target.value })}
                      className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md"
                    >
                      <option value="">Select source...</option>
                      {INCOME_SOURCES.map((source) => (
                        <option key={source} value={source}>{source}</option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700">Monthly Amount (R)</label>
                    <input
                      required
                      type="number"
                      step="0.01"
                      value={newIncome.monthlyAmount}
                      onChange={(e) => setNewIncome({ ...newIncome, monthlyAmount: e.target.value })}
                      className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md"
                      placeholder="0.00"
                    />
                  </div>
                  <div className="col-span-2">
                    <label className="block text-sm font-medium text-gray-700">Description</label>
                    <input
                      required
                      type="text"
                      value={newIncome.description}
                      onChange={(e) => setNewIncome({ ...newIncome, description: e.target.value })}
                      className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md"
                      placeholder="e.g., Salary from ABC Company"
                    />
                  </div>
                  <div className="col-span-2">
                    <label className="block text-sm font-medium text-gray-700">Notes</label>
                    <textarea
                      value={newIncome.notes}
                      onChange={(e) => setNewIncome({ ...newIncome, notes: e.target.value })}
                      className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md"
                      rows={2}
                      placeholder="Optional notes..."
                    />
                  </div>
                </div>
                <div className="flex gap-2">
                  <button type="submit" className="px-4 py-2 bg-primary-600 text-white rounded-md hover:bg-primary-700">Save Income</button>
                  <button type="button" onClick={() => setShowIncomeForm(false)} className="px-4 py-2 bg-gray-300 text-gray-700 rounded-md hover:bg-gray-400">Cancel</button>
                </div>
              </form>
            )}

            {loading ? (
              <p className="text-gray-500">Loading incomes...</p>
            ) : incomes.length === 0 ? (
              <p className="text-gray-500">No income recorded yet. Add your income sources.</p>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead className="bg-gray-50 border-b">
                    <tr>
                      <th className="text-left px-4 py-2 font-medium text-gray-700">Source</th>
                      <th className="text-left px-4 py-2 font-medium text-gray-700">Description</th>
                      <th className="text-right px-4 py-2 font-medium text-gray-700">Amount</th>
                      <th className="text-center px-4 py-2 font-medium text-gray-700">Status</th>
                      <th className="text-right px-4 py-2 font-medium text-gray-700">Action</th>
                    </tr>
                  </thead>
                  <tbody>
                    {incomes.map((income) => (
                      <tr key={income.id} className="border-b hover:bg-gray-50">
                        <td className="px-4 py-3">{income.sourceType}</td>
                        <td className="px-4 py-3">{income.description}</td>
                        <td className="px-4 py-3 text-right font-medium">R {income.monthlyAmount.toFixed(2)}</td>
                        <td className="px-4 py-3 text-center">
                          {income.isVerified ? (
                            <span className="flex items-center justify-center gap-1 text-green-600"><CheckIcon className="h-4 w-4" />Verified</span>
                          ) : (
                            <span className="text-yellow-600">Pending</span>
                          )}
                        </td>
                        <td className="px-4 py-3 text-right">
                          <button onClick={() => deleteIncome(income.id)} className="text-red-600 hover:text-red-800">
                            <TrashIcon className="h-4 w-4" />
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Expenses Tab */}
      {activeTab === 'expenses' && (
        <div className="space-y-6">
          <div className="bg-white rounded-lg shadow p-6">
            <div className="flex justify-between items-center mb-4">
              <h2 className="text-lg font-semibold">Monthly Expenses</h2>
              <button onClick={() => setShowExpenseForm(!showExpenseForm)} className="flex items-center gap-2 px-4 py-2 bg-primary-600 text-white rounded-md hover:bg-primary-700">
                <PlusIcon className="h-5 w-5" />
                Add Expense
              </button>
            </div>

            {showExpenseForm && (
              <form onSubmit={addExpense} className="mb-6 p-4 bg-gray-50 rounded-lg space-y-4">
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700">Category</label>
                    <select
                      required
                      value={newExpense.category}
                      onChange={(e) => setNewExpense({ ...newExpense, category: e.target.value })}
                      className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md"
                    >
                      <option value="">Select category...</option>
                      {EXPENSE_CATEGORIES.map((cat) => (
                        <option key={cat} value={cat}>{cat}</option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700">Monthly Amount (R)</label>
                    <input
                      required
                      type="number"
                      step="0.01"
                      value={newExpense.monthlyAmount}
                      onChange={(e) => setNewExpense({ ...newExpense, monthlyAmount: e.target.value })}
                      className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md"
                      placeholder="0.00"
                    />
                  </div>
                  <div className="col-span-2">
                    <label className="block text-sm font-medium text-gray-700">Description</label>
                    <input
                      required
                      type="text"
                      value={newExpense.description}
                      onChange={(e) => setNewExpense({ ...newExpense, description: e.target.value })}
                      className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md"
                      placeholder="e.g., Rent for apartment"
                    />
                  </div>
                  <div>
                    <label className="flex items-center gap-2">
                      <input
                        type="checkbox"
                        checked={newExpense.isEssential}
                        onChange={(e) => setNewExpense({ ...newExpense, isEssential: e.target.checked })}
                        className="rounded"
                      />
                      <span className="text-sm font-medium text-gray-700">Essential Expense</span>
                    </label>
                  </div>
                  <div>
                    <label className="flex items-center gap-2">
                      <input
                        type="checkbox"
                        checked={newExpense.isFixed}
                        onChange={(e) => setNewExpense({ ...newExpense, isFixed: e.target.checked })}
                        className="rounded"
                      />
                      <span className="text-sm font-medium text-gray-700">Fixed Amount</span>
                    </label>
                  </div>
                  <div className="col-span-2">
                    <label className="block text-sm font-medium text-gray-700">Notes</label>
                    <textarea
                      value={newExpense.notes}
                      onChange={(e) => setNewExpense({ ...newExpense, notes: e.target.value })}
                      className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md"
                      rows={2}
                      placeholder="Optional notes..."
                    />
                  </div>
                </div>
                <div className="flex gap-2">
                  <button type="submit" className="px-4 py-2 bg-primary-600 text-white rounded-md hover:bg-primary-700">Save Expense</button>
                  <button type="button" onClick={() => setShowExpenseForm(false)} className="px-4 py-2 bg-gray-300 text-gray-700 rounded-md hover:bg-gray-400">Cancel</button>
                </div>
              </form>
            )}

            {loading ? (
              <p className="text-gray-500">Loading expenses...</p>
            ) : expenses.length === 0 ? (
              <p className="text-gray-500">No expenses recorded yet. Add your expenses.</p>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead className="bg-gray-50 border-b">
                    <tr>
                      <th className="text-left px-4 py-2 font-medium text-gray-700">Category</th>
                      <th className="text-left px-4 py-2 font-medium text-gray-700">Description</th>
                      <th className="text-right px-4 py-2 font-medium text-gray-700">Amount</th>
                      <th className="text-center px-4 py-2 font-medium text-gray-700">Type</th>
                      <th className="text-right px-4 py-2 font-medium text-gray-700">Action</th>
                    </tr>
                  </thead>
                  <tbody>
                    {expenses.map((expense) => (
                      <tr key={expense.id} className="border-b hover:bg-gray-50">
                        <td className="px-4 py-3">{expense.category}</td>
                        <td className="px-4 py-3">{expense.description}</td>
                        <td className="px-4 py-3 text-right font-medium">R {expense.monthlyAmount.toFixed(2)}</td>
                        <td className="px-4 py-3 text-center text-xs">
                          <span className="inline-flex gap-1">
                            {expense.isEssential && <span className="bg-blue-100 text-blue-800 px-2 py-1 rounded">Essential</span>}
                            {expense.isFixed && <span className="bg-green-100 text-green-800 px-2 py-1 rounded">Fixed</span>}
                          </span>
                        </td>
                        <td className="px-4 py-3 text-right">
                          <button onClick={() => deleteExpense(expense.id)} className="text-red-600 hover:text-red-800">
                            <TrashIcon className="h-4 w-4" />
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Affordability Tab */}
      {activeTab === 'affordability' && (
        <div className="bg-white rounded-lg shadow p-6">
          <h2 className="text-lg font-semibold mb-6">Affordability Assessment</h2>

          {loading ? (
            <p className="text-gray-500">Calculating affordability...</p>
          ) : affordability ? (
            <div className="space-y-6">
              {/* Status Card */}
              <div className={`p-4 rounded-lg border-2 ${
                affordability.affordabilityStatus === 'Affordable' ? 'bg-green-50 border-green-200' :
                affordability.affordabilityStatus === 'LimitedAffordability' ? 'bg-yellow-50 border-yellow-200' :
                'bg-red-50 border-red-200'
              }`}>
                <p className={`text-sm font-medium ${
                  affordability.affordabilityStatus === 'Affordable' ? 'text-green-800' :
                  affordability.affordabilityStatus === 'LimitedAffordability' ? 'text-yellow-800' :
                  'text-red-800'
                }`}>
                  {affordability.affordabilityStatus === 'Affordable' ? '✓ ' : ''}
                  Affordability Status: {affordability.affordabilityStatus}
                </p>
                <p className="text-xs text-gray-600 mt-2">{affordability.assessmentNotes}</p>
              </div>

              {/* Key Metrics */}
              <div className="grid grid-cols-2 gap-4">
                <div className="bg-gray-50 p-4 rounded-lg">
                  <p className="text-sm text-gray-600">Gross Monthly Income</p>
                  <p className="text-2xl font-bold text-gray-900">R {affordability.grossMonthlyIncome.toFixed(2)}</p>
                </div>
                <div className="bg-gray-50 p-4 rounded-lg">
                  <p className="text-sm text-gray-600">Total Monthly Expenses</p>
                  <p className="text-2xl font-bold text-gray-900">R {affordability.totalMonthlyExpenses.toFixed(2)}</p>
                </div>
                <div className="bg-gray-50 p-4 rounded-lg">
                  <p className="text-sm text-gray-600">Available Funds</p>
                  <p className={`text-2xl font-bold ${affordability.availableFunds >= 0 ? 'text-green-600' : 'text-red-600'}`}>
                    R {affordability.availableFunds.toFixed(2)}
                  </p>
                </div>
                <div className="bg-gray-50 p-4 rounded-lg">
                  <p className="text-sm text-gray-600">Debt-to-Income Ratio</p>
                  <p className={`text-2xl font-bold ${affordability.debtToIncomeRatio <= 0.35 ? 'text-green-600' : 'text-red-600'}`}>
                    {(affordability.debtToIncomeRatio * 100).toFixed(1)}%
                  </p>
                  <p className="text-xs text-gray-500 mt-1">NCR Limit: 35%</p>
                </div>
              </div>

              {/* Expense Breakdown */}
              <div className="bg-gray-50 p-4 rounded-lg">
                <h3 className="font-semibold mb-3">Expense Breakdown</h3>
                <div className="space-y-2">
                  <div className="flex justify-between">
                    <span className="text-gray-700">Essential Expenses</span>
                    <span className="font-medium">R {affordability.essentialExpenses.toFixed(2)}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-700">Non-Essential Expenses</span>
                    <span className="font-medium">R {affordability.nonEssentialExpenses.toFixed(2)}</span>
                  </div>
                  <div className="border-t pt-2 flex justify-between font-semibold">
                    <span>Total Expenses</span>
                    <span>R {affordability.totalMonthlyExpenses.toFixed(2)}</span>
                  </div>
                </div>
              </div>

              {/* Recommended Loan Amount */}
              <div className="bg-primary-50 border border-primary-200 p-4 rounded-lg">
                <p className="text-sm text-gray-600">Maximum Recommended Loan Amount</p>
                <p className="text-3xl font-bold text-primary-700 mt-1">R {affordability.maxRecommendedLoanAmount.toFixed(2)}</p>
                <p className="text-xs text-gray-600 mt-2">Based on your income, expenses, and NCR compliance requirements</p>
              </div>

              {/* Assessment Info */}
              <div className="text-xs text-gray-500 p-4 bg-gray-50 rounded">
                <p>Assessment calculated on: {new Date(affordability.assessmentDate).toLocaleDateString()}</p>
                <p>Valid until: {new Date(affordability.expiryDate).toLocaleDateString()}</p>
              </div>
            </div>
          ) : (
            <p className="text-gray-500">No affordability assessment yet. Add your income and expenses to generate an assessment.</p>
          )}
        </div>
      )}
    </div>
  );
};

export default Profile;