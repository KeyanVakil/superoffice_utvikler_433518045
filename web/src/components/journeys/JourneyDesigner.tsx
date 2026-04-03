import { useState, useEffect, useCallback } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { journeys as api } from "../../api/client";
import type { JourneyDetail, CreateJourneyStep } from "../../types";

/* ── Step type definitions ─────────────────────────── */

interface LocalStep {
  id: string; // local unique key
  stepType: "send_email" | "wait" | "condition";
  config: Record<string, unknown>;
}

const TRIGGER_TYPES = [
  { value: "segment_entry", label: "Segment Entry" },
  { value: "tag_added", label: "Tag Added" },
  { value: "form_submit", label: "Form Submit" },
  { value: "manual", label: "Manual Enrollment" },
];

function generateId(): string {
  return Math.random().toString(36).slice(2, 10);
}

function makeDefaultStep(type: LocalStep["stepType"]): LocalStep {
  const base = { id: generateId(), stepType: type };
  switch (type) {
    case "send_email":
      return { ...base, config: { subject: "", body: "" } };
    case "wait":
      return { ...base, config: { days: 1 } };
    case "condition":
      return { ...base, config: { field: "engagementScore", operator: "greater_than", value: "0.5" } };
  }
}

function stepTypeIcon(type: string): { icon: string; cls: string } {
  switch (type) {
    case "send_email":
      return { icon: "\u2709", cls: "email" };
    case "wait":
      return { icon: "\u23F3", cls: "wait" };
    case "condition":
      return { icon: "?", cls: "condition" };
    default:
      return { icon: "\u25CF", cls: "trigger" };
  }
}

function stepDescription(step: LocalStep): string {
  switch (step.stepType) {
    case "send_email":
      return (step.config.subject as string) || "Send email";
    case "wait":
      return `Wait ${step.config.days || 1} day(s)`;
    case "condition":
      return `If ${step.config.field} ${step.config.operator} ${step.config.value}`;
  }
}

/* ── Component ─────────────────────────────────────── */

