import React, { useState, useEffect, useCallback } from 'react';
import { useAuthStore } from '../../store/authStore';
import apiService from '../../services/api';

interface Business {
  id: string;
  name: string;
  registrationNumber: string;
  contactPerson: string;
  contactEmail: string;
  contactPhone: string;
  address?: string;
  city: string;
  province: string;
  postalCode?: string;
  payrollContactName?: string;
  payrollContactEmail?: string;
  payrollDay: number;
  maxLoanPercentage: number;
  interestRate: number | null;
  adminFee: number | null;
  isActive: boolean;
  notes?: string;
  createdAt: string;
  updatedAt?: string;
  employeeCount: number;
}

interface Employee {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  idNumber: string;
  employeeNumber: string;
  payrollReference: string;
  employmentType: string;
  monthlyIncome: number;
  isVerified: boolean;
  createdAt: string;
}

interface BusinessFormData {
  name: string;
  registrationNumber: string;
  contactPerson: string;
  contactEmail: string;
  contactPhone: string;
  address: string;
  city: string;
  province: string;
  postalCode: string;
  payrollContactName: string;
  payrollContactEmail: string;
  payrollDay: number;
  maxLoanPercentage: number;
  interestRate: string;
  adminFee: string;
  notes: string;
}

const emptyForm: BusinessFormData = {
  name: '',
  registrationNumber: '',
  contactPerson: '',
  contactEmail: '',
  contactPhone: '',
  address: '',
  city: '',
  province: '',
  postalCode: '',
  payrollContactName: '',
  payrollContactEmail: '',
  payrollDay: 25,
  maxLoanPercentage: 30,
  interestRate: '',
  adminFee: '',
  notes: '',
};

