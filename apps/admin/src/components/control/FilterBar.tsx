import { useMemo, useState } from "react";

export type FilterChip = {
  id: string;
  label: string;
};

export function FilterBar({
  search,
  onSearchChange,
  searchPlaceholder = "Search…",
  chips,
  activeChipId,
  onChipChange,
  trailing,
}: {
  search: string;
  onSearchChange: (value: string) => void;
  searchPlaceholder?: string;
  chips?: FilterChip[];
  activeChipId?: string;
  onChipChange?: (id: string) => void;
  trailing?: React.ReactNode;
}) {
  return (
    <div className="card mb-3">
      <div className="card-body py-3">
        <div className="row g-2 align-items-center">
          <div className="col-md-5">
            <input
              className="form-control"
              value={search}
              onChange={(e) => onSearchChange(e.target.value)}
              placeholder={searchPlaceholder}
              aria-label="Search"
            />
          </div>
          {chips && chips.length > 0 ? (
            <div className="col-md-5">
              <div className="btn-list">
                {chips.map((chip) => (
                  <button
                    key={chip.id}
                    type="button"
                    className={activeChipId === chip.id ? "btn btn-primary btn-sm" : "btn btn-sm"}
                    onClick={() => onChipChange?.(chip.id)}
                  >
                    {chip.label}
                  </button>
                ))}
              </div>
            </div>
          ) : null}
          {trailing ? <div className="col-md-2 text-md-end">{trailing}</div> : null}
        </div>
      </div>
    </div>
  );
}

const PINS_KEY = "msf.admin.pinnedServices";

export function loadPinnedServices(): string[] {
  try {
    const raw = localStorage.getItem(PINS_KEY);
    return raw ? (JSON.parse(raw) as string[]) : [];
  } catch {
    return [];
  }
}

export function savePinnedServices(ids: string[]) {
  localStorage.setItem(PINS_KEY, JSON.stringify(ids.slice(0, 24)));
}

export function usePinnedServices() {
  const [pins, setPins] = useState<string[]>(() => loadPinnedServices());

  function togglePin(id: string) {
    setPins((prev) => {
      const next = prev.includes(id) ? prev.filter((x) => x !== id) : [id, ...prev].slice(0, 24);
      savePinnedServices(next);
      return next;
    });
  }

  const pinnedSet = useMemo(() => new Set(pins), [pins]);
  return { pins, pinnedSet, togglePin };
}
