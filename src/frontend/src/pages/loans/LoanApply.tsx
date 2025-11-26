import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ArrowLeftIcon } from '@heroicons/react/24/outline';
import { apiService } from '../../services/api';

const LoanApply: React.FC = () => {
  const navigate = useNavigate();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [formData, setFormData] = useState({
    amount: '',
    termMonths: '12',
    purpose: '',
  });

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!formData.amount || !formData.termMonths || !formData.purpose) {
      setError('Please fill in all required fields');
      return;
    }

    setIsSubmitting(true);
    try {
      const response = await apiService.createLoanApplication({
        amount: parseFloat(formData.amount),
        termMonths: parseInt(formData.termMonths),
        purpose: formData.purpose,
      });
      
      // Redirect to the loan detail page
      navigate(`/loans/${response.id}`);
    } catch (err) {
      console.error('Failed to create loan application:', err);
      setError(err instanceof Error ? err.message : 'Failed to create loan application');
    } finally {
      setIsSubmitting(false);
    }
  };

  const formatCurrency = (amount: string) => {
    const num = parseFloat(amount);
    if (isNaN(num)) return '';
    return new Intl.NumberFormat('en-ZA', {
      style: 'currency',
      currency: 'ZAR',
      minimumFractionDigits: 0,
      maximumFractionDigits: 0,
    }).format(num);
  };

  const estimateMonthlyPayment = () => {
    const principal = parseFloat(formData.amount);
    const months = parseInt(formData.termMonths);
    if (isNaN(principal) || isNaN(months) || principal <= 0 || months <= 0) return null;

    const annualRate = 0.12; // 12% base rate
    const monthlyRate = annualRate / 12;
    const payment = principal * (monthlyRate * Math.pow(1 + monthlyRate, months)) /
                   (Math.pow(1 + monthlyRate, months) - 1);
    
    return payment;
  };

  const monthlyPayment = estimateMonthlyPayment();

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
        <h1 className="text-2xl font-bold text-gray-900">Apply for a Loan</h1>
        <p className="text-gray-600">Complete the form below to submit your loan application</p>
      </div>

      <div className="max-w-2xl">
        {error && (
          <div className="mb-4 rounded-md bg-red-50 border border-red-200 p-4">
            <p className="text-sm text-red-700">{error}</p>
          </div>
        )}

        <form onSubmit={handleSubmit} className="card space-y-6">
          <div>
            <label htmlFor="amount" className="block text-sm font-medium text-gray-700 mb-2">
              Loan Amount <span className="text-red-500">*</span>
            </label>
            <div className="relative">
              <span className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-500">R</span>
              <input
                type="number"
                id="amount"
                name="amount"
                min="1000"
                max="500000"
                step="1000"
                required
                className="w-full pl-8 pr-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="50000"
                value={formData.amount}
                onChange={handleChange}
              />
            </div>
            <p className="mt-1 text-xs text-gray-500">Minimum: R1,000 | Maximum: R500,000</p>
          </div>

          <div>
            <label htmlFor="termMonths" className="block text-sm font-medium text-gray-700 mb-2">
              Loan Term <span className="text-red-500">*</span>
            </label>
            <select
              id="termMonths"
              name="termMonths"
              required
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={formData.termMonths}
              onChange={handleChange}
            >
              <option value="6">6 months</option>
              <option value="12">12 months</option>
              <option value="18">18 months</option>
              <option value="24">24 months</option>
              <option value="36">36 months</option>
              <option value="48">48 months</option>
              <option value="60">60 months</option>
            </select>
          </div>

          <div>
            <label htmlFor="purpose" className="block text-sm font-medium text-gray-700 mb-2">
              Purpose of Loan <span className="text-red-500">*</span>
            </label>
            <textarea
              id="purpose"
              name="purpose"
              rows={3}
              required
              maxLength={200}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="e.g., Home improvement, Debt consolidation, Education, etc."
              value={formData.purpose}
              onChange={handleChange}
            />
            <p className="mt-1 text-xs text-gray-500">{formData.purpose.length}/200 characters</p>
          </div>

          {monthlyPayment && (
            <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
              <h3 className="text-sm font-medium text-blue-900 mb-3">Estimated Payment</h3>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <p className="text-xs text-blue-700">Monthly Payment</p>
                  <p className="text-lg font-bold text-blue-900">{formatCurrency(monthlyPayment.toString())}</p>
                </div>
                <div>
                  <p className="text-xs text-blue-700">Total Repayable</p>
                  <p className="text-lg font-bold text-blue-900">
                    {formatCurrency((monthlyPayment * parseInt(formData.termMonths)).toString())}
                  </p>
                </div>
              </div>
              <p className="mt-3 text-xs text-blue-600">
                *Estimated at 12% annual interest rate. Final rate may vary based on affordability assessment.
              </p>
            </div>
          )}

          <div className="flex gap-3 pt-4">
            <button
              type="button"
              onClick={() => navigate('/loans')}
              className="flex-1 px-4 py-2 border border-gray-300 rounded-md text-gray-700 hover:bg-gray-50 font-medium"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={isSubmitting}
              className="flex-1 px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 font-medium disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isSubmitting ? 'Submitting...' : 'Submit Application'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default LoanApply;
