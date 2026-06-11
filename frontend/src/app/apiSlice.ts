import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react'
import type { BaseQueryFn, FetchArgs, FetchBaseQueryError } from '@reduxjs/toolkit/query'
import type { RootState } from './store'
import { clearCredentials, setCredentials } from '../features/auth/authSlice'

const rawBaseQuery = fetchBaseQuery({
  baseUrl: '/api/v1',
  prepareHeaders: (headers, { getState }) => {
    const token = (getState() as RootState).auth.accessToken
    if (token) headers.set('authorization', `Bearer ${token}`)
    return headers
  },
})

// Shared promise so concurrent 401s all wait on the same single refresh call.
// Without this, multiple simultaneous requests would each try to refresh the
// token independently — the second one would get "Refresh token revoked" because
// the first already rotated it.
let refreshLock: Promise<boolean> | null = null

const baseQuery: BaseQueryFn<string | FetchArgs, unknown, FetchBaseQueryError> = async (
  args,
  api,
  extraOptions,
) => {
  let result = await rawBaseQuery(args, api, extraOptions)

  if (result.error?.status === 401) {
    if (!refreshLock) {
      refreshLock = (async (): Promise<boolean> => {
        const rt = (api.getState() as RootState).auth.refreshToken
        if (!rt) return false

        const refreshResult = await rawBaseQuery(
          { url: '/auth/refresh', method: 'POST', body: { refreshToken: rt } },
          api,
          extraOptions,
        )

        if (refreshResult.data) {
          const d = (refreshResult.data as any).data
          api.dispatch(setCredentials({
            accessToken: d.accessToken,
            refreshToken: d.refreshToken,
            userId: d.userId,
            email: d.email,
            fullName: d.fullName,
            role: d.role,
          }))
          return true
        }

        api.dispatch(clearCredentials())
        return false
      })().finally(() => { refreshLock = null })
    }

    const refreshed = await refreshLock
    if (refreshed) {
      result = await rawBaseQuery(args, api, extraOptions)
    } else {
      window.location.href = '/login'
    }
  }

  return result
}

export const apiSlice = createApi({
  reducerPath: 'api',
  baseQuery,
  tagTypes: ['Document', 'User', 'Notification'],
  endpoints: () => ({}),
})
