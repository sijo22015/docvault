import { useNavigate } from 'react-router-dom'
import { Button } from '../../shared/components/ui'

export default function PendingPage() {
  const navigate = useNavigate()
  return (
    <div className="min-h-screen flex items-center justify-center font-sans" style={{ background: 'linear-gradient(135deg, #f5f3ff 0%, #ede9fe 50%, #e0e7ff 100%)' }}>
      <div className="bg-white rounded-3xl shadow-xl p-12 w-full max-w-md text-center mx-4">
        {/* Animated hourglass */}
        <div className="w-20 h-20 mx-auto mb-6 rounded-2xl bg-gradient-to-br from-amber-400 to-orange-500 flex items-center justify-center shadow-lg shadow-amber-200">
          <span className="text-4xl">⏳</span>
        </div>

        <div className="inline-flex items-center gap-2 bg-amber-50 text-amber-700 text-xs font-bold px-3 py-1.5 rounded-full mb-4 ring-1 ring-amber-200">
          <span className="w-2 h-2 rounded-full bg-amber-400 animate-pulse" />
          PENDING APPROVAL
        </div>

        <h1 className="text-2xl font-bold text-gray-900 mb-3">Registration Submitted!</h1>
        <p className="text-gray-500 text-sm leading-relaxed mb-8">
          Your account is pending admin approval. You will receive access once an administrator reviews and approves your request.
        </p>

        <div className="bg-gray-50 rounded-2xl p-4 mb-8 text-left space-y-2">
          {['Your request has been received', 'Admin will review your details', 'You\'ll gain access after approval'].map((step, i) => (
            <div key={i} className="flex items-center gap-3">
              <div className="w-6 h-6 rounded-full bg-violet-100 text-violet-600 flex items-center justify-center text-xs font-bold flex-shrink-0">
                {i + 1}
              </div>
              <span className="text-sm text-gray-600">{step}</span>
            </div>
          ))}
        </div>

        <Button variant="outline" onClick={() => navigate('/login')} className="w-full">
          Back to Login
        </Button>
      </div>
    </div>
  )
}
