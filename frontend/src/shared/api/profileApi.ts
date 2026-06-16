import { apiSlice } from '../../app/apiSlice'

export const profileApi = apiSlice.injectEndpoints({
  endpoints: (builder) => ({
    getMyProfile: builder.query<any, void>({
      query: () => '/me',
      providesTags: ['Profile'],
    }),
    updateMyProfile: builder.mutation<any, { MobileNumber?: string; WhatsAppNumber?: string }>({
      query: (body) => ({ url: '/me', method: 'PUT', body }),
      invalidatesTags: ['Profile'],
    }),
    uploadProfilePhoto: builder.mutation<any, FormData>({
      query: (formData) => ({ url: '/me/photo', method: 'POST', body: formData }),
      invalidatesTags: ['Profile'],
    }),
  }),
})

export const { useGetMyProfileQuery, useUpdateMyProfileMutation, useUploadProfilePhotoMutation } = profileApi
