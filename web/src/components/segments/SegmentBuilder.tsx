import { useState, useEffect, useCallback, useRef } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { segments as api } from "../../api/client";
import type { SegmentRule, SegmentDetail } from "../../types";

/* ── Field/operator definitions ────────────────────── */

interface FieldDef {
  label: string;
  operators: { value: string; label: string }[];
  inputType: "text" | "number" | "date" | "select";
  options?: string[];
}

const FIELD_DEFS: Record<string, FieldDef> = {
  industry: {
    label: "Industry",
    operators: [
      { value: "equals", label: "equals" },
      { value: "not_equals", label: "does not equal" },
    ],
    inputType: "select",
    options: [
      "Technology",
      "Finance",
      "Healthcare",
      "Energy",
      "Retail",
      "Manufacturing",
      "Education",
      "Other",
    ],
  },
  company: {
    label: "Company",
    operators: [
      { value: "equals", label: "equals" },
      { value: "not_equals", label: "does not equal" },
      { value: "contains", label: "contains" },
    ],
    inputType: "text",
  },
  tag: {
    label: "Tag",
    operators: [
      { value: "has", label: "has" },
      { value: "not_has", label: "does not have" },
    ],
    inputType: "text",
  },
  lastActivity: {
    label: "Last Activity",
    operators: [
      { value: "before", label: "before" },
      { value: "after", label: "after" },
      { value: "within_days", label: "within (days)" },
    ],
    inputType: "text",
  },
  engagementScore: {
    label: "Engagement Score",
    operators: [
      { value: "greater_than", label: "greater than" },
      { value: "less_than", label: "less than" },
      { value: "equals", label: "equals" },
    ],
    inputType: "number",
  },
};

const FIELD_OPTIONS = Object.entries(FIELD_DEFS).map(([value, def]) => ({
  value,
  label: def.label,
}));

/* ── Types ─────────────────────────────────────────── */

interface RuleGroup {
  rules: LocalRule[];
}

interface LocalRule {
  field: string;
  operator: string;
  value: string;
}

function toApiRules(groups: RuleGroup[]): SegmentRule[] {
  return groups.flatMap((group, gi) =>
    group.rules.map((r) => ({
      groupIndex: gi,
      field: r.field,
      operator: r.operator,
      value: r.value,
    })),
  );
}

function fromApiRules(rules: SegmentRule[]): RuleGroup[] {
  const groupMap = new Map<number, LocalRule[]>();
  for (const r of rules) {
    if (!groupMap.has(r.groupIndex)) {
      groupMap.set(r.groupIndex, []);
    }
    groupMap.get(r.groupIndex)!.push({
      field: r.field,
      operator: r.operator,
      value: r.value,
    });
  }
  if (groupMap.size === 0) {
    return [{ rules: [makeDefaultRule()] }];
  }
  return Array.from(groupMap.entries())
    .sort((a, b) => a[0] - b[0])
    .map(([, rules]) => ({ rules }));
}

function makeDefaultRule(): LocalRule {
  return { field: "industry", operator: "equals", value: "" };
}

/* ── Component ─────────────────────────────────────── */

