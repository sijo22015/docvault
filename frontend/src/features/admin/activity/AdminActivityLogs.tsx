import { useState } from 'react'
import { apiSlice } from '../../../app/apiSlice'
import { Card, Table, Th, Td, Tr, Badge, Input, Pagination, Spinner, Alert } from '../../../shared/components/ui'
import { SearchIcon } from '../../../shared/components/Icons'

const activityApi = apiSlice.injectEndpoints({
  endpoints: (builder) => ({
    getActivityLogs: builder.query<any, { page?: number; action?: string }>({
      query: ({ page = 1, action } = {}) => ({
        url: `/admin/activity-logs?page=${page}&pageSize=20${action ? `&action=${action}` : ''}`,
      }),
    }),
  }),
})
const { useGetActivityLogsQuery } = activityApi

const actionColor: Record<string, 'blue' | 'green' | 'red' | 'yellow' | 'purple' | 'orange' | 'gray'> = {
  LOGIN: 'blue', UPLOAD: 'green', DELETE: 'red', APPROVE_USER: 'green',
  REVOKE_USER: 'red', SUBMIT: 'purple', RESTORE: 'yellow', LOGOUT: 'orange',
}

export default function AdminActivityLogs() {
  const [page, setPage]     = useState(1)
  const [action, setAction] = useState('')

  const { data, isLoading, error } = useGetActivityLogsQuery({ page, action: action || undefined })
  const logs  = data?.data?.items ?? []
  const total = data?.data?.totalCount ?? 0

  return (
    <div className="space-y-5">
      <div>
        <h2 className="text-xl font-bold text-gray-900">Activity Logs</h2>
        <p className="text-sm text-gray-500 mt-0.5">Full audit trail of all system actions</p>
      </div>

      {/* Filter */}
      <div className="w-72">
        <Input
          placeholder="Filter by action (e.g. LOGIN, UPLOAD)"
          value={action}
          onChange={e => { setAction(e.target.value); setPage(1) }}
          icon={<SearchIcon className="w-4 h-4" />}
        />
      </div>

      {isLoading && <Spinner label="Loading logs…" />}
      {error && <Alert type="error">Failed to load activity logs.</Alert>}

      {!isLoading && (
        <Card>
          <Table>
            <thead>
              <tr>
                <Th>Action</Th>
                <Th>User</Th>
                <Th>Entity</Th>
                <Th>Details</Th>
                <Th>IP Address</Th>
                <Th>Time</Th>
              </tr>
            </thead>
            <tbody>
              {logs.map((l: any) => (
                <Tr key={l.id}>
                  <Td>
                    <Badge color={actionColor[l.action] ?? 'gray'}>{l.action}</Badge>
                  </Td>
                  <Td>
                    {l.userName
                      ? <div className="flex items-center gap-2">
                          <div className="w-6 h-6 rounded-md bg-gradient-to-br from-violet-400 to-indigo-500 flex items-center justify-center text-xs font-bold text-white">
                            {l.userName[0]?.toUpperCase()}
                          </div>
                          <span className="text-sm text-gray-700">{l.userName}</span>
                        </div>
                      : <span className="text-gray-400">—</span>
                    }
                  </Td>
                  <Td>
                    <span className="text-sm text-gray-700">{l.entityType}</span>
                    {l.entityId && <span className="text-xs text-gray-400 ml-1">({String(l.entityId).slice(0, 8)}…)</span>}
                  </Td>
                  <Td className="text-gray-500 text-xs max-w-40 truncate">{l.details ?? '—'}</Td>
                  <Td>
                    {l.ipAddress
                      ? <code className="text-xs bg-gray-100 px-2 py-0.5 rounded-md text-gray-600">{l.ipAddress}</code>
                      : <span className="text-gray-400">—</span>
                    }
                  </Td>
                  <Td className="text-gray-500 text-xs whitespace-nowrap">{new Date(l.createdAt).toLocaleString()}</Td>
                </Tr>
              ))}
              {logs.length === 0 && (
                <tr><td colSpan={6} className="px-4 py-10 text-center text-sm text-gray-400">No activity logs found.</td></tr>
              )}
            </tbody>
          </Table>
          <Pagination page={page} total={total} pageSize={20} onChange={setPage} />
        </Card>
      )}
    </div>
  )
}
