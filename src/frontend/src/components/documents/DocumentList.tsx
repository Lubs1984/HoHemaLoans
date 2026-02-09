import React, { useState } from 'react';
import {
  DocumentIcon,
  CheckCircleIcon,
  ClockIcon,
  XCircleIcon,
  CloudArrowDownIcon,
  TrashIcon,
  EyeIcon,
} from '@heroicons/react/24/outline';
import { DocumentViewer } from './DocumentViewer';

export interface Document {
  id: string;
  documentType: string;
  documentTypeName: string;
  fileName: string;
  fileSize: number;
  contentType: string;
  fileContentBase64?: string;
  status: string;
  statusName: string;
  uploadedAt: string;
  verifiedAt?: string;
  rejectionReason?: string;
  notes?: string;
}

interface DocumentListProps {
  documents: Document[];
  onDownload?: (documentId: string) => void;
  onDelete?: (documentId: string) => void;
  showActions?: boolean;
}

export const DocumentList: React.FC<DocumentListProps> = ({
  documents,
  onDownload,
  onDelete,
  showActions = true,
}) => {
  const [viewingDocument, setViewingDocument] = useState<Document | null>(null);

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'Approved':
        return (
          <span className="inline-flex items-center space-x-1 px-3 py-1 rounded-full text-sm font-medium bg-green-100 text-green-800">
            <CheckCircleIcon className="h-4 w-4" />
            <span>Approved</span>
          </span>
        );
      case 'Pending':
        return (
          <span className="inline-flex items-center space-x-1 px-3 py-1 rounded-full text-sm font-medium bg-yellow-100 text-yellow-800">
            <ClockIcon className="h-4 w-4" />
            <span>Pending Review</span>
          </span>
        );
      case 'Rejected':
        return (
          <span className="inline-flex items-center space-x-1 px-3 py-1 rounded-full text-sm font-medium bg-red-100 text-red-800">
            <XCircleIcon className="h-4 w-4" />
            <span>Rejected</span>
          </span>
        );
      default:
        return <span className="text-gray-500">{status}</span>;
    }
  };

  const formatFileSize = (bytes: number): string => {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(2) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(2) + ' MB';
  };

  const formatDate = (dateString: string): string => {
    return new Date(dateString).toLocaleDateString('en-ZA', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  };

  if (documents.length === 0) {
    return (
      <div className="text-center py-12 bg-gray-50 rounded-lg">
        <DocumentIcon className="mx-auto h-12 w-12 text-gray-400" />
        <h3 className="mt-2 text-sm font-medium text-gray-900">No documents</h3>
        <p className="mt-1 text-sm text-gray-500">
          Upload your documents to get verified
        </p>
      </div>
    );
  }

  return (
    <>
      {viewingDocument && (
        <DocumentViewer
          document={{
            fileName: viewingDocument.fileName,
            contentType: viewingDocument.contentType,
            fileContentBase64: viewingDocument.fileContentBase64,
          }}
          onClose={() => setViewingDocument(null)}
        />
      )}
      
      <div className="space-y-4">
        {documents.map((doc) => (
          <div
            key={doc.id}
            className="border border-gray-200 rounded-lg p-4 hover:shadow-md transition-shadow"
          >
            <div className="flex items-start justify-between">
              <div className="flex items-start space-x-3 flex-1">
                {/* Show image preview for ID documents with BASE64 data */}
                {doc.fileContentBase64 && doc.contentType.startsWith('image/') ? (
                  <div
                    className="flex-shrink-0 cursor-pointer hover:opacity-80 transition-opacity"
                    onClick={() => setViewingDocument(doc)}
                    title="Click to view full size"
                  >
                    <img
                      src={`data:${doc.contentType};base64,${doc.fileContentBase64}`}
                      alt={doc.fileName}
                      className="h-20 w-20 object-cover rounded border border-gray-300"
                    />
                  </div>
                ) : (
                  <DocumentIcon className="h-10 w-10 text-gray-400 flex-shrink-0" />
                )}
                <div className="flex-1 min-w-0">
                  <div className="flex items-center space-x-3">
                    <h4 className="font-medium text-gray-900 truncate">
                      {doc.fileName}
                    </h4>
                    {getStatusBadge(doc.statusName)}
                  </div>
                  <p className="text-sm text-gray-500 mt-1">
                    {doc.documentTypeName} • {formatFileSize(doc.fileSize)}
                  </p>
                  <p className="text-xs text-gray-400 mt-1">
                    Uploaded {formatDate(doc.uploadedAt)}
                    {doc.verifiedAt && ` • Verified ${formatDate(doc.verifiedAt)}`}
                  </p>
                  {doc.rejectionReason && (
                    <div className="mt-2 p-2 bg-red-50 border border-red-200 rounded">
                      <p className="text-sm text-red-700">
                        <span className="font-medium">Reason:</span>{' '}
                        {doc.rejectionReason}
                      </p>
                    </div>
                  )}
                  {doc.notes && doc.statusName !== 'Rejected' && (
                    <div className="mt-2 p-2 bg-blue-50 border border-blue-200 rounded">
                      <p className="text-sm text-blue-700">{doc.notes}</p>
                    </div>
                  )}
                </div>
              </div>
              {showActions && (
                <div className="flex items-center space-x-2 ml-4">
                  {doc.fileContentBase64 && (
                    <button
                      onClick={() => setViewingDocument(doc)}
                      className="p-2 text-green-600 hover:bg-green-50 rounded-md transition-colors"
                      title="View Document"
                    >
                      <EyeIcon className="h-5 w-5" />
                    </button>
                  )}
                  {onDownload && (
                    <button
                      onClick={() => onDownload(doc.id)}
                      className="p-2 text-blue-600 hover:bg-blue-50 rounded-md transition-colors"
                      title="Download"
                    >
                      <CloudArrowDownIcon className="h-5 w-5" />
                    </button>
                  )}
                  {onDelete && doc.statusName !== 'Approved' && (
                    <button
                      onClick={() => onDelete(doc.id)}
                      className="p-2 text-red-600 hover:bg-red-50 rounded-md transition-colors"
                      title="Delete"
                    >
                      <TrashIcon className="h-5 w-5" />
                    </button>
                  )}
                </div>
              )}
            </div>
          </div>
        ))}
      </div>
    </>
  );
};
