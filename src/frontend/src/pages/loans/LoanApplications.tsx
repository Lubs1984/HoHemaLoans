import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { PlusIcon, ClockIcon, CheckCircleIcon, XCircleIcon, EyeIcon } from '@heroicons/react/24/outline';
import { apiService } from '../../services/api';

interface LoanApplication {
  id: string;
  amount: number;
  termMonths: number;
  purpose: string;
  status: string;
  interestRate: number;
  monthlyPayment: number;
  totalAmount: number;
  applicationDate: string;
  approvalDate?: string;
  channelOrigin: string;
  currentStep: number;
  repaymentDay?: number;
}

const LoanApplications: React.FC = () => {
  const [applications, setApplications] = useState<LoanApplication[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadApplications();
  }, []);

  const loadApplications = async () => {
    try {
      setIsLoading(true);
      setError(null);
      const data = await apiService.getLoanApplications();
      setApplications(data);
    } catch (err) {
      console.error('Failed to load loan applications:', err);
      setError(err instanceof Error ? err.message : 'Failed to load applications');
    } finally {
      setIsLoading(false);
    }
  };

  const getStatusBadge = (status: string) => {
    const statusConfig: Record<string, { color: string; icon: any; text: string }> = {
      Draft: { color: 'bg-gray-100 text-gray-800', icon: ClockIcon, text: 'Draft' },
      Pending: { color: 'bg-yellow-100 text-yellow-800', icon: ClockIcon, text: 'Pending' },
      UnderReview: { color: 'bg-blue-100 text-blue-800', icon: ClockIcon, text: 'Under Review' },
      Approved: { color: 'bg-green-100 text-green-800', icon: CheckCircleIcon, text: 'Approved' },
      Rejected: { color: 'bg-red-100 text-red-800', icon: XCircleIcon, text: 'Rejected' },
      Disbursed: { color: 'bg-purple-100 text-purple-800', icon: CheckCircleIcon, text: 'Disbursed' },
      Closed: { color: 'bg-gray-100 text-gray-800', icon: CheckCircleIcon, text: 'Closed' },
    };

    const config = statusConfig[status] || statusConfig.Pending;
    const Icon = config.icon;

    return (
      <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${config.color}`}>
        <Icon className="w-4 h-4 mr-1" />
        {config.text}
      </span>
    );
  };

  const getChannelBadge = (channel: string) => {
    if (channel === 'WhatsApp') {
      return (
        <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">
          <span className="mr-1">📱</span>
          WhatsApp
        </span>
      );
    }
    return (
      <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
        <span className="mr-1">🌐</span>
        Web
      </span>
    );
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-ZA', {
      style: 'currency',
      currency: 'ZAR',
      minimumFractionDigits: 0,
      maximumFractionDigits: 0,
    }).format(amount);
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-ZA', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  };

  const getOrdinalSuffix = (day: number) => {
    if (day > 3 && day < 21) return 'th';
    switch (day % 10) {
      case 1: return 'st';
      case 2: return 'nd';
      case 3: return 'rd';
      default: return 'th';
    }
  };

  if (isLoading) {
    return (
      <div>
        <div className="mb-8">
          <h1 className="text-2xl font-bold text-gray-900">Loan Applications</h1>
          <p className="text-gray-600">View and manage your loan applications</p>
        </div>
        <div className="card">
          <div className="flex items-center justify-center py-12">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div>
      <div className="mb-8 flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Loan Applications</h1>
          <p className="text-gray-600">View and manage your loan applications</p>
        </div>
        <div className="flex gap-3">
          <Link
            to="/loans/new"
            className="btn btn-primary inline-flex items-center"
          >
            <PlusIcon className="w-5 h-5 mr-2" />
            New Loan (Worker)
          </Link>
          <Link
            to="/loans/apply"
            className="btn bg-gray-600 hover:bg-gray-700 text-white inline-flex items-center"
          >
            <PlusIcon className="w-5 h-5 mr-2" />
            Legacy Application
          </Link>
        </div>
      </div>

      {error && (
        <div className="mb-4 rounded-md bg-red-50 border border-red-200 p-4">
          <p className="text-sm text-red-700">{error}</p>
        </div>
      )}

      {applications.length === 0 ? (
        <div className="card text-center py-12">
          <div className="mx-auto h-12 w-12 text-gray-400">
            <svg className="h-12 w-12" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
            </svg>
          </div>
          <h3 className="mt-2 text-sm font-medium text-gray-900">No loan applications</h3>
          <p className="mt-1 text-sm text-gray-500">Get started by creating a new loan application.</p>
          <div className="mt-6">
            <Link
              to="/loans/apply"
              className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
            >
              <PlusIcon className="w-5 h-5 mr-2" />
              New Application
            </Link>
          </div>
        </div>
      ) : (
        <div className="card overflow-hidden">
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Application Date
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Amount
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Term
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Purpose
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Monthly Payment
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Repayment Date
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Status
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Channel
                  </th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Actions
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {applications.map((app) => (
                  <tr key={app.id} className="hover:bg-gray-50 transition-colors">
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      {formatDate(app.applicationDate)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                      {formatCurrency(app.amount)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
                      {app.termMonths} months
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
                      {app.purpose || 'N/A'}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      {formatCurrency(app.monthlyPayment)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
                      {app.repaymentDay ? `${app.repaymentDay}${getOrdinalSuffix(app.repaymentDay)} of month` : 'Not set'}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      {getStatusBadge(app.status)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      {getChannelBadge(app.channelOrigin)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium space-x-2">
                      {app.status === 'Draft' ? (
                        <Link
                          to={`/loans/apply?id=${app.id}`}
                          className="inline-flex items-center px-3 py-1.5 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition"
                        >
                          <span>Resume</span>
                          <span className="ml-1">→</span>
                        </Link>
                      ) : (
                        <Link
                          to={`/loans/${app.id}`}
                          className="text-blue-600 hover:text-blue-900 transition"
                        >
                          <EyeIcon className="w-5 h-5 inline" />
                        </Link>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          
          <div className="bg-gray-50 px-6 py-4 border-t border-gray-200">
            <p className="text-sm text-gray-700">
              Showing <span className="font-medium">{applications.length}</span> application{applications.length !== 1 ? 's' : ''}
            </p>
          </div>
        </div>
      )}
    </div>
  );
};

export default LoanApplications;