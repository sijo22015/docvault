import { apiSlice } from '../../app/apiSlice'

export const authApi = apiSlice.injectEndpoints({
  endpoints: (builder) => ({
    login: builder.mutation<any, { email: string; password: string }>({
      query: (body) => ({ url: '/auth/login', method: 'POST', body }),
    }),
    register: builder.mutation<any, { fullName: string; email: string; password: string; department: string }>({
      query: (body) => ({ url: '/auth/register', method: 'POST', body }),
    }),
    logout: builder.mutation<any, { refreshToken: string }>({
      query: (body) => ({ url: '/auth/logout', method: 'POST', body }),
    }),
    refresh: builder.mutation<any, { refreshToken: string }>({
      query: (body) => ({ url: '/auth/refresh', method: 'POST', body }),
    }),
  }),
})

export const { useLoginMutation, useRegisterMutation, useLogoutMutation, useRefreshMutation } = authApi
