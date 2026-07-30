import { apiRequest } from "./client";

export function createNotification(body: {
  userId: string;
  email: string;
  displayName: string;
  channel: string;
}): Promise<null> {
  return apiRequest<null>("/notification/api/v1/notifications", {
    method: "POST",
    body,
  });
}
