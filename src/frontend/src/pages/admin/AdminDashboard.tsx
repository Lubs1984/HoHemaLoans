import React, { useEffect, useState } from 'react';
import { apiService } from '../../services/api';

interface DashboardStats {
  applications: {
    total: number;
    pending: number;
    approved: number;
    rejected: number;
  };
  users: {
    total: number;
    verified: number;
  };
  whatsapp: {
    totalContacts: number;
    openConversations: number;
  };
}

const AdminDashboard: React.FC = () => {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchStats = async () => {
      try {
        const response = await apiService.request<DashboardStats>('/admin/dashboard/stats');
        setStats(response);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load dashboard stats');
      } finally {
        setLoading(false);
      }
    };

    fetchStats();
  }, []);

  if (loading) {
    return (
      <div className="flex items-center justify-center h-96">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-red-50 border border-red-200 rounded-lg p-4">
        <p className="text-red-700">{error}</p>
      </div>
    );
  }

  if (!stats) {
    return <div>No data available</div>;
  }

  const StatCard = ({ title, value, color, icon }: { title: string; value: number; color: string; icon: string }) => (
    <div className={`bg-white rounded-lg shadow p-6 border-l-4 border-${color}-500`}>
      <div className="flex items-center justify-between">
        <div>
          <p className="text-gray-600 text-sm font-medium">{title}</p>
          <p className={`text-3xl font-bold mt-2 text-${color}-600`}>{value}</p>
        </div>
        <span className="text-4xl">{icon}</span>
      </div>
    </div>
  );

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Dashboard</h1>
        <p className="text-gray-600 mt-2">System overview and key metrics</p>
      </div>

      {/* Main Stats Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <StatCard
          title="Total Applications"
          value={stats.applications.total}
          color="blue"
          icon="📋"
        />
        <StatCard
          title="Pending"
          value={stats.applications.pending}
          color="yellow"
          icon="⏳"
        />
        <StatCard
          title="Approved"
          value={stats.applications.approved}
          color="green"
          icon="✅"
        />
        <StatCard
          title="Rejected"
          value={stats.applications.rejected}
          color="red"
          icon="❌"
        />
      </div>

      {/* Secondary Stats */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div className="bg-white rounded-lg shadow p-6">
          <h3 className="text-gray-700 font-semibold mb-4">Users</h3>
          <div className="space-y-2">
            <div className="flex justify-between">
              <span className="text-gray-600">Total Users</span>
              <span className="font-bold text-lg">{stats.users.total}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-gray-600">Verified</span>
              <span className="font-bold text-lg text-green-600">{stats.users.verified}</span>
            </div>
            <div className="mt-3 pt-3 border-t">
              <div className="flex justify-between text-sm">
                <span>Verification Rate</span>
                <span className="font-semibold">
                  {stats.users.total > 0 ? ((stats.users.verified / stats.users.total) * 100).toFixed(0) : 0}%
                </span>
              </div>
            </div>
          </div>
        </div>

        <div className="bg-white rounded-lg shadow p-6">
          <h3 className="text-gray-700 font-semibold mb-4">WhatsApp</h3>
          <div className="space-y-2">
            <div className="flex justify-between">
              <span className="text-gray-600">Contacts</span>
              <span className="font-bold text-lg">{stats.whatsapp.totalContacts}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-gray-600">Open Conversations</span>
              <span className="font-bold text-lg text-blue-600">{stats.whatsapp.openConversations}</span>
            </div>
          </div>
        </div>

        <div className="bg-white rounded-lg shadow p-6">
          <h3 className="text-gray-700 font-semibold mb-4">Application Status</h3>
          <div className="space-y-2 text-sm">
            <div className="flex items-center justify-between">
              <div className="flex items-center space-x-2">
                <div className="w-2 h-2 bg-yellow-500 rounded-full"></div>
                <span>Pending</span>
              </div>
              <span className="font-semibold">{stats.applications.pending}</span>
            </div>
            <div className="flex items-center justify-between">
              <div className="flex items-center space-x-2">
                <div className="w-2 h-2 bg-green-500 rounded-full"></div>
                <span>Approved</span>
              </div>
              <span className="font-semibold">{stats.applications.approved}</span>
            </div>
            <div className="flex items-center justify-between">
              <div className="flex items-center space-x-2">
                <div className="w-2 h-2 bg-red-500 rounded-full"></div>
                <span>Rejected</span>
              </div>
              <span className="font-semibold">{stats.applications.rejected}</span>
            </div>
          </div>
        </div>
      </div>

      {/* Quick Actions */}
      <div className="bg-gradient-to-r from-blue-500 to-blue-600 rounded-lg shadow-lg p-6 text-white">
        <h3 className="text-lg font-semibold mb-4">Quick Actions</h3>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <a
            href="/admin/loans?status=Pending"
            className="bg-white bg-opacity-20 hover:bg-opacity-30 rounded-lg p-4 transition"
          >
            <p className="font-semibold">Review Pending Applications</p>
            <p className="text-sm text-blue-100">{stats.applications.pending} pending</p>
          </a>
          <a
            href="/admin/whatsapp"
            className="bg-white bg-opacity-20 hover:bg-opacity-30 rounded-lg p-4 transition"
          >
            <p className="font-semibold">WhatsApp Messages</p>
            <p className="text-sm text-blue-100">{stats.whatsapp.openConversations} open conversations</p>
          </a>
          <a
            href="/admin/users"
            className="bg-white bg-opacity-20 hover:bg-opacity-30 rounded-lg p-4 transition"
          >
            <p className="font-semibold">Manage Users</p>
            <p className="text-sm text-blue-100">{stats.users.total} total users</p>
          </a>
        </div>
      </div>
    </div>
  );
};

export default AdminDashboard;
