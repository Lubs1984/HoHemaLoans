import React, { useEffect, useState } from 'react';
import { PlusIcon, TrashIcon, PencilIcon, BanknotesIcon, CreditCardIcon, ChartBarIcon } from '@heroicons/react/24/outline';
import { apiService } from '../../services/api';
import { useToast } from '../../contexts/ToastContext';
import AddIncomeModal from '../../components/modals/AddIncomeModal';
import AddExpenseModal from '../../components/modals/AddExpenseModal';
import EditIncomeModal from '../../components/modals/EditIncomeModal';
import EditExpenseModal from '../../components/modals/EditExpenseModal';

interface Income {
  id: string;
  sourceType: string;
  description: string;
  monthlyAmount: number;
  frequency: string;
  isVerified: boolean;
  notes?: string;
}

interface Expense {
  id: string;
  category: string;
  description: string;
  monthlyAmount: number;
  frequency: string;
  isEssential: boolean;
  notes?: string;
}

interface AffordabilityAssessment {
  grossMonthlyIncome: number;
  netMonthlyIncome: number;
  totalMonthlyExpenses: number;
  essentialExpenses: number;
  nonEssentialExpenses: number;
  debtToIncomeRatio: number;
  expenseToIncomeRatio: number;
  availableFunds: number;
  affordabilityStatus: string;
  maxRecommendedLoanAmount: number;
  assessmentNotes?: string;
}

