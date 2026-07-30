import { apiRequest } from "./client";
import type { FileAsset } from "./types";

export function uploadFile(file: File, container: string): Promise<FileAsset> {
  const formData = new FormData();
  formData.append("file", file);
  formData.append("container", container);

  return apiRequest<FileAsset>("/file/api/v1/files/upload", {
    method: "POST",
    formData,
  });
}