const AdminBusinesses: React.FC = () => {
  const { token } = useAuthStore();
  const [businesses, setBusinesses] = useState<Business[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [showActiveOnly, setShowActiveOnly] = useState(false);
  const [error, setError] = useState('');

  // Modal state
  const [showModal, setShowModal] = useState(false);
  const [editingBusiness, setEditingBusiness] = useState<Business | null>(null);
  const [formData, setFormData] = useState<BusinessFormData>(emptyForm);
  const [saving, setSaving] = useState(false);

  // Detail/employee view
  const [selectedBusiness, setSelectedBusiness] = useState<Business | null>(null);
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [loadingEmployees, setLoadingEmployees] = useState(false);
  const [showDetailView, setShowDetailView] = useState(false);

  const fetchBusinesses = useCallback(async () => {
    try {
      setLoading(true);
      const params = new URLSearchParams();
      if (search) params.append('search', search);
      if (showActiveOnly) params.append('activeOnly', 'true');
      const url = `/admin/businesses${params.toString() ? '?' + params.toString() : ''}`;
      const data = await apiService.request(url, { token: token || '' });
      setBusinesses(data);
    } catch (err: any) {
      setError(err.message || 'Failed to load businesses');
    } finally {
      setLoading(false);
    }
  }, [token, search, showActiveOnly]);

  useEffect(() => {
    fetchBusinesses();
  }, [fetchBusinesses]);

  const fetchEmployees = async (businessId: string) => {
    try {
      setLoadingEmployees(true);
      const data = await apiService.request(`/admin/businesses/${businessId}/employees`, { token: token || '' });
      setEmployees(data);
    } catch (err: any) {
      setError(err.message || 'Failed to load employees');
    } finally {
      setLoadingEmployees(false);
    }
  };

  const handleViewBusiness = async (business: Business) => {
    setSelectedBusiness(business);
    setShowDetailView(true);
    await fetchEmployees(business.id);
  };

  const handleCreateNew = () => {
    setEditingBusiness(null);
    setFormData(emptyForm);
    setShowModal(true);
  };

  const handleEdit = (business: Business) => {
    setEditingBusiness(business);
    setFormData({
      name: business.name,
      registrationNumber: business.registrationNumber || '',
      contactPerson: business.contactPerson || '',
      contactEmail: business.contactEmail || '',
      contactPhone: business.contactPhone || '',
      address: business.address || '',
      city: business.city || '',
      province: business.province || '',
      postalCode: business.postalCode || '',
      payrollContactName: business.payrollContactName || '',
      payrollContactEmail: business.payrollContactEmail || '',
      payrollDay: business.payrollDay,
      maxLoanPercentage: business.maxLoanPercentage,
      interestRate: business.interestRate?.toString() || '',
      adminFee: business.adminFee?.toString() || '',
      notes: business.notes || '',
    });
    setShowModal(true);
  };

  const handleSave = async () => {
    if (!formData.name.trim()) {
      setError('Business name is required');
      return;
    }

    try {
      setSaving(true);
      setError('');
      const payload: any = {
        ...formData,
        interestRate: formData.interestRate ? parseFloat(formData.interestRate) : null,
        adminFee: formData.adminFee ? parseFloat(formData.adminFee) : null,
      };

      if (editingBusiness) {
        await apiService.request(`/admin/businesses/${editingBusiness.id}`, {
          method: 'PUT',
          body: payload,
          token: token || '',
        });
      } else {
        await apiService.request('/admin/businesses', {
          method: 'POST',
          body: payload,
          token: token || '',
        });
      }

      setShowModal(false);
      await fetchBusinesses();
    } catch (err: any) {
      setError(err.message || 'Failed to save business');
    } finally {
      setSaving(false);
    }
  };

  const handleToggleActive = async (business: Business) => {
    try {
      await apiService.request(`/admin/businesses/${business.id}`, {
        method: 'PUT',
        body: { isActive: !business.isActive },
        token: token || '',
      });
      await fetchBusinesses();
    } catch (err: any) {
      setError(err.message || 'Failed to update business status');
    }
  };

  const handleRemoveEmployee = async (userId: string) => {
    if (!selectedBusiness) return;
    if (!confirm('Remove this employee from the business?')) return;

    try {
      await apiService.request(`/admin/businesses/${selectedBusiness.id}/employees/${userId}`, {
        method: 'DELETE',
        token: token || '',
      });
      await fetchEmployees(selectedBusiness.id);
      await fetchBusinesses(); // refresh employee count
    } catch (err: any) {
      setError(err.message || 'Failed to remove employee');
    }
  };

  const handleChange = (field: keyof BusinessFormData, value: string | number) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  if (showDetailView && selectedBusiness) {
    return (
      <div className="p-6">
        {/* Header */}
        <div className="flex items-center justify-between mb-6">
          <div className="flex items-center space-x-3">
            <button
              onClick={() => setShowDetailView(false)}
              className="text-gray-500 hover:text-gray-700"
            >
              <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
              </svg>
            </button>
            <div>
              <h1 className="text-2xl font-bold text-gray-900">{selectedBusiness.name}</h1>
              <p className="text-sm text-gray-500">
                {selectedBusiness.registrationNumber && `Reg: ${selectedBusiness.registrationNumber} · `}
                {selectedBusiness.city}{selectedBusiness.province ? `, ${selectedBusiness.province}` : ''}
              </p>
            </div>
            <span className={`ml-3 px-2 py-1 text-xs rounded-full ${selectedBusiness.isActive ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}`}>
              {selectedBusiness.isActive ? 'Active' : 'Inactive'}
            </span>
          </div>
          <button
            onClick={() => handleEdit(selectedBusiness)}
            className="px-4 py-2 bg-indigo-600 text-white rounded-lg text-sm hover:bg-indigo-700"
          >
            Edit Business
          </button>
        </div>

        {error && (
          <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">{error}</div>
        )}

        {/* Business Details Cards */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
          <div className="bg-white rounded-lg shadow p-4">
            <h3 className="text-sm font-semibold text-gray-500 mb-2">Contact Information</h3>
            <div className="space-y-1 text-sm">
              {selectedBusiness.contactPerson && <p><span className="text-gray-500">Person:</span> {selectedBusiness.contactPerson}</p>}
              {selectedBusiness.contactEmail && <p><span className="text-gray-500">Email:</span> {selectedBusiness.contactEmail}</p>}
              {selectedBusiness.contactPhone && <p><span className="text-gray-500">Phone:</span> {selectedBusiness.contactPhone}</p>}
            </div>
          </div>
          <div className="bg-white rounded-lg shadow p-4">
            <h3 className="text-sm font-semibold text-gray-500 mb-2">Payroll Details</h3>
            <div className="space-y-1 text-sm">
              <p><span className="text-gray-500">Payroll Day:</span> {selectedBusiness.payrollDay}th of month</p>
              {selectedBusiness.payrollContactName && <p><span className="text-gray-500">Payroll Contact:</span> {selectedBusiness.payrollContactName}</p>}
              {selectedBusiness.payrollContactEmail && <p><span className="text-gray-500">Payroll Email:</span> {selectedBusiness.payrollContactEmail}</p>}
            </div>
          </div>
          <div className="bg-white rounded-lg shadow p-4">
            <h3 className="text-sm font-semibold text-gray-500 mb-2">Loan Settings</h3>
            <div className="space-y-1 text-sm">
              <p><span className="text-gray-500">Max Loan %:</span> {selectedBusiness.maxLoanPercentage}%</p>
              <p><span className="text-gray-500">Interest Rate:</span> {selectedBusiness.interestRate != null ? `${selectedBusiness.interestRate}%` : 'System default'}</p>
              <p><span className="text-gray-500">Admin Fee:</span> {selectedBusiness.adminFee != null ? `R${selectedBusiness.adminFee.toFixed(2)}` : 'System default'}</p>
            </div>
          </div>
        </div>

        {/* Employees */}
        <div className="bg-white rounded-lg shadow">
          <div className="p-4 border-b border-gray-200 flex items-center justify-between">
            <h2 className="text-lg font-semibold text-gray-900">
              Employees ({employees.length})
            </h2>
          </div>

          {loadingEmployees ? (
            <div className="p-8 text-center text-gray-500">Loading employees...</div>
          ) : employees.length === 0 ? (
            <div className="p-8 text-center text-gray-500">
              <p>No employees linked to this business yet.</p>
              <p className="text-sm mt-1">Assign employees from User Management.</p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-gray-200">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Name</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Email</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">ID Number</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Employee #</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Income</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Verified</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200">
                  {employees.map((emp) => (
                    <tr key={emp.id} className="hover:bg-gray-50">
                      <td className="px-4 py-3 text-sm font-medium text-gray-900">{emp.firstName} {emp.lastName}</td>
                      <td className="px-4 py-3 text-sm text-gray-500">{emp.email}</td>
                      <td className="px-4 py-3 text-sm text-gray-500">{emp.idNumber}</td>
                      <td className="px-4 py-3 text-sm text-gray-500">{emp.employeeNumber || '-'}</td>
                      <td className="px-4 py-3 text-sm text-gray-500">R{emp.monthlyIncome?.toLocaleString() || '0'}</td>
                      <td className="px-4 py-3 text-sm">
                        <span className={`px-2 py-1 rounded-full text-xs ${emp.isVerified ? 'bg-green-100 text-green-800' : 'bg-yellow-100 text-yellow-800'}`}>
                          {emp.isVerified ? 'Yes' : 'No'}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-sm">
                        <button
                          onClick={() => handleRemoveEmployee(emp.id)}
                          className="text-red-600 hover:text-red-800 text-xs"
                        >
                          Remove
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>
    );
  }

  return (
    <div className="p-6">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Business Management</h1>
          <p className="text-sm text-gray-500 mt-1">Onboard and manage employer businesses for B2B payroll lending</p>
        </div>
        <button
          onClick={handleCreateNew}
          className="px-4 py-2 bg-indigo-600 text-white rounded-lg text-sm font-medium hover:bg-indigo-700 flex items-center space-x-2"
        >
          <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
          </svg>
          <span>Add Business</span>
        </button>
      </div>

      {error && (
        <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
          {error}
          <button onClick={() => setError('')} className="ml-2 text-red-500 hover:text-red-700">&times;</button>
        </div>
      )}

      {/* Filters */}
      <div className="flex items-center space-x-4 mb-6">
        <div className="flex-1">
          <input
            type="text"
            placeholder="Search businesses..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="w-full border border-gray-300 rounded-lg px-4 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-500"
          />
        </div>
        <label className="flex items-center space-x-2 text-sm text-gray-600">
          <input
            type="checkbox"
            checked={showActiveOnly}
            onChange={(e) => setShowActiveOnly(e.target.checked)}
            className="rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
          />
          <span>Active only</span>
        </label>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
        <div className="bg-white rounded-lg shadow p-4">
          <p className="text-sm text-gray-500">Total Businesses</p>
          <p className="text-2xl font-bold text-gray-900">{businesses.length}</p>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <p className="text-sm text-gray-500">Active</p>
          <p className="text-2xl font-bold text-green-600">{businesses.filter(b => b.isActive).length}</p>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <p className="text-sm text-gray-500">Total Employees</p>
          <p className="text-2xl font-bold text-indigo-600">{businesses.reduce((sum, b) => sum + b.employeeCount, 0)}</p>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <p className="text-sm text-gray-500">Inactive</p>
          <p className="text-2xl font-bold text-red-600">{businesses.filter(b => !b.isActive).length}</p>
        </div>
      </div>

      {/* Business List */}
      {loading ? (
        <div className="text-center py-12 text-gray-500">Loading businesses...</div>
      ) : businesses.length === 0 ? (
        <div className="text-center py-12 bg-white rounded-lg shadow">
          <svg className="mx-auto h-12 w-12 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
          </svg>
          <h3 className="mt-2 text-sm font-medium text-gray-900">No businesses yet</h3>
          <p className="mt-1 text-sm text-gray-500">Get started by adding a business.</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {businesses.map((business) => (
            <div
              key={business.id}
              className={`bg-white rounded-lg shadow hover:shadow-md transition-shadow cursor-pointer border-l-4 ${
                business.isActive ? 'border-green-500' : 'border-gray-300'
              }`}
              onClick={() => handleViewBusiness(business)}
            >
              <div className="p-4">
                <div className="flex items-start justify-between mb-2">
                  <div>
                    <h3 className="font-semibold text-gray-900">{business.name}</h3>
                    {business.registrationNumber && (
                      <p className="text-xs text-gray-500">Reg: {business.registrationNumber}</p>
                    )}
                  </div>
                  <span className={`px-2 py-1 text-xs rounded-full ${business.isActive ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}`}>
                    {business.isActive ? 'Active' : 'Inactive'}
                  </span>
                </div>

                <div className="space-y-1 text-sm text-gray-600 mb-3">
                  {business.contactPerson && <p>Contact: {business.contactPerson}</p>}
                  <p>{business.city}{business.province ? `, ${business.province}` : ''}</p>
                  <p>Payroll Day: {business.payrollDay}th</p>
                </div>

                <div className="flex items-center justify-between text-sm">
                  <span className="text-indigo-600 font-medium">{business.employeeCount} employees</span>
                  <div className="flex items-center space-x-2">
                    <button
                      onClick={(e) => { e.stopPropagation(); handleEdit(business); }}
                      className="text-gray-400 hover:text-indigo-600"
                      title="Edit"
                    >
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                      </svg>
                    </button>
                    <button
                      onClick={(e) => { e.stopPropagation(); handleToggleActive(business); }}
                      className={`text-xs ${business.isActive ? 'text-red-500 hover:text-red-700' : 'text-green-500 hover:text-green-700'}`}
                      title={business.isActive ? 'Deactivate' : 'Activate'}
                    >
                      {business.isActive ? 'Deactivate' : 'Activate'}
                    </button>
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Create/Edit Modal */}
      {showModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-xl shadow-xl max-w-2xl w-full mx-4 max-h-[90vh] overflow-y-auto p-6">
            <h2 className="text-xl font-bold text-gray-900 mb-6">
              {editingBusiness ? 'Edit Business' : 'Add New Business'}
            </h2>

            {error && (
              <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">{error}</div>
            )}

            <div className="space-y-6">
              {/* Basic Info */}
              <div>
                <h3 className="font-semibold text-gray-900 mb-3">Business Information</h3>
                <div className="grid grid-cols-2 gap-4">
                  <div className="col-span-2">
                    <label className="block text-sm font-medium text-gray-700 mb-1">Business Name *</label>
                    <input
                      type="text"
                      value={formData.name}
                      onChange={(e) => handleChange('name', e.target.value)}
                      className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                      placeholder="e.g. Acme Holdings (Pty) Ltd"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Registration Number</label>
                    <input
                      type="text"
                      value={formData.registrationNumber}
                      onChange={(e) => handleChange('registrationNumber', e.target.value)}
                      className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                      placeholder="e.g. 2024/123456/07"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Province</label>
                    <select
                      value={formData.province}
                      onChange={(e) => handleChange('province', e.target.value)}
                      className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    >
                      <option value="">Select Province</option>
                      <option value="Gauteng">Gauteng</option>
                      <option value="Western Cape">Western Cape</option>
                      <option value="KwaZulu-Natal">KwaZulu-Natal</option>
                      <option value="Eastern Cape">Eastern Cape</option>
                      <option value="Free State">Free State</option>
                      <option value="Limpopo">Limpopo</option>
                      <option value="Mpumalanga">Mpumalanga</option>
                      <option value="North West">North West</option>
                      <option value="Northern Cape">Northern Cape</option>
                    </select>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">City</label>
                    <input
                      type="text"
                      value={formData.city}
                      onChange={(e) => handleChange('city', e.target.value)}
                      className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Postal Code</label>
                    <input
                      type="text"
                      value={formData.postalCode}
                      onChange={(e) => handleChange('postalCode', e.target.value)}
                      className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    />
                  </div>
                  <div className="col-span-2">
                    <label className="block text-sm font-medium text-gray-700 mb-1">Street Address</label>
                    <input
                      type="text"
                      value={formData.address}
                      onChange={(e) => handleChange('address', e.target.value)}
                      className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    />
                  </div>
                </div>
              </div>

              {/* Contact Info */}
              <div>
                <h3 className="font-semibold text-gray-900 mb-3">Contact Person</h3>
                <div className="grid grid-cols-2 gap-4">
                  <div className="col-span-2">
                    <label className="block text-sm font-medium text-gray-700 mb-1">Contact Person Name</label>
                    <input
                      type="text"
                      value={formData.contactPerson}
                      onChange={(e) => handleChange('contactPerson', e.target.value)}
                      className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Contact Email</label>
                    <input
                      type="email"
                      value={formData.contactEmail}
                      onChange={(e) => handleChange('contactEmail', e.target.value)}
                      className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Contact Phone</label>
                    <input
                      type="tel"
                      value={formData.contactPhone}
                      onChange={(e) => handleChange('contactPhone', e.target.value)}
                      className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    />
                  </div>
                </div>
              </div>

              {/* Payroll Info */}
              <div>
                <h3 className="font-semibold text-gray-900 mb-3">Payroll Information</h3>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Payroll Day (day of month)</label>
                    <input
                      type="number"
                      min={1}
                      max={31}
                      value={formData.payrollDay}
                      onChange={(e) => handleChange('payrollDay', parseInt(e.target.value) || 25)}
                      className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Payroll Contact Name</label>
                    <input
                      type="text"
                      value={formData.payrollContactName}
                      onChange={(e) => handleChange('payrollContactName', e.target.value)}
                      className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    />
                  </div>
                  <div className="col-span-2">
                    <label className="block text-sm font-medium text-gray-700 mb-1">Payroll Contact Email</label>
                    <input
                      type="email"
                      value={formData.payrollContactEmail}
                      onChange={(e) => handleChange('payrollContactEmail', e.target.value)}
                      className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    />
                  </div>
                </div>
              </div>

              {/* Loan Settings */}
              <div>
                <h3 className="font-semibold text-gray-900 mb-3">Loan Settings</h3>
                <div className="bg-blue-50 border border-blue-200 rounded-lg p-3 mb-3">
                  <p className="text-sm text-blue-700">Leave interest rate and admin fee blank to use system defaults.</p>
                </div>
                <div className="grid grid-cols-3 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Max Loan % of Salary</label>
                    <input
                      type="number"
                      min={0}
                      max={100}
                      value={formData.maxLoanPercentage}
                      onChange={(e) => handleChange('maxLoanPercentage', parseFloat(e.target.value) || 0)}
                      className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Interest Rate (%)</label>
                    <input
                      type="text"
                      value={formData.interestRate}
                      onChange={(e) => handleChange('interestRate', e.target.value)}
                      placeholder="System default"
                      className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Admin Fee (R)</label>
                    <input
                      type="text"
                      value={formData.adminFee}
                      onChange={(e) => handleChange('adminFee', e.target.value)}
                      placeholder="System default"
                      className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    />
                  </div>
                </div>
              </div>

              {/* Notes */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Notes</label>
                <textarea
                  value={formData.notes}
                  onChange={(e) => handleChange('notes', e.target.value)}
                  rows={3}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                  placeholder="Internal notes about this business..."
                />
              </div>

              {/* Actions */}
              <div className="flex justify-end space-x-3 pt-4 border-t border-gray-200">
                <button
                  onClick={() => setShowModal(false)}
                  className="px-4 py-2 border border-gray-300 rounded-lg text-sm font-medium text-gray-700 hover:bg-gray-50"
                >
                  Cancel
                </button>
                <button
                  onClick={handleSave}
                  disabled={saving}
                  className="px-6 py-2 bg-indigo-600 text-white rounded-lg text-sm font-medium hover:bg-indigo-700 disabled:opacity-50"
                >
                  {saving ? 'Saving...' : editingBusiness ? 'Save Changes' : 'Create Business'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default AdminBusinesses;
