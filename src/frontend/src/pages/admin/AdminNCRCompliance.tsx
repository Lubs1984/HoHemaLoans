import { useState, useEffect } from 'react';
import { apiService } from '../../services/api';
import { 
  ShieldCheckIcon, 
  ExclamationTriangleIcon, 
  DocumentTextIcon, 
  ClockIcon,
  ChatBubbleBottomCenterTextIcon
} from '@heroicons/react/24/outline';

interface NCRConfiguration {
  id: number;
  maxInterestRatePerAnnum: number;
  defaultInterestRatePerAnnum: number;
  maxInitiationFee: number;
  initiationFeePercentage: number;
  maxMonthlyServiceFee: number;
  defaultMonthlyServiceFee: number;
  maxDebtToIncomeRatio: number;
  minSafetyBuffer: number;
  minLoanAmount: number;
  maxLoanAmount: number;
  minLoanTermMonths: number;
  maxLoanTermMonths: number;
  coolingOffPeriodDays: number;
  ncrCPRegistrationNumber?: string;
  complianceOfficerName?: string;
  complianceOfficerEmail?: string;
  documentRetentionYears: number;
  enforceNCRCompliance: boolean;
  allowCoolingOffCancellation: boolean;
}

interface ConsumerComplaint {
  id: number;
  subject: string;
  description: string;
  category: string;
  status: string;
  priority: string;
  createdAt: string;
  user: {
    firstName: string;
    lastName: string;
    email: string;
  };
  loanApplication?: {
    id: string;
    amount: number;
  };
}

