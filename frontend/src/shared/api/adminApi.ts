import { apiSlice } from '../../app/apiSlice'

export const adminApi = apiSlice.injectEndpoints({
  endpoints: (builder) => ({
    getUsers: builder.query<any, { status?: string; page?: number; pageSize?: number }>({
      query: ({ status, page = 1, pageSize = 20 } = {}) => ({
        url: `/admin/users?page=${page}&pageSize=${pageSize}${status ? `&status=${status}` : ''}`,
      }),
      providesTags: ['User'],
    }),
    approveUser: builder.mutation<any, string>({
      query: (id) => ({ url: `/admin/users/${id}/approve`, method: 'POST' }),
      invalidatesTags: ['User'],
    }),
    revokeUser: builder.mutation<any, string>({
      query: (id) => ({ url: `/admin/users/${id}/revoke`, method: 'POST' }),
      invalidatesTags: ['User'],
    }),
    getDashboardSummary: builder.query<any, void>({
      query: () => '/admin/dashboard/summary',
    }),
    getAnalytics: builder.query<any, string>({
      query: (fy) => `/admin/dashboard/analytics?fy=${fy}`,
    }),
  }),
})

export const {
  useGetUsersQuery,
  useApproveUserMutation,
  useRevokeUserMutation,
  useGetDashboardSummaryQuery,
  useGetAnalyticsQuery,
} = adminApi
