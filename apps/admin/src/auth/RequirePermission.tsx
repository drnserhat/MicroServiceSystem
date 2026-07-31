import { ForbiddenState } from "@/components/control";
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
    return <ForbiddenState permission={permission} />;
  }

  return children;
}
