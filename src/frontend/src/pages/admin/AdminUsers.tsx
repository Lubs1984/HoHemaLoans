import React, { useEffect, useState } from 'react';
import { apiService } from '../../services/api';
import { CheckCircleIcon, XCircleIcon, EyeIcon, PencilIcon } from '@heroicons/react/24/outline';

interface UserProfile {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  idNumber?: string;
  monthlyIncome: number;
  isVerified: boolean;
  streetAddress?: string;
  city?: string;
  province?: string;
  postalCode?: string;
  employerName?: string;
  employmentType?: string;
  businessId?: string;
  businessName?: string;
  roles?: string[];
  createdAt: string;
  loanApplications?: Array<{
    id: string;
    amount: number;
    status: string;
    applicationDate: string;
  }>;
}

interface UsersResponse {
  totalCount: number;
  pageCount: number;
  currentPage: number;
  pageSize: number;
  data: UserProfile[];
}

const AdminUsers: React.FC = () => {
  const [users, setUsers] = useState<UserProfile[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [search, setSearch] = useState('');
  const [selectedUser, setSelectedUser] = useState<UserProfile | null>(null);
  const [editingUser, setEditingUser] = useState<UserProfile | null>(null);

  const fetchUsers = async (page: number = 1, searchTerm: string = '') => {
    try {
      setLoading(true);
      const params = new URLSearchParams({
        page: page.toString(),
        pageSize: '20',
      });

      if (searchTerm) params.append('search', searchTerm);

      const response = await apiService.request<UsersResponse>(`/admin/users?${params}`);
      setUsers(response.data);
      setTotalPages(response.pageCount);
      setCurrentPage(response.currentPage);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load users');
    } finally {
      setLoading(false);
    }
  };

  const handleUserUpdated = (updatedUser: UserProfile) => {
    setUsers(prev => prev.map(u => u.id === updatedUser.id ? updatedUser : u));
    setEditingUser(null);
  };

  useEffect(() => {
    fetchUsers(1, search);
  }, [search]);

  if (error) {
    return (
      <div className="bg-red-50 border border-red-200 rounded-lg p-4">
        <p className="text-red-700">{error}</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div>
        <h1 className="text-3xl font-bold text-gray-900">User Management</h1>
        <p className="text-gray-600 mt-2">View and manage registered users</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <StatCard
          title="Total Users"
          value={users.length}
          color="blue"
        />
        <StatCard
          title="Verified"
          value={users.filter(u => u.isVerified).length}
          color="green"
        />
        <StatCard
          title="Pending Verification"
          value={users.filter(u => !u.isVerified).length}
          color="yellow"
        />
        <StatCard
          title="Admins"
          value={users.filter(u => u.roles?.includes('Admin')).length}
          color="blue"
        />
      </div>

      {/* Search */}
      <div className="bg-white rounded-lg shadow p-4">
        <input
          type="text"
          placeholder="Search by name or email..."
          value={search}
          onChange={(e) => { setSearch(e.target.value); setCurrentPage(1); }}
          className="w-full border border-gray-300 rounded-lg px-4 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>

      {/* Users Table */}
      <div className="bg-white rounded-lg shadow overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">
                  Name
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">
                  Email
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">
                  Phone
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">
                  Roles
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">
                  Verified
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">
                  Joined
                </th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-700 uppercase tracking-wider">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200">
              {loading ? (
                <tr>
                  <td colSpan={7} className="px-6 py-4 text-center">
                    <div className="flex justify-center">
                      <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
                    </div>
                  </td>
                </tr>
              ) : users.length === 0 ? (
                <tr>
                  <td colSpan={7} className="px-6 py-4 text-center text-gray-500">
                    No users found
                  </td>
                </tr>
              ) : (
                users.map((user) => (
                  <tr key={user.id} className="hover:bg-gray-50 transition">
                    <td className="px-6 py-4">
                      <p className="font-medium text-gray-900">
                        {user.firstName} {user.lastName}
                      </p>
                    </td>
                    <td className="px-6 py-4 text-sm text-gray-600">
                      {user.email}
                    </td>
                    <td className="px-6 py-4 text-sm text-gray-600">
                      {user.phoneNumber}
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex flex-wrap gap-1">
                        {user.roles?.map((role) => (
                          <span
                            key={role}
                            className={`inline-flex px-2 py-0.5 rounded-full text-xs font-medium ${
                              role === 'Admin'
                                ? 'bg-purple-100 text-purple-800'
                                : 'bg-blue-100 text-blue-800'
                            }`}
                          >
                            {role}
                          </span>
                        ))}
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      {user.isVerified ? (
                        <span className="inline-flex items-center space-x-1 px-3 py-1 rounded-full text-sm font-medium bg-green-100 text-green-800">
                          <CheckCircleIcon className="h-4 w-4" />
                          <span>Verified</span>
                        </span>
                      ) : (
                        <span className="inline-flex items-center space-x-1 px-3 py-1 rounded-full text-sm font-medium bg-yellow-100 text-yellow-800">
                          <XCircleIcon className="h-4 w-4" />
                          <span>Pending</span>
                        </span>
                      )}
                    </td>
                    <td className="px-6 py-4 text-sm text-gray-600">
                      {new Date(user.createdAt).toLocaleDateString()}
                    </td>
                    <td className="px-6 py-4 text-right">
                      <div className="flex items-center justify-end space-x-3">
                        <button
                          onClick={() => setSelectedUser(user)}
                          className="inline-flex items-center space-x-1 text-blue-600 hover:text-blue-800 font-medium"
                        >
                          <EyeIcon className="h-4 w-4" />
                          <span>View</span>
                        </button>
                        <button
                          onClick={() => setEditingUser(user)}
                          className="inline-flex items-center space-x-1 text-indigo-600 hover:text-indigo-800 font-medium"
                        >
                          <PencilIcon className="h-4 w-4" />
                          <span>Edit</span>
                        </button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        {totalPages > 1 && (
          <div className="bg-gray-50 px-6 py-4 flex items-center justify-between">
            <p className="text-sm text-gray-600">
              Page {currentPage} of {totalPages}
            </p>
            <div className="flex space-x-2">
              <button
                onClick={() => fetchUsers(Math.max(1, currentPage - 1), search)}
                disabled={currentPage === 1}
                className="px-4 py-2 border border-gray-300 rounded-lg text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50"
              >
                Previous
              </button>
              <button
                onClick={() => fetchUsers(Math.min(totalPages, currentPage + 1), search)}
                disabled={currentPage === totalPages}
                className="px-4 py-2 border border-gray-300 rounded-lg text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50"
              >
                Next
              </button>
            </div>
          </div>
        )}
      </div>

      {/* Detail Modal */}
      {selectedUser && (
        <UserDetailModal user={selectedUser} onClose={() => setSelectedUser(null)} />
      )}

      {/* Edit Modal */}
      {editingUser && (
        <EditUserModal
          user={editingUser}
          onClose={() => setEditingUser(null)}
          onSaved={handleUserUpdated}
        />
      )}
    </div>
  );
};

interface StatCardProps {
  title: string;
  value: number;
  color: 'blue' | 'green' | 'yellow';
}

const StatCard: React.FC<StatCardProps> = ({ title, value, color }) => {
  const colorClasses = {
    blue: 'bg-blue-50 text-blue-700',
    green: 'bg-green-50 text-green-700',
    yellow: 'bg-yellow-50 text-yellow-700',
  };

  return (
    <div className={`rounded-lg shadow p-6 ${colorClasses[color]}`}>
      <p className="text-sm font-medium opacity-75">{title}</p>
      <p className="text-3xl font-bold mt-2">{value}</p>
    </div>
  );
};

interface UserDetailModalProps {
  user: UserProfile;
  onClose: () => void;
}

const UserDetailModal: React.FC<UserDetailModalProps> = ({ user, onClose }) => {
  return (
    <div className="fixed inset-0 z-50 bg-black bg-opacity-50 flex items-center justify-center p-4">
      <div className="bg-white rounded-lg max-w-2xl w-full max-h-[80vh] overflow-y-auto">
        <div className="sticky top-0 bg-gradient-to-r from-blue-500 to-blue-600 text-white px-6 py-4 flex items-center justify-between">
          <h2 className="text-xl font-bold">User Details</h2>
          <button onClick={onClose} className="text-2xl hover:bg-blue-500 p-1 rounded">&times;</button>
        </div>

        <div className="p-6 space-y-4">
          {/* Personal Info */}
          <div>
            <h3 className="font-semibold text-gray-900 mb-2">Personal Information</h3>
            <div className="grid grid-cols-2 gap-4 bg-gray-50 p-4 rounded-lg">
              <div>
                <p className="text-sm text-gray-600">First Name</p>
                <p className="font-medium">{user.firstName}</p>
              </div>
              <div>
                <p className="text-sm text-gray-600">Last Name</p>
                <p className="font-medium">{user.lastName}</p>
              </div>
              <div>
                <p className="text-sm text-gray-600">Email</p>
                <p className="font-medium">{user.email}</p>
              </div>
              <div>
                <p className="text-sm text-gray-600">Phone</p>
                <p className="font-medium">{user.phoneNumber}</p>
              </div>
              {user.idNumber && (
                <div>
                  <p className="text-sm text-gray-600">ID Number</p>
                  <p className="font-medium">{user.idNumber}</p>
                </div>
              )}
            </div>
          </div>

          {/* Roles */}
          <div>
            <h3 className="font-semibold text-gray-900 mb-2">Roles</h3>
            <div className="bg-gray-50 p-4 rounded-lg flex flex-wrap gap-2">
              {user.roles?.map((role) => (
                <span
                  key={role}
                  className={`inline-flex px-3 py-1 rounded-full text-sm font-medium ${
                    role === 'Admin'
                      ? 'bg-purple-100 text-purple-800'
                      : 'bg-blue-100 text-blue-800'
                  }`}
                >
                  {role}
                </span>
              ))}
              {(!user.roles || user.roles.length === 0) && (
                <span className="text-gray-500 text-sm">No roles assigned</span>
              )}
            </div>
          </div>

          {/* Financial Info */}
          <div>
            <h3 className="font-semibold text-gray-900 mb-2">Financial Information</h3>
            <div className="grid grid-cols-2 gap-4 bg-gray-50 p-4 rounded-lg">
              <div>
                <p className="text-sm text-gray-600">Monthly Income</p>
                <p className="font-medium">R {user.monthlyIncome?.toLocaleString() || '0'}</p>
              </div>
              <div>
                <p className="text-sm text-gray-600">Verified</p>
                <p className="font-medium flex items-center space-x-1">
                  {user.isVerified ? (
                    <>
                      <CheckCircleIcon className="h-5 w-5 text-green-600" />
                      <span className="text-green-600">Yes</span>
                    </>
                  ) : (
                    <>
                      <XCircleIcon className="h-5 w-5 text-yellow-600" />
                      <span className="text-yellow-600">Pending</span>
                    </>
                  )}
                </p>
              </div>
            </div>
          </div>

          {/* Address & Employment */}
          {(user.streetAddress || user.employerName) && (
            <div>
              <h3 className="font-semibold text-gray-900 mb-2">Address & Employment</h3>
              <div className="grid grid-cols-2 gap-4 bg-gray-50 p-4 rounded-lg">
                {user.streetAddress && (
                  <div>
                    <p className="text-sm text-gray-600">Address</p>
                    <p className="font-medium">{[user.streetAddress, user.city, user.province, user.postalCode].filter(Boolean).join(', ')}</p>
                  </div>
                )}
                {user.employerName && (
                  <div>
                    <p className="text-sm text-gray-600">Employer</p>
                    <p className="font-medium">
                      {user.businessName || user.employerName}
                      {user.businessName && <span className="ml-1 text-xs text-indigo-500">(linked)</span>}
                    </p>
                  </div>
                )}
                {user.employmentType && (
                  <div>
                    <p className="text-sm text-gray-600">Employment Type</p>
                    <p className="font-medium">{user.employmentType}</p>
                  </div>
                )}
              </div>
            </div>
          )}

          {/* Account Info */}
          <div>
            <h3 className="font-semibold text-gray-900 mb-2">Account Information</h3>
            <div className="bg-gray-50 p-4 rounded-lg">
              <p className="text-sm text-gray-600">Member Since</p>
              <p className="font-medium">{new Date(user.createdAt).toLocaleDateString()}</p>
              <p className="text-xs text-gray-500 mt-1">
                ID: {user.id}
              </p>
            </div>
          </div>

          {/* Loan History */}
          {user.loanApplications && user.loanApplications.length > 0 && (
            <div>
              <h3 className="font-semibold text-gray-900 mb-2">Loan Applications</h3>
              <div className="space-y-2">
                {user.loanApplications.map((loan) => (
                  <div key={loan.id} className="bg-gray-50 p-3 rounded-lg">
                    <div className="flex items-center justify-between">
                      <div>
                        <p className="font-medium">R {loan.amount.toLocaleString()}</p>
                        <p className="text-xs text-gray-600">{new Date(loan.applicationDate).toLocaleDateString()}</p>
                      </div>
                      <span className={`px-3 py-1 rounded-full text-sm font-medium ${
                        loan.status === 'Approved' ? 'bg-green-100 text-green-800' :
                        loan.status === 'Rejected' ? 'bg-red-100 text-red-800' :
                        'bg-yellow-100 text-yellow-800'
                      }`}>
                        {loan.status}
                      </span>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

// ============= Edit User Modal =============

interface EditUserModalProps {
  user: UserProfile;
  onClose: () => void;
  onSaved: (user: UserProfile) => void;
}

const AVAILABLE_ROLES = ['Admin', 'User'];

const EditUserModal: React.FC<EditUserModalProps> = ({ user, onClose, onSaved }) => {
  const [formData, setFormData] = useState({
    firstName: user.firstName || '',
    lastName: user.lastName || '',
    email: user.email || '',
    phoneNumber: user.phoneNumber || '',
    idNumber: user.idNumber || '',
    isVerified: user.isVerified || false,
    streetAddress: user.streetAddress || '',
    city: user.city || '',
    province: user.province || '',
    postalCode: user.postalCode || '',
    employerName: user.employerName || '',
    employmentType: user.employmentType || '',
    businessId: user.businessId || '',
    roles: user.roles || ['User'],
    newPassword: '',
  });
  const [showPassword, setShowPassword] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [businesses, setBusinesses] = useState<Array<{ id: string; name: string }>>([]);

  useEffect(() => {
    const fetchBusinesses = async () => {
      try {
        const token = localStorage.getItem('auth-store');
        const parsed = token ? JSON.parse(token) : null;
        const authToken = parsed?.state?.token || '';
        const data = await apiService.request('/admin/businesses?activeOnly=true', { token: authToken });
        setBusinesses(data.map((b: any) => ({ id: b.id, name: b.name })));
      } catch (err) {
        console.error('Failed to fetch businesses', err);
      }
    };
    fetchBusinesses();
  }, []);

  const handleChange = (field: string, value: string | boolean) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  const handleRoleToggle = (role: string) => {
    setFormData(prev => {
      const currentRoles = prev.roles;
      if (currentRoles.includes(role)) {
        // Don't allow removing all roles
        if (currentRoles.length <= 1) return prev;
        return { ...prev, roles: currentRoles.filter(r => r !== role) };
      }
      return { ...prev, roles: [...currentRoles, role] };
    });
  };

  const handleSave = async () => {
    try {
      setSaving(true);
      setError(null);
      const { newPassword, businessId, ...updateData } = formData;
      const payload: any = { ...updateData };
      if (newPassword) payload.newPassword = newPassword;
      
      // Handle business assignment
      if (businessId) {
        payload.businessId = businessId;
      } else {
        // If clearing business assignment
        payload.businessId = null;
        payload.clearBusiness = true;
      }

      const response = await apiService.request<UserProfile>(`/admin/users/${user.id}`, {
        method: 'PUT',
        body: JSON.stringify(payload),
      });
      onSaved(response);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update user');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 bg-black bg-opacity-50 flex items-center justify-center p-4">
      <div className="bg-white rounded-lg max-w-2xl w-full max-h-[80vh] overflow-y-auto">
        <div className="sticky top-0 bg-gradient-to-r from-indigo-500 to-indigo-600 text-white px-6 py-4 flex items-center justify-between">
          <h2 className="text-xl font-bold">Edit User</h2>
          <button onClick={onClose} className="text-2xl hover:bg-indigo-500 p-1 rounded">&times;</button>
        </div>

        <div className="p-6 space-y-6">
          {error && (
            <div className="bg-red-50 border border-red-200 rounded-lg p-3">
              <p className="text-red-700 text-sm">{error}</p>
            </div>
          )}

          {/* Roles */}
          <div>
            <h3 className="font-semibold text-gray-900 mb-3">Roles</h3>
            <div className="flex flex-wrap gap-3">
              {AVAILABLE_ROLES.map((role) => (
                <button
                  key={role}
                  type="button"
                  onClick={() => handleRoleToggle(role)}
                  className={`px-4 py-2 rounded-lg border-2 text-sm font-medium transition-all ${
                    formData.roles.includes(role)
                      ? role === 'Admin'
                        ? 'border-purple-500 bg-purple-50 text-purple-700'
                        : 'border-blue-500 bg-blue-50 text-blue-700'
                      : 'border-gray-200 bg-white text-gray-500 hover:border-gray-400'
                  }`}
                >
                  {formData.roles.includes(role) && (
                    <CheckCircleIcon className="h-4 w-4 inline mr-1" />
                  )}
                  {role}
                </button>
              ))}
            </div>
          </div>

          {/* Personal Details */}
          <div>
            <h3 className="font-semibold text-gray-900 mb-3">Personal Details</h3>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">First Name</label>
                <input
                  type="text"
                  value={formData.firstName}
                  onChange={(e) => handleChange('firstName', e.target.value)}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Last Name</label>
                <input
                  type="text"
                  value={formData.lastName}
                  onChange={(e) => handleChange('lastName', e.target.value)}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Email</label>
                <input
                  type="email"
                  value={formData.email}
                  onChange={(e) => handleChange('email', e.target.value)}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Phone Number</label>
                <input
                  type="text"
                  value={formData.phoneNumber}
                  onChange={(e) => handleChange('phoneNumber', e.target.value)}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">ID Number</label>
                <input
                  type="text"
                  value={formData.idNumber}
                  onChange={(e) => handleChange('idNumber', e.target.value)}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                />
              </div>
              <div className="flex items-center pt-6">
                <label className="flex items-center space-x-2 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={formData.isVerified}
                    onChange={(e) => handleChange('isVerified', e.target.checked)}
                    className="w-4 h-4 text-indigo-600 border-gray-300 rounded focus:ring-indigo-500"
                  />
                  <span className="text-sm font-medium text-gray-700">Verified User</span>
                </label>
              </div>
            </div>
          </div>

          {/* Address */}
          <div>
            <h3 className="font-semibold text-gray-900 mb-3">Address</h3>
            <div className="grid grid-cols-2 gap-4">
              <div className="col-span-2">
                <label className="block text-sm font-medium text-gray-700 mb-1">Street Address</label>
                <input
                  type="text"
                  value={formData.streetAddress}
                  onChange={(e) => handleChange('streetAddress', e.target.value)}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                />
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
                <label className="block text-sm font-medium text-gray-700 mb-1">Province</label>
                <select
                  value={formData.province}
                  onChange={(e) => handleChange('province', e.target.value)}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                >
                  <option value="">Select Province</option>
                  <option value="Eastern Cape">Eastern Cape</option>
                  <option value="Free State">Free State</option>
                  <option value="Gauteng">Gauteng</option>
                  <option value="KwaZulu-Natal">KwaZulu-Natal</option>
                  <option value="Limpopo">Limpopo</option>
                  <option value="Mpumalanga">Mpumalanga</option>
                  <option value="North West">North West</option>
                  <option value="Northern Cape">Northern Cape</option>
                  <option value="Western Cape">Western Cape</option>
                </select>
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
            </div>
          </div>

          {/* Employment */}
          <div>
            <h3 className="font-semibold text-gray-900 mb-3">Employment</h3>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Business / Employer</label>
                <select
                  value={formData.businessId}
                  onChange={(e) => {
                    handleChange('businessId', e.target.value);
                    // Auto-set employer name from business selection
                    const biz = businesses.find(b => b.id === e.target.value);
                    if (biz) handleChange('employerName', biz.name);
                  }}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                >
                  <option value="">-- No Business --</option>
                  {businesses.map((b) => (
                    <option key={b.id} value={b.id}>{b.name}</option>
                  ))}
                </select>
                {!formData.businessId && formData.employerName && (
                  <p className="text-xs text-amber-600 mt-1">Legacy: "{formData.employerName}" (not linked)</p>
                )}
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Employment Type</label>
                <select
                  value={formData.employmentType}
                  onChange={(e) => handleChange('employmentType', e.target.value)}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                >
                  <option value="">Select Type</option>
                  <option value="Permanent">Permanent</option>
                  <option value="Contract">Contract</option>
                  <option value="Self-Employed">Self-Employed</option>
                  <option value="Unemployed">Unemployed</option>
                </select>
              </div>
            </div>
          </div>

          {/* Password Reset */}
          <div>
            <h3 className="font-semibold text-gray-900 mb-3">Set Password</h3>
            <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-3 mb-3">
              <p className="text-sm text-yellow-700">Leave blank to keep the current password. Minimum 6 characters with uppercase, lowercase, digit, and special character.</p>
            </div>
            <div className="relative">
              <label className="block text-sm font-medium text-gray-700 mb-1">New Password</label>
              <div className="flex gap-2">
                <input
                  type={showPassword ? 'text' : 'password'}
                  value={formData.newPassword}
                  onChange={(e) => handleChange('newPassword', e.target.value)}
                  placeholder="Enter new password..."
                  className="flex-1 border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="px-3 py-2 border border-gray-300 rounded-lg text-sm text-gray-600 hover:bg-gray-50"
                >
                  {showPassword ? 'Hide' : 'Show'}
                </button>
              </div>
            </div>
          </div>

          {/* Save / Cancel */}
          <div className="flex justify-end space-x-3 pt-4 border-t border-gray-200">
            <button
              onClick={onClose}
              className="px-4 py-2 border border-gray-300 rounded-lg text-sm font-medium text-gray-700 hover:bg-gray-50"
            >
              Cancel
            </button>
            <button
              onClick={handleSave}
              disabled={saving}
              className="px-6 py-2 bg-indigo-600 text-white rounded-lg text-sm font-medium hover:bg-indigo-700 disabled:opacity-50"
            >
              {saving ? 'Saving...' : 'Save Changes'}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default AdminUsers;
