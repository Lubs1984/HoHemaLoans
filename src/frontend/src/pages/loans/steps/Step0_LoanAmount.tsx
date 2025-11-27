import React, { useState, useEffect } from 'react';

interface Step0Props {
  data: any;
  onNext: (data: any) => void;
  onPrev?: () => void;
  loading: boolean;
}

const Step0_LoanAmount: React.FC<Step0Props> = ({ data, onNext, loading }) => {
  const [amount, setAmount] = useState(data.amount || 5000);
  const [monthlyPayment, setMonthlyPayment] = useState(0);

  // Calculate estimated monthly payment
  useEffect(() => {
    const interestRate = 0.12; // 12% annual
    const months = data.termMonths || 12;
    const monthlyRate = interestRate / 12;
    const payment =
      amount * (monthlyRate * Math.pow(1 + monthlyRate, months)) /
      (Math.pow(1 + monthlyRate, months) - 1);
    setMonthlyPayment(Math.round(payment));
  }, [amount, data.termMonths]);

  const handleNext = () => {
    if (amount < 500 || amount > 50000) {
      alert('Loan amount must be between R500 and R50,000');
      return;
    }
    onNext({ amount });
  };

  return (
    <div className="space-y-6">
      <div className="text-center">
        <h3 className="text-4xl font-bold text-blue-600">R {amount.toLocaleString()}</h3>
        <p className="text-gray-600 mt-2">Estimated monthly payment: R {monthlyPayment.toLocaleString()}</p>
      </div>

      <div className="space-y-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Select loan amount
          </label>
          <input
            type="range"
            min="500"
            max="50000"
            step="100"
            value={amount}
            onChange={(e) => setAmount(parseInt(e.target.value))}
            className="w-full h-2 bg-blue-200 rounded-lg appearance-none cursor-pointer"
          />
          <div className="flex justify-between text-sm text-gray-600 mt-1">
            <span>R 500</span>
            <span>R 50,000</span>
          </div>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Or enter amount manually
          </label>
          <input
            type="number"
            min="500"
            max="50000"
            step="100"
            value={amount}
            onChange={(e) => setAmount(parseInt(e.target.value))}
            className="w-full border border-gray-300 rounded-lg px-4 py-3 text-lg font-semibold"
          />
        </div>
      </div>

      <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
        <p className="text-sm text-blue-800">
          <strong>Note:</strong> The final interest rate and monthly payment will be calculated based on your affordability assessment and loan term.
        </p>
      </div>

      <div className="flex justify-end">
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

export default Step0_LoanAmount;