export default function SegmentBuilder() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const isNew = !id || id === "new";

  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [groups, setGroups] = useState<RuleGroup[]>([
    { rules: [makeDefaultRule()] },
  ]);
  const [previewCount, setPreviewCount] = useState<number | null>(null);
  const [previewLoading, setPreviewLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);
  const debounceRef = useRef<ReturnType<typeof setTimeout>>();

  /* Load existing segment */
  useEffect(() => {
    if (isNew) return;
    let cancelled = false;
    api
      .get(Number(id))
      .then((seg: SegmentDetail) => {
        if (cancelled) return;
        setName(seg.name);
        setDescription(seg.description ?? "");
        setGroups(fromApiRules(seg.rules));
      })
      .catch((err: unknown) => {
        if (!cancelled) setLoadError(err instanceof Error ? err.message : String(err));
      });
    return () => {
      cancelled = true;
    };
  }, [id, isNew]);

  /* Preview debounce */
  const fetchPreview = useCallback(
    (ruleGroups: RuleGroup[]) => {
      if (debounceRef.current) clearTimeout(debounceRef.current);
      const rules = toApiRules(ruleGroups);
      if (rules.length === 0 || rules.some((r) => !r.value)) {
        setPreviewCount(null);
        return;
      }
      debounceRef.current = setTimeout(async () => {
        setPreviewLoading(true);
        try {
          const result = await api.preview(rules);
          setPreviewCount(result.matchingCount);
        } catch {
          setPreviewCount(null);
        } finally {
          setPreviewLoading(false);
        }
      }, 500);
    },
    [],
  );

  /* Trigger preview on rule changes */
  useEffect(() => {
    fetchPreview(groups);
  }, [groups, fetchPreview]);

  /* ── Rule mutations ──────────────────────────────── */

  const updateGroups = (newGroups: RuleGroup[]) => {
    setGroups(newGroups);
  };

  const addGroup = () => {
    updateGroups([...groups, { rules: [makeDefaultRule()] }]);
  };

  const removeGroup = (gi: number) => {
    const next = groups.filter((_, i) => i !== gi);
    updateGroups(next.length === 0 ? [{ rules: [makeDefaultRule()] }] : next);
  };

  const addRule = (gi: number) => {
    const next = [...groups];
    next[gi] = { rules: [...next[gi].rules, makeDefaultRule()] };
    updateGroups(next);
  };

  const removeRule = (gi: number, ri: number) => {
    const next = [...groups];
    const rules = next[gi].rules.filter((_, i) => i !== ri);
    if (rules.length === 0) {
      removeGroup(gi);
    } else {
      next[gi] = { rules };
      updateGroups(next);
    }
  };

  const updateRule = (
    gi: number,
    ri: number,
    patch: Partial<LocalRule>,
  ) => {
    const next = [...groups];
    const rules = [...next[gi].rules];
    rules[ri] = { ...rules[ri], ...patch };

    /* Reset operator/value if field changes */
    if (patch.field) {
      const def = FIELD_DEFS[patch.field];
      if (def) {
        rules[ri].operator = def.operators[0].value;
        rules[ri].value = "";
      }
    }

    next[gi] = { rules };
    updateGroups(next);
  };

  /* ── Save ────────────────────────────────────────── */

  const handleSave = async () => {
    if (!name.trim()) return;
    setSaving(true);
    try {
      const payload = {
        name: name.trim(),
        description: description.trim() || null,
        rules: toApiRules(groups),
      };
      if (isNew) {
        const created = await api.create(payload);
        navigate(`/segments/${created.id}`, { replace: true });
      } else {
        await api.update(Number(id), payload);
      }
      navigate("/segments");
    } catch (err) {
      alert(err instanceof Error ? err.message : "Failed to save");
    } finally {
      setSaving(false);
    }
  };

  if (loadError) {
    return <div className="error-banner">{loadError}</div>;
  }

  return (
    <div>
      <div className="page-header">
        <h1>{isNew ? "Create Segment" : "Edit Segment"}</h1>
      </div>

      <div className="card" style={{ marginBottom: 20 }}>
        <div className="card-body">
          <div className="form-row">
            <div className="form-group">
              <label>Segment Name *</label>
              <input
                className="form-control"
                placeholder="e.g., High-value Tech Contacts"
                value={name}
                onChange={(e) => setName(e.target.value)}
              />
            </div>
            <div className="form-group">
              <label>Description</label>
              <input
                className="form-control"
                placeholder="Optional description..."
                value={description}
                onChange={(e) => setDescription(e.target.value)}
              />
            </div>
          </div>
        </div>
      </div>

      <div className="card">
        <div className="card-header">Rules</div>
        <div className="card-body">
          {groups.map((group, gi) => (
            <div key={gi}>
              {gi > 0 && <div className="rule-or-divider">OR</div>}
              <div className="rule-group">
                <div className="rule-group-header">
                  <span className="rule-group-title">
                    Group {gi + 1} (AND)
                  </span>
                  <button
                    className="btn btn-ghost btn-sm"
                    style={{ color: "var(--color-danger)" }}
                    onClick={() => removeGroup(gi)}
                  >
                    Remove Group
                  </button>
                </div>

                {group.rules.map((rule, ri) => (
                  <div key={ri} className="rule-row">
                    <select
                      value={rule.field}
                      onChange={(e) =>
                        updateRule(gi, ri, { field: e.target.value })
                      }
                    >
                      {FIELD_OPTIONS.map((f) => (
                        <option key={f.value} value={f.value}>
                          {f.label}
                        </option>
                      ))}
                    </select>

                    <select
                      value={rule.operator}
                      onChange={(e) =>
                        updateRule(gi, ri, { operator: e.target.value })
                      }
                    >
                      {FIELD_DEFS[rule.field]?.operators.map((op) => (
                        <option key={op.value} value={op.value}>
                          {op.label}
                        </option>
                      ))}
                    </select>

                    {FIELD_DEFS[rule.field]?.inputType === "select" ? (
                      <select
                        value={rule.value}
                        onChange={(e) =>
                          updateRule(gi, ri, { value: e.target.value })
                        }
                      >
                        <option value="">-- Select --</option>
                        {FIELD_DEFS[rule.field].options?.map((opt) => (
                          <option key={opt} value={opt}>
                            {opt}
                          </option>
                        ))}
                      </select>
                    ) : (
                      <input
                        type={
                          FIELD_DEFS[rule.field]?.inputType === "number"
                            ? "number"
                            : "text"
                        }
                        placeholder="Value..."
                        value={rule.value}
                        onChange={(e) =>
                          updateRule(gi, ri, { value: e.target.value })
                        }
                      />
                    )}

                    <button
                      className="btn btn-ghost btn-sm"
                      style={{ color: "var(--color-danger)" }}
                      onClick={() => removeRule(gi, ri)}
                    >
                      x
                    </button>
                  </div>
                ))}

                <button
                  className="btn btn-secondary btn-sm"
                  onClick={() => addRule(gi)}
                  style={{ marginTop: 4 }}
                >
                  + Add Condition
                </button>
              </div>
            </div>
          ))}

          <div style={{ marginTop: 12 }}>
            <button className="btn btn-secondary" onClick={addGroup}>
              + Add Group (OR)
            </button>
          </div>

          {(previewCount !== null || previewLoading) && (
            <div className="segment-preview-box">
              {previewLoading
                ? "Calculating matching contacts..."
                : `${previewCount} contact${previewCount !== 1 ? "s" : ""} match these rules`}
            </div>
          )}
        </div>
      </div>

      <div className="form-actions">
        <button
          className="btn btn-primary"
          disabled={saving || !name.trim()}
          onClick={handleSave}
        >
          {saving ? "Saving..." : isNew ? "Create Segment" : "Update Segment"}
        </button>
        <button
          className="btn btn-secondary"
          onClick={() => navigate("/segments")}
        >
          Cancel
        </button>
      </div>
    </div>
  );
}

export { toApiRules, fromApiRules, makeDefaultRule, FIELD_DEFS };
export type { RuleGroup, LocalRule };
