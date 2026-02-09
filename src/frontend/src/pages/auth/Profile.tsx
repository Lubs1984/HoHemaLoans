import React, { useState, useEffect } from 'react';
import { useLocation } from 'react-router-dom';
import { useAuthStore } from '../../store/authStore';
import { apiService } from '../../services/api';
import { useToast } from '../../contexts/ToastContext';
import { DocumentUpload } from '../../components/documents/DocumentUpload';
import { DocumentList, type Document } from '../../components/documents/DocumentList';

const Profile: React.FC = () => {
  const { user, setUser } = useAuthStore();
  const { success, error: showError } = useToast();
  const location = useLocation();
  const [activeTab, setActiveTab] = useState<'personal' | 'documents'>(() => {
    // Set initial tab based on route
    return location.pathname === '/documents' ? 'documents' : 'personal';
  });
  const [documents, setDocuments] = useState<Document[]>([]);
  const [loading, setLoading] = useState(false);
  const [isEditing, setIsEditing] = useState(false);
  const [formData, setFormData] = useState({
    firstName: user?.firstName || '',
    lastName: user?.lastName || '',
    phoneNumber: user?.phoneNumber || '',
    streetAddress: user?.streetAddress || '',
    suburb: user?.suburb || '',
    city: user?.city || '',
    province: user?.province || '',
    postalCode: user?.postalCode || '',
    employerName: user?.employerName || '',
    employmentType: user?.employmentType || '',
    employeeNumber: user?.employeeNumber || '',
    payrollReference: user?.payrollReference || '',
    bankName: user?.bankName || '',
    accountType: user?.accountType || '',
    accountNumber: user?.accountNumber || '',
    branchCode: user?.branchCode || '',
    nextOfKinName: user?.nextOfKinName || '',
    nextOfKinRelationship: user?.nextOfKinRelationship || '',
    nextOfKinPhone: user?.nextOfKinPhone || '',
  });
  const [saving, setSaving] = useState(false);

  // Update tab when route changes
  useEffect(() => {
    if (location.pathname === '/documents') {
      setActiveTab('documents');
    }
  }, [location.pathname]);

  useEffect(() => {
    if (activeTab === 'documents') loadDocuments();
  }, [activeTab]);

  const loadDocuments = async () => {
    try {
      setLoading(true);
      const response = await apiService.get('/documents');
      setDocuments(response.data as Document[]);
    } catch (error) {
      showError('Failed to load documents. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const handleEdit = () => {
    setFormData({
      firstName: user?.firstName || '',
      lastName: user?.lastName || '',
      phoneNumber: user?.phoneNumber || '',
      streetAddress: user?.streetAddress || '',
      suburb: user?.suburb || '',
      city: user?.city || '',
      province: user?.province || '',
      postalCode: user?.postalCode || '',
      employerName: user?.employerName || '',
      employmentType: user?.employmentType || '',
      employeeNumber: user?.employeeNumber || '',
      payrollReference: user?.payrollReference || '',
      bankName: user?.bankName || '',
      accountType: user?.accountType || '',
      accountNumber: user?.accountNumber || '',
      branchCode: user?.branchCode || '',
      nextOfKinName: user?.nextOfKinName || '',
      nextOfKinRelationship: user?.nextOfKinRelationship || '',
      nextOfKinPhone: user?.nextOfKinPhone || '',
    });
    setIsEditing(true);
  };

  const handleCancel = () => {
    setIsEditing(false);
  };

  const handleSave = async () => {
    try {
      setSaving(true);
      const response = await apiService.updateProfile(formData);
      setUser(response as any);
      setIsEditing(false);
      success('Profile updated successfully!');
    } catch (error: any) {
      const errorMessage = error.message || 'Failed to update profile';
      showError(errorMessage);
    } finally {
      setSaving(false);
    }
  };

  const handleInputChange = (field: string, value: string) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  return (
    <div className="max-w-4xl mx-auto">
      <div className="mb-8 flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Profile</h1>
          <p className="text-gray-600">Manage your profile information and documents</p>
        </div>
        {activeTab === 'personal' && !isEditing && (
          <button
            onClick={handleEdit}
            className="btn btn-primary"
          >
            Edit Profile
          </button>
        )}
        {activeTab === 'personal' && isEditing && (
          <div className="flex gap-2">
            <button
              onClick={handleCancel}
              className="btn btn-secondary"
              disabled={saving}
            >
              Cancel
            </button>
            <button
              onClick={handleSave}
              className="btn btn-primary"
              disabled={saving}
            >
              {saving ? 'Saving...' : 'Save Changes'}
            </button>
          </div>
        )}
      </div>

      {/* Tabs */}
      <div className="border-b border-gray-200 mb-8">
        <div className="flex space-x-8">
          {(['personal', 'documents'] as const).map((tab) => (
            <button
              key={tab}
              onClick={() => setActiveTab(tab)}
              className={`px-4 py-2 border-b-2 font-medium text-sm ${
                activeTab === tab
                  ? 'border-primary-600 text-primary-600'
                  : 'border-transparent text-gray-600 hover:text-gray-900 hover:border-gray-300'
              }`}
            >
              {tab.charAt(0).toUpperCase() + tab.slice(1)}
            </button>
          ))}
        </div>
      </div>

      {/* Personal Information Tab */}
      {activeTab === 'personal' && (
        <div className="space-y-6">
          {/* Basic Information */}
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-lg font-semibold mb-4">Personal Details</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700">First Name</label>
                <input 
                  type="text" 
                  value={isEditing ? formData.firstName : (user?.firstName || '')} 
                  onChange={(e) => handleInputChange('firstName', e.target.value)}
                  disabled={!isEditing} 
                  className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" 
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Last Name</label>
                <input 
                  type="text" 
                  value={isEditing ? formData.lastName : (user?.lastName || '')} 
                  onChange={(e) => handleInputChange('lastName', e.target.value)}
                  disabled={!isEditing} 
                  className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" 
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">ID Number</label>
                <input type="text" value={user?.idNumber || ''} disabled className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50" />
                <p className="text-xs text-gray-500 mt-1">ID number cannot be changed</p>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Date of Birth</label>
                <input type="text" value={user?.dateOfBirth ? new Date(user.dateOfBirth).toLocaleDateString() : ''} disabled className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50" />
                <p className="text-xs text-gray-500 mt-1">Date of birth cannot be changed</p>
              </div>
            </div>
          </div>

          {/* Contact Information */}
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-lg font-semibold mb-4">Contact Information</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700">Email Address</label>
                <input type="email" value={user?.email || ''} disabled className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50" />
                <p className="text-xs text-gray-500 mt-1">Email cannot be changed</p>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Phone Number</label>
                <input 
                  type="tel" 
                  value={isEditing ? formData.phoneNumber : (user?.phoneNumber || '')} 
                  onChange={(e) => handleInputChange('phoneNumber', e.target.value)}
                  disabled={!isEditing} 
                  className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" 
                />
              </div>
            </div>
          </div>

          {/* Residential Address */}
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-lg font-semibold mb-4">Residential Address</h2>
            <div className="grid grid-cols-1 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700">Street Address</label>
                <input type="text" value={isEditing ? formData.streetAddress : (user?.streetAddress || '')} onChange={(e) => handleInputChange('streetAddress', e.target.value)} disabled={!isEditing} className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" placeholder="e.g., 123 Main Street" />
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700">Suburb</label>
                  <input type="text" value={isEditing ? formData.suburb : (user?.suburb || '')} onChange={(e) => handleInputChange('suburb', e.target.value)} disabled={!isEditing} className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" placeholder="e.g., Sandton" />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">City</label>
                  <input type="text" value={isEditing ? formData.city : (user?.city || '')} onChange={(e) => handleInputChange('city', e.target.value)} disabled={!isEditing} className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" placeholder="e.g., Johannesburg" />
                </div>
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700">Province</label>
                  <input type="text" value={isEditing ? formData.province : (user?.province || '')} onChange={(e) => handleInputChange('province', e.target.value)} disabled={!isEditing} className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" placeholder="e.g., Gauteng" />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">Postal Code</label>
                  <input type="text" value={isEditing ? formData.postalCode : (user?.postalCode || '')} onChange={(e) => handleInputChange('postalCode', e.target.value)} disabled={!isEditing} className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" placeholder="e.g., 2196" />
                </div>
              </div>
            </div>
          </div>

          {/* Employment Information */}
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-lg font-semibold mb-4">Employment Details</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700">Company/Employer Name</label>
                <input type="text" value={isEditing ? formData.employerName : (user?.employerName || '')} onChange={(e) => handleInputChange('employerName', e.target.value)} disabled={!isEditing} className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" placeholder="e.g., ABC Company (Pty) Ltd" />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Employment Type</label>
                <input type="text" value={isEditing ? formData.employmentType : (user?.employmentType || '')} onChange={(e) => handleInputChange('employmentType', e.target.value)} disabled={!isEditing} className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" placeholder="Permanent/Contract/Self-Employed" />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Employee Number</label>
                <input type="text" value={isEditing ? formData.employeeNumber : (user?.employeeNumber || '')} onChange={(e) => handleInputChange('employeeNumber', e.target.value)} disabled={!isEditing} className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" placeholder="Your employee ID" />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Payroll Reference</label>
                <input type="text" value={isEditing ? formData.payrollReference : (user?.payrollReference || '')} onChange={(e) => handleInputChange('payrollReference', e.target.value)} disabled={!isEditing} className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" placeholder="Payroll reference number" />
              </div>
            </div>
          </div>

          {/* Banking Information */}
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-lg font-semibold mb-4">Banking Details</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700">Bank Name</label>
                <input type="text" value={isEditing ? formData.bankName : (user?.bankName || '')} onChange={(e) => handleInputChange('bankName', e.target.value)} disabled={!isEditing} className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" placeholder="e.g., Standard Bank" />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Account Type</label>
                <input type="text" value={isEditing ? formData.accountType : (user?.accountType || '')} onChange={(e) => handleInputChange('accountType', e.target.value)} disabled={!isEditing} className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" placeholder="Cheque/Savings" />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Account Number</label>
                <input type="text" value={isEditing ? formData.accountNumber : (user?.accountNumber ? '****' + user.accountNumber.slice(-4) : '')} onChange={(e) => handleInputChange('accountNumber', e.target.value)} disabled={!isEditing} className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" placeholder="Full account number" />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Branch Code</label>
                <input type="text" value={isEditing ? formData.branchCode : (user?.branchCode || '')} onChange={(e) => handleInputChange('branchCode', e.target.value)} disabled={!isEditing} className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" placeholder="e.g., 051001" />
              </div>
            </div>
          </div>

          {/* Next of Kin */}
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-lg font-semibold mb-4">Next of Kin / Emergency Contact</h2>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700">Full Name</label>
                <input type="text" value={isEditing ? formData.nextOfKinName : (user?.nextOfKinName || '')} onChange={(e) => handleInputChange('nextOfKinName', e.target.value)} disabled={!isEditing} className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Relationship</label>
                <input type="text" value={isEditing ? formData.nextOfKinRelationship : (user?.nextOfKinRelationship || '')} onChange={(e) => handleInputChange('nextOfKinRelationship', e.target.value)} disabled={!isEditing} className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" placeholder="e.g., Spouse, Parent, Sibling" />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Contact Number</label>
                <input type="tel" value={isEditing ? formData.nextOfKinPhone : (user?.nextOfKinPhone || '')} onChange={(e) => handleInputChange('nextOfKinPhone', e.target.value)} disabled={!isEditing} className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" />
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Documents Tab */}
      {activeTab === 'documents' && (
        <div className="space-y-6">
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-lg font-semibold mb-4">Document Verification</h2>
            <p className="text-sm text-gray-600 mb-6">
              Upload your identity document and proof of address to complete your profile verification.
            </p>

            <div className="space-y-6">
              <div>
                <h3 className="text-md font-medium mb-3">Identity Document</h3>
                <DocumentUpload
                  documentType="IdDocument"
                  acceptedTypes="image/jpeg,image/png,image/jpg,application/pdf"
                  maxSizeMB={10}
                  onUploadSuccess={() => {
                    loadDocuments();
                    success('Identity document uploaded successfully!');
                  }}
                  onUploadError={(error) => showError('Failed to upload identity document: ' + error)}
                />
              </div>

              <div>
                <h3 className="text-md font-medium mb-3">Proof of Address</h3>
                <DocumentUpload
                  documentType="ProofOfAddress"
                  acceptedTypes="image/jpeg,image/png,image/jpg,application/pdf"
                  maxSizeMB={10}
                  onUploadSuccess={() => {
                    loadDocuments();
                    success('Proof of address uploaded successfully!');
                  }}
                  onUploadError={(error) => showError('Failed to upload proof of address: ' + error)}
                />
              </div>
            </div>
          </div>

          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-lg font-semibold mb-4">Uploaded Documents</h2>
            {loading ? (
              <div className="text-center py-8">
                <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600 mx-auto"></div>
                <p className="text-gray-600 mt-4">Loading documents...</p>
              </div>
            ) : (
              <DocumentList
                documents={documents}
                onDownload={async (id) => {
                  try {
                    window.open(`${import.meta.env.VITE_API_URL}/api/documents/${id}/download`, '_blank');
                  } catch (error) {
                    showError('Failed to download document. Please try again.');
                  }
                }}
                onDelete={async (id) => {
                  if (window.confirm('Are you sure you want to delete this document?')) {
                    try {
                      await apiService.delete(`/documents/${id}`);
                      await loadDocuments();
                      success('Document deleted successfully!');
                    } catch (error) {
                      showError('Failed to delete document. Please try again.');
                    }
                  }
                }}
                showActions={true}
              />
            )}
          </div>
        </div>
      )}
    </div>
  );
};

export default Profile;
