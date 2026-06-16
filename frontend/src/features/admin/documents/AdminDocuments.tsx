import { useState, useEffect } from 'react'
import { useSelector } from 'react-redux'
import type { RootState } from '../../../app/store'
import { useAdminSearchDocumentsQuery, useAdminRestoreDocumentMutation, useAdminPurgeDeletedMutation } from '../../../shared/api/documentsApi'
import { useGetDepartmentsQuery, useGetDocumentTypesQuery, useGetFinancialYearsQuery } from '../../../shared/api/referenceApi'
import { Card, Table, Th, Td, Tr, Badge, Button, Input, Select, Pagination, Spinner, Alert, Modal } from '../../../shared/components/ui'
import { DownloadIcon, SearchIcon, RestoreIcon, TrashIcon } from '../../../shared/components/Icons'

function MergeIcon({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth={1.75}>
      <path d="M4 6h8M4 10h5m-5 4h3" strokeLinecap="round" />
      <path d="M13 8l3 3-3 3" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  )
}

const statusColor: Record<string, 'yellow' | 'green' | 'purple' | 'gray'> = {
  DRAFT: 'yellow', SUBMITTED: 'green', ARCHIVED: 'purple',
}

type Tab = 'active' | 'deleted'

export default function AdminDocuments() {
  const { accessToken } = useSelector((s: RootState) => s.auth)
  const [tab, setTab] = useState<Tab>('active')
  const [page, setPage] = useState(1)
  const [filters, setFilters] = useState({ departmentId: '', financialYearId: '', documentTypeId: '', searchTerm: '', uploaderName: '' })
  const [merging, setMerging] = useState(false)
  const [mergeError, setMergeError] = useState('')
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set())
  const [purgeConfirm, setPurgeConfirm] = useState(false)
  const [purgeError, setPurgeError] = useState('')

  // Reset page and selection on tab or filter change
  useEffect(() => { setPage(1); setSelectedIds(new Set()) }, [tab, filters])

  const { data: deptsRes } = useGetDepartmentsQuery()
  const { data: typesRes } = useGetDocumentTypesQuery()
  const { data: fysRes }   = useGetFinancialYearsQuery()

  const activeFilters = Object.fromEntries(Object.entries(filters).filter(([, v]) => v !== ''))
  const queryParams = tab === 'deleted'
    ? { page, pageSize: 20, onlyDeleted: true, ...activeFilters }
    : { page, pageSize: 20, ...activeFilters }

  const { data, isLoading, error } = useAdminSearchDocumentsQuery(queryParams)
  const [adminRestore] = useAdminRestoreDocumentMutation()
  const [purgeDeleted, { isLoading: purging }] = useAdminPurgeDeletedMutation()

  const documents = data?.data?.items ?? []
  const total     = data?.data?.totalCount ?? 0
  const depts     = deptsRes?.data ?? []
  const types     = typesRes?.data ?? []
  const fys       = fysRes?.data ?? []

  const setFilter = (key: string) => (e: { target: { value: string } }) =>
    setFilters(f => ({ ...f, [key]: e.target.value }))

  const allPageSelected = documents.length > 0 && documents.every((d: any) => selectedIds.has(d.id))
  const somePageSelected = documents.some((d: any) => selectedIds.has(d.id))

  const toggleAll = () => {
    if (allPageSelected) setSelectedIds(new Set())
    else setSelectedIds(new Set(documents.map((d: any) => d.id)))
  }

  const toggleOne = (id: string) => {
    setSelectedIds(prev => {
      const next = new Set(prev)
      next.has(id) ? next.delete(id) : next.add(id)
      return next
    })
  }

  const handleDownload = (id: string, name: string) => {
    fetch(`/api/v1/admin/documents/${id}/download`, { headers: { Authorization: `Bearer ${accessToken}` } })
      .then(r => r.blob())
      .then(b => { const url = URL.createObjectURL(b); const a = document.createElement('a'); a.href = url; a.download = name; a.click() })
  }

  const handleMergePdf = async () => {
    setMerging(true)
    setMergeError('')
    try {
      const body: Record<string, unknown> = {}
      if (selectedIds.size > 0) {
        body.documentIds = [...selectedIds]
      } else {
        if (filters.searchTerm)      body.searchTerm      = filters.searchTerm
        if (filters.uploaderName)    body.uploaderName    = filters.uploaderName
        if (filters.departmentId)    body.departmentId    = Number(filters.departmentId)
        if (filters.financialYearId) body.financialYearId = Number(filters.financialYearId)
        if (filters.documentTypeId)  body.documentTypeId  = Number(filters.documentTypeId)
      }

      const res = await fetch('/api/v1/admin/documents/merge-pdf', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${accessToken}` },
        body: JSON.stringify(body),
      })
      if (!res.ok) {
        const text = await res.text().catch(() => '')
        let msg = 'Failed to generate merged PDF.'
        try {
          const err = JSON.parse(text)
          msg = err.error ?? err.title ?? err.detail ?? (text || msg)
          if (err.detail) msg += ' — ' + err.detail
        } catch { if (text) msg = text }
        setMergeError(msg)
        return
      }
      const blob = await res.blob()
      const disposition = res.headers.get('Content-Disposition') ?? ''
      const match = disposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/)
      const fileName = match ? match[1].replace(/['"]/g, '') : 'merged-documents.pdf'
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url; a.download = fileName; a.click()
      URL.revokeObjectURL(url)
    } finally {
      setMerging(false)
    }
  }

  return (
    <div className="space-y-5">
      <div className="flex items-start justify-between gap-4 flex-wrap">
        <div>
          <h2 className="text-xl font-bold text-gray-900">Documents</h2>
          <p className="text-sm text-gray-500 mt-0.5">Search and manage documents across all departments</p>
        </div>
        {tab === 'active' && total > 0 && (
          <Button
            variant="primary"
            icon={merging ? <Spinner label="" /> : <MergeIcon className="w-4 h-4" />}
            onClick={handleMergePdf}
            disabled={merging}
          >
            {merging
              ? 'Generating PDF…'
              : selectedIds.size > 0
              ? `Download Selected as PDF (${selectedIds.size})`
              : `Download All as PDF (${total})`}
          </Button>
        )}
        {tab === 'deleted' && total > 0 && (
          <Button
            variant="danger"
            icon={<TrashIcon className="w-4 h-4" />}
            onClick={() => setPurgeConfirm(true)}
            disabled={purging}
          >
            Delete All Permanently ({total})
          </Button>
        )}
      </div>
      {mergeError && <Alert type="error">{mergeError}</Alert>}

      {/* Tab switcher */}
      <div className="flex gap-1 border-b border-gray-200">
        {(['active', 'deleted'] as Tab[]).map(t => (
          <button
            key={t}
            onClick={() => setTab(t)}
            className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors ${
              tab === t
                ? 'border-violet-600 text-violet-700'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
            }`}
          >
            {t === 'active' ? 'All Documents' : 'Deleted Docs'}
            {tab === t && total > 0 && (
              <span className={`ml-2 text-xs px-1.5 py-0.5 rounded-full font-semibold ${
                t === 'deleted' ? 'bg-red-100 text-red-700' : 'bg-violet-100 text-violet-700'
              }`}>{total}</span>
            )}
          </button>
        ))}
      </div>

      {/* Filters */}
      <Card className="p-4">
        <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-5 gap-3">
          <Input placeholder="Search title…" value={filters.searchTerm} onChange={setFilter('searchTerm')}
            icon={<SearchIcon className="w-4 h-4" />} />
          <Input placeholder="Search uploader…" value={filters.uploaderName} onChange={setFilter('uploaderName')}
            icon={<SearchIcon className="w-4 h-4" />} />
          <Select value={filters.departmentId} onChange={setFilter('departmentId')} placeholder="All Departments">
            {depts.map((d: any) => <option key={d.id} value={d.id}>{d.name}</option>)}
          </Select>
          <Select value={filters.financialYearId} onChange={setFilter('financialYearId')} placeholder="All Financial Years">
            {fys.map((f: any) => <option key={f.id} value={f.id}>{f.label}</option>)}
          </Select>
          <Select value={filters.documentTypeId} onChange={setFilter('documentTypeId')} placeholder="All Document Types">
            {types.map((t: any) => <option key={t.id} value={t.id}>{t.name}</option>)}
          </Select>
        </div>
      </Card>

      {isLoading && <Spinner label="Loading documents…" />}
      {error && <Alert type="error">Failed to load documents.</Alert>}

      {!isLoading && (
        <Card>
          <Table>
            <thead>
              <tr>
                {tab === 'active' && (
                  <Th>
                    <input
                      type="checkbox"
                      checked={allPageSelected}
                      ref={el => { if (el) el.indeterminate = somePageSelected && !allPageSelected }}
                      onChange={toggleAll}
                      className="w-4 h-4 rounded border-gray-300 text-violet-600 focus:ring-violet-500 cursor-pointer"
                    />
                  </Th>
                )}
                <Th>Title</Th>
                <Th>Uploader</Th>
                <Th>Department</Th>
                <Th>Type</Th>
                <Th>FY</Th>
                <Th>Status</Th>
                <Th>{tab === 'deleted' ? 'Deleted On' : 'Uploaded'}</Th>
                <Th>Action</Th>
              </tr>
            </thead>
            <tbody>
              {documents.map((d: any) => (
                <Tr key={d.id} className={tab === 'active' && selectedIds.has(d.id) ? 'bg-violet-50' : ''}>
                  {tab === 'active' && (
                    <Td>
                      <input
                        type="checkbox"
                        checked={selectedIds.has(d.id)}
                        onChange={() => toggleOne(d.id)}
                        className="w-4 h-4 rounded border-gray-300 text-violet-600 focus:ring-violet-500 cursor-pointer"
                      />
                    </Td>
                  )}
                  <Td><span className="font-medium text-gray-900">{d.title}</span></Td>
                  <Td className="text-gray-500">{d.uploaderName}</Td>
                  <Td><Badge color="purple">{d.departmentName}</Badge></Td>
                  <Td className="text-gray-500">{d.documentTypeName}</Td>
                  <Td><Badge color="blue">{d.financialYearLabel}</Badge></Td>
                  <Td><Badge color={statusColor[d.status] ?? 'gray'}>{d.status}</Badge></Td>
                  <Td className="text-gray-500 text-xs">
                    {tab === 'deleted'
                      ? (d.deletedAt ? new Date(d.deletedAt).toLocaleDateString() : '—')
                      : new Date(d.uploadedAt).toLocaleDateString()}
                  </Td>
                  <Td>
                    {tab === 'deleted' ? (
                      <Button size="xs" variant="warning" icon={<RestoreIcon className="w-3.5 h-3.5" />}
                        onClick={() => adminRestore(d.id)}>
                        Restore
                      </Button>
                    ) : (
                      <Button size="xs" variant="ghost" icon={<DownloadIcon className="w-3.5 h-3.5" />}
                        onClick={() => handleDownload(d.id, d.originalFileName)}>
                        Download
                      </Button>
                    )}
                  </Td>
                </Tr>
              ))}
              {documents.length === 0 && (
                <tr>
                  <td colSpan={tab === 'active' ? 9 : 8} className="px-4 py-10 text-center text-sm text-gray-400">
                    {tab === 'deleted' ? 'No deleted documents.' : 'No documents found.'}
                  </td>
                </tr>
              )}
            </tbody>
          </Table>
          <Pagination page={page} total={total} pageSize={20} onChange={setPage} />
        </Card>
      )}

      <Modal open={purgeConfirm} onClose={() => { setPurgeConfirm(false); setPurgeError('') }} title="Permanently Delete All?">
        <p className="text-sm text-gray-600 mb-4">
          This will permanently delete <strong>{total}</strong> document{total !== 1 ? 's' : ''} from storage.
          This action <strong>cannot be undone</strong>.
        </p>
        {purgeError && <Alert type="error">{purgeError}</Alert>}
        <div className="flex gap-3 justify-end">
          <Button variant="ghost" onClick={() => setPurgeConfirm(false)}>Cancel</Button>
          <Button
            variant="danger"
            disabled={purging}
            icon={purging ? <Spinner label="" /> : <TrashIcon className="w-4 h-4" />}
            onClick={async () => {
              const result = await purgeDeleted()
              if ('error' in result) {
                setPurgeError('Failed to delete documents. Please try again.')
              } else {
                setPurgeConfirm(false)
                setPurgeError('')
              }
            }}
          >
            {purging ? 'Deleting…' : 'Delete All Permanently'}
          </Button>
        </div>
      </Modal>
    </div>
  )
}
