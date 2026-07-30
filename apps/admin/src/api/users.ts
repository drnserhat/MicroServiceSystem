import { apiRequest } from "./client";
import type { RegistrationResult, UserProfile } from "./types";

export function registerUser(input: {
  email: string;
  userName: string;
  password: string;
  firstName: string;
  lastName: string;
  displayName?: string;
  tenantId: string;
}): Promise<RegistrationResult> {
  return apiRequest<RegistrationResult>("/registration", {
    method: "POST",
    body: input,
  });
}

export function getUserProfile(id: string): Promise<UserProfile> {
  return apiRequest<UserProfile>(`/user/api/v1/users/profiles/${id}`);
}

export function updateUserProfile(
  id: string,
  body: { firstName: string; lastName: string; displayName?: string },
  ifMatch: number,
): Promise<UserProfile> {
  return apiRequest<UserProfile>(`/user/api/v1/users/profiles/${id}`, {
    method: "PUT",
    body,
    ifMatch,
  });
}
