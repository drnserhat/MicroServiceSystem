import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { PageFrame, PreviewBanner, StatusBadge } from "@/components/control";
import { BUILDING_BLOCKS, findBlock } from "./catalog";

export function BuildingBlocksPage() {
  const { t } = useTranslation(["architecture", "platform"]);
  const { blockId } = useParams<{ blockId?: string }>();
  const fallbackId = BUILDING_BLOCKS[0]!.id;
  const [selectedId, setSelectedId] = useState(
    () => (blockId && findBlock(blockId) ? blockId : fallbackId),
  );

  useEffect(() => {
    if (blockId && findBlock(blockId)) setSelectedId(blockId);
  }, [blockId]);

  const selected = useMemo(
    () => findBlock(selectedId) ?? BUILDING_BLOCKS[0]!,
    [selectedId],
  );

  return (
    <PageFrame
      pretitle={t("hubPretitle")}
      title={t("buildingBlocksTitle")}
      description={t("buildingBlocksDescription")}
      actions={
        <div className="btn-list">
          <Link className="btn" to="/architecture">
            {t("hubTitle")}
          </Link>
          <Link className="btn" to="/developer/building-block">
            {t("wizard")}
          </Link>
        </div>
      }
    >
      <PreviewBanner>{t("buildingBlocksBanner")}</PreviewBanner>

      <div className="row">
        <div className="col-md-4">
          <div
            className="list-group list-group-transparent mb-3"
            style={{ maxHeight: 520, overflow: "auto" }}
          >
            {BUILDING_BLOCKS.map((block) => (
              <Link
                key={block.id}
                to={`/building-blocks/${block.id}`}
                className={`list-group-item list-group-item-action ${selectedId === block.id ? "active" : ""}`}
                onClick={() => setSelectedId(block.id)}
              >
                {block.name}
              </Link>
            ))}
          </div>
        </div>
        <div className="col-md-8">
          <div className="card">
            <div className="card-header">
              <h3 className="card-title">{selected.name}</h3>
              <div className="card-actions">
                <StatusBadge tone="infra">{selected.version}</StatusBadge>
              </div>
            </div>
            <div className="card-body">
              <div className="subheader">Purpose</div>
              <p>{selected.purpose}</p>
              <div className="subheader">Dependencies</div>
              <div className="d-flex flex-wrap gap-1 mb-3">
                {selected.dependencies.length === 0 ? (
                  <span className="text-secondary">None</span>
                ) : (
                  selected.dependencies.map((item) => (
                    <span key={item} className="badge bg-secondary-lt">
                      {item}
                    </span>
                  ))
                )}
              </div>
              <div className="subheader">Used by</div>
              <div className="d-flex flex-wrap gap-1 mb-3">
                {selected.usedBy.map((item) => (
                  <span key={item} className="badge bg-blue-lt">
                    {item}
                  </span>
                ))}
              </div>
              {selected.docs ? (
                <Link className="btn btn-sm" to={selected.docs}>
                  Related docs / UI
                </Link>
              ) : null}
            </div>
          </div>
        </div>
      </div>
    </PageFrame>
  );
}
