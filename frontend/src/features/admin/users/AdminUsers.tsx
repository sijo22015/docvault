import { useState } from 'react'
import { useGetUsersQuery, useApproveUserMutation, useRevokeUserMutation } from '../../../shared/api/adminApi'
import { Button, Badge, Card, Table, Th, Td, Tr, Pagination, Spinner, Alert, Modal } from '../../../shared/components/ui'
import { CheckIcon, XIcon } from '../../../shared/components/Icons'

type StatusColor = 'green' | 'yellow' | 'red' | 'gray'
const statusMeta: Record<string, { color: StatusColor; dot: string }> = {
  APPROVED: { color: 'green',  dot: 'bg-emerald-400' },
  PENDING:  { color: 'yellow', dot: 'bg-amber-400'   },
  REVOKED:  { color: 'red',    dot: 'bg-rose-400'    },
}

const TABS = ['All', 'Pending', 'Approved', 'Revoked']
const TAB_STATUS = [undefined, 'PENDING', 'APPROVED', 'REVOKED']

export default function AdminUsers() {
  const [tab, setTab]       = useState(0)
  const [page, setPage]     = useState(1)
  const [confirm, setConfirm] = useState<{ id: string; action: 'approve' | 'revoke' } | null>(null)

  const { data, isLoading, error } = useGetUsersQuery({ status: TAB_STATUS[tab], page, pageSize: 20 })
  const [approve] = useApproveUserMutation()
  const [revoke]  = useRevokeUserMutation()

  const users = data?.data?.items ?? []
  const total = data?.data?.totalCount ?? 0

  const handleConfirm = async () => {
    if (!confirm) return
    confirm.action === 'approve' ? await approve(confirm.id) : await revoke(confirm.id)
    setConfirm(null)
  }

  return (
    <div className="space-y-5">
      <div>
        <h2 className="text-xl font-bold text-gray-900">User Management</h2>
        <p className="text-sm text-gray-500 mt-0.5">Approve, revoke, and monitor user access</p>
      </div>

      {/* Tabs */}
      <div className="flex gap-1 bg-gray-100 rounded-xl p-1 w-fit">
        {TABS.map((label, i) => (
          <button key={label} onClick={() => { setTab(i); setPage(1) }}
            className={`px-4 py-1.5 rounded-lg text-sm font-semibold transition-all ${
              tab === i ? 'bg-white text-violet-700 shadow-sm' : 'text-gray-500 hover:text-gray-700'
            }`}>
            {label}
          </button>
        ))}
      </div>

      {isLoading && <Spinner label="Loading users…" />}
      {error && <Alert type="error">Failed to load users.</Alert>}

      {!isLoading && (
        <Card>
          <Table>
            <thead>
              <tr>
                <Th>Name</Th>
                <Th>Email</Th>
                <Th>Department</Th>
                <Th>Status</Th>
                <Th>Registered</Th>
                <Th>Actions</Th>
              </tr>
            </thead>
            <tbody>
              {users.map((u: any) => {
                const meta = statusMeta[u.status] ?? { color: 'gray', dot: 'bg-gray-400' }
                return (
                  <Tr key={u.id}>
                    <Td>
                      <div className="flex items-center gap-2.5">
                        <div className="w-7 h-7 rounded-lg bg-gradient-to-br from-violet-400 to-indigo-500 flex items-center justify-center text-xs font-bold text-white flex-shrink-0">
                          {u.fullName?.[0]?.toUpperCase()}
                        </div>
                        <span className="font-medium text-gray-900">{u.fullName}</span>
                      </div>
                    </Td>
                    <Td className="text-gray-500">{u.email}</Td>
                    <Td>
                      <Badge color="purple">{u.department}</Badge>
                    </Td>
                    <Td>
                      <Badge color={meta.color}>
                        <span className={`w-1.5 h-1.5 rounded-full ${meta.dot}`} />
                        {u.status}
                      </Badge>
                    </Td>
                    <Td className="text-gray-500 text-xs">{new Date(u.createdAt).toLocaleDateString()}</Td>
                    <Td>
                      <div className="flex gap-2">
                        {(u.status === 'PENDING' || u.status === 'REVOKED') && (
                          <Button size="xs" variant="success" icon={<CheckIcon className="w-3.5 h-3.5" />}
                            onClick={() => setConfirm({ id: u.id, action: 'approve' })}>
                            Approve
                          </Button>
                        )}
                        {u.status === 'APPROVED' && (
                          <Button size="xs" variant="danger" icon={<XIcon className="w-3.5 h-3.5" />}
                            onClick={() => setConfirm({ id: u.id, action: 'revoke' })}>
                            Revoke
                          </Button>
                        )}
                      </div>
                    </Td>
                  </Tr>
                )
              })}
              {users.length === 0 && (
                <tr><td colSpan={6} className="px-4 py-10 text-center text-sm text-gray-400">No users found.</td></tr>
              )}
            </tbody>
          </Table>
          <Pagination page={page} total={total} pageSize={20} onChange={p => { setPage(p) }} />
        </Card>
      )}

      <Modal open={!!confirm} onClose={() => setConfirm(null)}
        title={confirm?.action === 'approve' ? 'Approve User?' : 'Revoke User Access?'}>
        <p className="text-sm text-gray-600 mb-6">
          {confirm?.action === 'approve'
            ? 'This will grant the user full access to the system.'
            : 'This will immediately revoke the user\'s access and invalidate their sessions.'}
        </p>
        <div className="flex gap-3 justify-end">
          <Button variant="ghost" onClick={() => setConfirm(null)}>Cancel</Button>
          <Button variant={confirm?.action === 'approve' ? 'success' : 'danger'} onClick={handleConfirm}>
            {confirm?.action === 'approve' ? 'Approve' : 'Revoke'}
          </Button>
        </div>
      </Modal>
    </div>
  )
}
