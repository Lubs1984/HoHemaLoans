import React, { useState, useEffect } from 'react';
import { useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuthStore } from '../../store/authStore';
import { apiService } from '../../services/api';
import { useToast } from '../../contexts/ToastContext';
import { DocumentUpload } from '../../components/documents/DocumentUpload';
import { DocumentList, type Document } from '../../components/documents/DocumentList';

const Profile: React.FC = () => {
  const { t } = useTranslation(['auth']);
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
      showError(t('auth:profile.messages.loadFailed'));
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteDocument = async (documentId: string) => {
    if (!confirm(t('auth:profile.messages.confirmDelete'))) return;
    try {
      await apiService.delete(`/documents/${documentId}`);
      success(t('auth:profile.messages.deleteSuccess'));
      loadDocuments();
    } catch (error: any) {
      showError(error.message || t('auth:profile.messages.deleteFailed'));
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
      success(t('auth:profile.messages.updateSuccess'));
    } catch (error: any) {
      const errorMessage = error.message || t('auth:profile.messages.updateFailed');
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
          <h1 className="text-3xl font-bold text-gray-900">{t('auth:profile.title')}</h1>
          <p className="text-gray-600">{t('auth:profile.subtitle')}</p>
        </div>
        {activeTab === 'personal' && !isEditing && (
          <button
            onClick={handleEdit}
            className="btn btn-primary"
          >
            {t('auth:profile.editProfile')}
          </button>
        )}
        {activeTab === 'personal' && isEditing && (
          <div className="flex gap-2">
            <button
              onClick={handleCancel}
              className="btn btn-secondary"
              disabled={saving}
            >
              {t('auth:profile.cancel')}
            </button>
            <button
              onClick={handleSave}
              className="btn btn-primary"
              disabled={saving}
            >
              {saving ? t('auth:profile.saving') : t('auth:profile.saveChanges')}
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
              {t(`auth:profile.tabs.${tab}`)}
            </button>
          ))}
        </div>
      </div>

      {/* Personal Information Tab */}
      {activeTab === 'personal' && (
        <div className="space-y-6">
          {/* Basic Information */}
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-lg font-semibold mb-4">{t('auth:profile.sections.personalDetails')}</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700">{t('auth:profile.fields.firstName')}</label>
                <input 
                  type="text" 
                  value={isEditing ? formData.firstName : (user?.firstName || '')} 
                  onChange={(e) => handleInputChange('firstName', e.target.value)}
                  disabled={!isEditing} 
                  className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" 
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">{t('auth:profile.fields.lastName')}</label>
                <input 
                  type="text" 
                  value={isEditing ? formData.lastName : (user?.lastName || '')} 
                  onChange={(e) => handleInputChange('lastName', e.target.value)}
                  disabled={!isEditing} 
                  className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" 
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">{t('auth:profile.fields.idNumber')}</label>
                <input type="text" value={user?.idNumber || ''} disabled className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50" />
                <p className="text-xs text-gray-500 mt-1">{t('auth:profile.helperText.idNoChange')}</p>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">{t('auth:profile.fields.dateOfBirth')}</label>
                <input type="text" value={user?.dateOfBirth ? new Date(user.dateOfBirth).toLocaleDateString() : ''} disabled className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50" />
                <p className="text-xs text-gray-500 mt-1">{t('auth:profile.helperText.dobNoChange')}</p>
              </div>
            </div>
          </div>

          {/* Contact Information */}
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-lg font-semibold mb-4">{t('auth:profile.sections.contactInfo')}</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700">{t('auth:profile.fields.email')}</label>
                <input type="email" value={user?.email || ''} disabled className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50" />
                <p className="text-xs text-gray-500 mt-1">{t('auth:profile.helperText.emailNoChange')}</p>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">{t('auth:profile.fields.phone')}</label>
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
            <h2 className="text-lg font-semibold mb-4">{t('auth:profile.sections.address')}</h2>
            <div className="grid grid-cols-1 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700">{t('auth:profile.fields.streetAddress')}</label>
                <input type="text" value={isEditing ? formData.streetAddress : (user?.streetAddress || '')} onChange={(e) => handleInputChange('streetAddress', e.target.value)} disabled={!isEditing} className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" placeholder={t('auth:profile.helperText.streetExample')} />
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700">{t('auth:profile.fields.suburb')}</label>
                  <input type="text" value={isEditing ? formData.suburb : (user?.suburb || '')} onChange={(e) => handleInputChange('suburb', e.target.value)} disabled={!isEditing} className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" placeholder={t('auth:profile.helperText.suburbExample')} />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">{t('auth:profile.fields.city')}</label>
                  <input type="text" value={isEditing ? formData.city : (user?.city || '')} onChange={(e) => handleInputChange('city', e.target.value)} disabled={!isEditing} className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" placeholder={t('auth:profile.helperText.cityExample')} />
                </div>
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700">{t('auth:profile.fields.province')}</label>
                  <input type="text" value={isEditing ? formData.province : (user?.province || '')} onChange={(e) => handleInputChange('province', e.target.value)} disabled={!isEditing} className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" placeholder={t('auth:profile.helperText.provinceExample')} />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">{t('auth:profile.fields.postalCode')}</label>
                  <input type="text" value={isEditing ? formData.postalCode : (user?.postalCode || '')} onChange={(e) => handleInputChange('postalCode', e.target.value)} disabled={!isEditing} className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" placeholder={t('auth:profile.helperText.postalExample')} />
                </div>
              </div>
            </div>
          </div>

          {/* Employment Information */}
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-lg font-semibold mb-4">{t('auth:profile.sections.employment')}</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700">{t('auth:profile.fields.employerName')}</label>
                <input type="text" value={isEditing ? formData.employerName : (user?.employerName || '')} onChange={(e) => handleInputChange('employerName', e.target.value)} disabled={!isEditing} className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" placeholder={t('auth:profile.helperText.employerExample')} />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">{t('auth:profile.fields.employmentType')}</label>
                <input type="text" value={isEditing ? formData.employmentType : (user?.employmentType || '')} onChange={(e) => handleInputChange('employmentType', e.target.value)} disabled={!isEditing} className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" placeholder={t('auth:profile.helperText.employmentTypeExample')} />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">{t('auth:profile.fields.employeeNumber')}</label>
                <input type="text" value={isEditing ? formData.employeeNumber : (user?.employeeNumber || '')} onChange={(e) => handleInputChange('employeeNumber', e.target.value)} disabled={!isEditing} className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" placeholder={t('auth:profile.helperText.employeeIdExample')} />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">{t('auth:profile.fields.payrollReference')}</label>
                <input type="text" value={isEditing ? formData.payrollReference : (user?.payrollReference || '')} onChange={(e) => handleInputChange('payrollReference', e.target.value)} disabled={!isEditing} className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" placeholder={t('auth:profile.helperText.payrollExample')} />
              </div>
            </div>
          </div>

          {/* Banking Information */}
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-lg font-semibold mb-4">{t('auth:profile.sections.banking')}</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700">{t('auth:profile.fields.bankName')}</label>
                <input type="text" value={isEditing ? formData.bankName : (user?.bankName || '')} onChange={(e) => handleInputChange('bankName', e.target.value)} disabled={!isEditing} className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" placeholder={t('auth:profile.helperText.bankExample')} />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">{t('auth:profile.fields.accountType')}</label>
                <input type="text" value={isEditing ? formData.accountType : (user?.accountType || '')} onChange={(e) => handleInputChange('accountType', e.target.value)} disabled={!isEditing} className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" placeholder={t('auth:profile.helperText.accountTypeExample')} />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">{t('auth:profile.fields.accountNumber')}</label>
                <input type="text" value={isEditing ? formData.accountNumber : (user?.accountNumber ? '****' + user.accountNumber.slice(-4) : '')} onChange={(e) => handleInputChange('accountNumber', e.target.value)} disabled={!isEditing} className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" placeholder={t('auth:profile.helperText.accountNumberExample')} />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">{t('auth:profile.fields.branchCode')}</label>
                <input type="text" value={isEditing ? formData.branchCode : (user?.branchCode || '')} onChange={(e) => handleInputChange('branchCode', e.target.value)} disabled={!isEditing} className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" placeholder={t('auth:profile.helperText.branchCodeExample')} />
              </div>
            </div>
          </div>

  
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-lg font-semibold mb-4">{t('auth:profile.sections.nextOfKin')}</h2>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700">{t('auth:profile.fields.nextOfKinName')}</label>
                <input type="text" value={isEditing ? formData.nextOfKinName : (user?.nextOfKinName || '')} onChange={(e) => handleInputChange('nextOfKinName', e.target.value)} disabled={!isEditing} className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">{t('auth:profile.fields.nextOfKinRelationship')}</label>
                <input type="text" value={isEditing ? formData.nextOfKinRelationship : (user?.nextOfKinRelationship || '')} onChange={(e) => handleInputChange('nextOfKinRelationship', e.target.value)} disabled={!isEditing} className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" placeholder={t('auth:profile.helperText.relationshipExample')} />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">{t('auth:profile.fields.nextOfKinPhone')}</label>
                <input type="tel" value={isEditing ? formData.nextOfKinPhone : (user?.nextOfKinPhone || '')} onChange={(e) => handleInputChange('nextOfKinPhone', e.target.value)} disabled={!isEditing} className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 disabled:bg-gray-50 enabled:bg-white" />
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Documents Tab */}
      {activeTab === 'documents' && (
        <div className="space-y-6">
          {/* Document Upload */}
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-lg font-semibold mb-4">{t('auth:profile.sections.uploadDocs')}</h2>
            <p className="text-sm text-gray-500 mb-4">{t('auth:profile.documents.instruction')}</p>
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              <DocumentUpload
                documentType="IdDocument"
                label={t('auth:profile.documents.idDocument')}
                onUploadSuccess={(_document) => {
                  success(t('auth:profile.messages.idUploaded'));
                  loadDocuments();
                }}
                onUploadError={(error) => {
                  showError(t('auth:profile.messages.idUploadFailed', { error }));
                }}
                acceptedTypes=".jpg,.jpeg,.png,.pdf"
                maxSizeMB={5}
              />
              <DocumentUpload
                documentType="ProofOfAddress"
                label={t('auth:profile.documents.proofOfAddress')}
                onUploadSuccess={(_document) => {
                  success(t('auth:profile.messages.proofUploaded'));
                  loadDocuments();
                }}
                onUploadError={(error) => {
                  showError(t('auth:profile.messages.proofUploadFailed', { error }));
                }}
                acceptedTypes=".jpg,.jpeg,.png,.pdf"
                maxSizeMB={5}
              />
              <DocumentUpload
                documentType="Payslip"
                label={t('auth:profile.documents.payslip')}
                onUploadSuccess={(_document) => {
                  success(t('auth:profile.messages.payslipUploaded'));
                  loadDocuments();
                }}
                onUploadError={(error) => {
                  showError(t('auth:profile.messages.payslipUploadFailed', { error }));
                }}
                acceptedTypes=".jpg,.jpeg,.png,.pdf"
                maxSizeMB={5}
              />
              <DocumentUpload
                documentType="BankStatement"
                label={t('auth:profile.documents.bankStatement')}
                onUploadSuccess={(_document) => {
                  success(t('auth:profile.messages.bankStatementUploaded'));
                  loadDocuments();
                }}
                onUploadError={(error) => {
                  showError(t('auth:profile.messages.bankStatementUploadFailed', { error }));
                }}
                acceptedTypes=".jpg,.jpeg,.png,.pdf"
                maxSizeMB={5}
              />
            </div>
          </div>

          {/* Document List */}
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-lg font-semibold mb-4">{t('auth:profile.sections.yourDocs')}</h2>
            {loading ? (
              <div className="text-center py-4">
                <p className="text-gray-500">{t('auth:profile.documents.loading')}</p>
              </div>
            ) : (
              <DocumentList 
                documents={documents}
                onDelete={handleDeleteDocument}
              />
            )}
          </div>
        </div>
      )}
           

    </div>
  );
};

export default Profile;
