import React, { useState } from 'react';
import { XMarkIcon, MagnifyingGlassPlusIcon, MagnifyingGlassMinusIcon } from '@heroicons/react/24/outline';

interface DocumentViewerProps {
  document: {
    fileName: string;
    contentType: string;
    fileContentBase64?: string;
  };
  onClose: () => void;
}

export const DocumentViewer: React.FC<DocumentViewerProps> = ({ document, onClose }) => {
  const [zoom, setZoom] = useState(100);

  const handleZoomIn = () => setZoom((prev) => Math.min(prev + 25, 200));
  const handleZoomOut = () => setZoom((prev) => Math.max(prev - 25, 50));

  if (!document.fileContentBase64) {
    return null;
  }

  return (
    <div className="fixed inset-0 z-50 overflow-hidden bg-black bg-opacity-75 flex items-center justify-center">
      <div className="relative w-full h-full max-w-6xl max-h-screen p-4">
        {/* Header */}
        <div className="absolute top-4 left-4 right-4 bg-white rounded-t-lg shadow-lg p-4 flex items-center justify-between z-10">
          <h3 className="text-lg font-semibold text-gray-900 truncate">{document.fileName}</h3>
          <div className="flex items-center space-x-2">
            <button
              onClick={handleZoomOut}
              className="p-2 text-gray-600 hover:bg-gray-100 rounded"
              title="Zoom Out"
            >
              <MagnifyingGlassMinusIcon className="h-5 w-5" />
            </button>
            <span className="text-sm font-medium text-gray-700 min-w-[60px] text-center">
              {zoom}%
            </span>
            <button
              onClick={handleZoomIn}
              className="p-2 text-gray-600 hover:bg-gray-100 rounded"
              title="Zoom In"
            >
              <MagnifyingGlassPlusIcon className="h-5 w-5" />
            </button>
            <button
              onClick={onClose}
              className="p-2 text-gray-600 hover:bg-gray-100 rounded ml-4"
              title="Close"
            >
              <XMarkIcon className="h-6 w-6" />
            </button>
          </div>
        </div>

        {/* Document Display */}
        <div className="absolute top-20 bottom-4 left-4 right-4 bg-white rounded-b-lg shadow-lg overflow-auto">
          <div className="flex items-center justify-center min-h-full p-8">
            {document.contentType.startsWith('image/') ? (
              <img
                src={`data:${document.contentType};base64,${document.fileContentBase64}`}
                alt={document.fileName}
                style={{ width: `${zoom}%` }}
                className="max-w-full h-auto shadow-lg"
              />
            ) : document.contentType === 'application/pdf' ? (
              <embed
                src={`data:${document.contentType};base64,${document.fileContentBase64}`}
                type="application/pdf"
                className="w-full h-full"
                style={{ minHeight: '600px' }}
              />
            ) : (
              <div className="text-center">
                <p className="text-gray-600">Preview not available for this document type.</p>
                <p className="text-sm text-gray-500 mt-2">Download to view the file.</p>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};
