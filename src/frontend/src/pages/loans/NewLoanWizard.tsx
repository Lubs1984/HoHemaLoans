import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { apiService } from '../../services/api';

interface LoanCalculation {
  monthlyEarnings: number;
  maxLoanAmount: number;
  approvedLoanAmount: number;
  interestRate: number;
  interestAmount: number;
  adminFee: number;
  totalRepayment: number;
  isWithinLimits: boolean;
}

const NewLoanWizard: React.FC = () => {
  const navigate = useNavigate();
  const [currentStep, setCurrentStep] = useState(1);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Step 1: Earnings
  const [hoursWorked, setHoursWorked] = useState<string>('160');
  const [hourlyRate, setHourlyRate] = useState<string>('100');
  const [monthlyEarnings, setMonthlyEarnings] = useState<number>(0);

  // Step 2: Loan Amount
  const [requestedAmount, setRequestedAmount] = useState<string>('');
  const [calculation, setCalculation] = useState<LoanCalculation | null>(null);

  // Step 3: Repayment Date
  const [repaymentDay, setRepaymentDay] = useState<number>(30);

  // Step 4: Purpose
  const [purpose, setPurpose] = useState<string>('');

  // Step 5: Income/Expense Verification
  const [hasChanges, setHasChanges] = useState<boolean | null>(null);

  const loanPurposes = [
    'Emergency Expense',
    'Medical Bills',
    'Education',
    'Home Improvement',
    'Debt Consolidation',
    'Vehicle Repair',
    'Business Investment',
    'Other',
  ];

  useEffect(() => {
    if (hoursWorked && hourlyRate) {
      const hours = parseFloat(hoursWorked) || 0;
      const rate = parseFloat(hourlyRate) || 0;
      setMonthlyEarnings(hours * rate);
    }
  }, [hoursWorked, hourlyRate]);

  const calculateLoan = async () => {
    if (!requestedAmount || parseFloat(requestedAmount) <= 0) {
      setError('Please enter a valid loan amount');
      return;
    }

    try {
      setLoading(true);
      setError(null);
      const data = await apiService.request<LoanCalculation>('/systemsettings/calculate', {
        method: 'POST',
        body: JSON.stringify({
          hoursWorked: parseFloat(hoursWorked),
          hourlyRate: parseFloat(hourlyRate),
          requestedAmount: parseFloat(requestedAmount),
        }),
      });
      setCalculation(data);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to calculate loan');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (currentStep === 2 && requestedAmount) {
      calculateLoan();
    }
  }, [requestedAmount, currentStep]);

  const handleNext = async () => {
    setError(null);

    if (currentStep === 1) {
      if (!hoursWorked || !hourlyRate || monthlyEarnings <= 0) {
        setError('Please enter valid hours and hourly rate');
        return;
      }
      setCurrentStep(2);
    } else if (currentStep === 2) {
      if (!calculation) {
        setError('Please wait for loan calculation');
        return;
      }
      if (!calculation.isWithinLimits) {
        setError(
          `Requested amount exceeds maximum allowed (R${calculation.maxLoanAmount.toFixed(2)})`
        );
        return;
      }
      setCurrentStep(3);
    } else if (currentStep === 3) {
      if (!repaymentDay || repaymentDay < 25 || repaymentDay > 31) {
        setError('Please select a repayment day between 25-31');
        return;
      }
      setCurrentStep(4);
    } else if (currentStep === 4) {
      if (!purpose) {
        setError('Please select a loan purpose');
        return;
      }
      setCurrentStep(5);
    } else if (currentStep === 5) {
      if (hasChanges === null) {
        setError('Please indicate if your income/expenses have changed');
        return;
      }
      // If changes, redirect to income/expense management
      if (hasChanges) {
        navigate('/profile?tab=income');
        return;
      }
      // Otherwise, proceed to final submission
      await submitLoanApplication();
    }
  };

  const handleBack = () => {
    if (currentStep > 1) {
      setCurrentStep(currentStep - 1);
      setError(null);
    }
  };

  const submitLoanApplication = async () => {
    try {
      setLoading(true);
      setError(null);

      const applicationData = {
        hoursWorked: parseFloat(hoursWorked),
        hourlyRate: parseFloat(hourlyRate),
        monthlyEarnings: monthlyEarnings,
        amount: calculation?.approvedLoanAmount,
        maxLoanAmount: calculation?.maxLoanAmount,
        appliedInterestRate: calculation?.interestRate,
        appliedAdminFee: calculation?.adminFee,
        totalAmount: calculation?.totalRepayment,
        repaymentDay: repaymentDay,
        purpose: purpose,
        hasIncomeExpenseChanged: hasChanges,
        termMonths: 1, // Single month-end payment
      };

      await apiService.request('/loanapplications', {
        method: 'POST',
        body: JSON.stringify(applicationData),
      });

      // Success - redirect to applications list
      navigate('/loans', { state: { success: 'Loan application submitted successfully!' } });
    } catch (err: any) {
      setError(err.message || 'Failed to submit loan application');
      setLoading(false);
    }
  };

  const renderStepIndicator = () => {
    const steps = [
      'Earnings',
      'Loan Amount',
      'Repayment Date',
      'Purpose',
      'Verification',
    ];

    return (
      <div className="mb-8">
        <div className="flex items-center justify-between">
          {steps.map((step, index) => (
            <React.Fragment key={index}>
              <div className="flex flex-col items-center">
                <div
                  className={`w-10 h-10 rounded-full flex items-center justify-center font-semibold ${
                    index + 1 === currentStep
                      ? 'bg-blue-600 text-white'
                      : index + 1 < currentStep
                      ? 'bg-green-500 text-white'
                      : 'bg-gray-300 text-gray-600'
                  }`}
                >
                  {index + 1 < currentStep ? '✓' : index + 1}
                </div>
                <span className="text-xs mt-2 text-gray-600">{step}</span>
              </div>
              {index < steps.length - 1 && (
                <div
                  className={`flex-1 h-1 mx-2 ${
                    index + 1 < currentStep ? 'bg-green-500' : 'bg-gray-300'
                  }`}
                />
              )}
            </React.Fragment>
          ))}
        </div>
      </div>
    );
  };

  const renderStep1 = () => (
    <div className="space-y-6">
      <h2 className="text-2xl font-bold text-gray-800 mb-4">
        Step 1: Your Earnings
      </h2>
      <p className="text-gray-600 mb-6">
        Tell us about your work hours and hourly rate to calculate your monthly earnings.
      </p>

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">
          Hours Worked per Month
        </label>
        <input
          type="number"
          step="0.5"
          min="0"
          value={hoursWorked}
          onChange={(e) => setHoursWorked(e.target.value)}
          className="w-full px-4 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          placeholder="e.g., 160"
        />
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">
          Hourly Rate (R)
        </label>
        <input
          type="number"
          step="0.01"
          min="0"
          value={hourlyRate}
          onChange={(e) => setHourlyRate(e.target.value)}
          className="w-full px-4 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          placeholder="e.g., 100"
        />
      </div>

      {monthlyEarnings > 0 && (
        <div className="bg-blue-50 border border-blue-200 rounded-md p-4">
          <p className="text-sm text-gray-600">Your Monthly Earnings:</p>
          <p className="text-3xl font-bold text-blue-600">
            R{monthlyEarnings.toFixed(2)}
          </p>
          <p className="text-xs text-gray-500 mt-1">
            {hoursWorked} hours × R{hourlyRate}/hour
          </p>
        </div>
      )}
    </div>
  );

  const renderStep2 = () => (
    <div className="space-y-6">
      <h2 className="text-2xl font-bold text-gray-800 mb-4">
        Step 2: Loan Amount
      </h2>
      <p className="text-gray-600 mb-6">
        How much would you like to borrow? You can borrow up to 20% of your monthly earnings.
      </p>

      <div className="bg-gray-50 border border-gray-200 rounded-md p-4 mb-4">
        <p className="text-sm text-gray-600">Your Monthly Earnings:</p>
        <p className="text-2xl font-bold text-gray-800">R{monthlyEarnings.toFixed(2)}</p>
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">
          Requested Loan Amount (R)
        </label>
        <input
          type="number"
          step="100"
          min="100"
          value={requestedAmount}
          onChange={(e) => setRequestedAmount(e.target.value)}
          className="w-full px-4 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          placeholder="Enter amount"
        />
      </div>

      {calculation && (
        <div className={`border rounded-md p-4 ${
          calculation.isWithinLimits
            ? 'bg-green-50 border-green-200'
            : 'bg-red-50 border-red-200'
        }`}>
          <h3 className="font-semibold text-gray-800 mb-3">Loan Breakdown:</h3>
          <div className="space-y-2 text-sm">
            <div className="flex justify-between">
              <span className="text-gray-600">Maximum Eligible:</span>
              <span className="font-medium">R{calculation.maxLoanAmount.toFixed(2)}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-gray-600">Loan Amount:</span>
              <span className="font-medium">R{calculation.approvedLoanAmount.toFixed(2)}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-gray-600">Interest ({calculation.interestRate}%):</span>
              <span className="font-medium">R{calculation.interestAmount.toFixed(2)}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-gray-600">Admin Fee:</span>
              <span className="font-medium">R{calculation.adminFee.toFixed(2)}</span>
            </div>
            <div className="flex justify-between border-t border-gray-300 pt-2 mt-2">
              <span className="font-semibold text-gray-800">Total Repayment:</span>
              <span className="font-bold text-lg text-blue-600">
                R{calculation.totalRepayment.toFixed(2)}
              </span>
            </div>
          </div>
          {!calculation.isWithinLimits && (
            <p className="text-red-600 text-sm mt-3">
              ⚠️ Requested amount exceeds your maximum eligible loan amount.
            </p>
          )}
        </div>
      )}
    </div>
  );

  const renderStep3 = () => (
    <div className="space-y-6">
      <h2 className="text-2xl font-bold text-gray-800 mb-4">
        Step 3: Repayment Date
      </h2>
      <p className="text-gray-600 mb-6">
        Select your preferred repayment date (between 25th and 31st of the month).
      </p>

      {calculation && (
        <div className="bg-blue-50 border border-blue-200 rounded-md p-4 mb-4">
          <p className="text-sm text-gray-600">Total to Repay:</p>
          <p className="text-3xl font-bold text-blue-600">
            R{calculation.totalRepayment.toFixed(2)}
          </p>
        </div>
      )}

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">
          Repayment Day of Month
        </label>
        <select
          value={repaymentDay}
          onChange={(e) => setRepaymentDay(parseInt(e.target.value))}
          className="w-full px-4 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-transparent"
        >
          {[25, 26, 27, 28, 29, 30, 31].map((day) => (
            <option key={day} value={day}>
              {day}th of the month
            </option>
          ))}
        </select>
        <p className="text-xs text-gray-500 mt-2">
          Choose a date that aligns with when you typically receive your pay.
        </p>
      </div>

      <div className="bg-yellow-50 border border-yellow-200 rounded-md p-4">
        <p className="text-sm text-gray-700">
          <strong>Note:</strong> Your loan will be due on the <strong>{repaymentDay}th</strong> of next month.
          Make sure you have sufficient funds available on this date.
        </p>
      </div>
    </div>
  );

  const renderStep4 = () => (
    <div className="space-y-6">
      <h2 className="text-2xl font-bold text-gray-800 mb-4">
        Step 4: Loan Purpose
      </h2>
      <p className="text-gray-600 mb-6">
        Please tell us what you'll be using this loan for.
      </p>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
        {loanPurposes.map((purposeOption) => (
          <button
            key={purposeOption}
            onClick={() => setPurpose(purposeOption)}
            className={`p-4 border-2 rounded-md text-left transition ${
              purpose === purposeOption
                ? 'border-blue-500 bg-blue-50'
                : 'border-gray-300 hover:border-blue-300'
            }`}
          >
            <div className="flex items-center">
              <div
                className={`w-5 h-5 rounded-full border-2 mr-3 flex items-center justify-center ${
                  purpose === purposeOption
                    ? 'border-blue-500 bg-blue-500'
                    : 'border-gray-300'
                }`}
              >
                {purpose === purposeOption && (
                  <div className="w-2 h-2 bg-white rounded-full" />
                )}
              </div>
              <span className="font-medium text-gray-800">{purposeOption}</span>
            </div>
          </button>
        ))}
      </div>
    </div>
  );

  const renderStep5 = () => (
    <div className="space-y-6">
      <h2 className="text-2xl font-bold text-gray-800 mb-4">
        Step 5: Income & Expense Verification
      </h2>
      <p className="text-gray-600 mb-6">
        Before we finalize your application, we need to verify your financial information.
      </p>

      <div className="bg-gray-50 border border-gray-200 rounded-md p-6">
        <h3 className="font-semibold text-gray-800 mb-4 text-lg">
          Has anything changed with your income or expenses since your last update?
        </h3>
        <div className="space-y-3">
          <button
            onClick={() => setHasChanges(false)}
            className={`w-full p-4 border-2 rounded-md text-left transition ${
              hasChanges === false
                ? 'border-green-500 bg-green-50'
                : 'border-gray-300 hover:border-green-300'
            }`}
          >
            <div className="flex items-center">
              <div
                className={`w-6 h-6 rounded-full border-2 mr-3 flex items-center justify-center ${
                  hasChanges === false
                    ? 'border-green-500 bg-green-500'
                    : 'border-gray-300'
                }`}
              >
                {hasChanges === false && <span className="text-white text-sm">✓</span>}
              </div>
              <div>
                <p className="font-medium text-gray-800">No, everything is up to date</p>
                <p className="text-sm text-gray-600">My income and expenses haven't changed</p>
              </div>
            </div>
          </button>

          <button
            onClick={() => setHasChanges(true)}
            className={`w-full p-4 border-2 rounded-md text-left transition ${
              hasChanges === true
                ? 'border-blue-500 bg-blue-50'
                : 'border-gray-300 hover:border-blue-300'
            }`}
          >
            <div className="flex items-center">
              <div
                className={`w-6 h-6 rounded-full border-2 mr-3 flex items-center justify-center ${
                  hasChanges === true
                    ? 'border-blue-500 bg-blue-500'
                    : 'border-gray-300'
                }`}
              >
                {hasChanges === true && <span className="text-white text-sm">✓</span>}
              </div>
              <div>
                <p className="font-medium text-gray-800">Yes, I need to update my information</p>
                <p className="text-sm text-gray-600">I'll update my income/expenses before continuing</p>
              </div>
            </div>
          </button>
        </div>

        {hasChanges === true && (
          <div className="mt-4 p-4 bg-blue-50 border border-blue-200 rounded-md">
            <p className="text-sm text-blue-800">
              <strong>Next:</strong> You'll be redirected to update your income and expenses.
              After updating, you can complete your loan application.
            </p>
          </div>
        )}
      </div>

      {calculation && hasChanges === false && (
        <div className="bg-green-50 border border-green-200 rounded-md p-6">
          <h3 className="font-semibold text-gray-800 mb-4">Application Summary:</h3>
          <div className="space-y-2 text-sm">
            <div className="flex justify-between">
              <span className="text-gray-600">Monthly Earnings:</span>
              <span className="font-medium">R{monthlyEarnings.toFixed(2)}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-gray-600">Loan Amount:</span>
              <span className="font-medium">R{calculation.approvedLoanAmount.toFixed(2)}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-gray-600">Repayment Date:</span>
              <span className="font-medium">{repaymentDay}th of next month</span>
            </div>
            <div className="flex justify-between">
              <span className="text-gray-600">Purpose:</span>
              <span className="font-medium">{purpose}</span>
            </div>
            <div className="flex justify-between border-t border-gray-300 pt-2 mt-2">
              <span className="font-semibold text-gray-800">Total to Repay:</span>
              <span className="font-bold text-lg text-green-600">
                R{calculation.totalRepayment.toFixed(2)}
              </span>
            </div>
          </div>
        </div>
      )}
    </div>
  );

  return (
    <div className="max-w-4xl mx-auto p-6">
      <div className="bg-white shadow-md rounded-lg p-8">
        <h1 className="text-3xl font-bold text-gray-800 mb-2">Apply for a Loan</h1>
        <p className="text-gray-600 mb-8">
          Complete the steps below to apply for your loan.
        </p>

        {renderStepIndicator()}

        {error && (
          <div className="mb-6 p-4 bg-red-100 border border-red-400 text-red-700 rounded">
            {error}
          </div>
        )}

        <div className="min-h-[400px]">
          {currentStep === 1 && renderStep1()}
          {currentStep === 2 && renderStep2()}
          {currentStep === 3 && renderStep3()}
          {currentStep === 4 && renderStep4()}
          {currentStep === 5 && renderStep5()}
        </div>

        <div className="flex justify-between mt-8 pt-6 border-t border-gray-200">
          <button
            onClick={handleBack}
            disabled={currentStep === 1 || loading}
            className="px-6 py-2 border border-gray-300 rounded-md text-gray-700 hover:bg-gray-50 transition disabled:opacity-50 disabled:cursor-not-allowed"
          >
            Back
          </button>
          <button
            onClick={handleNext}
            disabled={loading}
            className="px-6 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition disabled:bg-gray-400"
          >
            {loading ? 'Processing...' : currentStep === 5 ? 'Submit Application' : 'Next'}
          </button>
        </div>
      </div>
    </div>
  );
};

export default NewLoanWizard;
