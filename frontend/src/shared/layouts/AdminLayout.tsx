import { Outlet, NavLink, useNavigate } from 'react-router-dom'
import { useDispatch, useSelector } from 'react-redux'
import type { RootState } from '../../app/store'
import { clearCredentials } from '../../features/auth/authSlice'
import { useLogoutMutation } from '../../features/auth/authApi'
import { useGetMyProfileQuery } from '../api/profileApi'
import { DashboardIcon, UsersIcon, FolderIcon, ChartIcon, ClockIcon, LogoutIcon, ShieldIcon } from '../components/Icons'

const navItems = [
  { to: '/admin/dashboard', label: 'Dashboard',     Icon: DashboardIcon },
  { to: '/admin/users',     label: 'Users',         Icon: UsersIcon     },
  { to: '/admin/documents', label: 'Documents',     Icon: FolderIcon    },
  { to: '/admin/analytics', label: 'Analytics',     Icon: ChartIcon     },
  { to: '/admin/activity',  label: 'Activity Logs', Icon: ClockIcon     },
]

export default function AdminLayout() {
  const dispatch = useDispatch()
  const navigate = useNavigate()
  const { fullName, email, refreshToken } = useSelector((s: RootState) => s.auth)
  const [logout] = useLogoutMutation()
  const { data: profileRes } = useGetMyProfileQuery(undefined, { refetchOnMountOrArgChange: true })

  const photoUrl = profileRes?.data?.profilePhotoUrl
  const initials = (fullName ?? 'AD').split(' ').map((w: string) => w[0]).join('').slice(0, 2).toUpperCase()

  const handleLogout = async () => {
    if (refreshToken) { try { await logout({ refreshToken }) } catch { /* ignore */ } }
    dispatch(clearCredentials())
    navigate('/login')
  }

  const Avatar = ({ size }: { size: 'sm' | 'md' }) => {
    const cls = size === 'sm' ? 'w-8 h-8 rounded-lg text-xs' : 'w-8 h-8 rounded-xl text-xs'
    return photoUrl ? (
      <img src={photoUrl} alt="Profile" className={`${cls} object-cover flex-shrink-0`} />
    ) : (
      <div className={`${cls} bg-gradient-to-br from-violet-500 to-pink-600 flex items-center justify-center font-bold text-white flex-shrink-0`}>
        {initials}
      </div>
    )
  }

  return (
    <div className="flex min-h-screen bg-slate-50 font-sans">
      {/* Sidebar */}
      <aside className="w-64 flex-shrink-0 flex flex-col" style={{ background: 'linear-gradient(160deg, #0f0f1a 0%, #1a0a2e 50%, #0f0f1a 100%)' }}>

        {/* Logo */}
        <div className="px-5 py-5 border-b border-white/5">
          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-xl bg-gradient-to-br from-violet-500 to-indigo-600 flex items-center justify-center shadow-lg">
              <ShieldIcon className="w-5 h-5 text-white" />
            </div>
            <div className="flex items-center gap-2">
              <span className="text-white font-bold text-lg tracking-tight">DocVault</span>
              <span className="text-xs bg-violet-500/30 text-violet-300 px-1.5 py-0.5 rounded-md font-bold tracking-widest">ADMIN</span>
            </div>
          </div>
        </div>

        {/* Nav */}
        <nav className="flex-1 px-3 py-4 space-y-1">
          <p className="px-3 pb-2 text-[10px] font-bold text-white/25 uppercase tracking-widest">Navigation</p>
          {navItems.map(({ to, label, Icon }) => (
            <NavLink key={to} to={to}
              className={({ isActive }) =>
                `flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium transition-all group ${
                  isActive
                    ? 'bg-gradient-to-r from-violet-600/50 to-indigo-600/30 text-white border border-violet-400/20'
                    : 'text-gray-400 hover:text-white hover:bg-white/5'
                }`
              }>
              {({ isActive }) => (
                <>
                  <span className={isActive ? 'text-violet-300' : 'text-gray-500 group-hover:text-violet-400'}>
                    <Icon className="w-5 h-5" />
                  </span>
                  {label}
                  {isActive && <span className="ml-auto w-1.5 h-1.5 rounded-full bg-violet-400 flex-shrink-0" />}
                </>
              )}
            </NavLink>
          ))}
        </nav>

        {/* Footer */}
        <div className="p-3 border-t border-white/5">
          <div className="flex items-center gap-3 px-2 py-2 rounded-xl hover:bg-white/5 transition group">
            <Avatar size="sm" />
            <div className="flex-1 min-w-0">
              <p className="text-sm font-semibold text-white/90 truncate">{fullName}</p>
              <p className="text-xs text-gray-500 truncate">{email}</p>
            </div>
            <button onClick={handleLogout} title="Logout"
              className="text-gray-600 hover:text-rose-400 transition-colors">
              <LogoutIcon className="w-4 h-4" />
            </button>
          </div>
        </div>
      </aside>

      {/* Main content */}
      <div className="flex-1 flex flex-col min-w-0">
        <header className="bg-white border-b border-gray-100 px-6 py-4 flex items-center justify-between sticky top-0 z-10">
          <div>
            <h1 className="font-bold text-gray-900">Admin Panel</h1>
            <p className="text-xs text-gray-400 mt-0.5">Manage documents, users and analytics</p>
          </div>
          <Avatar size="md" />
        </header>
        <main className="flex-1 p-6 overflow-auto">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
