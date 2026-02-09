import { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { CheckCircle, Clock, FileText, Shield } from 'lucide-react';
import { apiService } from '../../services/api';

interface Contract {
  id: number;
  loanApplicationId: string;
  contractType: string;
  status: string;
  createdAt: string;
  sentAt?: string;
  signedAt?: string;
  expiresAt?: string;
  contractContent: string;
  loanApplication?: {
    id: string;
    amount: number;
    termMonths: number;
    monthlyPayment: number;
    totalAmount: number;
    status: string;
  };
  digitalSignature?: {
    id: number;
    signatureMethod: string;
    signedAt?: string;
    isValid: boolean;
    pinExpiresAt?: string;
  };
}

interface ContractResponse {
  success: boolean;
  contract: Contract;
}

interface SendPinResponse {
  success: boolean;
  pin?: string;
}

interface VerifyPinResponse {
  success: boolean;
}

export default function ContractView() {
  const { contractId } = useParams();
  const [contract, setContract] = useState<Contract | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showPinModal, setShowPinModal] = useState(false);
  const [pin, setPin] = useState('');
  const [phoneNumber, setPhoneNumber] = useState('');
  const [pinSending, setPinSending] = useState(false);
  const [pinVerifying, setPinVerifying] = useState(false);
  const [pinError, setPinError] = useState('');

  useEffect(() => {
    fetchContract();
  }, [contractId]);

  const fetchContract = async () => {
    try {
      setLoading(true);
      const response = await apiService.get<ContractResponse>(`/contracts/${contractId}`);
      if (response.data.success) {
        setContract(response.data.contract);
      }
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to load contract');
    } finally {
      setLoading(false);
    }
  };

  const sendSigningPin = async () => {
    if (!phoneNumber.trim()) {
      setPinError('Please enter your phone number');
      return;
    }

    try {
      setPinSending(true);
      setPinError('');
      const response = await apiService.post<SendPinResponse>(`/contracts/${contractId}/send-pin`, {
        phoneNumber: phoneNumber.trim()
      });

      if (response.data.success) {
        alert('A 6-digit PIN has been sent to your WhatsApp. It expires in 10 minutes.');
        // In development, the PIN might be returned
        if (response.data.pin) {
          alert(`Development Mode: Your PIN is ${response.data.pin}`);
        }
      }
    } catch (err: any) {
      setPinError(err.response?.data?.message || 'Failed to send PIN');
    } finally {
      setPinSending(false);
    }
  };

  const verifyAndSign = async () => {
    if (!pin.trim() || pin.length !== 6) {
      setPinError('Please enter the 6-digit PIN');
      return;
    }

    try {
      setPinVerifying(true);
      setPinError('');
      const response = await apiService.post<VerifyPinResponse>(`/contracts/${contractId}/verify-pin`, {
        pin: pin.trim()
      });

      if (response.data.success) {
        alert('Contract signed successfully! 🎉');
        setShowPinModal(false);
        fetchContract(); // Refresh to show signed status
      }
    } catch (err: any) {
      setPinError(err.response?.data?.message || 'Invalid PIN');
    } finally {
      setPinVerifying(false);
    }
  };

  const getStatusBadge = (status: string) => {
    const styles = {
      Draft: 'bg-gray-100 text-gray-800',
      Sent: 'bg-blue-100 text-blue-800',
      Signed: 'bg-green-100 text-green-800',
      Expired: 'bg-red-100 text-red-800',
      Cancelled: 'bg-red-100 text-red-800',
    };
    return (
      <span className={`px-3 py-1 rounded-full text-sm font-medium ${styles[status as keyof typeof styles] || styles.Draft}`}>
        {status}
      </span>
    );
  };

  if (loading) {
    return (
      <div className="flex justify-center items-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (error || !contract) {
    return (
      <div className="max-w-4xl mx-auto p-6">
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <p className="text-red-800">{error || 'Contract not found'}</p>
        </div>
      </div>
    );
  }

  const isSigned = contract.digitalSignature?.isValid;
  const canSign = contract.status === 'Sent' && !isSigned;

  return (
    <div className="max-w-5xl mx-auto p-6">
      {/* Header */}
      <div className="bg-white rounded-lg shadow-md p-6 mb-6">
        <div className="flex items-start justify-between mb-4">
          <div>
            <h1 className="text-3xl font-bold text-gray-900 mb-2">
              Credit Agreement
            </h1>
            <p className="text-gray-600">Contract #{contract.id}</p>
          </div>
          {getStatusBadge(contract.status)}
        </div>

        {/* Loan Details */}
        {contract.loanApplication && (
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mt-6 pt-6 border-t">
            <div>
              <p className="text-sm text-gray-500">Loan Amount</p>
              <p className="text-lg font-semibold">R{contract.loanApplication.amount.toLocaleString()}</p>
            </div>
            <div>
              <p className="text-sm text-gray-500">Term</p>
              <p className="text-lg font-semibold">{contract.loanApplication.termMonths} months</p>
            </div>
            <div>
              <p className="text-sm text-gray-500">Monthly Payment</p>
              <p className="text-lg font-semibold">R{contract.loanApplication.monthlyPayment.toLocaleString()}</p>
            </div>
            <div>
              <p className="text-sm text-gray-500">Total Repayable</p>
              <p className="text-lg font-semibold">R{contract.loanApplication.totalAmount.toLocaleString()}</p>
            </div>
          </div>
        )}

        {/* Signature Status */}
        {contract.digitalSignature && (
          <div className="mt-6 pt-6 border-t">
            <div className="flex items-center space-x-3">
              {contract.digitalSignature.isValid ? (
                <>
                  <CheckCircle className="h-6 w-6 text-green-600" />
                  <div>
                    <p className="font-semibold text-green-800">Digitally Signed</p>
                    <p className="text-sm text-gray-600">
                      Signed on {new Date(contract.digitalSignature.signedAt!).toLocaleString()} via {contract.digitalSignature.signatureMethod}
                    </p>
                  </div>
                </>
              ) : (
                <>
                  <Clock className="h-6 w-6 text-yellow-600" />
                  <div>
                    <p className="font-semibold text-yellow-800">Awaiting Signature</p>
                    <p className="text-sm text-gray-600">
                      PIN expires: {new Date(contract.digitalSignature.pinExpiresAt!).toLocaleString()}
                    </p>
                  </div>
                </>
              )}
            </div>
          </div>
        )}
      </div>

      {/* Sign Contract Button */}
      {canSign && (
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-6 mb-6">
          <div className="flex items-start space-x-4">
            <Shield className="h-8 w-8 text-blue-600 flex-shrink-0 mt-1" />
            <div className="flex-1">
              <h3 className="text-lg font-semibold text-gray-900 mb-2">
                Ready to Sign
              </h3>
              <p className="text-gray-700 mb-4">
                Review the contract below. When ready, click "Sign Contract" to receive a secure PIN via WhatsApp.
              </p>
              <button
                onClick={() => setShowPinModal(true)}
                className="bg-blue-600 text-white px-6 py-3 rounded-lg hover:bg-blue-700 font-semibold transition"
              >
                Sign Contract
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Contract Content */}
      <div className="bg-white rounded-lg shadow-md p-8">
        <div className="flex items-center space-x-2 mb-6 pb-4 border-b">
          <FileText className="h-6 w-6 text-gray-600" />
          <h2 className="text-xl font-semibold">Contract Document</h2>
        </div>
        <div 
          className="prose max-w-none"
          dangerouslySetInnerHTML={{ __html: contract.contractContent }}
        />
      </div>

      {/* PIN Modal */}
      {showPinModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-lg max-w-md w-full p-6">
            <h3 className="text-xl font-bold mb-4">Sign Contract via WhatsApp PIN</h3>
            
            {!contract.digitalSignature && (
              <div className="mb-6">
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Phone Number (with country code)
                </label>
                <input
                  type="tel"
                  value={phoneNumber}
                  onChange={(e) => setPhoneNumber(e.target.value)}
                  placeholder="+27..."
                  className="w-full border border-gray-300 rounded-lg px-4 py-2 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                />
                <button
                  onClick={sendSigningPin}
                  disabled={pinSending}
                  className="w-full mt-3 bg-blue-600 text-white px-4 py-3 rounded-lg hover:bg-blue-700 disabled:opacity-50 font-semibold"
                >
                  {pinSending ? 'Sending...' : 'Send PIN via WhatsApp'}
                </button>
              </div>
            )}

            <div className="mb-6">
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Enter 6-Digit PIN
              </label>
              <input
                type="text"
                value={pin}
                onChange={(e) => setPin(e.target.value.replace(/\D/g, '').substring(0, 6))}
                placeholder="000000"
                maxLength={6}
                className="w-full border border-gray-300 rounded-lg px-4 py-3 text-center text-2xl font-mono focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              />
              {pinError && (
                <p className="text-red-600 text-sm mt-2">{pinError}</p>
              )}
            </div>

            <div className="flex space-x-3">
              <button
                onClick={() => {
                  setShowPinModal(false);
                  setPinError('');
                  setPin('');
                }}
                className="flex-1 bg-gray-200 text-gray-800 px-4 py-3 rounded-lg hover:bg-gray-300 font-semibold"
              >
                Cancel
              </button>
              <button
                onClick={verifyAndSign}
                disabled={pinVerifying || pin.length !== 6}
                className="flex-1 bg-green-600 text-white px-4 py-3 rounded-lg hover:bg-green-700 disabled:opacity-50 font-semibold"
              >
                {pinVerifying ? 'Verifying...' : 'Sign Contract'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
