import { useEffect, useMemo, useState } from "react";
import { ApiClientError } from "@/api/client";
import { listIdentityUsers } from "@/api/identityAdmin";
import type { IdentityUserItem } from "@/api/types";

type UserPickerProps = {
  value: string;
  onChange: (user: IdentityUserItem | null) => void;
  label?: string;
  required?: boolean;
  activeOnly?: boolean;
};

export function UserPicker({
  value,
  onChange,
  label = "User",
  required = true,
  activeOnly = true,
}: UserPickerProps) {
  const [users, setUsers] = useState<IdentityUserItem[]>([]);
  const [search, setSearch] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    async function load() {
      setLoading(true);
      setError(null);
      try {
        const data = await listIdentityUsers(1, 100, search);
        if (!cancelled) {
          setUsers([...data.items]);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof ApiClientError ? err.message : "Failed to load users.");
          setUsers([]);
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    }

    const handle = window.setTimeout(() => void load(), 250);
    return () => {
      cancelled = true;
      window.clearTimeout(handle);
    };
  }, [search]);

  const options = useMemo(
    () => (activeOnly ? users.filter((user) => user.isActive) : users),
    [users, activeOnly],
  );

  return (
    <div>
      <label className="form-label">{label}</label>
      <input
        className="form-control mb-2"
        placeholder="Search by email or username"
        value={search}
        onChange={(e) => setSearch(e.target.value)}
      />
      <select
        className="form-select"
        value={value}
        required={required}
        disabled={loading || options.length === 0}
        onChange={(e) => {
          const selected = options.find((user) => user.id === e.target.value) ?? null;
          onChange(selected);
        }}
      >
        <option value="">{loading ? "Loading users…" : "Select a user"}</option>
        {options.map((user) => (
          <option key={user.id} value={user.id}>
            {user.email} ({user.userName})
          </option>
        ))}
      </select>
      {error ? <div className="text-danger small mt-1">{error}</div> : null}
      {!loading && !error && options.length === 0 ? (
        <div className="text-secondary small mt-1">No users found. Register one first.</div>
      ) : null}
    </div>
  );
}