export default function JourneyDesigner() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const isNew = !id || id === "new";

  const [name, setName] = useState("");
  const [triggerType, setTriggerType] = useState("segment_entry");
  const [triggerConfig, setTriggerConfig] = useState<Record<string, unknown>>({});
  const [steps, setSteps] = useState<LocalStep[]>([]);
  const [editingStep, setEditingStep] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);

  /* Load existing journey */
  useEffect(() => {
    if (isNew) return;
    let cancelled = false;
    api
      .get(Number(id))
      .then((j: JourneyDetail) => {
        if (cancelled) return;
        setName(j.name);
        setTriggerType(j.triggerType);
        setTriggerConfig(j.triggerConfig ?? {});
        setSteps(
          j.steps
            .sort((a, b) => a.stepOrder - b.stepOrder)
            .map((s) => ({
              id: String(s.id),
              stepType: s.stepType as LocalStep["stepType"],
              config: (s.config as Record<string, unknown>) ?? {},
            })),
        );
      })
      .catch((err: unknown) => {
        if (!cancelled) setLoadError(err instanceof Error ? err.message : String(err));
      });
    return () => {
      cancelled = true;
    };
  }, [id, isNew]);

  /* Step mutations */
  const addStep = useCallback((type: LocalStep["stepType"]) => {
    setSteps((prev) => [...prev, makeDefaultStep(type)]);
  }, []);

  const removeStep = useCallback((stepId: string) => {
    setSteps((prev) => prev.filter((s) => s.id !== stepId));
  }, []);

  const updateStepConfig = useCallback(
    (stepId: string, key: string, value: unknown) => {
      setSteps((prev) =>
        prev.map((s) =>
          s.id === stepId
            ? { ...s, config: { ...s.config, [key]: value } }
            : s,
        ),
      );
    },
    [],
  );

  /* Save */
  const handleSave = async () => {
    if (!name.trim()) return;
    setSaving(true);
    try {
      const apiSteps: CreateJourneyStep[] = steps.map((s, i) => ({
        stepOrder: i,
        stepType: s.stepType,
        config: s.config,
      }));
      const payload = {
        name: name.trim(),
        triggerType,
        triggerConfig: Object.keys(triggerConfig).length > 0 ? triggerConfig : null,
        steps: apiSteps,
      };
      if (isNew) {
        const created = await api.create(payload);
        navigate(`/journeys/${created.id}`, { replace: true });
      } else {
        await api.update(Number(id), payload);
      }
      navigate("/journeys");
    } catch (err) {
      alert(err instanceof Error ? err.message : "Failed to save");
    } finally {
      setSaving(false);
    }
  };

  if (loadError) return <div className="error-banner">{loadError}</div>;

  return (
    <div>
      <div className="page-header">
        <h1>{isNew ? "Create Journey" : "Edit Journey"}</h1>
      </div>

      {/* Journey settings */}
      <div className="card" style={{ marginBottom: 20 }}>
        <div className="card-body">
          <div className="form-row">
            <div className="form-group">
              <label>Journey Name *</label>
              <input
                className="form-control"
                placeholder="e.g., Welcome Series"
                value={name}
                onChange={(e) => setName(e.target.value)}
              />
            </div>
            <div className="form-group">
              <label>Trigger Type</label>
              <select
                className="form-control"
                value={triggerType}
                onChange={(e) => setTriggerType(e.target.value)}
              >
                {TRIGGER_TYPES.map((t) => (
                  <option key={t.value} value={t.value}>
                    {t.label}
                  </option>
                ))}
              </select>
            </div>
          </div>
          {triggerType === "tag_added" && (
            <div className="form-group">
              <label>Tag Name</label>
              <input
                className="form-control"
                placeholder="e.g., newsletter-signup"
                value={(triggerConfig.tag as string) ?? ""}
                onChange={(e) =>
                  setTriggerConfig({ ...triggerConfig, tag: e.target.value })
                }
              />
            </div>
          )}
          {triggerType === "segment_entry" && (
            <div className="form-group">
              <label>Segment ID</label>
              <input
                className="form-control"
                type="number"
                placeholder="Segment ID"
                value={(triggerConfig.segmentId as string) ?? ""}
                onChange={(e) =>
                  setTriggerConfig({
                    ...triggerConfig,
                    segmentId: e.target.value ? Number(e.target.value) : undefined,
                  })
                }
              />
            </div>
          )}
        </div>
      </div>

      {/* Journey flow */}
      <div className="card">
        <div className="card-header">Journey Steps</div>
        <div className="card-body">
          <div className="journey-flow">
            {/* Trigger node */}
            <div className="journey-node">
              <div className="journey-node-icon journey-node-icon--trigger">
                {"\u25B6"}
              </div>
              <div className="journey-node-info">
                <div className="journey-node-type">Trigger</div>
                <div className="journey-node-desc">
                  {TRIGGER_TYPES.find((t) => t.value === triggerType)?.label ??
                    triggerType}
                </div>
              </div>
            </div>

            {steps.map((step) => {
              const { icon, cls } = stepTypeIcon(step.stepType);
              return (
                <div key={step.id}>
                  <div className="journey-connector" />
                  <div
                    className="journey-node"
                    onClick={() =>
                      setEditingStep(
                        editingStep === step.id ? null : step.id,
                      )
                    }
                  >
                    <div className={`journey-node-icon journey-node-icon--${cls}`}>
                      {icon}
                    </div>
                    <div className="journey-node-info">
                      <div className="journey-node-type">{step.stepType.replace("_", " ")}</div>
                      <div className="journey-node-desc">
                        {stepDescription(step)}
                      </div>
                    </div>
                    <button
                      className="journey-node-remove"
                      onClick={(e) => {
                        e.stopPropagation();
                        removeStep(step.id);
                      }}
                    >
                      x
                    </button>
                  </div>

                  {/* Inline step editor */}
                  {editingStep === step.id && (
                    <StepEditor
                      step={step}
                      onUpdate={updateStepConfig}
                    />
                  )}
                </div>
              );
            })}

            {/* Add step buttons */}
            <div className="journey-connector" />
            <div style={{ display: "flex", gap: 8 }} className="journey-add-btn">
              <button
                className="btn btn-secondary btn-sm"
                onClick={() => addStep("send_email")}
              >
                + Send Email
              </button>
              <button
                className="btn btn-secondary btn-sm"
                onClick={() => addStep("wait")}
              >
                + Wait
              </button>
              <button
                className="btn btn-secondary btn-sm"
                onClick={() => addStep("condition")}
              >
                + Condition
              </button>
            </div>
          </div>
        </div>
      </div>

      <div className="form-actions">
        <button
          className="btn btn-primary"
          disabled={saving || !name.trim()}
          onClick={handleSave}
        >
          {saving ? "Saving..." : isNew ? "Create Journey" : "Update Journey"}
        </button>
        <button
          className="btn btn-secondary"
          onClick={() => navigate("/journeys")}
        >
          Cancel
        </button>
      </div>
    </div>
  );
}

