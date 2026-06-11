import { createSlice, type PayloadAction } from '@reduxjs/toolkit'

interface AuthState {
  accessToken: string | null
  refreshToken: string | null
  userId: string | null
  email: string | null
  fullName: string | null
  role: string | null
}

const stored = localStorage.getItem('auth')
const initial: AuthState = stored ? JSON.parse(stored) : {
  accessToken: null, refreshToken: null, userId: null, email: null, fullName: null, role: null,
}

const authSlice = createSlice({
  name: 'auth',
  initialState: initial,
  reducers: {
    setCredentials(state, action: PayloadAction<AuthState>) {
      Object.assign(state, action.payload)
      localStorage.setItem('auth', JSON.stringify(action.payload))
    },
    clearCredentials(state) {
      state.accessToken = null
      state.refreshToken = null
      state.userId = null
      state.email = null
      state.fullName = null
      state.role = null
      localStorage.removeItem('auth')
    },
  },
})

export const { setCredentials, clearCredentials } = authSlice.actions
export default authSlice.reducer
