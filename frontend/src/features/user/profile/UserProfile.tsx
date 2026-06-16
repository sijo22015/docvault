import { useState, useRef } from 'react'
import { useSelector } from 'react-redux'
import type { RootState } from '../../../app/store'
import { Card, Button, Input, Alert, Spinner } from '../../../shared/components/ui'
import { UserIcon, ShieldIcon, CameraIcon, PhoneIcon, XIcon } from '../../../shared/components/Icons'
import { SpinnerIcon } from '../../../shared/components/Icons'
import { useGetMyProfileQuery, useUpdateMyProfileMutation, useUploadProfilePhotoMutation } from '../../../shared/api/profileApi'

function ReadOnlyField({ label, value }: { label: string; value: string }) {
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

  const { data: profileRes, isLoading, isError, refetch } = useGetMyProfileQuery(undefined, { refetchOnMountOrArgChange: true })
  const [updateProfile, { isLoading: isSaving }]     = useUpdateMyProfileMutation()
  const [uploadPhoto,   { isLoading: isUploadingPhoto }] = useUploadProfilePhotoMutation()

  const [editing, setEditing]     = useState(false)
  const [form, setForm]           = useState({ mobileNumber: '', whatsAppNumber: '' })
  const [error, setError]         = useState('')
  const [success, setSuccess]     = useState('')
  const [lightboxOpen, setLightboxOpen] = useState(false)
  const photoInputRef             = useRef<HTMLInputElement>(null)

  const profile  = profileRes?.data
  const initials = (fullName ?? 'U').split(' ').map((w: string) => w[0]).join('').slice(0, 2).toUpperCase()

  const startEdit = () => {
    setForm({
      mobileNumber:   profile?.mobileNumber   ?? '',
      whatsAppNumber: profile?.whatsAppNumber ?? '',
    })
    setError('')
    setSuccess('')
    setEditing(true)
  }

  const cancelEdit = () => { setEditing(false); setError(''); setSuccess('') }

  const handleSave = async () => {
    setError('')
    try {
      await updateProfile({
        MobileNumber:   form.mobileNumber   || undefined,
        WhatsAppNumber: form.whatsAppNumber || undefined,
      }).unwrap()
      setSuccess('Profile updated successfully.')
      setEditing(false)
      refetch()
    } catch (err: any) {
      setError(err?.data?.error || 'Failed to update profile. Please try again.')
    }
  }

  const handlePhotoChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return
    const allowed = ['image/jpeg', 'image/png', 'image/webp']
    if (!allowed.includes(file.type)) { setError('Please upload a JPEG, PNG, or WebP image.'); e.target.value = ''; return }
    if (file.size > 5 * 1024 * 1024)  { setError('Photo must be under 5 MB.');                 e.target.value = ''; return }
    setError('')
    try {
      const fd = new FormData()
      fd.append('photo', file)
      await uploadPhoto(fd).unwrap()
      setSuccess('Profile photo updated.')
      refetch()
    } catch (err: any) {
      setError(err?.data?.error || 'Failed to upload photo.')
    }
    e.target.value = ''
  }

  if (isLoading) return <Spinner label="Loading profile…" />
  if (isError) return (
    <div className="max-w-lg mx-auto">
      <Alert type="error">Failed to load profile. <button onClick={() => refetch()} className="underline font-semibold ml-1">Retry</button></Alert>
    </div>
  )

  return (
    <div className="max-w-lg mx-auto space-y-6">
      <div>
        <h2 className="text-xl font-bold text-gray-900">My Profile</h2>
        <p className="text-sm text-gray-500 mt-0.5">View and update your account information</p>
      </div>

      {error   && <Alert type="error">{error}</Alert>}
      {success && <Alert type="success">{success}</Alert>}

      {/* Avatar card */}
      <Card className="p-6">
        <div className="flex items-center gap-5">
          <div className="relative flex-shrink-0">
            {profile?.profilePhotoUrl ? (
              <img src={profile.profilePhotoUrl} alt="Profile photo"
                onClick={() => setLightboxOpen(true)}
                className="w-20 h-20 rounded-2xl object-cover shadow-lg shadow-blue-100 cursor-pointer hover:opacity-90 transition-opacity" />
            ) : (
              <div className="w-20 h-20 rounded-2xl bg-gradient-to-br from-sky-500 to-blue-600 flex items-center justify-center text-2xl font-bold text-white shadow-lg shadow-blue-200">
                {initials}
              </div>
            )}
            <button type="button" title="Change profile photo"
              onClick={() => { setError(''); photoInputRef.current?.click() }}
              disabled={isUploadingPhoto}
              className="absolute -bottom-2 -right-2 w-8 h-8 bg-white border-2 border-gray-200 rounded-xl flex items-center justify-center hover:bg-gray-50 transition shadow-sm disabled:opacity-60">
              {isUploadingPhoto
                ? <SpinnerIcon className="w-3.5 h-3.5 text-gray-500" />
                : <CameraIcon  className="w-3.5 h-3.5 text-gray-500" />}
            </button>
            <input ref={photoInputRef} type="file" accept="image/jpeg,image/png,image/webp"
              className="hidden" onChange={handlePhotoChange} />
          </div>
          <div>
            <h3 className="text-lg font-bold text-gray-900">{fullName}</h3>
            <p className="text-sm text-gray-500">{email}</p>
            <span className={`inline-flex items-center gap-1.5 mt-1.5 px-2.5 py-1 rounded-full text-xs font-bold ${
              role === 'Admin' ? 'bg-violet-100 text-violet-700' : 'bg-sky-100 text-sky-700'
            }`}>
              <ShieldIcon className="w-3.5 h-3.5" />{role}
            </span>
          </div>
        </div>
        <p className="text-xs text-gray-400 mt-4">
          Click the camera icon to change your photo · JPEG, PNG or WebP · Max 5 MB
        </p>
      </Card>

      {/* Details card */}
      <Card className="p-6 space-y-5">
        <div className="flex items-center justify-between mb-1">
          <div className="flex items-center gap-2">
            <UserIcon className="w-5 h-5 text-gray-400" />
            <h4 className="font-semibold text-gray-700">Account Details</h4>
          </div>
          {!editing && <Button size="sm" variant="outline" onClick={startEdit}>Edit</Button>}
        </div>

        {/* Always read-only */}
        <ReadOnlyField label="Full Name"      value={fullName ?? ''} />
        <ReadOnlyField label="Email Address"  value={email    ?? ''} />
        <ReadOnlyField label="Role"           value={role     ?? ''} />

        {/* Contact numbers — editable */}
        <div className="flex items-center gap-2 pt-1 pb-0.5">
          <PhoneIcon className="w-4 h-4 text-gray-400" />
          <span className="text-xs font-bold text-gray-400 uppercase tracking-wider">Contact Numbers</span>
        </div>

        {editing ? (
          <>
            <Input label="Mobile Number" type="tel"
              value={form.mobileNumber}
              onChange={e => setForm(f => ({ ...f, mobileNumber: e.target.value }))}
              placeholder="+91 98765 43210" />
            <Input label="WhatsApp Number" type="tel"
              value={form.whatsAppNumber}
              onChange={e => setForm(f => ({ ...f, whatsAppNumber: e.target.value }))}
              placeholder="+91 98765 43210" />
            <div className="flex gap-3 pt-1">
              <Button variant="ghost" onClick={cancelEdit} disabled={isSaving}>Cancel</Button>
              <Button loading={isSaving} onClick={handleSave}>Save Changes</Button>
            </div>
          </>
        ) : (
          <>
            <ReadOnlyField label="Mobile Number"   value={profile?.mobileNumber   ?? ''} />
            <ReadOnlyField label="WhatsApp Number" value={profile?.whatsAppNumber ?? ''} />
          </>
        )}
      </Card>

      {/* Photo lightbox */}
      {lightboxOpen && profile?.profilePhotoUrl && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/75 backdrop-blur-sm"
          onClick={() => setLightboxOpen(false)}
        >
          <div className="relative" onClick={e => e.stopPropagation()}>
            <img
              src={profile.profilePhotoUrl}
              alt="Profile photo"
              className="max-w-[90vw] max-h-[85vh] rounded-2xl shadow-2xl object-contain"
            />
            <button
              onClick={() => setLightboxOpen(false)}
              className="absolute -top-3 -right-3 w-9 h-9 bg-white rounded-full flex items-center justify-center shadow-lg hover:bg-gray-100 transition-colors"
            >
              <XIcon className="w-5 h-5 text-gray-700" />
            </button>
          </div>
        </div>
      )}
    </div>
  )
}
