import { useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { useRegisterMutation } from './authApi'
import { Button, Input, Select, Alert } from '../../shared/components/ui'
import { ShieldIcon } from '../../shared/components/Icons'

const DEPARTMENTS = ['Physics', 'Chemistry', 'Mathematics', 'English', 'Malayalam', 'Computer Science', 'Biology', 'History', 'Economics', 'Commerce']

export default function RegisterPage() {
  const [form, setForm] = useState({ fullName: '', email: '', password: '', department: '' })
  const [error, setError] = useState('')
  const [register, { isLoading }] = useRegisterMutation()
  const navigate = useNavigate()

  const set = (key: string) => (e: { target: { value: string } }) =>
    setForm(f => ({ ...f, [key]: e.target.value }))

  const handleSubmit = async () => {
    setError('')
    try {
      await register(form).unwrap()
      navigate('/pending')
    } catch (err: any) {
      setError(err?.data?.error || 'Registration failed. Please try again.')
    }
  }

  return (
    <div className="min-h-screen flex font-sans">
      {/* Left panel */}
      <div className="hidden lg:flex lg:w-1/2 flex-col justify-between p-12" style={{ background: 'linear-gradient(135deg, #064e3b 0%, #065f46 40%, #1e3a5f 100%)' }}>
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-white/20 backdrop-blur flex items-center justify-center">
            <ShieldIcon className="w-6 h-6 text-white" />
          </div>
          <span className="text-white font-bold text-xl">DocVault</span>
        </div>
        <div>
          <h1 className="text-4xl font-bold text-white leading-tight mb-4">
            Join DocVault<br />Today
          </h1>
          <p className="text-emerald-200 text-lg">
            Register with your department credentials and get access after admin approval.
          </p>
          <div className="mt-8 space-y-3">
            {['Submit documents for your department', 'Track submissions and status', 'Secure, audited document storage'].map(t => (
              <div key={t} className="flex items-center gap-3">
                <div className="w-5 h-5 rounded-full bg-emerald-400/30 flex items-center justify-center flex-shrink-0">
                  <div className="w-2 h-2 rounded-full bg-emerald-400" />
                </div>
                <span className="text-emerald-100 text-sm">{t}</span>
              </div>
            ))}
          </div>
        </div>
        <p className="text-emerald-300 text-sm">© 2026 DocVault · All rights reserved</p>
      </div>

      {/* Right panel */}
      <div className="flex-1 flex items-center justify-center bg-gray-50 p-6">
        <div className="w-full max-w-sm">
          <div className="lg:hidden flex items-center gap-3 mb-8">
            <div className="w-9 h-9 rounded-xl bg-gradient-to-br from-emerald-600 to-blue-600 flex items-center justify-center">
              <ShieldIcon className="w-5 h-5 text-white" />
            </div>
            <span className="font-bold text-xl text-gray-900">DocVault</span>
          </div>

          <div className="mb-8">
            <h2 className="text-2xl font-bold text-gray-900">Create account</h2>
            <p className="text-gray-500 mt-1 text-sm">Fill in your details to request access</p>
          </div>

          {error && <div className="mb-4"><Alert type="error">{error}</Alert></div>}

          <form onSubmit={e => { e.preventDefault(); handleSubmit() }} className="flex flex-col gap-4">
            <Input label="Full Name" placeholder="Jane Smith" value={form.fullName} onChange={set('fullName')} required />
            <Input label="Email address" type="email" placeholder="you@college.edu" value={form.email} onChange={set('email')} required />
            <div>
              <Input label="Password" type="password" placeholder="••••••••••" value={form.password} onChange={set('password')} required />
              <p className="text-xs text-gray-400 mt-1">Min 10 characters — must include uppercase, lowercase, digit & special character</p>
            </div>
            <Select label="Department" value={form.department} onChange={set('department')} placeholder="Select your department" required>
              {DEPARTMENTS.map(d => <option key={d} value={d}>{d}</option>)}
            </Select>
            <Button type="submit" size="lg" loading={isLoading} className="w-full mt-2" variant="success">
              Create Account
            </Button>
          </form>

          <p className="text-center text-sm text-gray-500 mt-6">
            Already have an account?{' '}
            <Link to="/login" className="text-violet-600 font-semibold hover:text-violet-800 transition-colors">
              Sign in
            </Link>
          </p>
        </div>
      </div>
    </div>
  )
}
