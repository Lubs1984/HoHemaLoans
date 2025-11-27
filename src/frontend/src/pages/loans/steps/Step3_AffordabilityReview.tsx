import React, { useEffect, useState } from 'react';
import { apiService } from '../../../services/api';

interface Step3Props {
  data: any;
  onNext: (data: any) => void;
  onPrev: () => void;
  loading: boolean;
}

interface AffordabilityAssessment {
  canAfford: boolean;
  maxAffordableAmount: number;
  monthlyIncome: number;
  monthlyExpenses: number;
  disposableIncome: number;
  message: string;
}

const Step3_AffordabilityReview: React.FC<Step3Props> = ({ data, onNext, onPrev, loading }) => {
  const [assessment, setAssessment] = useState<AffordabilityAssessment | null>(null);
  const [assessmentLoading, setAssessmentLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchAffordabilityAssessment();
  }, []);

  const fetchAffordabilityAssessment = async () => {
    try {
      setAssessmentLoading(true);
      const response = await apiService.request<AffordabilityAssessment>('/affordability/assessment');
      setAssessment(response);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load affordability assessment');
    } finally {
      setAssessmentLoading(false);
    }
  };

  const estimatedMonthlyPayment = React.useMemo(() => {
    const interestRate = 0.12;
    const monthlyRate = interestRate / 12;
    const months = data.termMonths;
    const amount = data.amount;
    return Math.round(
      amount * (monthlyRate * Math.pow(1 + monthlyRate, months)) /
      (Math.pow(1 + monthlyRate, months) - 1)
    );
  }, [data.amount, data.termMonths]);

  const handleNext = () => {
    if (!assessment?.canAfford && data.amount > (assessment?.maxAffordableAmount || 0)) {
      const proceed = confirm(
        'Our assessment shows you may not be able to afford this loan. Do you want to continue anyway? ' +
        'This may affect your approval chances.'
      );
      if (!proceed) return;
    }
    onNext({});
  };

  if (assessmentLoading) {
    return (
      <div className="flex justify-center items-center py-12">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (error || !assessment) {
    return (
      <div className="space-y-6">
        <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
          <p className="text-yellow-800">
            {error || 'Unable to load affordability assessment. Please complete your income and expenses information first.'}
          </p>
        </div>
        <div className="flex justify-between">
          <button
            onClick={onPrev}
            className="bg-gray-300 hover:bg-gray-400 text-gray-800 px-8 py-3 rounded-lg font-medium"
          >
            ← Back
          </button>
          <a
            href="/affordability"
            className="bg-blue-600 hover:bg-blue-700 text-white px-8 py-3 rounded-lg font-medium"
          >
            Complete Affordability →
          </a>
        </div>
      </div>
    );
  }

  const canAffordLoan = assessment.disposableIncome >= estimatedMonthlyPayment;

  return (
    <div className="space-y-6">
      <div className="text-center">
        <p className="text-gray-600">
          Loan Amount: <span className="font-semibold">R {data.amount.toLocaleString()}</span> | 
          Term: <span className="font-semibold">{data.termMonths} months</span>
        </p>
      </div>

      <div className={`border-2 rounded-lg p-6 ${canAffordLoan ? 'border-green-500 bg-green-50' : 'border-yellow-500 bg-yellow-50'}`}>
        <div className="flex items-center space-x-3 mb-4">
          <div className={`text-4xl ${canAffordLoan ? 'text-green-600' : 'text-yellow-600'}`}>
            {canAffordLoan ? '✓' : '⚠️'}
          </div>
          <div>
            <h3 className="text-xl font-bold text-gray-900">
              {canAffordLoan ? 'You can afford this loan!' : 'Affordability Check'}
            </h3>
            <p className={canAffordLoan ? 'text-green-700' : 'text-yellow-700'}>
              {canAffordLoan 
                ? 'Based on your income and expenses, you should be able to afford this loan.' 
                : 'This loan may stretch your budget. Consider a smaller amount or longer term.'}
            </p>
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div className="bg-gray-50 border border-gray-200 rounded-lg p-4">
          <p className="text-sm text-gray-600">Monthly Income</p>
          <p className="text-2xl font-bold text-gray-900">R {assessment.monthlyIncome.toLocaleString()}</p>
        </div>
        <div className="bg-gray-50 border border-gray-200 rounded-lg p-4">
          <p className="text-sm text-gray-600">Monthly Expenses</p>
          <p className="text-2xl font-bold text-gray-900">R {assessment.monthlyExpenses.toLocaleString()}</p>
        </div>
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
          <p className="text-sm text-blue-600">Disposable Income</p>
          <p className="text-2xl font-bold text-blue-900">R {assessment.disposableIncome.toLocaleString()}</p>
        </div>
        <div className="bg-purple-50 border border-purple-200 rounded-lg p-4">
          <p className="text-sm text-purple-600">Estimated Monthly Payment</p>
          <p className="text-2xl font-bold text-purple-900">R {estimatedMonthlyPayment.toLocaleString()}</p>
        </div>
      </div>

      {!canAffordLoan && (
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
          <p className="text-sm text-blue-800">
            <strong>Recommendation:</strong> Based on your disposable income of R {assessment.disposableIncome.toLocaleString()}, 
            we recommend a maximum loan amount of R {Math.floor(assessment.maxAffordableAmount).toLocaleString()}.
          </p>
        </div>
      )}

      <div className="flex justify-between">
        <button
          onClick={onPrev}
          className="bg-gray-300 hover:bg-gray-400 text-gray-800 px-8 py-3 rounded-lg font-medium"
        >
          ← Back
        </button>
        <button
          onClick={handleNext}
          disabled={loading}
          className="bg-blue-600 hover:bg-blue-700 text-white px-8 py-3 rounded-lg font-medium disabled:opacity-50"
        >
          {loading ? 'Saving...' : 'Continue →'}
        </button>
      </div>
    </div>
  );
};

export default Step3_AffordabilityReview;
