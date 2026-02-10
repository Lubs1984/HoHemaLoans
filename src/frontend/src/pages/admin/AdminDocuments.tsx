import { useState, useEffect, useCallback } from 'react';
import { CheckCircle, XCircle, Eye, X, Clock, FileText, User, Search, Filter } from 'lucide-react';
import { apiService } from '../../services/api';

interface DocumentDto {
  id: string;
  userId: string;
  userName: string;
  documentType: number;
  documentTypeName: string;
  fileName: string;
  fileSize: number;
  contentType: string;
  fileContentBase64: string;
  status: number;
  statusName: string;
  rejectionReason?: string;
  verifiedByUserId?: string;
  verifiedByUserName?: string;
  uploadedAt: string;
  verifiedAt?: string;
  notes?: string;
}

type TabType = 'pending' | 'all';

const documentTypeLabels: Record<string, string> = {
  IdDocument: 'ID Document',
  ProofOfAddress: 'Proof of Address',
  BankStatement: 'Bank Statement',
  Payslip: 'Payslip',
  EmploymentLetter: 'Employment Letter',
  Other: 'Other',
};

const statusColors: Record<string, string> = {
  Pending: 'bg-yellow-100 text-yellow-800',
  Approved: 'bg-green-100 text-green-800',
  Rejected: 'bg-red-100 text-red-800',
};

const statusIcons: Record<string, React.ReactNode> = {
  Pending: <Clock className="w-4 h-4" />,
  Approved: <CheckCircle className="w-4 h-4" />,
  Rejected: <XCircle className="w-4 h-4" />,
};

