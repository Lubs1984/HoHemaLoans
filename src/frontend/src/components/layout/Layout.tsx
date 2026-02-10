import React from 'react';
import { Outlet, Link, useLocation } from 'react-router-dom';
import { 
  HomeIcon, 
  DocumentTextIcon, 
  UserCircleIcon,
  ArrowRightOnRectangleIcon,
  Bars3Icon,
  XMarkIcon,
  Cog6ToothIcon,
  ChartBarIcon,
  BanknotesIcon,
  FolderIcon,
  DocumentDuplicateIcon,
  CloudArrowUpIcon,
  ShieldCheckIcon
} from '@heroicons/react/24/outline';
import { useAuthStore } from '../../store/authStore';
import HohemaLogo from '../../assets/hohema-logo.png';

const navigation = [
  { name: 'Dashboard', href: '/', icon: HomeIcon },
  { name: 'Profile', href: '/profile', icon: UserCircleIcon },
  { name: 'Documents', href: '/documents', icon: FolderIcon },
  { name: 'Affordability', href: '/affordability', icon: ChartBarIcon },
  { name: 'Loan Applications', href: '/loans', icon: DocumentTextIcon },
  { name: 'Contracts', href: '/contracts', icon: DocumentDuplicateIcon },
];

const adminNavigation = [
  { name: 'Admin Dashboard', href: '/admin', icon: Cog6ToothIcon },
  { name: 'System Settings', href: '/admin/settings', icon: Cog6ToothIcon },
  { name: 'Loan Management', href: '/admin/loans', icon: DocumentTextIcon },
  { name: 'Loan Payouts', href: '/admin/payouts', icon: BanknotesIcon },
  { name: 'WhatsApp Messages', href: '/admin/whatsapp', icon: HomeIcon },
  { name: 'User Management', href: '/admin/users', icon: UserCircleIcon },
  { name: 'Bulk User Import', href: '/admin/bulk-import', icon: CloudArrowUpIcon },
  { name: 'NCR Compliance', href: '/admin/ncr-compliance', icon: ShieldCheckIcon },
];

