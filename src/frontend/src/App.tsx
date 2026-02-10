import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Layout } from './components/layout/Layout';
import { useAuthStore } from './store/authStore';
import { ToastProvider } from './contexts/ToastContext';

// Page components
const Dashboard = React.lazy(() => import('./pages/dashboard/Dashboard'));
const Login = React.lazy(() => import('./pages/auth/Login'));
const Register = React.lazy(() => import('./pages/auth/Register'));
const LoanApplications = React.lazy(() => import('./pages/loans/LoanApplications'));
const LoanApplicationWizard = React.lazy(() => import('./pages/loans/LoanApplicationWizard'));
const NewLoanWizard = React.lazy(() => import('./pages/loans/NewLoanWizard'));
const LoanApplicationDetail = React.lazy(() => import('./pages/loans/LoanApplicationDetail'));
const Affordability = React.lazy(() => import('./pages/affordability/Affordability'));
const Profile = React.lazy(() => import('./pages/auth/Profile'));
const ContractsList = React.lazy(() => import('./pages/contracts/ContractsList'));
const ContractView = React.lazy(() => import('./pages/contracts/ContractView'));

// Legal pages
const PrivacyPolicy = React.lazy(() => import('./pages/legal/PrivacyPolicy'));
const TermsOfService = React.lazy(() => import('./pages/legal/TermsOfService'));

// Admin components
const AdminDashboard = React.lazy(() => import('./pages/admin/AdminDashboard'));
const AdminSettings = React.lazy(() => import('./pages/admin/AdminSettings'));
const AdminLoans = React.lazy(() => import('./pages/admin/AdminLoans'));
const AdminPayouts = React.lazy(() => import('./pages/admin/AdminPayouts'));
const AdminWhatsApp = React.lazy(() => import('./pages/admin/AdminWhatsApp'));
const AdminUsers = React.lazy(() => import('./pages/admin/AdminUsers'));
const AdminBulkImport = React.lazy(() => import('./pages/admin/AdminBulkImport'));
const AdminNCRCompliance = React.lazy(() => import('./pages/admin/AdminNCRCompliance'));

// Create a client
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});

// Protected Route component
const ProtectedRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { isAuthenticated } = useAuthStore();
  
  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }
  
  return <>{children}</>;
};

// Admin Route component
const AdminRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { isAuthenticated, user } = useAuthStore();
  
  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }
  
  // Check if user has admin role
  const hasAdminRole = user?.roles?.includes('Admin');
  
  if (!hasAdminRole) {
    return <Navigate to="/" replace />;
  }
  
  return <>{children}</>;
};

// Public Route component (redirect if authenticated)
const PublicRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { isAuthenticated } = useAuthStore();
  
  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }
  
  return <>{children}</>;
};

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <Router>
          <div className="min-h-screen bg-gray-50">
            <React.Suspense 
              fallback={
                <div className="min-h-screen flex items-center justify-center">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600"></div>
                </div>
              }
            >
              <Routes>
              {/* Public routes */}
              <Route 
                path="/login" 
                element={
                  <PublicRoute>
                    <Login />
                  </PublicRoute>
                } 
              />
              <Route 
                path="/register" 
                element={
                  <PublicRoute>
                    <Register />
                  </PublicRoute>
                } 
              />
              <Route path="/privacy" element={<PrivacyPolicy />} />
              <Route path="/terms" element={<TermsOfService />} />
              
              {/* Admin routes */}
              <Route
                path="/admin"
                element={
                  <AdminRoute>
                    <Layout />
                  </AdminRoute>
                }
              >
                <Route index element={<AdminDashboard />} />
                <Route path="settings" element={<AdminSettings />} />
                <Route path="loans" element={<AdminLoans />} />
                <Route path="payouts" element={<AdminPayouts />} />
                <Route path="whatsapp" element={<AdminWhatsApp />} />
                <Route path="users" element={<AdminUsers />} />
                <Route path="bulk-import" element={<AdminBulkImport />} />
                <Route path="ncr-compliance" element={<AdminNCRCompliance />} />
              </Route>
              
              {/* Protected routes */}
              <Route 
                path="/" 
                element={
                  <ProtectedRoute>
                    <Layout />
                  </ProtectedRoute>
                }
              >
                <Route index element={<Dashboard />} />
                <Route path="loans" element={<LoanApplications />} />
                <Route path="loans/new" element={<NewLoanWizard />} />
                <Route path="loans/apply" element={<LoanApplicationWizard />} />
                <Route path="loans/:id" element={<LoanApplicationDetail />} />
                <Route path="affordability" element={<Affordability />} />
                <Route path="profile" element={<Profile />} />
                <Route path="documents" element={<Profile />} />
                <Route path="contracts" element={<ContractsList />} />
                <Route path="contracts/:contractId" element={<ContractView />} />
              </Route>
              
              {/* Catch all route */}
              <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
          </React.Suspense>
        </div>
      </Router>
      </ToastProvider>
    </QueryClientProvider>
  );
}

export default App;
