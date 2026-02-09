import React, { useState } from 'react';
import { useToast } from '../../../contexts/ToastContext';

interface Step6Props {
  data: any;
  onNext?: (data: any) => void;
  onPrev: () => void;
  onSubmit: (data: any) => void;
  loading: boolean;
}

const Step6_DigitalSignature: React.FC<Step6Props> = ({ data, onPrev, onSubmit, loading }) => {
  const { success, error: showError, warning } = useToast();
  const [agreedToTerms, setAgreedToTerms] = useState(false);
  const [otp, setOtp] = useState('');
  const [otpSent, setOtpSent] = useState(false);
  const [sendingOtp, setSendingOtp] = useState(false);

  const handleSendOtp = async () => {
    try {
      setSendingOtp(true);
      // TODO: Implement actual OTP sending via backend
      // For now, we'll simulate it
      await new Promise((resolve) => setTimeout(resolve, 1000));
      setOtpSent(true);
      success('OTP sent to your registered phone number');
    } catch (err) {
      showError('Failed to send OTP. Please try again.');
    } finally {
      setSendingOtp(false);
    }
  };

  const handleSubmit = () => {
    if (!agreedToTerms) {
      warning('Please accept the terms and conditions to continue');
      return;
    }
    
    // For demo purposes, allow submission without OTP
    // In production, OTP would be mandatory
    if (otpSent && !otp) {
      warning('Please enter the OTP sent to your phone');
      return;
    }

    onSubmit({ otp });
  };

  return (
    <div className="space-y-6">
      <div className="bg-gradient-to-r from-green-500 to-green-600 text-white rounded-lg p-6">
        <h3 className="text-2xl font-bold mb-2">🎉 Almost Done!</h3>
        <p>Review the final details and digitally sign your loan agreement.</p>
      </div>

      <div className="space-y-4">
        <div className="border border-gray-200 rounded-lg p-4">
          <h4 className="font-semibold text-gray-900 mb-3">Application Summary</h4>
          <div className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <p className="text-gray-600">Loan Amount</p>
              <p className="font-bold text-lg text-gray-900">R {data.amount.toLocaleString()}</p>
            </div>
            <div>
              <p className="text-gray-600">Loan Term</p>
              <p className="font-bold text-lg text-gray-900">{data.termMonths} months</p>
            </div>
            <div>
              <p className="text-gray-600">Purpose</p>
              <p className="font-medium text-gray-900">{data.purpose}</p>
            </div>
            <div>
              <p className="text-gray-600">Bank Account</p>
              <p className="font-medium text-gray-900">{data.bankName}</p>
              <p className="text-xs text-gray-600">****{data.accountNumber?.slice(-4)}</p>
            </div>
          </div>
        </div>

        <div className="border border-gray-200 rounded-lg p-4 space-y-3">
          <h4 className="font-semibold text-gray-900">Terms & Conditions</h4>
          <div className="bg-gray-50 border border-gray-200 rounded p-3 max-h-48 overflow-y-auto text-sm">
            <p className="text-gray-700 mb-2">
              <strong>1. Loan Agreement</strong><br />
              By signing this agreement, I confirm that all information provided is true and accurate. I understand that this application will be reviewed by Ho Hema Loans.
            </p>
            <p className="text-gray-700 mb-2">
              <strong>2. Interest & Fees</strong><br />
              I agree to pay the interest rate and fees as specified in this application. I understand these are estimates subject to final approval.
            </p>
            <p className="text-gray-700 mb-2">
              <strong>3. Repayment Terms</strong><br />
              I agree to repay the loan in monthly installments as per the agreed schedule. Late payments may incur additional fees.
            </p>
            <p className="text-gray-700 mb-2">
              <strong>4. National Credit Act Compliance</strong><br />
              This loan is subject to the National Credit Act 34 of 2005. I have the right to a cooling-off period and to receive a full disclosure of terms.
            </p>
            <p className="text-gray-700 mb-2">
              <strong>5. Data Protection (POPIA)</strong><br />
              I consent to Ho Hema Loans processing my personal information for the purpose of assessing and managing my loan application.
            </p>
          </div>

          <label className="flex items-start space-x-3 cursor-pointer">
            <input
              type="checkbox"
              checked={agreedToTerms}
              onChange={(e) => setAgreedToTerms(e.target.checked)}
              className="mt-1 h-5 w-5 text-blue-600"
            />
            <span className="text-sm text-gray-700">
              I have read and agree to the terms and conditions. I understand that this constitutes a digital signature and is legally binding.
            </span>
          </label>
        </div>

        {/* OTP Section */}
        <div className="border border-gray-200 rounded-lg p-4 space-y-3">
          <h4 className="font-semibold text-gray-900">Verification</h4>
          
          {!otpSent ? (
            <div>
              <p className="text-sm text-gray-600 mb-3">
                For security, we'll send a one-time password (OTP) to your registered phone number to verify your identity.
              </p>
              <button
                onClick={handleSendOtp}
                disabled={sendingOtp || !agreedToTerms}
                className="w-full bg-green-600 hover:bg-green-700 text-white px-4 py-3 rounded-lg font-medium disabled:opacity-50"
              >
                {sendingOtp ? 'Sending OTP...' : 'Send OTP to My Phone'}
              </button>
            </div>
          ) : (
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Enter OTP sent to your phone
              </label>
              <input
                type="text"
                value={otp}
                onChange={(e) => setOtp(e.target.value.replace(/\D/g, '').slice(0, 6))}
                className="w-full border border-gray-300 rounded-lg px-4 py-3 text-center text-2xl font-mono tracking-widest"
                placeholder="000000"
                maxLength={6}
              />
              <p className="text-xs text-gray-500 mt-2 text-center">
                Didn't receive it? <button onClick={handleSendOtp} className="text-blue-600 hover:underline">Resend OTP</button>
              </p>
            </div>
          )}
        </div>
      </div>

      <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
        <p className="text-sm text-blue-800">
          <strong>What happens next?</strong> Your application will be reviewed by our team. You'll receive a notification within 24-48 hours regarding approval status.
        </p>
      </div>

      <div className="flex justify-between">
        <button
          onClick={onPrev}
          disabled={loading}
          className="bg-gray-300 hover:bg-gray-400 text-gray-800 px-8 py-3 rounded-lg font-medium disabled:opacity-50"
        >
          ← Back
        </button>
        <button
          onClick={handleSubmit}
          disabled={loading || !agreedToTerms}
          className="bg-green-600 hover:bg-green-700 text-white px-8 py-3 rounded-lg font-medium disabled:opacity-50"
        >
          {loading ? 'Submitting...' : '✓ Submit Application'}
        </button>
      </div>
    </div>
  );
};

export default Step6_DigitalSignature;
