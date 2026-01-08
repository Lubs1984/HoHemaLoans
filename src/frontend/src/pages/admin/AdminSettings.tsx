import React, { useState, useEffect } from 'react';
import { apiService } from '../../services/api';

interface SystemSettings {
  id: number;
  interestRatePercentage: number;
  adminFee: number;
  maxLoanPercentage: number;
  minLoanAmount: number;
  maxLoanAmount: number;
  lastModifiedDate: string;
  lastModifiedBy?: string;
}

const AdminSettings: React.FC = () => {
  const [settings, setSettings] = useState<SystemSettings | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const [formData, setFormData] = useState({
    interestRatePercentage: 5.0,
    adminFee: 50.0,
    maxLoanPercentage: 20.0,
    minLoanAmount: 100.0,
    maxLoanAmount: 10000.0,
  });

  useEffect(() => {
    fetchSettings();
  }, []);

  const fetchSettings = async () => {
    try {
      setLoading(true);
      const data = await apiService.request<SystemSettings>('/systemsettings');
      setSettings(data);
      setFormData({
        interestRatePercentage: data.interestRatePercentage,
        adminFee: data.adminFee,
        maxLoanPercentage: data.maxLoanPercentage,
        minLoanAmount: data.minLoanAmount,
        maxLoanAmount: data.maxLoanAmount,
      });
      setError(null);
    } catch (err: any) {
      setError(err.message || 'Failed to load settings');
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    try {
      setSaving(true);
      setError(null);
      setSaving(true);
      setError(null);
      setSuccess(false);

      const data = await apiService.request<SystemSettings>('/systemsettings', {
        method: 'PUT',
        body: JSON.stringify(formData),
      });

      setSettings(data);
      setSuccess(true);
      
      // Clear success message after 3 seconds
      setTimeout(() => setSuccess(false), 3000);
    } catch (err: any) {
      setError(err.message || 'Failed to update settings');
    } finally {
      setSaving(false);
    }
  };

  const handleChange = (field: keyof typeof formData, value: string) => {
    setFormData((prev) => ({
      ...prev,
      [field]: parseFloat(value) || 0,
    }));
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-gray-600">Loading settings...</div>
      </div>
    );
  }

  return (
    <div className="max-w-4xl mx-auto p-6">
      <div className="bg-white shadow-md rounded-lg p-8">
        <h1 className="text-3xl font-bold text-gray-800 mb-6">System Settings</h1>
        
        {error && (
          <div className="mb-4 p-4 bg-red-100 border border-red-400 text-red-700 rounded">
            {error}
          </div>
        )}

        {success && (
          <div className="mb-4 p-4 bg-green-100 border border-green-400 text-green-700 rounded">
            Settings updated successfully!
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-6">
          {/* Interest Rate */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Interest Rate (%)
            </label>
            <input
              type="number"
              step="0.01"
              min="0"
              max="100"
              value={formData.interestRatePercentage}
              onChange={(e) => handleChange('interestRatePercentage', e.target.value)}
              className="w-full px-4 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              required
            />
            <p className="mt-1 text-sm text-gray-500">
              Current: {formData.interestRatePercentage}% (Applied to all loans)
            </p>
          </div>

          {/* Admin Fee */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Admin Fee (R)
            </label>
            <input
              type="number"
              step="0.01"
              min="0"
              value={formData.adminFee}
              onChange={(e) => handleChange('adminFee', e.target.value)}
              className="w-full px-4 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              required
            />
            <p className="mt-1 text-sm text-gray-500">
              Current: R{formData.adminFee.toFixed(2)} (Fixed fee per loan)
            </p>
          </div>

          {/* Max Loan Percentage */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Max Loan Percentage (%)
            </label>
            <input
              type="number"
              step="0.01"
              min="1"
              max="100"
              value={formData.maxLoanPercentage}
              onChange={(e) => handleChange('maxLoanPercentage', e.target.value)}
              className="w-full px-4 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              required
            />
            <p className="mt-1 text-sm text-gray-500">
              Current: {formData.maxLoanPercentage}% (Maximum % of monthly earnings that can be borrowed)
            </p>
          </div>

          {/* Min Loan Amount */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Minimum Loan Amount (R)
            </label>
            <input
              type="number"
              step="0.01"
              min="0"
              value={formData.minLoanAmount}
              onChange={(e) => handleChange('minLoanAmount', e.target.value)}
              className="w-full px-4 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              required
            />
            <p className="mt-1 text-sm text-gray-500">
              Current: R{formData.minLoanAmount.toFixed(2)}
            </p>
          </div>

          {/* Max Loan Amount */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Maximum Loan Amount (R)
            </label>
            <input
              type="number"
              step="0.01"
              min="0"
              value={formData.maxLoanAmount}
              onChange={(e) => handleChange('maxLoanAmount', e.target.value)}
              className="w-full px-4 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              required
            />
            <p className="mt-1 text-sm text-gray-500">
              Current: R{formData.maxLoanAmount.toFixed(2)}
            </p>
          </div>

          {/* Last Modified Info */}
          {settings && (
            <div className="bg-gray-50 p-4 rounded-md">
              <p className="text-sm text-gray-600">
                <strong>Last Modified:</strong>{' '}
                {new Date(settings.lastModifiedDate).toLocaleString()}
              </p>
              {settings.lastModifiedBy && (
                <p className="text-sm text-gray-600">
                  <strong>Modified By:</strong> {settings.lastModifiedBy}
                </p>
              )}
            </div>
          )}

          {/* Submit Button */}
          <div className="flex justify-end space-x-4">
            <button
              type="button"
              onClick={fetchSettings}
              className="px-6 py-2 border border-gray-300 rounded-md text-gray-700 hover:bg-gray-50 transition"
              disabled={saving}
            >
              Reset
            </button>
            <button
              type="submit"
              className="px-6 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition disabled:bg-gray-400"
              disabled={saving}
            >
              {saving ? 'Saving...' : 'Save Settings'}
            </button>
          </div>
        </form>

        {/* Calculation Example */}
        <div className="mt-8 pt-8 border-t border-gray-200">
          <h2 className="text-xl font-semibold text-gray-800 mb-4">Example Calculation</h2>
          <div className="bg-blue-50 p-4 rounded-md space-y-2 text-sm">
            <p>
              <strong>Worker earns:</strong> 160 hours × R100/hour = R16,000/month
            </p>
            <p>
              <strong>Max loan:</strong> R16,000 × {formData.maxLoanPercentage}% = R
              {(16000 * (formData.maxLoanPercentage / 100)).toFixed(2)}
            </p>
            <p>
              <strong>If borrows R3,000:</strong>
            </p>
            <ul className="ml-6 space-y-1">
              <li>
                Loan amount: R3,000.00
              </li>
              <li>
                Interest ({formData.interestRatePercentage}%): R
                {(3000 * (formData.interestRatePercentage / 100)).toFixed(2)}
              </li>
              <li>
                Admin fee: R{formData.adminFee.toFixed(2)}
              </li>
              <li className="font-semibold pt-1 border-t border-blue-200 mt-2">
                Total repayment: R
                {(
                  3000 +
                  3000 * (formData.interestRatePercentage / 100) +
                  formData.adminFee
                ).toFixed(2)}
              </li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  );
};

export default AdminSettings;
