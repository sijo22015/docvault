import { apiSlice } from '../../app/apiSlice'

export const documentsApi = apiSlice.injectEndpoints({
  endpoints: (builder) => ({
    getMyDocuments: builder.query<any, { page?: number; pageSize?: number; financialYearId?: number; documentTypeId?: number; searchTerm?: string }>({
      query: ({ page = 1, pageSize = 20, financialYearId, documentTypeId, searchTerm } = {}) => {
        const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
        if (financialYearId) params.append('financialYearId', String(financialYearId))
        if (documentTypeId)  params.append('documentTypeId',  String(documentTypeId))
        if (searchTerm)      params.append('searchTerm',      searchTerm)
        return { url: `/documents?${params}` }
      },
      providesTags: ['Document'],
    }),
    uploadDocument: builder.mutation<any, FormData>({
      query: (formData) => ({ url: '/documents', method: 'POST', body: formData }),
      invalidatesTags: ['Document'],
    }),
    deleteDocument: builder.mutation<any, string>({
      query: (id) => ({ url: `/documents/${id}`, method: 'DELETE' }),
      invalidatesTags: ['Document'],
    }),
    restoreDocument: builder.mutation<any, string>({
      query: (id) => ({ url: `/documents/${id}/restore`, method: 'POST' }),
      invalidatesTags: ['Document'],
    }),
    adminSearchDocuments: builder.query<any, Record<string, any>>({
      query: (params) => ({ url: '/admin/documents', params }),
      providesTags: ['Document'],
    }),
    adminRestoreDocument: builder.mutation<any, string>({
      query: (id) => ({ url: `/admin/documents/${id}/restore`, method: 'POST' }),
      invalidatesTags: ['Document'],
    }),
    adminPurgeDeleted: builder.mutation<any, void>({
      query: () => ({ url: '/admin/documents/deleted', method: 'DELETE' }),
      invalidatesTags: ['Document'],
    }),
    verifyIntegrity: builder.query<any, number>({
      query: (fyId) => `/admin/documents/verify-integrity?fyId=${fyId}`,
    }),
    lockFY: builder.mutation<any, number>({
      query: (id) => ({ url: `/admin/financial-years/${id}/lock`, method: 'POST' }),
    }),
    exportFY: builder.query<any, number>({
      query: (fyId) => ({ url: `/admin/export/fy/${fyId}` }),
    }),
  }),
})

export const {
  useGetMyDocumentsQuery,
  useUploadDocumentMutation,
  useDeleteDocumentMutation,
  useRestoreDocumentMutation,
  useAdminSearchDocumentsQuery,
  useAdminRestoreDocumentMutation,
  useAdminPurgeDeletedMutation,
} = documentsApi