export default function AdminNCRCompliance() {
  const [activeTab, setActiveTab] = useState('configuration');
  const [loading, setLoading] = useState(true);
  const [config, setConfig] = useState<NCRConfiguration | null>(null);
  const [complaints, setComplaints] = useState<ConsumerComplaint[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      setError(null);

      try {
        const configData = await apiService.request<NCRConfiguration>('/ncr/configuration');
        setConfig(configData);
      } catch (err) {
        console.error('Error loading NCR configuration:', err);
        // Set defaults if no config exists yet
        setConfig({
          id: 0,
          maxInterestRatePerAnnum: 27.5,
          defaultInterestRatePerAnnum: 24.0,
          maxInitiationFee: 1140,
          initiationFeePercentage: 15,
          maxMonthlyServiceFee: 60,
          defaultMonthlyServiceFee: 60,
          maxDebtToIncomeRatio: 35,
          minSafetyBuffer: 500,
          minLoanAmount: 500,
          maxLoanAmount: 50000,
          minLoanTermMonths: 1,
          maxLoanTermMonths: 72,
          coolingOffPeriodDays: 5,
          documentRetentionYears: 5,
          enforceNCRCompliance: true,
          allowCoolingOffCancellation: true,
        });
      }

      try {
        const complaintsData = await apiService.request<ConsumerComplaint[]>('/ncr/complaints');
        setComplaints(complaintsData);
      } catch (err) {
        console.error('Error loading complaints:', err);
        setComplaints([]);
      }
    } catch (err) {
      setError('Failed to load NCR data');
      console.error('Error loading NCR data:', err);
    } finally {
      setLoading(false);
    }
  };

  const saveConfiguration = async () => {
    if (!config) return;

    try {
      setSaving(true);
      await apiService.request('/ncr/configuration', {
        method: 'PUT',
        body: JSON.stringify(config)
      });
      alert('NCR configuration saved successfully');
    } catch (err) {
      alert('Error saving configuration');
      console.error('Error saving config:', err);
    } finally {
      setSaving(false);
    }
  };

  const updateConfig = (field: keyof NCRConfiguration, value: any) => {
    if (config) {
      setConfig({ ...config, [field]: value });
    }
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900 flex items-center">
            <ShieldCheckIcon className="h-8 w-8 mr-3 text-green-600" />
            NCR Compliance Management
          </h1>
          <p className="mt-2 text-gray-600">
            Manage NCR (National Credit Regulator) compliance settings and consumer complaints
          </p>
        </div>

        {/* Error Alert */}
        {error && (
          <div className="mb-6 bg-red-50 border border-red-200 rounded-lg p-4">
            <div className="flex">
              <ExclamationTriangleIcon className="h-5 w-5 text-red-400" />
              <div className="ml-3">
                <p className="text-sm text-red-800">{error}</p>
              </div>
            </div>
          </div>
        )}

        {/* Tabs */}
        <div className="border-b border-gray-200 mb-6">
          <nav className="-mb-px flex space-x-8">
            <button
              onClick={() => setActiveTab('configuration')}
              className={`py-2 px-1 border-b-2 font-medium text-sm ${
                activeTab === 'configuration'
                  ? 'border-blue-500 text-blue-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700'
              }`}
            >
              <DocumentTextIcon className="h-5 w-5 inline mr-2" />
              Configuration
            </button>
            <button
              onClick={() => setActiveTab('complaints')}
              className={`py-2 px-1 border-b-2 font-medium text-sm ${
                activeTab === 'complaints'
                  ? 'border-blue-500 text-blue-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700'
              }`}
            >
              <ChatBubbleBottomCenterTextIcon className="h-5 w-5 inline mr-2" />
              Consumer Complaints ({complaints.length})
            </button>
          </nav>
        </div>

        {/* Configuration Tab */}
        {activeTab === 'configuration' && config && (
          <div className="bg-white shadow rounded-lg">
            <div className="px-6 py-4 border-b border-gray-200">
              <h2 className="text-lg font-medium text-gray-900">NCR Configuration Settings</h2>
              <p className="text-sm text-gray-600">Configure limits and parameters for NCR compliance</p>
            </div>

            <div className="p-6 space-y-8">
              {/* Interest Rate Limits */}
              <div>
                <h3 className="text-lg font-medium text-gray-900 mb-4">Interest Rate Limits</h3>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700">
                      Maximum Interest Rate (% per annum)
                    </label>
                    <input
                      type="number"
                      step="0.01"
                      max="30"
                      value={config.maxInterestRatePerAnnum}
                      onChange={(e) => updateConfig('maxInterestRatePerAnnum', parseFloat(e.target.value))}
                      className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
                    />
                    <p className="text-xs text-gray-500 mt-1">NCR maximum is 27.5%</p>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700">
                      Default Interest Rate (% per annum)
                    </label>
                    <input
                      type="number"
                      step="0.01"
                      value={config.defaultInterestRatePerAnnum}
                      onChange={(e) => updateConfig('defaultInterestRatePerAnnum', parseFloat(e.target.value))}
                      className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
                    />
                  </div>
                </div>
              </div>

              {/* Fee Limits */}
              <div>
                <h3 className="text-lg font-medium text-gray-900 mb-4">Fee Limits</h3>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700">
                      Maximum Initiation Fee (R)
                    </label>
                    <input
                      type="number"
                      step="0.01"
                      value={config.maxInitiationFee}
                      onChange={(e) => updateConfig('maxInitiationFee', parseFloat(e.target.value))}
                      className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
                    />
                    <p className="text-xs text-gray-500 mt-1">NCR maximum is R1,140</p>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700">
                      Initiation Fee Percentage (% of loan)
                    </label>
                    <input
                      type="number"
                      step="0.1"
                      value={config.initiationFeePercentage}
                      onChange={(e) => updateConfig('initiationFeePercentage', parseFloat(e.target.value))}
                      className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700">
                      Maximum Monthly Service Fee (R)
                    </label>
                    <input
                      type="number"
                      step="0.01"
                      value={config.maxMonthlyServiceFee}
                      onChange={(e) => updateConfig('maxMonthlyServiceFee', parseFloat(e.target.value))}
                      className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
                    />
                    <p className="text-xs text-gray-500 mt-1">NCR maximum is R60</p>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700">
                      Default Monthly Service Fee (R)
                    </label>
                    <input
                      type="number"
                      step="0.01"
                      value={config.defaultMonthlyServiceFee}
                      onChange={(e) => updateConfig('defaultMonthlyServiceFee', parseFloat(e.target.value))}
                      className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
                    />
                  </div>
                </div>
              </div>

              {/* Affordability Settings */}
              <div>
                <h3 className="text-lg font-medium text-gray-900 mb-4">Affordability Settings</h3>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700">
                      Maximum Debt-to-Income Ratio (%)
                    </label>
                    <input
                      type="number"
                      step="0.1"
                      value={config.maxDebtToIncomeRatio}
                      onChange={(e) => updateConfig('maxDebtToIncomeRatio', parseFloat(e.target.value))}
                      className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
                    />
                    <p className="text-xs text-gray-500 mt-1">NCR guideline is 35%</p>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700">
                      Minimum Safety Buffer (R)
                    </label>
                    <input
                      type="number"
                      step="0.01"
                      value={config.minSafetyBuffer}
                      onChange={(e) => updateConfig('minSafetyBuffer', parseFloat(e.target.value))}
                      className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
                    />
                  </div>
                </div>
              </div>

              {/* Loan Limits */}
              <div>
                <h3 className="text-lg font-medium text-gray-900 mb-4">Loan Limits</h3>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700">
                      Minimum Loan Amount (R)
                    </label>
                    <input
                      type="number"
                      step="0.01"
                      value={config.minLoanAmount}
                      onChange={(e) => updateConfig('minLoanAmount', parseFloat(e.target.value))}
                      className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700">
                      Maximum Loan Amount (R)
                    </label>
                    <input
                      type="number"
                      step="0.01"
                      value={config.maxLoanAmount}
                      onChange={(e) => updateConfig('maxLoanAmount', parseFloat(e.target.value))}
                      className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700">
                      Minimum Loan Term (months)
                    </label>
                    <input
                      type="number"
                      value={config.minLoanTermMonths}
                      onChange={(e) => updateConfig('minLoanTermMonths', parseInt(e.target.value))}
                      className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700">
                      Maximum Loan Term (months)
                    </label>
                    <input
                      type="number"
                      value={config.maxLoanTermMonths}
                      onChange={(e) => updateConfig('maxLoanTermMonths', parseInt(e.target.value))}
                      className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
                    />
                  </div>
                </div>
              </div>

              {/* NCR Registration */}
              <div>
                <h3 className="text-lg font-medium text-gray-900 mb-4">NCR Registration</h3>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700">
                      NCRCP Registration Number
                    </label>
                    <input
                      type="text"
                      value={config.ncrCPRegistrationNumber || ''}
                      onChange={(e) => updateConfig('ncrCPRegistrationNumber', e.target.value)}
                      className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
                      placeholder="NCRCP12345"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700">
                      Compliance Officer Name
                    </label>
                    <input
                      type="text"
                      value={config.complianceOfficerName || ''}
                      onChange={(e) => updateConfig('complianceOfficerName', e.target.value)}
                      className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700">
                      Compliance Officer Email
                    </label>
                    <input
                      type="email"
                      value={config.complianceOfficerEmail || ''}
                      onChange={(e) => updateConfig('complianceOfficerEmail', e.target.value)}
                      className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700">
                      Cooling-off Period (days)
                    </label>
                    <input
                      type="number"
                      min="1"
                      max="30"
                      value={config.coolingOffPeriodDays}
                      onChange={(e) => updateConfig('coolingOffPeriodDays', parseInt(e.target.value))}
                      className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
                    />
                    <p className="text-xs text-gray-500 mt-1">NCR requirement is 5 days</p>
                  </div>
                </div>
              </div>

              {/* Compliance Toggles */}
              <div>
                <h3 className="text-lg font-medium text-gray-900 mb-4">Compliance Settings</h3>
                <div className="space-y-4">
                  <div className="flex items-center">
                    <input
                      type="checkbox"
                      checked={config.enforceNCRCompliance}
                      onChange={(e) => updateConfig('enforceNCRCompliance', e.target.checked)}
                      className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                    />
                    <label className="ml-3 text-sm font-medium text-gray-700">
                      Enforce NCR Compliance Checks
                    </label>
                  </div>
                  <div className="flex items-center">
                    <input
                      type="checkbox"
                      checked={config.allowCoolingOffCancellation}
                      onChange={(e) => updateConfig('allowCoolingOffCancellation', e.target.checked)}
                      className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                    />
                    <label className="ml-3 text-sm font-medium text-gray-700">
                      Allow Cooling-off Period Cancellations
                    </label>
                  </div>
                </div>
              </div>

              {/* Save Button */}
              <div className="flex justify-end pt-6 border-t border-gray-200">
                <button
                  onClick={saveConfiguration}
                  disabled={saving}
                  className="bg-blue-600 text-white px-6 py-2 rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {saving ? 'Saving...' : 'Save Configuration'}
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Complaints Tab */}
        {activeTab === 'complaints' && (
          <div className="bg-white shadow rounded-lg">
            <div className="px-6 py-4 border-b border-gray-200">
              <h2 className="text-lg font-medium text-gray-900">Consumer Complaints</h2>
              <p className="text-sm text-gray-600">Manage consumer complaints as required by NCR</p>
            </div>

            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-gray-200">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Complaint
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Customer
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Category
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Status
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Priority
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Created
                    </th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {complaints.length === 0 ? (
                    <tr>
                      <td colSpan={6} className="px-6 py-8 text-center text-gray-500">
                        <ChatBubbleBottomCenterTextIcon className="h-12 w-12 mx-auto mb-2 text-gray-300" />
                        No active complaints
                      </td>
                    </tr>
                  ) : (
                    complaints.map((complaint) => (
                      <tr key={complaint.id} className="hover:bg-gray-50">
                        <td className="px-6 py-4">
                          <div>
                            <p className="text-sm font-medium text-gray-900">{complaint.subject}</p>
                            <p className="text-sm text-gray-500 truncate max-w-xs">
                              {complaint.description}
                            </p>
                          </div>
                        </td>
                        <td className="px-6 py-4">
                          <div>
                            <p className="text-sm font-medium text-gray-900">
                              {complaint.user.firstName} {complaint.user.lastName}
                            </p>
                            <p className="text-sm text-gray-500">{complaint.user.email}</p>
                          </div>
                        </td>
                        <td className="px-6 py-4">
                          <span className="inline-flex px-2 py-1 text-xs font-medium rounded-full bg-blue-100 text-blue-800">
                            {complaint.category}
                          </span>
                        </td>
                        <td className="px-6 py-4">
                          <span className={`inline-flex px-2 py-1 text-xs font-medium rounded-full ${
                            complaint.status === 'Open' ? 'bg-red-100 text-red-800' :
                            complaint.status === 'InProgress' ? 'bg-yellow-100 text-yellow-800' :
                            complaint.status === 'Resolved' ? 'bg-green-100 text-green-800' :
                            'bg-gray-100 text-gray-800'
                          }`}>
                            {complaint.status}
                          </span>
                        </td>
                        <td className="px-6 py-4">
                          <span className={`inline-flex px-2 py-1 text-xs font-medium rounded-full ${
                            complaint.priority === 'Critical' ? 'bg-red-100 text-red-800' :
                            complaint.priority === 'High' ? 'bg-orange-100 text-orange-800' :
                            complaint.priority === 'Medium' ? 'bg-yellow-100 text-yellow-800' :
                            'bg-green-100 text-green-800'
                          }`}>
                            {complaint.priority}
                          </span>
                        </td>
                        <td className="px-6 py-4 text-sm text-gray-500">
                          <div className="flex items-center">
                            <ClockIcon className="h-4 w-4 mr-1" />
                            {new Date(complaint.createdAt).toLocaleDateString()}
                          </div>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}