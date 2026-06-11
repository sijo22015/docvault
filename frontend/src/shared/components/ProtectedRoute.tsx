import { useSelector } from 'react-redux'
import { Navigate, Outlet } from 'react-router-dom'
import type { RootState } from '../../app/store'

interface Props { role?: string }

export default function ProtectedRoute({ role }: Props) {
  const auth = useSelector((s: RootState) => s.auth)
  if (!auth.accessToken) return <Navigate to="/login" replace />
  if (role && auth.role !== role) {
    return <Navigate to={auth.role === 'Admin' ? '/admin/dashboard' : '/dashboard'} replace />
  }
  return <Outlet />
}