const Affordability: React.FC = () => {
  const [incomes, setIncomes] = useState<Income[]>([]);
  const [expenses, setExpenses] = useState<Expense[]>([]);
  const [assessment, setAssessment] = useState<AffordabilityAssessment | null>(null);
  const [showIncomeModal, setShowIncomeModal] = useState(false);
  const [showExpenseModal, setShowExpenseModal] = useState(false);
  const [editingIncome, setEditingIncome] = useState<Income | null>(null);
  const [editingExpense, setEditingExpense] = useState<Expense | null>(null);
  const [assessmentError, setAssessmentError] = useState<string | null>(null);
  const { success, error: showError, warning, info } = useToast();

  useEffect(() => {
    loadData();
  }, []);

  useEffect(() => {
    console.log('Income state updated, count:', incomes.length, incomes);
  }, [incomes]);

  useEffect(() => {
    console.log('Expense state updated, count:', expenses.length, expenses);
  }, [expenses]);

  const loadData = async () => {
    try {
      setAssessmentError(null);

      const [incomeResult, expenseResult, assessmentResult] = await Promise.allSettled([
        apiService.getIncomes(),
        apiService.getExpenses(),
        apiService.getAffordability(),
      ]);

      if (incomeResult.status === 'fulfilled' && Array.isArray(incomeResult.value)) {
        console.log('Loaded incomes:', incomeResult.value);
        setIncomes(incomeResult.value);
      } else {
        console.error('Failed to load incomes', incomeResult);
        setIncomes([]);
      }

      if (expenseResult.status === 'fulfilled' && Array.isArray(expenseResult.value)) {
        console.log('Loaded expenses:', expenseResult.value);
        setExpenses(expenseResult.value);
      } else {
        console.error('Failed to load expenses', expenseResult);
        setExpenses([]);
      }

      if (assessmentResult.status === 'fulfilled') {
        console.log('Loaded assessment:', assessmentResult.value);
        setAssessment(assessmentResult.value);
      } else {
        console.error('Failed to load assessment', assessmentResult);
        setAssessment(null);
        setAssessmentError('Unable to calculate affordability right now. Please check your incomes/expenses and try again.');
      }
    } catch (error) {
      console.error('Failed to load affordability data:', error);
      setAssessmentError('Unable to load affordability data.');
      showError('Failed to load affordability data');
    }
  };

  const handleDeleteIncome = async (id: string) => {
    if (!confirm('Are you sure you want to delete this income source?')) return;
    
    try {
      await apiService.deleteIncome(id);
      success('Income source deleted successfully');
      await loadData(); // Reload to get updated assessment
    } catch (error) {
      console.error('Failed to delete income:', error);
      showError('Failed to delete income source');
    }
  };

  const handleDeleteExpense = async (id: string) => {
    if (!confirm('Are you sure you want to delete this expense?')) return;
    
    try {
      await apiService.deleteExpense(id);
      success('Expense deleted successfully');
      await loadData(); // Reload to get updated assessment
    } catch (error) {
      console.error('Failed to delete expense:', error);
      showError('Failed to delete expense');
    }
  };

  const handleAddIncome = async (data: any) => {
    try {
      await apiService.addIncome(data);
      setShowIncomeModal(false);
      success('Income source added successfully');
      await loadData();
    } catch (error) {
      console.error('Failed to add income:', error);
      throw error; // Re-throw so modal can show error
    }
  };

  const handleAddExpense = async (data: any) => {
    try {
      await apiService.addExpense(data);
      setShowExpenseModal(false);
      success('Expense added successfully');
      await loadData();
    } catch (error) {
      console.error('Failed to add expense:', error);
      throw error; // Re-throw so modal can show error
    }
  };

  const handleEditIncome = async (id: string, data: any) => {
    await apiService.updateIncome(id, data);
    success('Income source updated successfully');
    await loadData();
    setEditingIncome(null);
  };

  const handleEditExpense = async (id: string, data: any) => {
    await apiService.updateExpense(id, data);
    success('Expense updated successfully');
    await loadData();
    setEditingExpense(null);
  };



  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-ZA', {
      style: 'currency',
      currency: 'ZAR',
      minimumFractionDigits: 0,
      maximumFractionDigits: 0,
    }).format(amount);
  };

  const formatPercentage = (ratio: number) => {
    return `${(ratio * 100).toFixed(1)}%`;
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Good': return 'text-green-600 bg-green-100';
      case 'Fair': return 'text-yellow-600 bg-yellow-100';
      case 'Poor': return 'text-red-600 bg-red-100';
      default: return 'text-gray-600 bg-gray-100';
    }
  };

  return (
    <div>
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-gray-900">Affordability Assessment</h1>
        <p className="text-gray-600">Manage your income, expenses and assess your loan affordability</p>
      </div>

      {assessmentError && (
        <div className="mb-4 border border-yellow-200 bg-yellow-50 text-yellow-800 px-4 py-3 rounded-lg">
          {assessmentError}
        </div>
      )}

      {/* Income and Expenses Side by Side */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6">
        {/* Income Section */}
        <div className="card">
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center">
              <BanknotesIcon className="w-6 h-6 text-green-600 mr-2" />
              <h2 className="text-lg font-semibold text-gray-900">Income Sources</h2>
            </div>
            <button
              onClick={() => setShowIncomeModal(true)}
              className="btn btn-sm btn-primary inline-flex items-center"
            >
              <PlusIcon className="w-4 h-4 mr-1" />
              Add Income
            </button>
          </div>

          {incomes.length === 0 ? (
            <div className="text-center py-8 text-gray-500">
              <BanknotesIcon className="w-12 h-12 mx-auto mb-2 text-gray-400" />
              <p className="text-sm">No income sources added yet</p>
            </div>
          ) : (
            <div className="space-y-3">
              {incomes.map((income: Income) => (
                <div key={income.id} className="border border-gray-200 rounded-lg p-4 hover:bg-gray-50 transition-colors">
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <div className="flex items-center gap-2">
                        <h3 className="font-medium text-gray-900">{income.description}</h3>
                        {income.isVerified && (
                          <span className="text-xs bg-green-100 text-green-800 px-2 py-0.5 rounded">Verified</span>
                        )}
                      </div>
                      <p className="text-sm text-gray-500 mt-1">{income.sourceType} • {income.frequency}</p>
                      <p className="text-lg font-semibold text-green-600 mt-2">{formatCurrency(income.monthlyAmount)}/mo</p>
                    </div>
                    <div className="flex gap-2">
                      <button 
                        onClick={() => setEditingIncome(income)}
                        className="text-gray-400 hover:text-blue-600"
                      >
                        <PencilIcon className="w-4 h-4" />
                      </button>
                      <button 
                        onClick={() => handleDeleteIncome(income.id)}
                        className="text-gray-400 hover:text-red-600"
                      >
                        <TrashIcon className="w-4 h-4" />
                      </button>
                    </div>
                  </div>
                </div>
              ))}
              <div className="pt-3 border-t border-gray-200">
                <div className="flex justify-between items-center">
                  <span className="text-sm font-medium text-gray-700">Total Monthly Income</span>
                  <span className="text-lg font-bold text-green-600">
                    {formatCurrency(incomes.reduce((sum, inc) => sum + inc.monthlyAmount, 0))}
                  </span>
                </div>
              </div>
            </div>
          )}
        </div>

        {/* Expenses Section */}
        <div className="card">
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center">
              <CreditCardIcon className="w-6 h-6 text-red-600 mr-2" />
              <h2 className="text-lg font-semibold text-gray-900">Monthly Expenses</h2>
            </div>
            <button
              onClick={() => setShowExpenseModal(true)}
              className="btn btn-sm btn-primary inline-flex items-center"
            >
              <PlusIcon className="w-4 h-4 mr-1" />
              Add Expense
            </button>
          </div>

          {expenses.length === 0 ? (
            <div className="text-center py-8 text-gray-500">
              <CreditCardIcon className="w-12 h-12 mx-auto mb-2 text-gray-400" />
              <p className="text-sm">No expenses added yet</p>
            </div>
          ) : (
            <div className="space-y-3">
              {expenses.map((expense: Expense) => (
                <div key={expense.id} className="border border-gray-200 rounded-lg p-4 hover:bg-gray-50 transition-colors">
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <div className="flex items-center gap-2">
                        <h3 className="font-medium text-gray-900">{expense.description}</h3>
                        {expense.isEssential && (
                          <span className="text-xs bg-orange-100 text-orange-800 px-2 py-0.5 rounded">Essential</span>
                        )}
                      </div>
                      <p className="text-sm text-gray-500 mt-1">{expense.category} • {expense.frequency}</p>
                      <p className="text-lg font-semibold text-red-600 mt-2">{formatCurrency(expense.monthlyAmount)}/mo</p>
                    </div>
                    <div className="flex gap-2">
                      <button 
                        onClick={() => setEditingExpense(expense)}
                        className="text-gray-400 hover:text-blue-600"
                      >
                        <PencilIcon className="w-4 h-4" />
                      </button>
                      <button 
                        onClick={() => handleDeleteExpense(expense.id)}
                        className="text-gray-400 hover:text-red-600"
                      >
                        <TrashIcon className="w-4 h-4" />
                      </button>
                    </div>
                  </div>
                </div>
              ))}
              <div className="pt-3 border-t border-gray-200">
                <div className="flex justify-between items-center mb-2">
                  <span className="text-sm text-gray-600">Essential Expenses</span>
                  <span className="text-sm font-medium text-gray-900">
                    {formatCurrency(expenses.filter(e => e.isEssential).reduce((sum, exp) => sum + exp.monthlyAmount, 0))}
                  </span>
                </div>
                <div className="flex justify-between items-center mb-2">
                  <span className="text-sm text-gray-600">Non-Essential Expenses</span>
                  <span className="text-sm font-medium text-gray-900">
                    {formatCurrency(expenses.filter(e => !e.isEssential).reduce((sum, exp) => sum + exp.monthlyAmount, 0))}
                  </span>
                </div>
                <div className="flex justify-between items-center pt-2 border-t border-gray-200">
                  <span className="text-sm font-medium text-gray-700">Total Monthly Expenses</span>
                  <span className="text-lg font-bold text-red-600">
                    {formatCurrency(expenses.reduce((sum, exp) => sum + exp.monthlyAmount, 0))}
                  </span>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Affordability Assessment - Full Width */}
      {assessment && (
        <div className="card">
          <div className="flex items-center mb-6">
            <ChartBarIcon className="w-6 h-6 text-blue-600 mr-2" />
            <h2 className="text-xl font-semibold text-gray-900">Affordability Assessment</h2>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-6">
            <div className="bg-green-50 border border-green-200 rounded-lg p-4">
              <p className="text-sm text-green-700 font-medium mb-1">Total Monthly Income</p>
              <p className="text-2xl font-bold text-green-700">{formatCurrency(assessment.grossMonthlyIncome)}</p>
            </div>
            <div className="bg-red-50 border border-red-200 rounded-lg p-4">
              <p className="text-sm text-red-700 font-medium mb-1">Total Monthly Expenses</p>
              <p className="text-2xl font-bold text-red-700">{formatCurrency(assessment.totalMonthlyExpenses)}</p>
            </div>
            <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
              <p className="text-sm text-blue-700 font-medium mb-1">Available Funds</p>
              <p className="text-2xl font-bold text-blue-700">{formatCurrency(assessment.availableFunds)}</p>
            </div>
            <div className={`border rounded-lg p-4 ${getStatusColor(assessment.affordabilityStatus)}`}>
              <p className="text-sm font-medium mb-1">Affordability Status</p>
              <p className="text-2xl font-bold">{assessment.affordabilityStatus}</p>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-6">
            <div className="bg-gray-50 rounded-lg p-4">
              <p className="text-sm text-gray-600 mb-2">Expense to Income Ratio</p>
              <div className="flex items-end gap-2">
                <p className="text-xl font-bold text-gray-900">{formatPercentage(assessment.expenseToIncomeRatio)}</p>
                <p className="text-xs text-gray-500 mb-1">of income spent</p>
              </div>
              <div className="mt-3 bg-gray-200 rounded-full h-2">
                <div 
                  className={`h-2 rounded-full ${assessment.expenseToIncomeRatio > 0.7 ? 'bg-red-500' : assessment.expenseToIncomeRatio > 0.5 ? 'bg-yellow-500' : 'bg-green-500'}`}
                  style={{ width: `${Math.min(assessment.expenseToIncomeRatio * 100, 100)}%` }}
                />
              </div>
            </div>
            <div className="bg-gray-50 rounded-lg p-4">
              <p className="text-sm text-gray-600 mb-2">Essential vs Non-Essential</p>
              <div className="flex gap-4 mt-3">
                <div className="flex-1">
                  <p className="text-xs text-gray-500">Essential</p>
                  <p className="text-sm font-semibold text-gray-900">{formatCurrency(assessment.essentialExpenses)}</p>
                </div>
                <div className="flex-1">
                  <p className="text-xs text-gray-500">Non-Essential</p>
                  <p className="text-sm font-semibold text-gray-900">{formatCurrency(assessment.nonEssentialExpenses)}</p>
                </div>
              </div>
            </div>
            <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
              <p className="text-sm text-blue-700 mb-2">Max Recommended Loan</p>
              <p className="text-xl font-bold text-blue-700">{formatCurrency(assessment.maxRecommendedLoanAmount)}</p>
              <p className="text-xs text-blue-600 mt-1">Based on 3-year term</p>
            </div>
          </div>

          {assessment.assessmentNotes && (
            <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
              <p className="text-sm font-medium text-yellow-800 mb-1">Assessment Notes</p>
              <p className="text-sm text-yellow-700">{assessment.assessmentNotes}</p>
            </div>
          )}

          <div className="mt-6 pt-6 border-t border-gray-200">
            <h3 className="font-medium text-gray-900 mb-3">Recommendations</h3>
            <ul className="space-y-2 text-sm text-gray-600">
              {assessment.expenseToIncomeRatio > 0.7 && (
                <li className="flex items-start">
                  <span className="text-red-500 mr-2">•</span>
                  Your expenses are high relative to income. Consider reducing non-essential expenses.
                </li>
              )}
              {assessment.availableFunds < 5000 && (
                <li className="flex items-start">
                  <span className="text-yellow-500 mr-2">•</span>
                  Limited available funds. Build an emergency fund before taking on new debt.
                </li>
              )}
              {assessment.affordabilityStatus === 'Good' && (
                <li className="flex items-start">
                  <span className="text-green-500 mr-2">•</span>
                  Your financial position is healthy. You may qualify for favorable loan terms.
                </li>
              )}
            </ul>
          </div>
        </div>
      )}

      {/* Modals */}
      <AddIncomeModal
        isOpen={showIncomeModal}
        onClose={() => setShowIncomeModal(false)}
        onSubmit={handleAddIncome}
      />
      <AddExpenseModal
        isOpen={showExpenseModal}
        onClose={() => setShowExpenseModal(false)}
        onSubmit={handleAddExpense}
      />
      <EditIncomeModal
        isOpen={!!editingIncome}
        onClose={() => setEditingIncome(null)}
        onSubmit={handleEditIncome}
        income={editingIncome}
      />
      <EditExpenseModal
        isOpen={!!editingExpense}
        onClose={() => setEditingExpense(null)}
        onSubmit={handleEditExpense}
        expense={editingExpense}
      />
    </div>
  );
};

export default Affordability;