/* ── Step Editor ───────────────────────────────────── */

function StepEditor({
  step,
  onUpdate,
}: {
  step: LocalStep;
  onUpdate: (id: string, key: string, value: unknown) => void;
}) {
  const style: React.CSSProperties = {
    background: "var(--color-gray-50)",
    border: "1px solid var(--color-gray-200)",
    borderRadius: "var(--radius)",
    padding: 16,
    marginTop: 8,
    maxWidth: 340,
  };

  switch (step.stepType) {
    case "send_email":
      return (
        <div style={style}>
          <div className="form-group">
            <label>Subject</label>
            <input
              className="form-control"
              value={(step.config.subject as string) ?? ""}
              onChange={(e) => onUpdate(step.id, "subject", e.target.value)}
              placeholder="Email subject..."
            />
          </div>
          <div className="form-group">
            <label>Body (HTML)</label>
            <textarea
              className="form-control"
              rows={4}
              value={(step.config.body as string) ?? ""}
              onChange={(e) => onUpdate(step.id, "body", e.target.value)}
              placeholder="<p>Hello {{firstName}},</p>"
              style={{ fontFamily: "monospace", fontSize: 12 }}
            />
          </div>
        </div>
      );

    case "wait":
      return (
        <div style={style}>
          <div className="form-group">
            <label>Wait (days)</label>
            <input
              className="form-control"
              type="number"
              min={1}
              value={(step.config.days as number) ?? 1}
              onChange={(e) =>
                onUpdate(step.id, "days", Number(e.target.value))
              }
            />
          </div>
        </div>
      );

    case "condition":
      return (
        <div style={style}>
          <div className="form-group">
            <label>Field</label>
            <select
              className="form-control"
              value={(step.config.field as string) ?? "engagementScore"}
              onChange={(e) => onUpdate(step.id, "field", e.target.value)}
            >
              <option value="engagementScore">Engagement Score</option>
              <option value="industry">Industry</option>
              <option value="tag">Tag</option>
              <option value="lastActivity">Last Activity</option>
            </select>
          </div>
          <div className="form-group">
            <label>Operator</label>
            <select
              className="form-control"
              value={(step.config.operator as string) ?? "greater_than"}
              onChange={(e) => onUpdate(step.id, "operator", e.target.value)}
            >
              <option value="greater_than">greater than</option>
              <option value="less_than">less than</option>
              <option value="equals">equals</option>
              <option value="not_equals">does not equal</option>
              <option value="contains">contains</option>
            </select>
          </div>
          <div className="form-group">
            <label>Value</label>
            <input
              className="form-control"
              value={(step.config.value as string) ?? ""}
              onChange={(e) => onUpdate(step.id, "value", e.target.value)}
            />
          </div>
          <div style={{ display: "flex", gap: 20, marginTop: 8 }}>
            <div className="journey-branch">
              <span className="journey-branch-label journey-branch-label--true">
                True
              </span>
              <span style={{ fontSize: 12, color: "var(--color-gray-500)" }}>
                Continues to next step
              </span>
            </div>
            <div className="journey-branch">
              <span className="journey-branch-label journey-branch-label--false">
                False
              </span>
              <span style={{ fontSize: 12, color: "var(--color-gray-500)" }}>
                Exits journey
              </span>
            </div>
          </div>
        </div>
      );
  }
}
