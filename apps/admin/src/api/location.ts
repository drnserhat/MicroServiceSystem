import { apiRequest } from "./client";
import type { Country, PagedResult } from "./types";

export function listCountries(pageNumber = 1, pageSize = 20): Promise<PagedResult<Country>> {
  const query = new URLSearchParams({
    pageNumber: String(pageNumber),
    pageSize: String(pageSize),
  });

  return apiRequest<PagedResult<Country>>(`/location/api/v1/countries?${query.toString()}`);
}

export function getCountry(code: string): Promise<Country> {
  return apiRequest<Country>(`/location/api/v1/countries/${encodeURIComponent(code)}`);
}

export function createCountry(code: string, name: string): Promise<Country> {
  return apiRequest<Country>("/location/api/v1/countries", {
    method: "POST",
    body: { code, name },
  });
}

export function updateCountry(code: string, name: string, ifMatch: number): Promise<Country> {
  return apiRequest<Country>(`/location/api/v1/countries/${encodeURIComponent(code)}`, {
    method: "PUT",
    body: { name },
    ifMatch,
  });
}

export function deleteCountry(code: string, ifMatch: number): Promise<null> {
  return apiRequest<null>(`/location/api/v1/countries/${encodeURIComponent(code)}`, {
    method: "DELETE",
    ifMatch,
  });
}
