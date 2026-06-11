import type { ReactNode, ButtonHTMLAttributes, InputHTMLAttributes, SelectHTMLAttributes } from 'react'
import { SpinnerIcon } from './Icons'

// ── Button ────────────────────────────────────────────────────────────────────
type BtnVariant = 'primary' | 'danger' | 'success' | 'ghost' | 'outline' | 'warning'
type BtnSize = 'xs' | 'sm' | 'md' | 'lg'

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: BtnVariant
  size?: BtnSize
  loading?: boolean
  icon?: ReactNode
}

const btnVariants: Record<BtnVariant, string> = {
  primary: 'bg-gradient-to-r from-violet-600 to-indigo-600 text-white hover:from-violet-700 hover:to-indigo-700 shadow-md shadow-violet-200 active:scale-95',
  danger:  'bg-gradient-to-r from-rose-500 to-pink-600 text-white hover:from-rose-600 hover:to-pink-700 shadow-md shadow-rose-200 active:scale-95',
  success: 'bg-gradient-to-r from-emerald-500 to-teal-500 text-white hover:from-emerald-600 hover:to-teal-600 shadow-md shadow-emerald-200 active:scale-95',
  warning: 'bg-gradient-to-r from-amber-400 to-orange-500 text-white hover:from-amber-500 hover:to-orange-600 shadow-md shadow-amber-200 active:scale-95',
  ghost:   'bg-white/10 text-gray-700 hover:bg-gray-100 border border-gray-200 active:scale-95',
  outline: 'bg-transparent text-violet-700 border-2 border-violet-300 hover:bg-violet-50 active:scale-95',
}
const btnSizes: Record<BtnSize, string> = {
  xs: 'px-2.5 py-1 text-xs gap-1',
  sm: 'px-3.5 py-1.5 text-xs gap-1.5',
  md: 'px-5 py-2.5 text-sm gap-2',
  lg: 'px-7 py-3 text-base gap-2',
}

export function Button({ variant = 'primary', size = 'md', loading, icon, children, className = '', disabled, ...props }: ButtonProps) {
  return (
    <button
      className={`inline-flex items-center justify-center font-semibold rounded-xl transition-all duration-150 disabled:opacity-50 disabled:cursor-not-allowed ${btnVariants[variant]} ${btnSizes[size]} ${className}`}
      disabled={disabled || loading}
      {...props}
    >
      {loading ? <SpinnerIcon className="w-4 h-4" /> : icon}
      {children}
    </button>
  )
}

// ── Badge ─────────────────────────────────────────────────────────────────────
type BadgeColor = 'gray' | 'green' | 'red' | 'yellow' | 'purple' | 'blue' | 'orange' | 'pink' | 'teal'

const badgeColors: Record<BadgeColor, string> = {
  gray:   'bg-gray-100 text-gray-700 ring-gray-200',
  green:  'bg-emerald-100 text-emerald-700 ring-emerald-200',
  red:    'bg-rose-100 text-rose-700 ring-rose-200',
  yellow: 'bg-amber-100 text-amber-700 ring-amber-200',
  purple: 'bg-violet-100 text-violet-700 ring-violet-200',
  blue:   'bg-blue-100 text-blue-700 ring-blue-200',
  orange: 'bg-orange-100 text-orange-700 ring-orange-200',
  pink:   'bg-pink-100 text-pink-700 ring-pink-200',
  teal:   'bg-teal-100 text-teal-700 ring-teal-200',
}

export function Badge({ color = 'gray', children, className = '' }: { color?: BadgeColor; children: ReactNode; className?: string }) {
  return (
    <span className={`inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold ring-1 ring-inset ${badgeColors[color]} ${className}`}>
      {children}
    </span>
  )
}

// ── Card ──────────────────────────────────────────────────────────────────────
export function Card({ children, className = '' }: { children: ReactNode; className?: string }) {
  return (
    <div className={`bg-white rounded-2xl shadow-sm border border-gray-100 ${className}`}>
      {children}
    </div>
  )
}