export const Layout: React.FC = () => {
  const location = useLocation();
  const { user, logout } = useAuthStore();
  const [sidebarOpen, setSidebarOpen] = React.useState(false);
  const [isAdminMode, setIsAdminMode] = React.useState(location.pathname.startsWith('/admin'));

  React.useEffect(() => {
    setIsAdminMode(location.pathname.startsWith('/admin'));
  }, [location.pathname]);

  const handleLogout = () => {
    logout();
  };

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Mobile sidebar */}
      {sidebarOpen && (
        <div className="fixed inset-0 z-50 lg:hidden">
          <div className="fixed inset-0 bg-gray-600 bg-opacity-75" onClick={() => setSidebarOpen(false)} />
          <div className="fixed inset-y-0 left-0 z-50 w-64 bg-white shadow-xl">
            <div className="flex items-center justify-between h-16 px-6 border-b border-gray-200">
              <img className="h-10 w-auto" src={HohemaLogo} alt="Ho Hema Loans" />
              <button
                onClick={() => setSidebarOpen(false)}
                className="text-gray-400 hover:text-gray-600"
              >
                <XMarkIcon className="h-6 w-6" />
              </button>
            </div>
            <nav className="mt-6 flex flex-col h-full">
              <div className="flex-1">
                {!isAdminMode ? (
                  <>
                    {navigation.map((item) => {
                      const isActive = location.pathname === item.href;
                      return (
                        <Link
                          key={item.name}
                          to={item.href}
                          className={`flex items-center px-6 py-3 text-sm font-medium ${
                            isActive
                              ? 'bg-primary-50 border-r-2 border-primary-600 text-primary-700'
                              : 'text-gray-700 hover:bg-gray-50 hover:text-gray-900'
                          }`}
                          onClick={() => setSidebarOpen(false)}
                        >
                          <item.icon className="mr-3 h-5 w-5" />
                          {item.name}
                        </Link>
                      );
                    })}
                  </>
                ) : (
                  <>
                    <div className="px-6 py-3 text-xs font-semibold text-gray-400 uppercase tracking-wider">Admin</div>
                    {adminNavigation.map((item) => {
                      const isActive = location.pathname === item.href;
                      return (
                        <Link
                          key={item.name}
                          to={item.href}
                          className={`flex items-center px-6 py-3 text-sm font-medium ${
                            isActive
                              ? 'bg-primary-50 border-r-2 border-primary-600 text-primary-700'
                              : 'text-gray-700 hover:bg-gray-50 hover:text-gray-900'
                          }`}
                          onClick={() => setSidebarOpen(false)}
                        >
                          <item.icon className="mr-3 h-5 w-5" />
                          {item.name}
                        </Link>
                      );
                    })}
                    <Link
                      to="/"
                      className="flex items-center px-6 py-3 text-sm font-medium text-gray-700 hover:bg-gray-50 hover:text-gray-900 mt-4 border-t border-gray-200"
                      onClick={() => setSidebarOpen(false)}
                    >
                      <ArrowRightOnRectangleIcon className="mr-3 h-5 w-5" />
                      Back to User Menu
                    </Link>
                  </>
                )}
              </div>
              {user?.roles?.includes('Admin') && !isAdminMode && (
                <Link
                  to="/admin"
                  className="flex items-center px-6 py-3 text-sm font-medium text-gray-700 hover:bg-gray-50 hover:text-gray-900 border-t border-gray-200"
                  onClick={() => setSidebarOpen(false)}
                >
                  <Cog6ToothIcon className="mr-3 h-5 w-5" />
                  Admin Dashboard
                </Link>
              )}
              <button
                onClick={() => {
                  handleLogout();
                  setSidebarOpen(false);
                }}
                className="flex items-center px-6 py-3 text-sm font-medium text-gray-700 hover:bg-gray-50 hover:text-gray-900 border-t border-gray-200 w-full"
              >
                <ArrowRightOnRectangleIcon className="mr-3 h-5 w-5" />
                Sign out
              </button>
            </nav>
          </div>
        </div>
      )}

      {/* Desktop sidebar */}
      <div className="hidden lg:fixed lg:inset-y-0 lg:z-50 lg:flex lg:w-64 lg:flex-col">
        <div className="flex grow flex-col gap-y-5 overflow-y-auto bg-white border-r border-gray-200 px-6 py-4">
          <div className="flex h-16 shrink-0 items-center">
            <img className="h-20 w-auto" src={HohemaLogo} alt="Ho Hema Loans" />
          </div>
          <nav className="flex flex-1 flex-col">
            <ul role="list" className="flex flex-1 flex-col gap-y-7">
              {!isAdminMode ? (
                <>
                  <li>
                    <ul role="list" className="-mx-2 space-y-1">
                      {navigation.map((item) => {
                        const isActive = location.pathname === item.href;
                        return (
                          <li key={item.name}>
                            <Link
                              to={item.href}
                              className={`group flex gap-x-3 rounded-md p-2 text-sm font-semibold leading-6 ${
                                isActive
                                  ? 'bg-primary-50 text-primary-700'
                                  : 'text-gray-700 hover:text-primary-700 hover:bg-gray-50'
                              }`}
                            >
                              <item.icon className="h-5 w-5 shrink-0" />
                              {item.name}
                            </Link>
                          </li>
                        );
                      })}
                    </ul>
                  </li>
                </>
              ) : (
                <>
                  <li>
                    <div className="text-xs font-semibold leading-6 text-gray-400 uppercase tracking-wider">Admin</div>
                    <ul role="list" className="-mx-2 space-y-1 mt-2">
                      {adminNavigation.map((item) => {
                        const isActive = location.pathname === item.href;
                        return (
                          <li key={item.name}>
                            <Link
                              to={item.href}
                              className={`group flex gap-x-3 rounded-md p-2 text-sm font-semibold leading-6 ${
                                isActive
                                  ? 'bg-primary-50 text-primary-700'
                                  : 'text-gray-700 hover:text-primary-700 hover:bg-gray-50'
                              }`}
                            >
                              <item.icon className="h-5 w-5 shrink-0" />
                              {item.name}
                            </Link>
                          </li>
                        );
                      })}
                    </ul>
                  </li>
                  <li>
                    <Link
                      to="/"
                      className="group flex gap-x-3 rounded-md p-2 text-sm font-semibold leading-6 text-gray-700 hover:text-primary-700 hover:bg-gray-50"
                    >
                      <ArrowRightOnRectangleIcon className="h-5 w-5 shrink-0" />
                      Back to User Menu
                    </Link>
                  </li>
                </>
              )}
              {user?.roles?.includes('Admin') && !isAdminMode && (
                <li>
                  <Link
                    to="/admin"
                    className={`group flex gap-x-3 rounded-md p-2 text-sm font-semibold leading-6 ${
                      location.pathname === '/admin'
                        ? 'bg-primary-50 text-primary-700'
                        : 'text-gray-700 hover:text-primary-700 hover:bg-gray-50'
                    }`}
                  >
                    <Cog6ToothIcon className="h-5 w-5 shrink-0" />
                    Admin Dashboard
                  </Link>
                </li>
              )}
              <li className="mt-auto">
                <button
                  onClick={handleLogout}
                  className="group -mx-2 flex w-full gap-x-3 rounded-md p-2 text-sm font-semibold leading-6 text-gray-700 hover:bg-gray-50 hover:text-gray-900"
                >
                  <ArrowRightOnRectangleIcon className="h-5 w-5 shrink-0" />
                  Sign out
                </button>
              </li>
            </ul>
          </nav>
        </div>
      </div>

      {/* Main content */}
      <div className="lg:pl-64">
        {/* Top navigation */}
        <div className="sticky top-0 z-40 flex h-16 shrink-0 items-center gap-x-4 border-b border-gray-200 bg-white px-4 shadow-sm sm:gap-x-6 sm:px-6 lg:px-8">
          <button
            type="button"
            className="-m-2.5 p-2.5 text-gray-700 lg:hidden"
            onClick={() => setSidebarOpen(true)}
          >
            <Bars3Icon className="h-6 w-6" />
          </button>

          {/* Separator */}
          <div className="h-6 w-px bg-gray-200 lg:hidden" />

          <div className="flex flex-1 gap-x-4 self-stretch lg:gap-x-6">
            <div className="flex items-center gap-x-4 lg:gap-x-6">
              {/* User info */}
              <div className="hidden lg:block lg:h-6 lg:w-px lg:bg-gray-200" />
              <div className="flex items-center gap-x-3">
                <div className="h-8 w-8 rounded-full bg-primary-100 flex items-center justify-center">
                  <span className="text-sm font-medium text-primary-700">
                    {user?.firstName?.charAt(0) || user?.phoneNumber?.charAt(-2) || 'U'}
                  </span>
                </div>
                <div className="hidden lg:block">
                  <p className="text-sm font-semibold text-gray-900">
                    {user?.firstName && user?.lastName 
                      ? `${user.firstName} ${user.lastName}`
                      : user?.phoneNumber
                    }
                  </p>
                  <p className="text-xs text-gray-500">{user?.role}</p>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Page content */}
        <main className="py-6">
          <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
};