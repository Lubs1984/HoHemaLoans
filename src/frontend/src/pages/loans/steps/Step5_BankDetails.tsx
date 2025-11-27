import React, { useState } from 'react';

interface Step5Props {
  data: any;
  onNext: (data: any) => void;
  onPrev: () => void;
  loading: boolean;
}

const SOUTH_AFRICAN_BANKS = [
  'ABSA Bank',
  'African Bank',
  'Bank of Athens',
  'Bidvest Bank',
  'Capitec Bank',
  'Discovery Bank',
  'FirstRand Bank (FNB)',
  'Investec Bank',
  'Nedbank',
  'Standard Bank',
  'TymeBank',
  'Other',
];

const Step5_BankDetails: React.FC<Step5Props> = ({ data, onNext, onPrev, loading }) => {
  const [bankName, setBankName] = useState(data.bankName || '');
  const [accountNumber, setAccountNumber] = useState(data.accountNumber || '');
  const [accountHolderName, setAccountHolderName] = useState(data.accountHolderName || '');

  const handleNext = () => {
    if (!bankName) {
      alert('Please select your bank');
      return;
    }
    if (!accountNumber || accountNumber.length < 8) {
      alert('Please enter a valid account number (minimum 8 digits)');
      return;
    }
    if (!accountHolderName || accountHolderName.trim().length < 3) {
      alert('Please enter the account holder name');
      return;
    }

    onNext({ bankName, accountNumber, accountHolderName });
  };

  return (
    <div className="space-y-6">
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
        <p className="text-blue-800">
          <strong>Funds will be deposited into this account.</strong> Please ensure all details are correct to avoid delays.
        </p>
      </div>

      <div className="space-y-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Bank Name <span className="text-red-500">*</span>
          </label>
          <select
            value={bankName}
            onChange={(e) => setBankName(e.target.value)}
            className="w-full border border-gray-300 rounded-lg px-4 py-3"
          >
            <option value="">Select your bank</option>
            {SOUTH_AFRICAN_BANKS.map((bank) => (
              <option key={bank} value={bank}>
                {bank}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Account Holder Name <span className="text-red-500">*</span>
          </label>
          <input
            type="text"
            value={accountHolderName}
            onChange={(e) => setAccountHolderName(e.target.value)}
            className="w-full border border-gray-300 rounded-lg px-4 py-3"
            placeholder="Full name as it appears on your account"
          />
          <p className="text-xs text-gray-500 mt-1">
            This should match the name on your ID document
          </p>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Account Number <span className="text-red-500">*</span>
          </label>
          <input
            type="text"
            value={accountNumber}
            onChange={(e) => setAccountNumber(e.target.value.replace(/\D/g, ''))}
            className="w-full border border-gray-300 rounded-lg px-4 py-3 font-mono"
            placeholder="Enter your account number"
            maxLength={20}
          />
          <p className="text-xs text-gray-500 mt-1">
            Typically 9-11 digits. Do not include spaces or dashes.
          </p>
        </div>
      </div>

      <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
        <p className="text-sm text-yellow-800">
          <strong>Security Note:</strong> We will never ask for your banking PIN, password, or OTP. Your banking details are encrypted and stored securely.
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
          {loading ? 'Saving...' : 'Next →'}
        </button>
      </div>
    </div>
  );
};

export default Step5_BankDetails;