export function CardHeader({ title, subtitle, action }: { title: string; subtitle?: string; action?: ReactNode }) {
  return (
    <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100">
      <div>
        <h3 className="font-semibold text-gray-900">{title}</h3>
        {subtitle && <p className="text-xs text-gray-500 mt-0.5">{subtitle}</p>}
      </div>
      {action}
    </div>
  )
}

// ── Input ─────────────────────────────────────────────────────────────────────
interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string
  error?: string
  icon?: ReactNode
}

export function Input({ label, error, icon, className = '', ...props }: InputProps) {
  return (
    <div className="flex flex-col gap-1">
      {label && <label className="text-sm font-medium text-gray-700">{label}</label>}
      <div className="relative">
        {icon && <span className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400">{icon}</span>}
        <input
          className={`w-full rounded-xl border border-gray-200 bg-gray-50 px-4 py-2.5 text-sm text-gray-900 placeholder-gray-400 transition focus:border-violet-400 focus:bg-white focus:outline-none focus:ring-2 focus:ring-violet-100 ${icon ? 'pl-10' : ''} ${error ? 'border-rose-300 focus:border-rose-400 focus:ring-rose-100' : ''} ${className}`}
          {...props}
        />
      </div>
      {error && <p className="text-xs text-rose-600">{error}</p>}
    </div>
  )
}

// ── Select ────────────────────────────────────────────────────────────────────
interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  label?: string
  error?: string
  placeholder?: string
}

export function Select({ label, error, placeholder, children, className = '', ...props }: SelectProps) {
  return (
    <div className="flex flex-col gap-1">
      {label && <label className="text-sm font-medium text-gray-700">{label}</label>}
      <select
        className={`w-full rounded-xl border border-gray-200 bg-gray-50 px-4 py-2.5 text-sm text-gray-900 transition focus:border-violet-400 focus:bg-white focus:outline-none focus:ring-2 focus:ring-violet-100 ${error ? 'border-rose-300' : ''} ${className}`}
        {...props}
      >
        {placeholder && <option value="">{placeholder}</option>}
        {children}
      </select>
      {error && <p className="text-xs text-rose-600">{error}</p>}
    </div>
  )
}

// ── Alert ─────────────────────────────────────────────────────────────────────
type AlertType = 'error' | 'success' | 'warning' | 'info'

const alertStyles: Record<AlertType, string> = {
  error:   'bg-rose-50 border-rose-200 text-rose-800',
  success: 'bg-emerald-50 border-emerald-200 text-emerald-800',
  warning: 'bg-amber-50 border-amber-200 text-amber-800',
  info:    'bg-blue-50 border-blue-200 text-blue-800',
}

export function Alert({ type = 'info', children }: { type?: AlertType; children: ReactNode }) {
  return (
    <div className={`rounded-xl border px-4 py-3 text-sm font-medium ${alertStyles[type]}`}>
      {children}
    </div>
  )
}

// ── Modal ─────────────────────────────────────────────────────────────────────
export function Modal({ open, onClose, title, children }: { open: boolean; onClose: () => void; title: string; children: ReactNode }) {
  if (!open) return null
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/40 backdrop-blur-sm" onClick={onClose} />
      <div className="relative bg-white rounded-2xl shadow-2xl w-full max-w-md p-6 z-10">
        <h2 className="text-lg font-bold text-gray-900 mb-4">{title}</h2>
        {children}
      </div>
    </div>
  )
}

// ── KPI Card ──────────────────────────────────────────────────────────────────
type GradientVariant = 'violet' | 'pink' | 'amber' | 'emerald' | 'blue' | 'orange' | 'teal' | 'rose'
const gradients: Record<GradientVariant, string> = {
  violet:  'from-violet-500 to-purple-700',
  pink:    'from-pink-500 to-rose-600',
  amber:   'from-amber-400 to-orange-500',
  emerald: 'from-emerald-400 to-teal-600',
  blue:    'from-blue-500 to-indigo-600',
  orange:  'from-orange-400 to-red-500',
  teal:    'from-teal-400 to-cyan-600',
  rose:    'from-rose-400 to-pink-600',
}

