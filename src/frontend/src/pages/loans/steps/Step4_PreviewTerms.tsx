import React from 'react';

interface Step4Props {
  data: any;
  onNext: (data: any) => void;
  onPrev: () => void;
  loading: boolean;
}

const Step4_PreviewTerms: React.FC<Step4Props> = ({ data, onNext, onPrev, loading }) => {
  // Calculate loan details
  const interestRate = data.interestRate || 12; // 12% default
  const monthlyRate = interestRate / 100 / 12;
  const months = data.termMonths;
  const principal = data.amount;
  
  const monthlyPayment = data.monthlyPayment || Math.round(
    principal * (monthlyRate * Math.pow(1 + monthlyRate, months)) /
    (Math.pow(1 + monthlyRate, months) - 1)
  );
  
  const totalAmount = data.totalAmount || monthlyPayment * months;
  const totalInterest = totalAmount - principal;
  const initiationFee = principal * 0.015; // 1.5% initiation fee (NCR compliant)

  const handleNext = () => {
    onNext({});
  };

  return (
    <div className="space-y-6">
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-6">
        <h3 className="text-xl font-bold text-blue-900 mb-2">Loan Summary</h3>
        <p className="text-blue-700">Please review your loan details carefully before proceeding.</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div className="bg-gray-50 border border-gray-200 rounded-lg p-4">
          <p className="text-sm text-gray-600">Loan Amount</p>
          <p className="text-2xl font-bold text-gray-900">R {principal.toLocaleString()}</p>
        </div>
        <div className="bg-gray-50 border border-gray-200 rounded-lg p-4">
          <p className="text-sm text-gray-600">Loan Term</p>
          <p className="text-2xl font-bold text-gray-900">{months} months</p>
        </div>
        <div className="bg-gray-50 border border-gray-200 rounded-lg p-4">
          <p className="text-sm text-gray-600">Interest Rate</p>
          <p className="text-2xl font-bold text-gray-900">{interestRate}% per annum</p>
        </div>
        <div className="bg-gray-50 border border-gray-200 rounded-lg p-4">
          <p className="text-sm text-gray-600">Loan Purpose</p>
          <p className="text-xl font-bold text-gray-900">{data.purpose}</p>
        </div>
      </div>

      <div className="bg-gradient-to-r from-blue-500 to-blue-600 text-white rounded-lg p-6">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div>
            <p className="text-blue-100 text-sm">Monthly Payment</p>
            <p className="text-3xl font-bold">R {monthlyPayment.toLocaleString()}</p>
          </div>
          <div>
            <p className="text-blue-100 text-sm">Total Interest</p>
            <p className="text-3xl font-bold">R {Math.round(totalInterest).toLocaleString()}</p>
          </div>
          <div>
            <p className="text-blue-100 text-sm">Total Repayable</p>
            <p className="text-3xl font-bold">R {Math.round(totalAmount).toLocaleString()}</p>
          </div>
        </div>
      </div>

      <div className="border border-gray-200 rounded-lg p-4">
        <h4 className="font-semibold text-gray-900 mb-3">Fee Breakdown</h4>
        <div className="space-y-2 text-sm">
          <div className="flex justify-between">
            <span className="text-gray-600">Initiation Fee (1.5%)</span>
            <span className="font-medium text-gray-900">R {initiationFee.toFixed(2)}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-gray-600">Monthly Admin Fee</span>
            <span className="font-medium text-gray-900">R 69.00</span>
          </div>
          <div className="border-t pt-2 mt-2 flex justify-between">
            <span className="text-gray-900 font-semibold">Total Fees (once-off + monthly)</span>
            <span className="font-bold text-gray-900">R {(initiationFee + 69 * months).toFixed(2)}</span>
          </div>
        </div>
      </div>

      <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
        <p className="text-sm text-yellow-800">
          <strong>Important:</strong> This is an estimate. Final terms will be confirmed after your application is reviewed and approved by our team.
          You have a cooling-off period as per the National Credit Act.
        </p>
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
          {loading ? 'Saving...' : 'Accept & Continue →'}
        </button>
      </div>
    </div>
  );
};

export default Step4_PreviewTerms;
