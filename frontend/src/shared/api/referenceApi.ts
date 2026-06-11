import { apiSlice } from '../../app/apiSlice'

export const referenceApi = apiSlice.injectEndpoints({
  endpoints: (builder) => ({
    getDepartments: builder.query<any, void>({
      query: () => '/reference/departments',
    }),
    getDocumentTypes: builder.query<any, void>({
      query: () => '/reference/document-types',
    }),
    getFinancialYears: builder.query<any, void>({
      query: () => '/reference/financial-years',
    }),
  }),
})

export const { useGetDepartmentsQuery, useGetDocumentTypesQuery, useGetFinancialYearsQuery } = referenceApi
