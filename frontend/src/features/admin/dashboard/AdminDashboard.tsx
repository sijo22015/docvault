import { PieChart, Pie, Cell, Tooltip, ResponsiveContainer, Legend } from 'recharts'
import { useGetDashboardSummaryQuery } from '../../../shared/api/adminApi'
import { KpiCard, Card, CardHeader, Spinner, Alert, Badge } from '../../../shared/components/ui'
import { UsersIcon, FolderIcon, TrendIcon, ClockIcon } from '../../../shared/components/Icons'

const COLORS = ['#7c3aed', '#db2777', '#f59e0b', '#10b981', '#3b82f6', '#f97316', '#06b6d4', '#8b5cf6']

const actionColor: Record<string, 'blue' | 'green' | 'red' | 'yellow' | 'purple' | 'orange'> = {
  LOGIN: 'blue', UPLOAD: 'green', DELETE: 'red', APPROVE_USER: 'green',
  REVOKE_USER: 'red', SUBMIT: 'purple', RESTORE: 'yellow',
}

export default function AdminDashboard() {
  const { data, isLoading, error } = useGetDashboardSummaryQuery()

  if (isLoading) return <Spinner label="Loading dashboard…" />
  if (error) return <Alert type="error">Failed to load dashboard. Check server logs.</Alert>

  const s = data?.data
  const deptData: { name: string; value: number; fill: string }[] =
    ((s?.byDepartment ?? []) as { department: string; count: number }[])
      .filter(d => d.count > 0)
      .map((d, i) => ({
        name: d.department, value: d.count, fill: COLORS[i % COLORS.length],
      }))

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-bold text-gray-900">Dashboard Overview</h2>
        <p className="text-sm text-gray-500 mt-0.5">Real-time summary of your organization</p>
      </div>

      {/* KPI row */}
      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
        <KpiCard label="Total Users" value={s?.totalUsers ?? 0}
          sub="All registered accounts" gradient="violet"
          icon={<UsersIcon className="w-8 h-8 opacity-80" />} />
        <KpiCard label="Pending Approvals" value={s?.pendingApprovals ?? 0}
          sub={s?.pendingApprovals ? 'Needs attention' : 'All clear'} gradient={(s?.pendingApprovals ?? 0) > 0 ? 'rose' : 'emerald'}
          icon={<ClockIcon className="w-8 h-8 opacity-80" />} />
        <KpiCard label="Total Documents" value={s?.totalDocuments ?? 0}
          sub="Across all departments" gradient="blue"
          icon={<FolderIcon className="w-8 h-8 opacity-80" />} />
        <KpiCard label="Uploads This Month" value={s?.documentsThisMonth ?? 0}
          sub="Current month activity" gradient="amber"
          icon={<TrendIcon className="w-8 h-8 opacity-80" />} />
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
        {/* Pie chart */}
        <Card>
          <CardHeader title="Documents by Department" subtitle="Distribution across all departments" />
          <div className="p-4">
            {deptData.length === 0
              ? <p className="text-sm text-gray-400 text-center py-8">No data yet</p>
              : <ResponsiveContainer width="100%" height={300}>
                  <PieChart>
                    <Pie data={deptData} dataKey="value" nameKey="name" cx="50%" cy="45%" outerRadius={90} innerRadius={45}>
                      {deptData.map((entry, i) => (
                        <Cell key={i} fill={entry.fill} />
                      ))}
                    </Pie>
                    <Tooltip
                      formatter={(value, name) => { const v = Number(value ?? 0); return [`${v} doc${v !== 1 ? 's' : ''}`, String(name)] }}
                      contentStyle={{ borderRadius: 12, border: 'none', boxShadow: '0 4px 24px rgba(0,0,0,0.1)' }}
                    />
                    <Legend iconType="circle" iconSize={8} wrapperStyle={{ paddingTop: 12, fontSize: 12 }} />
                  </PieChart>
                </ResponsiveContainer>
            }
          </div>
        </Card>

        {/* Recent activity */}
        <Card>
          <CardHeader title="Recent Activity" subtitle="Latest 8 actions across the system" />
          <div className="divide-y divide-gray-50">
            {(s?.recentActivity ?? []).slice(0, 8).map((a: any, i: number) => (
              <div key={i} className="flex items-center gap-3 px-5 py-3 hover:bg-gray-50/60 transition-colors">
                <Badge color={actionColor[a.action] ?? 'gray'}>{a.action}</Badge>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-gray-800 truncate">{a.userName ?? 'System'}</p>
                  <p className="text-xs text-gray-400">{a.entityType}</p>
                </div>
                <p className="text-xs text-gray-400 flex-shrink-0">{new Date(a.createdAt).toLocaleString()}</p>
              </div>
            ))}
            {(s?.recentActivity ?? []).length === 0 && (
              <p className="text-sm text-gray-400 text-center py-8">No activity yet</p>
            )}
          </div>
        </Card>
      </div>
    </div>
  )
}
