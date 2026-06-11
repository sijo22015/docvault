import { useState } from 'react'
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, LineChart, Line } from 'recharts'
import { useGetAnalyticsQuery } from '../../../shared/api/adminApi'
import { useGetFinancialYearsQuery } from '../../../shared/api/referenceApi'
import { Card, CardHeader, Select, Spinner, Alert } from '../../../shared/components/ui'

function activeFyLabel(): string {
  const now = new Date()
  const year = now.getFullYear()
  const startYear = now.getMonth() >= 3 ? year : year - 1 // April = month 3 (0-based)
  return `${startYear}-${startYear + 1}`
}

export default function AdminAnalytics() {
  const { data: fysRes } = useGetFinancialYearsQuery()
  const fyList = fysRes?.data ?? []
  const [fy, setFy] = useState('')
  const defaultFy = fyList.find((f: any) => f.label === activeFyLabel())?.label ?? fyList[0]?.label ?? ''
  const currentFy = fy || defaultFy

  const { data, isLoading, error } = useGetAnalyticsQuery(currentFy, { skip: !currentFy })
  const analytics = data?.data

  const tooltipStyle = { borderRadius: 12, border: 'none', boxShadow: '0 4px 24px rgba(0,0,0,0.1)', fontSize: 12 }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between flex-wrap gap-3">
        <div>
          <h2 className="text-xl font-bold text-gray-900">Analytics</h2>
          <p className="text-sm text-gray-500 mt-0.5">Document trends and department performance</p>
        </div>
        <div className="w-44">
          <Select value={currentFy} onChange={e => setFy(e.target.value)} disabled={fyList.length === 0}>
            {fyList.map((f: any) => <option key={f.id} value={f.label}>{f.label}</option>)}
          </Select>
        </div>
      </div>

      {isLoading && <Spinner label="Loading analytics…" />}
      {error && <Alert type="error">Failed to load analytics.</Alert>}

      {analytics && (
        <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
          {/* Department progress */}
          <Card>
            <CardHeader title="Department Progress" subtitle="Submissions vs required documents" />
            <div className="px-6 py-4 space-y-4">
              {(analytics.departmentProgress ?? []).map((d: any) => (
                <div key={d.department}>
                  <div className="flex justify-between items-center mb-1.5">
                    <span className="text-sm font-medium text-gray-700">{d.department}</span>
                    <span className="text-xs font-semibold text-gray-500">{d.submitted}/{d.required} · {d.percentComplete}%</span>
                  </div>
                  <div className="h-2.5 bg-gray-100 rounded-full overflow-hidden">
                    <div
                      className="h-full rounded-full transition-all duration-500"
                      style={{
                        width: `${d.percentComplete}%`,
                        background: d.percentComplete >= 80
                          ? 'linear-gradient(90deg, #10b981, #059669)'
                          : d.percentComplete >= 50
                          ? 'linear-gradient(90deg, #f59e0b, #d97706)'
                          : 'linear-gradient(90deg, #f43f5e, #e11d48)',
                      }}
                    />
                  </div>
                </div>
              ))}
              {(analytics.departmentProgress ?? []).length === 0 && (
                <p className="text-sm text-gray-400 text-center py-4">No data for this period</p>
              )}
            </div>
          </Card>

          {/* Monthly trend */}
          <Card>
            <CardHeader title="Monthly Uploads" subtitle="Document upload trend over time" />
            <div className="p-4">
              <ResponsiveContainer width="100%" height={260}>
                <LineChart data={analytics.monthlyTrend ?? []}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
                  <XAxis dataKey="month" tick={{ fontSize: 11, fill: '#9ca3af' }} axisLine={false} tickLine={false} />
                  <YAxis tick={{ fontSize: 11, fill: '#9ca3af' }} axisLine={false} tickLine={false} />
                  <Tooltip contentStyle={tooltipStyle} />
                  <Line type="monotone" dataKey="count" stroke="#7c3aed" strokeWidth={2.5} dot={{ fill: '#7c3aed', r: 4 }} activeDot={{ r: 6 }} />
                </LineChart>
              </ResponsiveContainer>
            </div>
          </Card>

          {/* Top contributors */}
          <Card className="xl:col-span-2">
            <CardHeader title="Top Contributors" subtitle="Users with the most document submissions" />
            <div className="p-4">
              <ResponsiveContainer width="100%" height={260}>
                <BarChart data={(analytics.topContributors ?? []).slice(0, 10)} barSize={32}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" vertical={false} />
                  <XAxis dataKey="userName" tick={{ fontSize: 11, fill: '#9ca3af' }} axisLine={false} tickLine={false} />
                  <YAxis tick={{ fontSize: 11, fill: '#9ca3af' }} axisLine={false} tickLine={false} />
                  <Tooltip contentStyle={tooltipStyle} />
                  <Bar dataKey="documentCount" radius={[6, 6, 0, 0]}
                    fill="url(#barGrad)" />
                  <defs>
                    <linearGradient id="barGrad" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="0%" stopColor="#7c3aed" />
                      <stop offset="100%" stopColor="#4f46e5" />
                    </linearGradient>
                  </defs>
                </BarChart>
              </ResponsiveContainer>
            </div>
          </Card>
        </div>
      )}
    </div>
  )
}
