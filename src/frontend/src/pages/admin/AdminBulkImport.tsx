import { useState, useRef } from 'react';
import { Upload, Download, Check, X, AlertCircle, Users, FileText } from 'lucide-react';
import api from '../../services/api';

interface BulkUserImportDto {
  rowNumber: number;
  email: string;
  firstName: string;
  lastName: string;
  idNumber: string;
  dateOfBirth: string;
  address: string;
  phoneNumber: string;
  monthlyIncome: number;
}

interface ValidationError {
  rowNumber: number;
  field: string;
  error: string;
}

interface ValidationResult {
  isValid: boolean;
  validationErrors: ValidationError[];
  validUsers: BulkUserImportDto[];
  totalRows: number;
}

interface ImportResult {
  totalUsers: number;
  successCount: number;
  failureCount: number;
  errors: string[];
}

export default function AdminBulkImport() {
  const [file, setFile] = useState<File | null>(null);
  const [validationResult, setValidationResult] = useState<ValidationResult | null>(null);
  const [importResult, setImportResult] = useState<ImportResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [step, setStep] = useState<'upload' | 'validate' | 'confirm' | 'complete'>('upload');
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleFileSelect = (event: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFile = event.target.files?.[0];
    if (selectedFile) {
      setFile(selectedFile);
      setValidationResult(null);
      setImportResult(null);
      setStep('upload');
    }
  };

  const handleDragOver = (event: React.DragEvent) => {
    event.preventDefault();
    event.currentTarget.classList.add('bg-blue-50');
  };

  const handleDragLeave = (event: React.DragEvent) => {
    event.preventDefault();
    event.currentTarget.classList.remove('bg-blue-50');
  };

  const handleDrop = (event: React.DragEvent) => {
    event.preventDefault();
    event.currentTarget.classList.remove('bg-blue-50');
    
    const droppedFile = event.dataTransfer.files?.[0];
    if (droppedFile && droppedFile.name.endsWith('.csv')) {
      setFile(droppedFile);
      setValidationResult(null);
      setImportResult(null);
      setStep('upload');
    }
  };

  const validateFile = async () => {
    if (!file) return;

    setLoading(true);
    try {
      const formData = new FormData();
      formData.append('file', file);

      const response = await api.post('/admin/users/bulk-import/validate', formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      });

      if (response.data) {
        setValidationResult(response.data);
        setStep('validate');
      }
    } catch (error: any) {
      console.error('Validation failed:', error);
      // Handle error
    } finally {
      setLoading(false);
    }
  };

  const importUsers = async () => {
    if (!validationResult?.validUsers) return;

    setLoading(true);
    try {
      const response = await api.post('/admin/users/bulk-import/import', validationResult.validUsers);
      
      if (response.data) {
        setImportResult(response.data);
        setStep('complete');
      }
    } catch (error: any) {
      console.error('Import failed:', error);
      // Handle error
    } finally {
      setLoading(false);
    }
  };

  const downloadTemplate = async () => {
    try {
      const response = await api.get('/admin/users/bulk-import/template', {
        responseType: 'blob',
      });
      
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', 'bulk_import_template.csv');
      document.body.appendChild(link);
      link.click();
      link.remove();
    } catch (error) {
      console.error('Download failed:', error);
    }
  };

  const resetImport = () => {
    setFile(null);
    setValidationResult(null);
    setImportResult(null);
    setStep('upload');
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  return (
    <div className="max-w-6xl mx-auto p-6">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 mb-2">Bulk User Import</h1>
        <p className="text-gray-600">Upload a CSV file to import multiple users at once</p>
      </div>

      {/* Progress Steps */}
      <div className="mb-8">
        <div className="flex items-center justify-between">
          {[
            { step: 'upload', label: 'Upload File', icon: Upload },
            { step: 'validate', label: 'Validate Data', icon: FileText },
            { step: 'confirm', label: 'Confirm Import', icon: Check },
            { step: 'complete', label: 'Complete', icon: Users },
          ].map(({ step: stepName, label, icon: Icon }, index) => (
            <div key={stepName} className="flex items-center">
              <div className={`flex items-center justify-center w-10 h-10 rounded-full ${
                step === stepName || (index < ['upload', 'validate', 'confirm', 'complete'].indexOf(step)) 
                  ? 'bg-blue-600 text-white' 
                  : 'bg-gray-200 text-gray-600'
              }`}>
                <Icon className="w-5 h-5" />
              </div>
              <span className="ml-2 text-sm font-medium text-gray-700">{label}</span>
              {index < 3 && <div className="w-16 h-0.5 bg-gray-200 ml-4" />}
            </div>
          ))}
        </div>
      </div>

      {/* Upload Step */}
      {step === 'upload' && (
        <div className="space-y-6">
          {/* Template Download */}
          <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
            <div className="flex items-start">
              <FileText className="h-5 w-5 text-blue-600 mt-0.5 mr-3" />
              <div className="flex-1">
                <h3 className="text-sm font-medium text-blue-900 mb-1">Download Template</h3>
                <p className="text-sm text-blue-700 mb-3">
                  Download the CSV template with the correct format and sample data.
                </p>
                <button
                  onClick={downloadTemplate}
                  className="inline-flex items-center px-3 py-2 border border-blue-300 shadow-sm text-sm leading-4 font-medium rounded-md text-blue-700 bg-white hover:bg-blue-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
                >
                  <Download className="w-4 h-4 mr-2" />
                  Download Template
                </button>
              </div>
            </div>
          </div>

          {/* File Upload */}
          <div className="bg-white rounded-lg border-2 border-dashed border-gray-300 p-12">
            <div
              className="text-center"
              onDragOver={handleDragOver}
              onDragLeave={handleDragLeave}
              onDrop={handleDrop}
            >
              <Upload className="mx-auto h-12 w-12 text-gray-400 mb-4" />
              <h3 className="text-lg font-medium text-gray-900 mb-2">Upload CSV File</h3>
              <p className="text-gray-600 mb-4">
                Drag and drop your CSV file here, or click to browse
              </p>

              <input
                ref={fileInputRef}
                type="file"
                accept=".csv"
                onChange={handleFileSelect}
                className="hidden"
              />

              <button
                onClick={() => fileInputRef.current?.click()}
                className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-blue-600 hover:bg-blue-700"
              >
                Choose File
              </button>
            </div>
          </div>

          {file && (
            <div className="bg-white rounded-lg border p-4">
              <div className="flex items-center justify-between">
                <div className="flex items-center">
                  <FileText className="h-5 w-5 text-green-600 mr-3" />
                  <div>
                    <p className="text-sm font-medium text-gray-900">{file.name}</p>
                    <p className="text-xs text-gray-600">{(file.size / 1024).toFixed(2)} KB</p>
                  </div>
                </div>
                <button
                  onClick={validateFile}
                  disabled={loading}
                  className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-green-600 hover:bg-green-700 disabled:opacity-50"
                >
                  {loading ? 'Validating...' : 'Validate File'}
                </button>
              </div>
            </div>
          )}
        </div>
      )}

      {/* Validation Results */}
      {step === 'validate' && validationResult && (
        <div className="space-y-6">
          <div className="bg-white rounded-lg border p-6">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-lg font-semibold text-gray-900">Validation Results</h2>
              <button
                onClick={resetImport}
                className="text-sm text-gray-600 hover:text-gray-900"
              >
                Start Over
              </button>
            </div>

            <div className="grid grid-cols-3 gap-4 mb-6">
              <div className="bg-blue-50 rounded-lg p-4">
                <div className="flex items-center">
                  <FileText className="h-6 w-6 text-blue-600 mr-2" />
                  <div>
                    <p className="text-2xl font-bold text-blue-600">{validationResult.totalRows}</p>
                    <p className="text-sm text-gray-600">Total Rows</p>
                  </div>
                </div>
              </div>
              <div className="bg-green-50 rounded-lg p-4">
                <div className="flex items-center">
                  <Check className="h-6 w-6 text-green-600 mr-2" />
                  <div>
                    <p className="text-2xl font-bold text-green-600">{validationResult.validUsers.length}</p>
                    <p className="text-sm text-gray-600">Valid Users</p>
                  </div>
                </div>
              </div>
              <div className="bg-red-50 rounded-lg p-4">
                <div className="flex items-center">
                  <X className="h-6 w-6 text-red-600 mr-2" />
                  <div>
                    <p className="text-2xl font-bold text-red-600">{validationResult.validationErrors.length}</p>
                    <p className="text-sm text-gray-600">Errors</p>
                  </div>
                </div>
              </div>
            </div>

            {validationResult.validationErrors.length > 0 && (
              <div className="mb-6">
                <h3 className="text-md font-medium text-red-900 mb-3">Validation Errors</h3>
                <div className="bg-red-50 border border-red-200 rounded-lg max-h-64 overflow-y-auto">
                  <table className="min-w-full divide-y divide-red-200">
                    <thead className="bg-red-100 sticky top-0">
                      <tr>
                        <th className="px-4 py-2 text-left text-xs font-medium text-red-900 uppercase tracking-wider">Row</th>
                        <th className="px-4 py-2 text-left text-xs font-medium text-red-900 uppercase tracking-wider">Field</th>
                        <th className="px-4 py-2 text-left text-xs font-medium text-red-900 uppercase tracking-wider">Error</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-red-200">
                      {validationResult.validationErrors.map((error, index) => (
                        <tr key={index}>
                          <td className="px-4 py-2 text-sm text-red-900">{error.rowNumber}</td>
                          <td className="px-4 py-2 text-sm text-red-900">{error.field}</td>
                          <td className="px-4 py-2 text-sm text-red-900">{error.error}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            )}

            {validationResult.validUsers.length > 0 && (
              <div className="flex justify-end">
                <button
                  onClick={() => setStep('confirm')}
                  className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-green-600 hover:bg-green-700"
                >
                  Continue to Import ({validationResult.validUsers.length} users)
                </button>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Confirmation Step */}
      {step === 'confirm' && validationResult && (
        <div className="space-y-6">
          <div className="bg-white rounded-lg border p-6">
            <div className="flex items-center mb-4">
              <AlertCircle className="h-6 w-6 text-yellow-600 mr-3" />
              <h2 className="text-lg font-semibold text-gray-900">Confirm Import</h2>
            </div>
            
            <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4 mb-6">
              <p className="text-sm text-yellow-800">
                You are about to import <strong>{validationResult.validUsers.length} users</strong> into the system.
                Each user will receive a temporary password and will need to reset it on their first login.
                This action cannot be undone.
              </p>
            </div>

            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setStep('validate')}
                className="px-4 py-2 border border-gray-300 rounded-md shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50"
              >
                Back to Results
              </button>
              <button
                onClick={importUsers}
                disabled={loading}
                className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-red-600 hover:bg-red-700 disabled:opacity-50"
              >
                {loading ? 'Importing...' : 'Import Users'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Completion Step */}
      {step === 'complete' && importResult && (
        <div className="space-y-6">
          <div className="bg-white rounded-lg border p-6">
            <div className="flex items-center mb-4">
              <Check className="h-6 w-6 text-green-600 mr-3" />
              <h2 className="text-lg font-semibold text-gray-900">Import Complete</h2>
            </div>

            <div className="grid grid-cols-3 gap-4 mb-6">
              <div className="bg-blue-50 rounded-lg p-4">
                <div className="flex items-center">
                  <Users className="h-6 w-6 text-blue-600 mr-2" />
                  <div>
                    <p className="text-2xl font-bold text-blue-600">{importResult.totalUsers}</p>
                    <p className="text-sm text-gray-600">Total Processed</p>
                  </div>
                </div>
              </div>
              <div className="bg-green-50 rounded-lg p-4">
                <div className="flex items-center">
                  <Check className="h-6 w-6 text-green-600 mr-2" />
                  <div>
                    <p className="text-2xl font-bold text-green-600">{importResult.successCount}</p>
                    <p className="text-sm text-gray-600">Successful</p>
                  </div>
                </div>
              </div>
              <div className="bg-red-50 rounded-lg p-4">
                <div className="flex items-center">
                  <X className="h-6 w-6 text-red-600 mr-2" />
                  <div>
                    <p className="text-2xl font-bold text-red-600">{importResult.failureCount}</p>
                    <p className="text-sm text-gray-600">Failed</p>
                  </div>
                </div>
              </div>
            </div>

            {importResult.errors.length > 0 && (
              <div className="mb-6">
                <h3 className="text-md font-medium text-red-900 mb-3">Import Errors</h3>
                <div className="bg-red-50 border border-red-200 rounded-lg p-4 max-h-64 overflow-y-auto">
                  {importResult.errors.map((error, index) => (
                    <div key={index} className="text-sm text-red-800 mb-1">
                      {error}
                    </div>
                  ))}
                </div>
              </div>
            )}

            <div className="flex justify-end">
              <button
                onClick={resetImport}
                className="px-4 py-2 border border-gray-300 rounded-md shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50"
              >
                Import Another File
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}