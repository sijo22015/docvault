import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts'
import { useGetMyDocumentsQuery } from '../../../shared/api/documentsApi'
import { useGetFinancialYearsQuery } from '../../../shared/api/referenceApi'
import { KpiCard, Card, CardHeader, Spinner } from '../../../shared/components/ui'
import { FolderIcon, CheckIcon, ClockIcon, FileIcon } from '../../../shared/components/Icons'

const tooltipStyle = { borderRadius: 12, border: 'none', boxShadow: '0 4px 24px rgba(0,0,0,0.1)', fontSize: 12 }

export default function UserDashboard() {
  const { data: fysRes } = useGetFinancialYearsQuery()
  const today = new Date().toISOString().slice(0, 10)
  const currentFy = fysRes?.data?.find(
    (fy: any) => fy.fyStart <= today && today <= fy.fyEnd
  ) ?? fysRes?.data?.[0]
  const { data: docs, isLoading } = useGetMyDocumentsQuery(
    { financialYearId: currentFy?.id },
    { skip: !currentFy }
  )

  const documents = docs?.data?.items ?? []
  const totalCount = docs?.data?.totalCount ?? 0

  const byType: Record<string, number> = {}
  for (const d of documents) {
    byType[d.documentTypeName] = (byType[d.documentTypeName] ?? 0) + 1
  }
  const chartData = Object.entries(byType).map(([name, count]) => ({ name, count }))
  const submitted = documents.filter((d: any) => d.status === 'SUBMITTED').length

  if (isLoading) return <Spinner label="Loading dashboard…" />

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-bold text-gray-900">My Dashboard</h2>
        <p className="text-sm text-gray-500 mt-0.5">Your document activity for {currentFy?.label ?? '—'}</p>
      </div>

      {/* KPIs */}
      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
        <KpiCard label="My Documents" value={totalCount}
          sub="This financial year" gradient="blue"
          icon={<FolderIcon className="w-8 h-8 opacity-80" />} />
        <KpiCard label="Submitted" value={submitted}
          sub="Completed submissions" gradient="emerald"
          icon={<CheckIcon className="w-8 h-8 opacity-80" />} />
        <KpiCard label="Current FY" value={currentFy?.label ?? '—'}
          sub="Active financial year" gradient="violet"
          icon={<ClockIcon className="w-8 h-8 opacity-80" />} />
        <KpiCard label="Document Types" value={Object.keys(byType).length}
          sub="Unique types uploaded" gradient="pink"
          icon={<FileIcon className="w-8 h-8 opacity-80" />} />
      </div>

      {/* Chart */}
      {chartData.length > 0 && (
        <Card>
          <CardHeader title="Documents by Type" subtitle="Breakdown of your uploaded document types" />
          <div className="p-4">
            <ResponsiveContainer width="100%" height={260}>
              <BarChart data={chartData} barSize={36}>
                <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" vertical={false} />
                <XAxis dataKey="name" tick={{ fontSize: 12, fill: '#9ca3af' }} axisLine={false} tickLine={false} />
                <YAxis tick={{ fontSize: 12, fill: '#9ca3af' }} axisLine={false} tickLine={false} allowDecimals={false} />
                <Tooltip contentStyle={tooltipStyle} />
                <Bar dataKey="count" radius={[6, 6, 0, 0]} fill="url(#userBarGrad)" />
                <defs>
                  <linearGradient id="userBarGrad" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="0%" stopColor="#0ea5e9" />
                    <stop offset="100%" stopColor="#2563eb" />
                  </linearGradient>
                </defs>
              </BarChart>
            </ResponsiveContainer>
          </div>
        </Card>
      )}

      {/* Recent docs */}
      {documents.length > 0 && (
        <Card>
          <CardHeader title="Recent Documents" subtitle="Your latest uploaded files" />
          <div className="divide-y divide-gray-50">
            {documents.slice(0, 5).map((d: any) => (
              <div key={d.id} className="flex items-center gap-4 px-5 py-3 hover:bg-gray-50/60 transition-colors">
                <div className="w-9 h-9 rounded-xl bg-gradient-to-br from-sky-400 to-blue-500 flex items-center justify-center flex-shrink-0">
                  <FileIcon className="w-5 h-5 text-white" />
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-semibold text-gray-900 truncate">{d.title}</p>
                  <p className="text-xs text-gray-400">{d.documentTypeName} · {d.financialYearLabel}</p>
                </div>
                <span className={`text-xs font-semibold px-2.5 py-1 rounded-full ${
                  d.status === 'SUBMITTED' ? 'bg-emerald-100 text-emerald-700' :
                  d.status === 'ARCHIVED'  ? 'bg-violet-100 text-violet-700'  :
                  'bg-amber-100 text-amber-700'
                }`}>{d.status}</span>
              </div>
            ))}
          </div>
        </Card>
      )}
    </div>
  )
}
