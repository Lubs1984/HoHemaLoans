import React, { useState, useEffect } from 'react';
import { useLocation } from 'react-router-dom';
import { useAuthStore } from '../../store/authStore';
import { apiService } from '../../services/api';
import { DocumentUpload } from '../../components/documents/DocumentUpload';
import { DocumentList, type Document } from '../../components/documents/DocumentList';

const Profile: React.FC = () => {
  const { user } = useAuthStore();
  const location = useLocation();
  const [activeTab, setActiveTab] = useState<'personal' | 'documents'>(() => {
    // Set initial tab based on route
    return location.pathname === '/documents' ? 'documents' : 'personal';
  });
  const [documents, setDocuments] = useState<Document[]>([]);
  const [loading, setLoading] = useState(false);

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
      console.error('Failed to load documents:', error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="max-w-4xl mx-auto">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900">Profile</h1>
        <p className="text-gray-600">Manage your profile information and documents</p>
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
                <input type="text" value={user?.firstName || ''} disabled className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50" />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Last Name</label>
                <input type="text" value={user?.lastName || ''} disabled className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50" />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">ID Number</label>
                <input type="text" value={user?.idNumber || ''} disabled className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50" />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Date of Birth</label>
                <input type="text" value={user?.dateOfBirth ? new Date(user.dateOfBirth).toLocaleDateString() : ''} disabled className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50" />
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
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Phone Number</label>
                <input type="tel" value={user?.phoneNumber || ''} disabled className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50" />
              </div>
            </div>
          </div>

          {/* Residential Address */}
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-lg font-semibold mb-4">Residential Address</h2>
            <div className="grid grid-cols-1 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700">Street Address</label>
                <input type="text" value={user?.streetAddress || ''} disabled className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50" placeholder="e.g., 123 Main Street" />
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700">Suburb</label>
                  <input type="text" value={user?.suburb || ''} disabled className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50" placeholder="e.g., Sandton" />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">City</label>
                  <input type="text" value={user?.city || ''} disabled className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50" placeholder="e.g., Johannesburg" />
                </div>
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700">Province</label>
                  <input type="text" value={user?.province || ''} disabled className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50" placeholder="e.g., Gauteng" />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">Postal Code</label>
                  <input type="text" value={user?.postalCode || ''} disabled className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50" placeholder="e.g., 2196" />
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
                <input type="text" value={user?.employerName || ''} disabled className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50" placeholder="e.g., ABC Company (Pty) Ltd" />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Employment Type</label>
                <input type="text" value={user?.employmentType || ''} disabled className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50" placeholder="Permanent/Contract/Self-Employed" />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Employee Number</label>
                <input type="text" value={user?.employeeNumber || ''} disabled className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50" placeholder="Your employee ID" />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Payroll Reference</label>
                <input type="text" value={user?.payrollReference || ''} disabled className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50" placeholder="Payroll reference number" />
              </div>
            </div>
          </div>

          {/* Banking Information */}
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-lg font-semibold mb-4">Banking Details</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700">Bank Name</label>
                <input type="text" value={user?.bankName || ''} disabled className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50" placeholder="e.g., Standard Bank" />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Account Type</label>
                <input type="text" value={user?.accountType || ''} disabled className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50" placeholder="Cheque/Savings" />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Account Number</label>
                <input type="text" value={user?.accountNumber ? '****' + user.accountNumber.slice(-4) : ''} disabled className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50" />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Branch Code</label>
                <input type="text" value={user?.branchCode || ''} disabled className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50" placeholder="e.g., 051001" />
              </div>
            </div>
          </div>

          {/* Next of Kin */}
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-lg font-semibold mb-4">Next of Kin / Emergency Contact</h2>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700">Full Name</label>
                <input type="text" value={user?.nextOfKinName || ''} disabled className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50" />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Relationship</label>
                <input type="text" value={user?.nextOfKinRelationship || ''} disabled className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50" placeholder="e.g., Spouse, Parent, Sibling" />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Contact Number</label>
                <input type="tel" value={user?.nextOfKinPhone || ''} disabled className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50" />
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
                  onUploadSuccess={() => loadDocuments()}
                  onUploadError={(error) => console.error('Upload failed:', error)}
                />
              </div>

              <div>
                <h3 className="text-md font-medium mb-3">Proof of Address</h3>
                <DocumentUpload
                  documentType="ProofOfAddress"
                  acceptedTypes="image/jpeg,image/png,image/jpg,application/pdf"
                  maxSizeMB={10}
                  onUploadSuccess={() => loadDocuments()}
                  onUploadError={(error) => console.error('Upload failed:', error)}
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
                    console.error('Download failed:', error);
                  }
                }}
                onDelete={async (id) => {
                  if (window.confirm('Are you sure you want to delete this document?')) {
                    try {
                      await apiService.delete(`/documents/${id}`);
                      await loadDocuments();
                    } catch (error) {
                      console.error('Delete failed:', error);
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
