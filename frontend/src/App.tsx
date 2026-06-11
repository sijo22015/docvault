import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import LoginPage from './features/auth/LoginPage'
import RegisterPage from './features/auth/RegisterPage'
import PendingPage from './features/auth/PendingPage'
import ProtectedRoute from './shared/components/ProtectedRoute'
import AdminLayout from './shared/layouts/AdminLayout'
import UserLayout from './shared/layouts/UserLayout'
import AdminDashboard from './features/admin/dashboard/AdminDashboard'
import AdminUsers from './features/admin/users/AdminUsers'
import AdminDocuments from './features/admin/documents/AdminDocuments'
import AdminAnalytics from './features/admin/analytics/AdminAnalytics'
import AdminActivityLogs from './features/admin/activity/AdminActivityLogs'
import UserDashboard from './features/user/dashboard/UserDashboard'
import UserDocuments from './features/user/documents/UserDocuments'
import UploadDocument from './features/user/documents/UploadDocument'
import UserProfile from './features/user/profile/UserProfile'

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/pending" element={<PendingPage />} />

        <Route element={<ProtectedRoute role="Admin" />}>
          <Route element={<AdminLayout />}>
            <Route path="/admin" element={<Navigate to="/admin/dashboard" replace />} />
            <Route path="/admin/dashboard" element={<AdminDashboard />} />
            <Route path="/admin/users" element={<AdminUsers />} />
            <Route path="/admin/documents" element={<AdminDocuments />} />
            <Route path="/admin/analytics" element={<AdminAnalytics />} />
            <Route path="/admin/activity" element={<AdminActivityLogs />} />
          </Route>
        </Route>

        <Route element={<ProtectedRoute role="User" />}>
          <Route element={<UserLayout />}>
            <Route path="/dashboard" element={<UserDashboard />} />
            <Route path="/documents" element={<UserDocuments />} />
            <Route path="/documents/upload" element={<UploadDocument />} />
            <Route path="/profile" element={<UserProfile />} />
          </Route>
        </Route>

        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    </BrowserRouter>
  )
}
