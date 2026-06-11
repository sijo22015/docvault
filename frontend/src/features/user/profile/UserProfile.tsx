import { useSelector } from 'react-redux'
import type { RootState } from '../../../app/store'
import { Card } from '../../../shared/components/ui'
import { UserIcon, ShieldIcon } from '../../../shared/components/Icons'

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <label className="text-xs font-bold text-gray-400 uppercase tracking-wider">{label}</label>
      <p className="mt-1 text-base font-semibold text-gray-900 bg-gray-50 rounded-xl px-4 py-2.5 border border-gray-100">
        {value || '—'}
      </p>
    </div>
  )
}

export default function UserProfile() {
  const { fullName, email, role } = useSelector((s: RootState) => s.auth)
  const initials = (fullName ?? 'U').split(' ').map(w => w[0]).join('').slice(0, 2).toUpperCase()

  return (
    <div className="max-w-lg mx-auto space-y-6">
      <div>
        <h2 className="text-xl font-bold text-gray-900">My Profile</h2>
        <p className="text-sm text-gray-500 mt-0.5">Your account information</p>
      </div>

      {/* Avatar card */}
      <Card className="p-6">
        <div className="flex items-center gap-5">
          <div className="w-16 h-16 rounded-2xl bg-gradient-to-br from-sky-500 to-blue-600 flex items-center justify-center text-2xl font-bold text-white shadow-lg shadow-blue-200 flex-shrink-0">
            {initials}
          </div>
          <div>
            <h3 className="text-lg font-bold text-gray-900">{fullName}</h3>
            <p className="text-sm text-gray-500">{email}</p>
            <span className={`inline-flex items-center gap-1.5 mt-1.5 px-2.5 py-1 rounded-full text-xs font-bold ${
              role === 'Admin'
                ? 'bg-violet-100 text-violet-700'
                : 'bg-sky-100 text-sky-700'
            }`}>
              <ShieldIcon className="w-3.5 h-3.5" />
              {role}
            </span>
          </div>
        </div>
      </Card>

      {/* Details card */}
      <Card className="p-6 space-y-5">
        <div className="flex items-center gap-2 mb-1">
          <UserIcon className="w-5 h-5 text-gray-400" />
          <h4 className="font-semibold text-gray-700">Account Details</h4>
        </div>
        <Field label="Full Name" value={fullName ?? ''} />
        <Field label="Email Address" value={email ?? ''} />
        <Field label="Role" value={role ?? ''} />
      </Card>

      <p className="text-center text-xs text-gray-400">
        To update your profile details, please contact your administrator.
      </p>
    </div>
  )
}
