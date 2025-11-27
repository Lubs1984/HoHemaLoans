import React, { useState } from 'react';

interface Step2Props {
  data: any;
  onNext: (data: any) => void;
  onPrev: () => void;
  loading: boolean;
}

const PURPOSE_OPTIONS = [
  { value: 'Emergency', label: 'Emergency', icon: '🚨', description: 'Unexpected medical or urgent expenses' },
  { value: 'Education', label: 'Education', icon: '📚', description: 'School fees, courses, or training' },
  { value: 'Medical', label: 'Medical', icon: '⚕️', description: 'Health-related expenses' },
  { value: 'Home', label: 'Home Improvement', icon: '🏠', description: 'Repairs or improvements' },
  { value: 'Debt', label: 'Debt Consolidation', icon: '💳', description: 'Consolidate existing debts' },
  { value: 'Other', label: 'Other', icon: '📝', description: 'Other valid purposes' },
];

const Step2_Purpose: React.FC<Step2Props> = ({ data, onNext, onPrev, loading }) => {
  const [purpose, setPurpose] = useState(data.purpose || '');
  const [purposeDescription, setPurposeDescription] = useState(data.purposeDescription || '');

  const handleNext = () => {
    if (!purpose) {
      alert('Please select a loan purpose');
      return;
    }
    onNext({ purpose, purposeDescription });
  };

  return (
    <div className="space-y-6">
      <div className="text-center">
        <p className="text-gray-600">
          Loan Amount: <span className="font-semibold">R {data.amount.toLocaleString()}</span> | 
          Term: <span className="font-semibold">{data.termMonths} months</span>
        </p>
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-3">
          What will you use this loan for?
        </label>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {PURPOSE_OPTIONS.map((option) => (
            <button
              key={option.value}
              onClick={() => setPurpose(option.value)}
              className={`p-4 rounded-lg border-2 text-left transition ${
                purpose === option.value
                  ? 'border-blue-600 bg-blue-50'
                  : 'border-gray-200 hover:border-blue-300'
              }`}
            >
              <div className="flex items-center space-x-3">
                <span className="text-3xl">{option.icon}</span>
                <div>
                  <h4 className="font-semibold text-gray-900">{option.label}</h4>
                  <p className="text-sm text-gray-600">{option.description}</p>
                </div>
              </div>
            </button>
          ))}
        </div>
      </div>

      {(purpose === 'Other' || purposeDescription) && (
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Additional details (optional)
          </label>
          <textarea
            value={purposeDescription}
            onChange={(e) => setPurposeDescription(e.target.value)}
            className="w-full border border-gray-300 rounded-lg px-4 py-3 resize-none"
            rows={3}
            placeholder="Provide more details about your loan purpose..."
          />
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
          {loading ? 'Saving...' : 'Next →'}
        </button>
      </div>
    </div>
  );
};

export default Step2_Purpose;
