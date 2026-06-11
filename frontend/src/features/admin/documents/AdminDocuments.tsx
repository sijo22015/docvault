import { useState, useEffect } from 'react'
import { useSelector } from 'react-redux'
import type { RootState } from '../../../app/store'
import { useAdminSearchDocumentsQuery } from '../../../shared/api/documentsApi'
import { useGetDepartmentsQuery, useGetDocumentTypesQuery, useGetFinancialYearsQuery } from '../../../shared/api/referenceApi'
import { Card, Table, Th, Td, Tr, Badge, Button, Input, Select, Pagination, Spinner, Alert } from '../../../shared/components/ui'
import { DownloadIcon, SearchIcon } from '../../../shared/components/Icons'

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

export default function AdminDocuments() {
  const { accessToken } = useSelector((s: RootState) => s.auth)
  const [page, setPage] = useState(1)
  const [filters, setFilters] = useState({ departmentId: '', financialYearId: '', documentTypeId: '', searchTerm: '' })
  const [merging, setMerging] = useState(false)
  const [mergeError, setMergeError] = useState('')
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set())

  // Clear selection whenever filters or page change
  useEffect(() => { setSelectedIds(new Set()) }, [filters, page])

  const { data: deptsRes } = useGetDepartmentsQuery()
  const { data: typesRes } = useGetDocumentTypesQuery()
  const { data: fysRes }   = useGetFinancialYearsQuery()

  const activeFilters = Object.fromEntries(Object.entries(filters).filter(([, v]) => v !== ''))
  const { data, isLoading, error } = useAdminSearchDocumentsQuery({ page, pageSize: 20, ...activeFilters })

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
    if (allPageSelected) {
      setSelectedIds(new Set())
    } else {
      setSelectedIds(new Set(documents.map((d: any) => d.id)))
    }
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
          <h2 className="text-xl font-bold text-gray-900">All Documents</h2>
          <p className="text-sm text-gray-500 mt-0.5">Search and download documents across all departments</p>
        </div>
        {total > 0 && (
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
      </div>
      {mergeError && <Alert type="error">{mergeError}</Alert>}

      {/* Filters */}
      <Card className="p-4">
        <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-3">
          <Input placeholder="Search title…" value={filters.searchTerm} onChange={setFilter('searchTerm')}
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
                <Th>
                  <input
                    type="checkbox"
                    checked={allPageSelected}
                    ref={el => { if (el) el.indeterminate = somePageSelected && !allPageSelected }}
                    onChange={toggleAll}
                    className="w-4 h-4 rounded border-gray-300 text-violet-600 focus:ring-violet-500 cursor-pointer"
                  />
                </Th>
                <Th>Title</Th>
                <Th>Uploader</Th>
                <Th>Department</Th>
                <Th>Type</Th>
                <Th>FY</Th>
                <Th>Status</Th>
                <Th>Uploaded</Th>
                <Th>Action</Th>
              </tr>
            </thead>
            <tbody>
              {documents.map((d: any) => (
                <Tr key={d.id} className={selectedIds.has(d.id) ? 'bg-violet-50' : ''}>
                  <Td>
                    <input
                      type="checkbox"
                      checked={selectedIds.has(d.id)}
                      onChange={() => toggleOne(d.id)}
                      className="w-4 h-4 rounded border-gray-300 text-violet-600 focus:ring-violet-500 cursor-pointer"
                    />
                  </Td>
                  <Td><span className="font-medium text-gray-900">{d.title}</span></Td>
                  <Td className="text-gray-500">{d.uploaderName}</Td>
                  <Td><Badge color="purple">{d.departmentName}</Badge></Td>
                  <Td className="text-gray-500">{d.documentTypeName}</Td>
                  <Td><Badge color="blue">{d.financialYearLabel}</Badge></Td>
                  <Td><Badge color={statusColor[d.status] ?? 'gray'}>{d.status}</Badge></Td>
                  <Td className="text-gray-500 text-xs">{new Date(d.uploadedAt).toLocaleDateString()}</Td>
                  <Td>
                    <Button size="xs" variant="ghost" icon={<DownloadIcon className="w-3.5 h-3.5" />}
                      onClick={() => handleDownload(d.id, d.originalFileName)}>
                      Download
                    </Button>
                  </Td>
                </Tr>
              ))}
              {documents.length === 0 && (
                <tr><td colSpan={9} className="px-4 py-10 text-center text-sm text-gray-400">No documents found.</td></tr>
              )}
            </tbody>
          </Table>
          <Pagination page={page} total={total} pageSize={20} onChange={setPage} />
        </Card>
      )}
    </div>
  )
}
