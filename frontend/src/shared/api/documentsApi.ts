import { apiSlice } from '../../app/apiSlice'

export const documentsApi = apiSlice.injectEndpoints({
  endpoints: (builder) => ({
    getMyDocuments: builder.query<any, { page?: number; pageSize?: number; fyId?: number }>({
      query: ({ page = 1, pageSize = 20, fyId } = {}) => ({
        url: `/documents?page=${page}&pageSize=${pageSize}${fyId ? `&fyId=${fyId}` : ''}`,
      }),
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
} = documentsApi
