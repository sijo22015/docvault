import { useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { useDispatch } from 'react-redux'
import { useLoginMutation } from './authApi'
import { setCredentials } from './authSlice'
import { Button, Input, Alert } from '../../shared/components/ui'
import { ShieldIcon } from '../../shared/components/Icons'

export default function LoginPage() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [login, { isLoading }] = useLoginMutation()
  const dispatch = useDispatch()
  const navigate = useNavigate()

  const handleSubmit = async () => {
    setError('')
    try {
      const res = await login({ email, password }).unwrap()
      const d = res.data
      dispatch(setCredentials({ accessToken: d.accessToken, refreshToken: d.refreshToken, userId: d.userId, email: d.email, fullName: d.fullName, role: d.role }))
      navigate(d.role === 'Admin' ? '/admin/dashboard' : '/dashboard')
    } catch (err: any) {
      setError(err?.data?.error || 'Login failed. Please check your credentials.')
    }
  }

  return (
    <div className="min-h-screen flex font-sans">
      {/* Left panel */}
      <div className="hidden lg:flex lg:w-1/2 flex-col justify-between p-12" style={{ background: 'linear-gradient(135deg, #4c1d95 0%, #6d28d9 40%, #1e40af 100%)' }}>
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-white/20 backdrop-blur flex items-center justify-center">
            <ShieldIcon className="w-6 h-6 text-white" />
          </div>
          <span className="text-white font-bold text-xl">DocVault</span>
        </div>
        <div>
          <h1 className="text-4xl font-bold text-white leading-tight mb-4">
            Secure Document<br />Management System
          </h1>
          <p className="text-violet-200 text-lg">
            Centralized storage for your department's documents with audit trails and role-based access.
          </p>
          <div className="mt-8 flex gap-6">
            {[['🔐', 'Secure'], ['📁', 'Organized'], ['📊', 'Audited']].map(([icon, label]) => (
              <div key={label} className="flex items-center gap-2">
                <span className="text-2xl">{icon}</span>
                <span className="text-violet-200 font-medium">{label}</span>
              </div>
            ))}
          </div>
        </div>
        <p className="text-violet-300 text-sm">© 2026 DocVault · All rights reserved</p>
      </div>

      {/* Right panel */}
      <div className="flex-1 flex items-center justify-center bg-gray-50 p-6">
        <div className="w-full max-w-sm">
          {/* Mobile logo */}
          <div className="lg:hidden flex items-center gap-3 mb-8">
            <div className="w-9 h-9 rounded-xl bg-gradient-to-br from-violet-600 to-blue-600 flex items-center justify-center">
              <ShieldIcon className="w-5 h-5 text-white" />
            </div>
            <span className="font-bold text-xl text-gray-900">DocVault</span>
          </div>

          <div className="mb-8">
            <h2 className="text-2xl font-bold text-gray-900">Welcome back</h2>
            <p className="text-gray-500 mt-1 text-sm">Sign in to your account to continue</p>
          </div>

          {error && <div className="mb-4"><Alert type="error">{error}</Alert></div>}

          <form onSubmit={e => { e.preventDefault(); handleSubmit() }} className="flex flex-col gap-4">
            <Input
              label="Email address"
              type="email"
              placeholder="you@example.com"
              value={email}
              onChange={e => setEmail(e.target.value)}
              required
              autoComplete="email"
            />
            <Input
              label="Password"
              type="password"
              placeholder="••••••••••"
              value={password}
              onChange={e => setPassword(e.target.value)}
              required
              autoComplete="current-password"
            />
            <Button type="submit" size="lg" loading={isLoading} className="w-full mt-2">
              Sign In
            </Button>
          </form>

          <p className="text-center text-sm text-gray-500 mt-6">
            Don't have an account?{' '}
            <Link to="/register" className="text-violet-600 font-semibold hover:text-violet-800 transition-colors">
              Create one
            </Link>
          </p>
        </div>
      </div>
    </div>
  )
}
