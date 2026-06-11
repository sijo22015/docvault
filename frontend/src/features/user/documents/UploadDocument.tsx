import { useState, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { useDropzone } from 'react-dropzone'
import { useUploadDocumentMutation } from '../../../shared/api/documentsApi'
import { useGetDepartmentsQuery, useGetDocumentTypesQuery, useGetFinancialYearsQuery } from '../../../shared/api/referenceApi'
import { Button, Input, Select, Alert, Card } from '../../../shared/components/ui'
import { UploadIcon, FileIcon, XIcon } from '../../../shared/components/Icons'

export default function UploadDocument() {
  const navigate = useNavigate()
  const { data: deptsRes } = useGetDepartmentsQuery()
  const { data: typesRes } = useGetDocumentTypesQuery()
  const { data: fysRes   } = useGetFinancialYearsQuery()
  const [upload, { isLoading }] = useUploadDocumentMutation()

  const [form, setForm] = useState({ title: '', description: '', departmentId: '', financialYearId: '', documentTypeId: '' })
  const [file, setFile]       = useState<File | null>(null)
  const [error, setError]     = useState('')
  const [success, setSuccess] = useState(false)

  const departments    = deptsRes?.data ?? []
  const docTypes       = typesRes?.data ?? []
  const financialYears = fysRes?.data ?? []

  const onDrop = useCallback((accepted: File[]) => { if (accepted[0]) setFile(accepted[0]) }, [])
  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: {
      'application/pdf': ['.pdf'],
      'application/msword': ['.doc'],
      'application/vnd.openxmlformats-officedocument.wordprocessingml.document': ['.docx'],
      'text/plain': ['.txt'],
    },
    maxSize: 25 * 1024 * 1024,
    multiple: false,
  })

  const set = (key: string) => (e: { target: { value: string } }) =>
    setForm(f => ({ ...f, [key]: e.target.value }))

  const handleSubmit = async () => {
    if (!file) { setError('Please select a file.'); return }
    setError('')
    try {
      const fd = new FormData()
      fd.append('file', file)
      Object.entries(form).forEach(([k, v]) => fd.append(k, v))
      await upload(fd).unwrap()
      setSuccess(true)
      setTimeout(() => navigate('/documents'), 1500)
    } catch (err: any) {
      setError(err?.data?.error || 'Upload failed. Please try again.')
    }
  }

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <div>
        <h2 className="text-xl font-bold text-gray-900">Upload Document</h2>
        <p className="text-sm text-gray-500 mt-0.5">Add a new document to your department records</p>
      </div>

      {success && <Alert type="success">Document uploaded successfully! Redirecting…</Alert>}
      {error   && <Alert type="error">{error}</Alert>}

      {/* Dropzone */}
      <div {...getRootProps()}
        className={`border-2 border-dashed rounded-2xl p-10 text-center cursor-pointer transition-all duration-150 ${
          isDragActive
            ? 'border-sky-400 bg-sky-50 scale-[1.01]'
            : file
            ? 'border-emerald-400 bg-emerald-50'
            : 'border-gray-200 bg-gray-50 hover:border-sky-300 hover:bg-sky-50/50'
        }`}>
        <input {...getInputProps()} />
        {file ? (
          <div className="flex flex-col items-center gap-2">
            <div className="w-14 h-14 rounded-2xl bg-gradient-to-br from-emerald-400 to-teal-500 flex items-center justify-center">
              <FileIcon className="w-7 h-7 text-white" />
            </div>
            <p className="font-semibold text-gray-900">{file.name}</p>
            <p className="text-sm text-gray-500">{(file.size / 1024).toFixed(1)} KB</p>
            <button onClick={e => { e.stopPropagation(); setFile(null) }}
              className="flex items-center gap-1 text-xs text-rose-500 hover:text-rose-700 transition-colors mt-1">
              <XIcon className="w-3.5 h-3.5" /> Remove file
            </button>
          </div>
        ) : (
          <div className="flex flex-col items-center gap-3">
            <div className={`w-14 h-14 rounded-2xl flex items-center justify-center transition-colors ${
              isDragActive ? 'bg-sky-200' : 'bg-gray-200'
            }`}>
              <UploadIcon className={`w-7 h-7 ${isDragActive ? 'text-sky-600' : 'text-gray-500'}`} />
            </div>
            <div>
              <p className="font-semibold text-gray-700">
                {isDragActive ? 'Drop it here!' : 'Drag & drop or click to select'}
              </p>
              <p className="text-xs text-gray-400 mt-1">PDF, DOC, DOCX, TXT · Max 25 MB</p>
            </div>
          </div>
        )}
      </div>

      {/* Form fields */}
      <Card className="p-5 space-y-4">
        <Input label="Document Title" placeholder="e.g. Q1 Financial Report" value={form.title} onChange={set('title')} required />
        <div className="flex flex-col gap-1">
          <label className="text-sm font-medium text-gray-700">Description <span className="text-gray-400 font-normal">(optional)</span></label>
          <textarea
            rows={2}
            placeholder="Brief description of this document…"
            value={form.description}
            onChange={e => setForm(f => ({ ...f, description: e.target.value }))}
            className="w-full rounded-xl border border-gray-200 bg-gray-50 px-4 py-2.5 text-sm text-gray-900 placeholder-gray-400 transition focus:border-sky-400 focus:bg-white focus:outline-none focus:ring-2 focus:ring-sky-100 resize-none"
          />
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <Select label="Department" value={form.departmentId} onChange={set('departmentId')} placeholder="Select…" required>
            {departments.map((d: any) => <option key={d.id} value={d.id}>{d.name}</option>)}
          </Select>
          <Select label="Financial Year" value={form.financialYearId} onChange={set('financialYearId')} placeholder="Select…" required>
            {financialYears.map((f: any) => <option key={f.id} value={f.id}>{f.label}</option>)}
          </Select>
          <Select label="Document Type" value={form.documentTypeId} onChange={set('documentTypeId')} placeholder="Select…" required>
            {docTypes.map((t: any) => <option key={t.id} value={t.id}>{t.name}</option>)}
          </Select>
        </div>
      </Card>

      {/* Progress bar */}
      {isLoading && (
        <div className="h-1.5 bg-gray-100 rounded-full overflow-hidden">
          <div className="h-full bg-gradient-to-r from-sky-500 to-blue-600 animate-pulse rounded-full w-3/4" />
        </div>
      )}

      <div className="flex gap-3 justify-end">
        <Button variant="ghost" onClick={() => navigate('/documents')} disabled={isLoading}>Cancel</Button>
        <Button loading={isLoading} disabled={!file} onClick={handleSubmit}
          icon={<UploadIcon className="w-4 h-4" />}>
          Upload Document
        </Button>
      </div>
    </div>
  )
}
