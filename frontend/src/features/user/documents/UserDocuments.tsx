import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useSelector } from 'react-redux'
import type { RootState } from '../../../app/store'
import { useGetMyDocumentsQuery, useDeleteDocumentMutation, useRestoreDocumentMutation } from '../../../shared/api/documentsApi'
import { Button, Badge, Card, Table, Th, Td, Tr, Pagination, Spinner, Alert, Modal } from '../../../shared/components/ui'
import { UploadIcon, DownloadIcon, TrashIcon, RestoreIcon } from '../../../shared/components/Icons'

const statusMeta: Record<string, { color: 'yellow' | 'green' | 'purple' | 'gray'; label: string }> = {
  DRAFT:     { color: 'yellow', label: 'Draft'     },
  SUBMITTED: { color: 'green',  label: 'Submitted' },
  ARCHIVED:  { color: 'purple', label: 'Archived'  },
}

export default function UserDocuments() {
  const navigate = useNavigate()
  const { accessToken } = useSelector((s: RootState) => s.auth)
  const [page, setPage]       = useState(1)
  const [deleteId, setDeleteId] = useState<string | null>(null)

  const { data, isLoading, error } = useGetMyDocumentsQuery({ page, pageSize: 20 })
  const [deleteDoc]   = useDeleteDocumentMutation()
  const [restoreDoc]  = useRestoreDocumentMutation()

  const documents = data?.data?.items ?? []
  const total     = data?.data?.totalCount ?? 0

  const handleDelete = async () => {
    if (deleteId) { await deleteDoc(deleteId); setDeleteId(null) }
  }

  const handleDownload = (id: string, name: string) => {
    fetch(`/api/v1/documents/${id}/download`, { headers: { Authorization: `Bearer ${accessToken}` } })
      .then(r => r.blob())
      .then(b => { const url = URL.createObjectURL(b); const a = document.createElement('a'); a.href = url; a.download = name; a.click() })
  }

  if (isLoading) return <Spinner label="Loading documents…" />
  if (error) return <Alert type="error">Failed to load documents.</Alert>

  return (
    <div className="space-y-5">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-bold text-gray-900">My Documents</h2>
          <p className="text-sm text-gray-500 mt-0.5">{total} document{total !== 1 ? 's' : ''} in your account</p>
        </div>
        <Button icon={<UploadIcon className="w-4 h-4" />} onClick={() => navigate('/documents/upload')}>
          Upload New
        </Button>
      </div>

      {documents.length === 0 ? (
        <Card className="p-12 text-center">
          <div className="w-16 h-16 mx-auto mb-4 rounded-2xl bg-gradient-to-br from-sky-100 to-blue-100 flex items-center justify-center">
            <UploadIcon className="w-8 h-8 text-sky-500" />
          </div>
          <h3 className="font-semibold text-gray-900 mb-1">No documents yet</h3>
          <p className="text-sm text-gray-500 mb-4">Upload your first document to get started</p>
          <Button onClick={() => navigate('/documents/upload')}>Upload Document</Button>
        </Card>
      ) : (
        <Card>
          <Table>
            <thead>
              <tr>
                <Th>Title</Th>
                <Th>Type</Th>
                <Th>Financial Year</Th>
                <Th>Size</Th>
                <Th>Status</Th>
                <Th>Uploaded</Th>
                <Th>Actions</Th>
              </tr>
            </thead>
            <tbody>
              {documents.map((d: any) => {
                const meta = statusMeta[d.status] ?? { color: 'gray', label: d.status }
                return (
                  <Tr key={d.id} className={d.isDeleted ? 'opacity-50' : ''}>
                    <Td>
                      <div className="flex items-center gap-2.5">
                        <div className="w-7 h-7 rounded-lg bg-gradient-to-br from-sky-400 to-blue-500 flex items-center justify-center flex-shrink-0">
                          <span className="text-white text-xs font-bold">
                            {d.originalFileName?.split('.').pop()?.toUpperCase()?.slice(0, 3) ?? 'DOC'}
                          </span>
                        </div>
                        <span className="font-medium text-gray-900 max-w-40 truncate">{d.title}</span>
                      </div>
                    </Td>
                    <Td className="text-gray-500">{d.documentTypeName}</Td>
                    <Td><Badge color="blue">{d.financialYearLabel}</Badge></Td>
                    <Td className="text-gray-500 text-xs">{(d.fileSizeBytes / 1024).toFixed(1)} KB</Td>
                    <Td><Badge color={meta.color}>{meta.label}</Badge></Td>
                    <Td className="text-gray-500 text-xs">{new Date(d.uploadedAt).toLocaleDateString()}</Td>
                    <Td>
                      <div className="flex gap-1.5">
                        {!d.isDeleted && (
                          <>
                            <Button size="xs" variant="ghost" icon={<DownloadIcon className="w-3.5 h-3.5" />}
                              onClick={() => handleDownload(d.id, d.originalFileName)} />
                            <Button size="xs" variant="danger" icon={<TrashIcon className="w-3.5 h-3.5" />}
                              onClick={() => setDeleteId(d.id)} />
                          </>
                        )}
                        {d.isDeleted && (
                          <Button size="xs" variant="warning" icon={<RestoreIcon className="w-3.5 h-3.5" />}
                            onClick={() => restoreDoc(d.id)}>
                            Restore
                          </Button>
                        )}
                      </div>
                    </Td>
                  </Tr>
                )
              })}
            </tbody>
          </Table>
          <Pagination page={page} total={total} pageSize={20} onChange={setPage} />
        </Card>
      )}

      <Modal open={!!deleteId} onClose={() => setDeleteId(null)} title="Delete Document?">
        <p className="text-sm text-gray-600 mb-6">
          This document will be soft-deleted. You can restore it within 30 days.
        </p>
        <div className="flex gap-3 justify-end">
          <Button variant="ghost" onClick={() => setDeleteId(null)}>Cancel</Button>
          <Button variant="danger" onClick={handleDelete}>Delete</Button>
        </div>
      </Modal>
    </div>
  )
}