function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('en-ZA', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

export default function AdminDocuments() {
  const [documents, setDocuments] = useState<DocumentDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<TabType>('pending');
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedDoc, setSelectedDoc] = useState<DocumentDto | null>(null);
  const [showPreview, setShowPreview] = useState(false);
  const [showRejectModal, setShowRejectModal] = useState(false);
  const [rejectDocId, setRejectDocId] = useState<string | null>(null);
  const [rejectionReason, setRejectionReason] = useState('');
  const [rejectionNotes, setRejectionNotes] = useState('');
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [filterType, setFilterType] = useState<string>('all');

  const loadPendingDocuments = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await apiService.request<DocumentDto[]>('/admin/documents/pending');
      setDocuments(data);
    } catch (err: any) {
      setError(err.message || 'Failed to load documents');
    } finally {
      setLoading(false);
    }
  }, []);

  const loadAllDocuments = useCallback(async () => {
    // The backend doesn't have a "get all" endpoint, so we use pending for now
    // In a real scenario, you'd add a GET /api/admin/documents endpoint
    try {
      setLoading(true);
      setError(null);
      const data = await apiService.request<DocumentDto[]>('/admin/documents/pending');
      setDocuments(data);
    } catch (err: any) {
      setError(err.message || 'Failed to load documents');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (activeTab === 'pending') {
      loadPendingDocuments();
    } else {
      loadAllDocuments();
    }
  }, [activeTab, loadPendingDocuments, loadAllDocuments]);

  const handleApprove = async (docId: string) => {
    try {
      setActionLoading(docId);
      await apiService.request(`/admin/documents/${docId}/verify`, {
        method: 'POST',
        body: JSON.stringify({ status: 2, notes: '' }), // 2 = Approved
      });
      setSuccessMessage('Document approved successfully');
      setTimeout(() => setSuccessMessage(null), 3000);
      // Reload documents
      if (activeTab === 'pending') {
        loadPendingDocuments();
      } else {
        loadAllDocuments();
      }
    } catch (err: any) {
      setError(err.message || 'Failed to approve document');
    } finally {
      setActionLoading(null);
    }
  };

  const openRejectModal = (docId: string) => {
    setRejectDocId(docId);
    setRejectionReason('');
    setRejectionNotes('');
    setShowRejectModal(true);
  };

  const handleReject = async () => {
    if (!rejectDocId || !rejectionReason.trim()) return;
    try {
      setActionLoading(rejectDocId);
      await apiService.request(`/admin/documents/${rejectDocId}/verify`, {
        method: 'POST',
        body: JSON.stringify({
          status: 3, // 3 = Rejected
          rejectionReason: rejectionReason.trim(),
          notes: rejectionNotes.trim() || undefined,
        }),
      });
      setShowRejectModal(false);
      setSuccessMessage('Document rejected successfully');
      setTimeout(() => setSuccessMessage(null), 3000);
      if (activeTab === 'pending') {
        loadPendingDocuments();
      } else {
        loadAllDocuments();
      }
    } catch (err: any) {
      setError(err.message || 'Failed to reject document');
    } finally {
      setActionLoading(null);
    }
  };

  const openPreview = (doc: DocumentDto) => {
    setSelectedDoc(doc);
    setShowPreview(true);
  };

  const filteredDocuments = documents.filter((doc) => {
    const matchesSearch =
      !searchQuery ||
      doc.userName.toLowerCase().includes(searchQuery.toLowerCase()) ||
      doc.fileName.toLowerCase().includes(searchQuery.toLowerCase()) ||
      doc.documentTypeName.toLowerCase().includes(searchQuery.toLowerCase());
    const matchesType = filterType === 'all' || doc.documentTypeName === filterType;
    return matchesSearch && matchesType;
  });

  const uniqueDocTypes = [...new Set(documents.map((d) => d.documentTypeName))];

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-gray-900">Document Verification</h1>
        <p className="mt-1 text-sm text-gray-500">
          Review and verify user-submitted documents for KYC compliance.
        </p>
      </div>

      {/* Success message */}
      {successMessage && (
        <div className="mb-4 bg-green-50 border border-green-200 text-green-700 px-4 py-3 rounded-lg flex items-center gap-2">
          <CheckCircle className="w-5 h-5" />
          {successMessage}
        </div>
      )}

      {/* Error message */}
      {error && (
        <div className="mb-4 bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg flex items-center gap-2">
          <XCircle className="w-5 h-5" />
          {error}
          <button onClick={() => setError(null)} className="ml-auto">
            <X className="w-4 h-4" />
          </button>
        </div>
      )}

      {/* Tabs */}
      <div className="border-b border-gray-200 mb-6">
        <nav className="-mb-px flex space-x-8">
          <button
            onClick={() => setActiveTab('pending')}
            className={`pb-4 px-1 border-b-2 text-sm font-medium ${
              activeTab === 'pending'
                ? 'border-blue-500 text-blue-600'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
            }`}
          >
            <div className="flex items-center gap-2">
              <Clock className="w-4 h-4" />
              Pending Review
              {documents.length > 0 && activeTab === 'pending' && (
                <span className="bg-yellow-100 text-yellow-800 text-xs font-semibold px-2 py-0.5 rounded-full">
                  {documents.length}
                </span>
              )}
            </div>
          </button>
          <button
            onClick={() => setActiveTab('all')}
            className={`pb-4 px-1 border-b-2 text-sm font-medium ${
              activeTab === 'all'
                ? 'border-blue-500 text-blue-600'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
            }`}
          >
            <div className="flex items-center gap-2">
              <FileText className="w-4 h-4" />
              All Documents
            </div>
          </button>
        </nav>
      </div>

      {/* Filters */}
      <div className="flex flex-col sm:flex-row gap-4 mb-6">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-4 h-4" />
          <input
            type="text"
            placeholder="Search by name, filename, or type..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          />
        </div>
        <div className="relative">
          <Filter className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-4 h-4" />
          <select
            value={filterType}
            onChange={(e) => setFilterType(e.target.value)}
            className="pl-10 pr-8 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 focus:border-transparent appearance-none bg-white"
          >
            <option value="all">All Types</option>
            {uniqueDocTypes.map((type) => (
              <option key={type} value={type}>
                {documentTypeLabels[type] || type}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Document list */}
      {loading ? (
        <div className="flex items-center justify-center py-12">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
          <span className="ml-3 text-gray-500">Loading documents...</span>
        </div>
      ) : filteredDocuments.length === 0 ? (
        <div className="text-center py-12 bg-white rounded-lg border border-gray-200">
          <FileText className="mx-auto h-12 w-12 text-gray-400" />
          <h3 className="mt-2 text-sm font-medium text-gray-900">
            {activeTab === 'pending' ? 'No pending documents' : 'No documents found'}
          </h3>
          <p className="mt-1 text-sm text-gray-500">
            {activeTab === 'pending'
              ? 'All documents have been reviewed.'
              : 'No documents match your search criteria.'}
          </p>
        </div>
      ) : (
        <div className="bg-white shadow-sm rounded-lg border border-gray-200 overflow-hidden">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  User
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Document
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Type
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Status
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Uploaded
                </th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {filteredDocuments.map((doc) => (
                <tr key={doc.id} className="hover:bg-gray-50">
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="flex items-center">
                      <div className="flex-shrink-0 h-8 w-8 bg-blue-100 rounded-full flex items-center justify-center">
                        <User className="h-4 w-4 text-blue-600" />
                      </div>
                      <div className="ml-3">
                        <div className="text-sm font-medium text-gray-900">{doc.userName}</div>
                        <div className="text-xs text-gray-500">{doc.userId.substring(0, 8)}...</div>
                      </div>
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm text-gray-900">{doc.fileName}</div>
                    <div className="text-xs text-gray-500">{formatFileSize(doc.fileSize)}</div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
                      {documentTypeLabels[doc.documentTypeName] || doc.documentTypeName}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <span
                      className={`inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-medium ${
                        statusColors[doc.statusName] || 'bg-gray-100 text-gray-800'
                      }`}
                    >
                      {statusIcons[doc.statusName]}
                      {doc.statusName}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                    {formatDate(doc.uploadedAt)}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                    <div className="flex items-center justify-end gap-2">
                      <button
                        onClick={() => openPreview(doc)}
                        className="inline-flex items-center gap-1 px-3 py-1.5 text-xs font-medium text-gray-700 bg-gray-100 hover:bg-gray-200 rounded-md transition-colors"
                        title="Preview document"
                      >
                        <Eye className="w-3.5 h-3.5" />
                        View
                      </button>
                      {doc.statusName === 'Pending' && (
                        <>
                          <button
                            onClick={() => handleApprove(doc.id)}
                            disabled={actionLoading === doc.id}
                            className="inline-flex items-center gap-1 px-3 py-1.5 text-xs font-medium text-white bg-green-600 hover:bg-green-700 rounded-md transition-colors disabled:opacity-50"
                            title="Approve document"
                          >
                            {actionLoading === doc.id ? (
                              <div className="animate-spin rounded-full h-3.5 w-3.5 border-b-2 border-white"></div>
                            ) : (
                              <CheckCircle className="w-3.5 h-3.5" />
                            )}
                            Approve
                          </button>
                          <button
                            onClick={() => openRejectModal(doc.id)}
                            disabled={actionLoading === doc.id}
                            className="inline-flex items-center gap-1 px-3 py-1.5 text-xs font-medium text-white bg-red-600 hover:bg-red-700 rounded-md transition-colors disabled:opacity-50"
                            title="Reject document"
                          >
                            <XCircle className="w-3.5 h-3.5" />
                            Reject
                          </button>
                        </>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Document Preview Modal */}
      {showPreview && selectedDoc && (
        <div className="fixed inset-0 z-50 overflow-y-auto">
          <div className="flex items-center justify-center min-h-screen px-4 pt-4 pb-20">
            <div className="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity" onClick={() => setShowPreview(false)} />
            <div className="relative bg-white rounded-lg shadow-xl max-w-4xl w-full max-h-[90vh] overflow-hidden">
              <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200">
                <div>
                  <h3 className="text-lg font-semibold text-gray-900">Document Preview</h3>
                  <p className="text-sm text-gray-500">
                    {documentTypeLabels[selectedDoc.documentTypeName] || selectedDoc.documentTypeName} — {selectedDoc.userName}
                  </p>
                </div>
                <button
                  onClick={() => setShowPreview(false)}
                  className="text-gray-400 hover:text-gray-600"
                >
                  <X className="h-6 w-6" />
                </button>
              </div>
              <div className="p-6 overflow-y-auto max-h-[70vh]">
                {/* Document details */}
                <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6 bg-gray-50 p-4 rounded-lg">
                  <div>
                    <dt className="text-xs text-gray-500">File Name</dt>
                    <dd className="text-sm font-medium text-gray-900">{selectedDoc.fileName}</dd>
                  </div>
                  <div>
                    <dt className="text-xs text-gray-500">File Size</dt>
                    <dd className="text-sm font-medium text-gray-900">{formatFileSize(selectedDoc.fileSize)}</dd>
                  </div>
                  <div>
                    <dt className="text-xs text-gray-500">Uploaded</dt>
                    <dd className="text-sm font-medium text-gray-900">{formatDate(selectedDoc.uploadedAt)}</dd>
                  </div>
                  <div>
                    <dt className="text-xs text-gray-500">Status</dt>
                    <dd>
                      <span
                        className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium ${
                          statusColors[selectedDoc.statusName] || 'bg-gray-100 text-gray-800'
                        }`}
                      >
                        {statusIcons[selectedDoc.statusName]}
                        {selectedDoc.statusName}
                      </span>
                    </dd>
                  </div>
                  {selectedDoc.verifiedByUserName && (
                    <div>
                      <dt className="text-xs text-gray-500">Verified By</dt>
                      <dd className="text-sm font-medium text-gray-900">{selectedDoc.verifiedByUserName}</dd>
                    </div>
                  )}
                  {selectedDoc.verifiedAt && (
                    <div>
                      <dt className="text-xs text-gray-500">Verified At</dt>
                      <dd className="text-sm font-medium text-gray-900">{formatDate(selectedDoc.verifiedAt)}</dd>
                    </div>
                  )}
                  {selectedDoc.rejectionReason && (
                    <div className="col-span-2">
                      <dt className="text-xs text-gray-500">Rejection Reason</dt>
                      <dd className="text-sm font-medium text-red-600">{selectedDoc.rejectionReason}</dd>
                    </div>
                  )}
                </div>

                {/* Document content */}
                {selectedDoc.fileContentBase64 ? (
                  selectedDoc.contentType?.startsWith('image/') ? (
                    <div className="flex justify-center bg-gray-100 rounded-lg p-4">
                      <img
                        src={`data:${selectedDoc.contentType};base64,${selectedDoc.fileContentBase64}`}
                        alt={selectedDoc.fileName}
                        className="max-w-full max-h-[50vh] object-contain rounded"
                      />
                    </div>
                  ) : selectedDoc.contentType === 'application/pdf' ? (
                    <div className="bg-gray-100 rounded-lg p-4">
                      <iframe
                        src={`data:application/pdf;base64,${selectedDoc.fileContentBase64}`}
                        title={selectedDoc.fileName}
                        className="w-full h-[50vh] rounded"
                      />
                    </div>
                  ) : (
                    <div className="text-center py-8 bg-gray-50 rounded-lg">
                      <FileText className="mx-auto h-12 w-12 text-gray-400" />
                      <p className="mt-2 text-sm text-gray-500">
                        Preview not available for this file type ({selectedDoc.contentType})
                      </p>
                      <a
                        href={`data:${selectedDoc.contentType};base64,${selectedDoc.fileContentBase64}`}
                        download={selectedDoc.fileName}
                        className="inline-flex items-center mt-3 px-4 py-2 text-sm font-medium text-blue-600 bg-blue-50 hover:bg-blue-100 rounded-md transition-colors"
                      >
                        Download File
                      </a>
                    </div>
                  )
                ) : (
                  <div className="text-center py-8 bg-gray-50 rounded-lg">
                    <FileText className="mx-auto h-12 w-12 text-gray-400" />
                    <p className="mt-2 text-sm text-gray-500">No file content available</p>
                  </div>
                )}
              </div>

              {/* Action buttons in preview */}
              {selectedDoc.statusName === 'Pending' && (
                <div className="flex items-center justify-end gap-3 px-6 py-4 border-t border-gray-200 bg-gray-50">
                  <button
                    onClick={() => {
                      setShowPreview(false);
                      openRejectModal(selectedDoc.id);
                    }}
                    className="inline-flex items-center gap-2 px-4 py-2 text-sm font-medium text-red-700 bg-red-100 hover:bg-red-200 rounded-md transition-colors"
                  >
                    <XCircle className="w-4 h-4" />
                    Reject
                  </button>
                  <button
                    onClick={() => {
                      handleApprove(selectedDoc.id);
                      setShowPreview(false);
                    }}
                    className="inline-flex items-center gap-2 px-4 py-2 text-sm font-medium text-white bg-green-600 hover:bg-green-700 rounded-md transition-colors"
                  >
                    <CheckCircle className="w-4 h-4" />
                    Approve
                  </button>
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Rejection Modal */}
      {showRejectModal && (
        <div className="fixed inset-0 z-50 overflow-y-auto">
          <div className="flex items-center justify-center min-h-screen px-4">
            <div className="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity" onClick={() => setShowRejectModal(false)} />
            <div className="relative bg-white rounded-lg shadow-xl max-w-md w-full">
              <div className="px-6 py-4 border-b border-gray-200">
                <h3 className="text-lg font-semibold text-gray-900">Reject Document</h3>
                <p className="text-sm text-gray-500">Provide a reason for rejection. The user will be notified.</p>
              </div>
              <div className="px-6 py-4 space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Rejection Reason <span className="text-red-500">*</span>
                  </label>
                  <select
                    value={rejectionReason}
                    onChange={(e) => setRejectionReason(e.target.value)}
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                  >
                    <option value="">Select a reason...</option>
                    <option value="Document is blurry or unreadable">Document is blurry or unreadable</option>
                    <option value="Document is expired">Document is expired</option>
                    <option value="Wrong document type uploaded">Wrong document type uploaded</option>
                    <option value="Document is incomplete or cut off">Document is incomplete or cut off</option>
                    <option value="Name does not match application">Name does not match application</option>
                    <option value="Document appears altered or fraudulent">Document appears altered or fraudulent</option>
                    <option value="Address not visible or legible">Address not visible or legible</option>
                    <option value="Other">Other (specify in notes)</option>
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Additional Notes
                  </label>
                  <textarea
                    value={rejectionNotes}
                    onChange={(e) => setRejectionNotes(e.target.value)}
                    rows={3}
                    placeholder="Optional additional notes for the user..."
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                  />
                </div>
              </div>
              <div className="flex items-center justify-end gap-3 px-6 py-4 border-t border-gray-200 bg-gray-50 rounded-b-lg">
                <button
                  onClick={() => setShowRejectModal(false)}
                  className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 hover:bg-gray-50 rounded-md transition-colors"
                >
                  Cancel
                </button>
                <button
                  onClick={handleReject}
                  disabled={!rejectionReason.trim() || actionLoading === rejectDocId}
                  className="inline-flex items-center gap-2 px-4 py-2 text-sm font-medium text-white bg-red-600 hover:bg-red-700 rounded-md transition-colors disabled:opacity-50"
                >
                  {actionLoading === rejectDocId ? (
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                  ) : (
                    <XCircle className="w-4 h-4" />
                  )}
                  Reject Document
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