export function KpiCard({ label, value, sub, gradient = 'violet', icon }: {
  label: string; value: string | number; sub?: string; gradient?: GradientVariant; icon?: ReactNode
}) {
  return (
    <div className={`bg-gradient-to-br ${gradients[gradient]} rounded-2xl p-5 text-white shadow-lg`}>
      <div className="flex items-start justify-between">
        <div>
          <p className="text-white/80 text-sm font-medium">{label}</p>
          <p className="text-3xl font-bold mt-1">{value}</p>
          {sub && <p className="text-white/70 text-xs mt-1">{sub}</p>}
        </div>
        {icon && <div className="opacity-80">{icon}</div>}
      </div>
    </div>
  )
}

// ── Spinner ───────────────────────────────────────────────────────────────────
export function Spinner({ label }: { label?: string }) {
  return (
    <div className="flex flex-col items-center justify-center gap-3 py-12">
      <SpinnerIcon className="w-8 h-8 text-violet-500" />
      {label && <p className="text-sm text-gray-500">{label}</p>}
    </div>
  )
}

// ── Table ─────────────────────────────────────────────────────────────────────
export function Table({ children, className = '' }: { children: ReactNode; className?: string }) {
  return (
    <div className={`overflow-x-auto ${className}`}>
      <table className="w-full text-sm">{children}</table>
    </div>
  )
}
export function Th({ children, className = '' }: { children: ReactNode; className?: string }) {
  return <th className={`px-4 py-3 text-left text-xs font-bold text-gray-500 uppercase tracking-wider bg-gray-50 ${className}`}>{children}</th>
}
export function Td({ children, className = '' }: { children: ReactNode; className?: string }) {
  return <td className={`px-4 py-3 text-gray-700 border-b border-gray-50 ${className}`}>{children}</td>
}
export function Tr({ children, className = '' }: { children: ReactNode; className?: string }) {
  return <tr className={`hover:bg-violet-50/40 transition-colors ${className}`}>{children}</tr>
}

// ── Pagination ────────────────────────────────────────────────────────────────
export function Pagination({ page, total, pageSize, onChange }: { page: number; total: number; pageSize: number; onChange: (p: number) => void }) {
  const pages = Math.ceil(total / pageSize)
  if (pages <= 1) return null
  return (
    <div className="flex items-center justify-between px-4 py-3 border-t border-gray-100">
      <p className="text-xs text-gray-500">
        Showing {(page - 1) * pageSize + 1}–{Math.min(page * pageSize, total)} of {total}
      </p>
      <div className="flex gap-1">
        <button onClick={() => onChange(page - 1)} disabled={page <= 1}
          className="px-3 py-1.5 rounded-lg text-xs font-medium text-gray-600 hover:bg-gray-100 disabled:opacity-40 disabled:cursor-not-allowed transition">
          Prev
        </button>
        {Array.from({ length: Math.min(pages, 7) }, (_, i) => i + 1).map(p => (
          <button key={p} onClick={() => onChange(p)}
            className={`px-3 py-1.5 rounded-lg text-xs font-medium transition ${p === page ? 'bg-violet-600 text-white shadow-sm' : 'text-gray-600 hover:bg-gray-100'}`}>
            {p}
          </button>
        ))}
        <button onClick={() => onChange(page + 1)} disabled={page >= pages}
          className="px-3 py-1.5 rounded-lg text-xs font-medium text-gray-600 hover:bg-gray-100 disabled:opacity-40 disabled:cursor-not-allowed transition">
          Next
        </button>
      </div>
    </div>
  )
}
