# Document Upload & Verification - Quick Start

## ✅ What's Implemented

**ID documents are now stored as BASE64 in the database** and displayed directly in the user profile with zoom and preview capabilities.

### Backend (C# / ASP.NET Core)
- ✅ UserDocument model with FileContentBase64 field
- ✅ DocumentsController (upload, list, download, delete, verification status)
- ✅ AdminController document verification endpoints
- ✅ ProfileVerificationService (auto-verifies users)
- ✅ LocalFileStorageService with BASE64 conversion
- ✅ Database migrations applied

### Frontend (React / TypeScript)
- ✅ DocumentUpload component (drag-and-drop)
- ✅ DocumentList component (thumbnail previews)
- ✅ DocumentViewer component (full-screen with zoom)

---

## 🚀 How to Use

### 1. Apply Database Migrations

```bash
cd src/api/HoHemaLoans.Api
dotnet ef database update
```

### 2. Test the API

**Upload ID Document:**
```bash
curl -X POST http://localhost:5149/api/documents/upload \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "file=@/path/to/id.jpg" \
  -F "documentType=IdDocument" \
  -F "notes=My ID document"
```

**Get Documents:**
```bash
curl http://localhost:5149/api/documents \
  -H "Authorization: Bearer YOUR_TOKEN"
```

**Verification Status:**
```bash
curl http://localhost:5149/api/documents/verification-status \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### 3. Admin Approval (as Admin user)

```bash
curl -X POST http://localhost:5149/api/admin/documents/{documentId}/verify \
  -H "Authorization: Bearer ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "status": "Approved",
    "notes": "Document verified"
  }'
```

---

## 📱 Frontend Integration

Add to your Profile page:

```tsx
import { DocumentUpload } from '@/components/documents/DocumentUpload';
import { DocumentList } from '@/components/documents/DocumentList';
import { useState, useEffect } from 'react';

export function ProfileDocuments() {
  const [documents, setDocuments] = useState([]);
  
  const fetchDocuments = async () => {
    const token = localStorage.getItem('token');
    const response = await fetch('http://localhost:5149/api/documents', {
      headers: { Authorization: `Bearer ${token}` }
    });
    const data = await response.json();
    setDocuments(data);
  };
  
  useEffect(() => {
    fetchDocuments();
  }, []);
  
  const handleDownload = async (id: string) => {
    const token = localStorage.getItem('token');
    const response = await fetch(`http://localhost:5149/api/documents/${id}/download`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    const blob = await response.blob();
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'document.jpg';
    a.click();
  };
  
  const handleDelete = async (id: string) => {
    if (confirm('Delete this document?')) {
      const token = localStorage.getItem('token');
      await fetch(`http://localhost:5149/api/documents/${id}`, {
        method: 'DELETE',
        headers: { Authorization: `Bearer ${token}` }
      });
      fetchDocuments();
    }
  };

  return (
    <div>
      <h2>Upload Your ID Document</h2>
      <DocumentUpload
        documentType="IdDocument"
        onUploadSuccess={() => fetchDocuments()}
        onUploadError={(error) => alert(error)}
      />
      
      <h2 className="mt-8">Your Documents</h2>
      <DocumentList
        documents={documents}
        onDownload={handleDownload}
        onDelete={handleDelete}
      />
    </div>
  );
}
```

---

## 🎯 Key Features

1. **BASE64 Storage**: ID documents stored directly in database
2. **Thumbnail Preview**: Small image shown in list
3. **Click to View**: Full-screen viewer with zoom
4. **Admin Verification**: Approve/reject with reasons
5. **Auto-Verification**: User.IsVerified updates automatically
6. **Required Docs**: ID + Proof of Address = Verified

---

## 📊 Verification Logic

```
User uploads ID document
         ↓
    Status = Pending
         ↓
Admin approves document
         ↓
ProfileVerificationService checks:
  - Has approved ID? ✅
  - Has approved Proof of Address? ✅
         ↓
User.IsVerified = true
```

---

## 📂 Files Created

### Backend
- `/Models/UserDocument.cs`
- `/Models/DocumentDto.cs`
- `/Services/IDocumentStorageService.cs`
- `/Services/LocalFileStorageService.cs`
- `/Services/ProfileVerificationService.cs`
- `/Controllers/DocumentsController.cs`
- `/Data/ApplicationDbContext.cs` (updated)
- `/Controllers/AdminController.cs` (updated)
- `/Migrations/***_AddUserDocuments.cs`
- `/Migrations/***_AddBase64ToDocuments.cs`

### Frontend
- `/components/documents/DocumentUpload.tsx`
- `/components/documents/DocumentList.tsx`
- `/components/documents/DocumentViewer.tsx`

### Documentation
- `/docs/DOCUMENT_UPLOAD_VERIFICATION.md` (full guide)
- `/docs/DOCUMENT_UPLOAD_QUICKSTART.md` (this file)

---

## 🔐 Security Notes

- Files validated: size (10MB max), type (.jpg, .png, .pdf, etc.)
- Users can only see their own documents
- Admins can see all documents
- BASE64 only for ID documents (others use file system)
- Soft delete (IsDeleted flag, not permanent)

---

## ✅ Next Steps

1. Update the Profile page to include document section
2. Create admin page for document verification
3. Test upload → approve → verify flow
4. Add email notifications on verification
5. Consider OCR for automatic ID number extraction

---

**Full documentation:** See `/docs/DOCUMENT_UPLOAD_VERIFICATION.md`
