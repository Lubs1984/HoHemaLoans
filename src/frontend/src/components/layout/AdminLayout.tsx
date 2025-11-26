import React from 'react';
import { Link, Outlet, useNavigate } from 'react-router-dom';
import { ArrowRightOnRectangleIcon, Bars3Icon, XMarkIcon } from '@heroicons/react/24/outline';
import { useAuthStore } from '../../store/authStore';

const AdminLayout: React.FC = () => {
  const navigate = useNavigate();
  const { user, logout } = useAuthStore();
  const [sidebarOpen, setSidebarOpen] = React.useState(false);

  const handleLogout = async () => {
    logout();
    navigate('/login');
  };

  const navItems = [
    { label: 'Dashboard', path: '/admin', icon: '📊' },
    { label: 'Loan Applications', path: '/admin/loans', icon: '📋' },
    { label: 'WhatsApp', path: '/admin/whatsapp', icon: '💬' },
    { label: 'Users', path: '/admin/users', icon: '👥' },
  ];

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Sidebar */}
      <aside className={`fixed inset-y-0 left-0 z-50 w-64 bg-gradient-to-b from-blue-600 to-blue-700 text-white transform transition-transform duration-300 ${
        sidebarOpen ? 'translate-x-0' : '-translate-x-full'
      } lg:translate-x-0 lg:static lg:z-auto`}>
        <div className="flex flex-col h-full">
          {/* Header */}
          <div className="p-6 border-b border-blue-500">
            <div className="flex items-center justify-between">
              <h1 className="text-2xl font-bold">HH Admin</h1>
              <button
                onClick={() => setSidebarOpen(false)}
                className="lg:hidden text-white hover:bg-blue-500 p-2 rounded"
              >
                <XMarkIcon className="h-6 w-6" />
              </button>
            </div>
          </div>

          {/* Navigation */}
          <nav className="flex-1 p-6 space-y-2">
            {navItems.map((item) => (
              <Link
                key={item.path}
                to={item.path}
                className="flex items-center space-x-3 px-4 py-3 rounded-lg hover:bg-blue-500 transition"
                onClick={() => setSidebarOpen(false)}
              >
                <span className="text-xl">{item.icon}</span>
                <span className="font-medium">{item.label}</span>
              </Link>
            ))}
          </nav>

          {/* User Info */}
          <div className="border-t border-blue-500 p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-blue-100">Admin</p>
                <p className="font-medium truncate">{user?.firstName} {user?.lastName}</p>
              </div>
              <button
                onClick={handleLogout}
                className="p-2 hover:bg-blue-500 rounded transition"
                title="Logout"
              >
                <ArrowRightOnRectangleIcon className="h-5 w-5" />
              </button>
            </div>
          </div>
        </div>
      </aside>

      {/* Main Content */}
      <div className="lg:ml-0 flex-1">
        {/* Top Bar */}
        <div className="bg-white border-b border-gray-200 px-6 py-4 flex items-center justify-between sticky top-0 z-40">
          <button
            onClick={() => setSidebarOpen(true)}
            className="lg:hidden p-2 hover:bg-gray-100 rounded"
          >
            <Bars3Icon className="h-6 w-6 text-gray-600" />
          </button>
          <div className="flex-1"></div>
          <div className="text-right">
            <p className="text-sm text-gray-600">Welcome back, {user?.firstName}!</p>
          </div>
        </div>

        {/* Content */}
        <main className="p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
};

export default AdminLayout;
