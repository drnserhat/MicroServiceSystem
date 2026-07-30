import { Navigate } from "react-router-dom";
import { useAuth } from "./AuthContext";

export function RequirePermission({
  permission,
  children,
}: {
  permission: string | string[];
  children: React.ReactNode;
}) {
  const { can } = useAuth();

  if (!can(permission)) {
    return <Navigate to="/" replace />;
  }

  return children;
}
