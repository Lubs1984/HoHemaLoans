import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { FileText, CheckCircle, Clock, AlertCircle } from 'lucide-react';
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
  loanAmount?: number;
  isSigned: boolean;
}

interface ContractsResponse {
  success: boolean;
  contracts: Contract[];
}

export default function ContractsList() {
  const [contracts, setContracts] = useState<Contract[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    fetchContracts();
  }, []);

  const fetchContracts = async () => {
    try {
      setLoading(true);
      const response = await apiService.get<ContractsResponse>('/contracts');
      if (response.data.success) {
        setContracts(response.data.contracts);
      }
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to load contracts');
    } finally {
      setLoading(false);
    }
  };

  const getStatusIcon = (contract: Contract) => {
    if (contract.isSigned) {
      return <CheckCircle className="h-5 w-5 text-green-600" />;
    }
    if (contract.status === 'Sent') {
      return <Clock className="h-5 w-5 text-yellow-600" />;
    }
    if (contract.status === 'Expired') {
      return <AlertCircle className="h-5 w-5 text-red-600" />;
    }
    return <FileText className="h-5 w-5 text-gray-600" />;
  };

  const getStatusBadge = (status: string, isSigned: boolean) => {
    if (isSigned) {
      return <span className="px-3 py-1 rounded-full text-sm font-medium bg-green-100 text-green-800">Signed</span>;
    }
    
    const styles = {
      Draft: 'bg-gray-100 text-gray-800',
      Sent: 'bg-blue-100 text-blue-800',
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

  return (
    <div className="max-w-6xl mx-auto p-6">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 mb-2">My Contracts</h1>
        <p className="text-gray-600">View and sign your loan agreements</p>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4 mb-6">
          <p className="text-red-800">{error}</p>
        </div>
      )}

      {contracts.length === 0 ? (
        <div className="bg-white rounded-lg shadow-md p-12 text-center">
          <FileText className="h-16 w-16 text-gray-400 mx-auto mb-4" />
          <h3 className="text-xl font-semibold text-gray-900 mb-2">No Contracts Yet</h3>
          <p className="text-gray-600">
            Once your loan application is approved, your contract will appear here.
          </p>
        </div>
      ) : (
        <div className="space-y-4">
          {contracts.map((contract) => (
            <Link
              key={contract.id}
              to={`/contracts/${contract.id}`}
              className="block bg-white rounded-lg shadow-md hover:shadow-lg transition-shadow p-6"
            >
              <div className="flex items-start justify-between">
                <div className="flex items-start space-x-4 flex-1">
                  <div className="mt-1">
                    {getStatusIcon(contract)}
                  </div>
                  <div className="flex-1">
                    <div className="flex items-start justify-between mb-2">
                      <div>
                        <h3 className="text-lg font-semibold text-gray-900">
                          {contract.contractType.replace(/([A-Z])/g, ' $1').trim()}
                        </h3>
                        <p className="text-sm text-gray-600">Contract #{contract.id}</p>
                      </div>
                      {getStatusBadge(contract.status, contract.isSigned)}
                    </div>

                    {contract.loanAmount && (
                      <p className="text-gray-700 mb-2">
                        Loan Amount: <span className="font-semibold">R{contract.loanAmount.toLocaleString()}</span>
                      </p>
                    )}

                    <div className="flex items-center space-x-6 text-sm text-gray-600">
                      <div>
                        <span className="font-medium">Created:</span> {new Date(contract.createdAt).toLocaleDateString()}
                      </div>
                      {contract.sentAt && (
                        <div>
                          <span className="font-medium">Sent:</span> {new Date(contract.sentAt).toLocaleDateString()}
                        </div>
                      )}
                      {contract.signedAt && (
                        <div className="text-green-700">
                          <span className="font-medium">Signed:</span> {new Date(contract.signedAt).toLocaleDateString()}
                        </div>
                      )}
                    </div>

                    {contract.status === 'Sent' && !contract.isSigned && (
                      <div className="mt-3 inline-flex items-center space-x-2 bg-blue-50 text-blue-700 px-3 py-1 rounded-lg text-sm">
                        <Clock className="h-4 w-4" />
                        <span>Awaiting your signature</span>
                      </div>
                    )}
                  </div>
                </div>
              </div>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
