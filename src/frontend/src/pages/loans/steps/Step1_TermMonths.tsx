import React, { useState, useEffect } from 'react';

interface Step1Props {
  data: any;
  onNext: (data: any) => void;
  onPrev: () => void;
  loading: boolean;
}

const TERM_OPTIONS = [
  { months: 6, label: '6 Months', description: 'Short-term, higher monthly payments' },
  { months: 12, label: '12 Months', description: 'Most popular option' },
  { months: 24, label: '24 Months', description: 'Lower monthly payments' },
  { months: 36, label: '36 Months', description: 'Lowest monthly payments' },
];

const Step1_TermMonths: React.FC<Step1Props> = ({ data, onNext, onPrev, loading }) => {
  const [termMonths, setTermMonths] = useState(data.termMonths || 12);
  const [monthlyPayment, setMonthlyPayment] = useState(0);

  useEffect(() => {
    const interestRate = 0.12; // 12% annual
    const monthlyRate = interestRate / 12;
    const amount = data.amount || 5000;
    const payment =
      amount * (monthlyRate * Math.pow(1 + monthlyRate, termMonths)) /
      (Math.pow(1 + monthlyRate, termMonths) - 1);
    setMonthlyPayment(Math.round(payment));
  }, [termMonths, data.amount]);

  const handleNext = () => {
    onNext({ termMonths });
  };

  return (
    <div className="space-y-6">
      <div className="text-center">
        <p className="text-gray-600">Loan Amount: <span className="font-semibold text-gray-900">R {data.amount.toLocaleString()}</span></p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {TERM_OPTIONS.map((option) => {
          const isSelected = termMonths === option.months;
          const interestRate = 0.12;
          const monthlyRate = interestRate / 12;
          const payment =
            data.amount * (monthlyRate * Math.pow(1 + monthlyRate, option.months)) /
            (Math.pow(1 + monthlyRate, option.months) - 1);

          return (
            <button
              key={option.months}
              onClick={() => setTermMonths(option.months)}
              className={`p-6 rounded-lg border-2 text-left transition ${
                isSelected
                  ? 'border-blue-600 bg-blue-50'
                  : 'border-gray-200 hover:border-blue-300'
              }`}
            >
              <h4 className="text-xl font-bold text-gray-900">{option.label}</h4>
              <p className="text-sm text-gray-600 mt-1">{option.description}</p>
              <p className="text-lg font-semibold text-blue-600 mt-3">
                R {Math.round(payment).toLocaleString()} / month
              </p>
            </button>
          );
        })}
      </div>

      <div className="bg-gray-50 border border-gray-200 rounded-lg p-4">
        <div className="flex justify-between items-center">
          <span className="text-gray-700">Estimated monthly payment:</span>
          <span className="text-2xl font-bold text-blue-600">R {monthlyPayment.toLocaleString()}</span>
        </div>
      </div>

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
          {loading ? 'Saving...' : 'Next →'}
        </button>
      </div>
    </div>
  );
};

export default Step1_TermMonths;
