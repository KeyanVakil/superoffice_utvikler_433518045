import { useState, useEffect, useCallback } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { campaigns as api, segments as segApi, ai } from "../../api/client";
import CampaignPreview from "./CampaignPreview";
import type { Segment, SubjectSuggestion } from "../../types";

const PERSONALIZATION_TOKENS = [
  "{{firstName}}",
  "{{lastName}}",
  "{{email}}",
  "{{company}}",
  "{{industry}}",
];

export default function CampaignEditor() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const isNew = !id || id === "new";

  const [name, setName] = useState("");
  const [subject, setSubject] = useState("");
  const [htmlBody, setHtmlBody] = useState("");
  const [segmentId, setSegmentId] = useState<number | null>(null);
  const [status, setStatus] = useState("Draft");
  const [tab, setTab] = useState<"edit" | "preview">("edit");

  const [segmentList, setSegmentList] = useState<Segment[]>([]);
  const [suggestions, setSuggestions] = useState<SubjectSuggestion[]>([]);
  const [showSuggestions, setShowSuggestions] = useState(false);
  const [suggestionsLoading, setSuggestionsLoading] = useState(false);

  const [saving, setSaving] = useState(false);
  const [sending, setSending] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [nameError, setNameError] = useState<string | null>(null);
  const [subjectError, setSubjectError] = useState<string | null>(null);

  /* Load existing campaign + segment list */
  useEffect(() => {
    segApi.list().then(setSegmentList).catch(() => {});

    if (isNew) return;
    let cancelled = false;
    api
      .get(Number(id))
      .then((c) => {
        if (cancelled) return;
        setName(c.name);
        setSubject(c.subject);
        setHtmlBody(c.htmlBody);
        setSegmentId(c.segmentId);
        setStatus(c.status);
      })
      .catch((err: unknown) => {
        if (!cancelled) setLoadError(err instanceof Error ? err.message : String(err));
      });
    return () => {
      cancelled = true;
    };
  }, [id, isNew]);

  /* AI subject suggestions */
  const handleSuggestSubjects = useCallback(async () => {
    if (!subject.trim()) return;
    setSuggestionsLoading(true);
    setShowSuggestions(true);
    try {
      const res = await ai.suggestSubjects(subject, name || undefined);
      setSuggestions(res.suggestions);
    } catch {
      setSuggestions([]);
    } finally {
      setSuggestionsLoading(false);
    }
  }, [subject, name]);

  const pickSuggestion = (s: SubjectSuggestion) => {
    setSubject(s.subject);
    setShowSuggestions(false);
  };

  /* Validation */
  const validate = (): boolean => {
    let valid = true;
    if (!name.trim()) {
      setNameError("Campaign name is required");
      valid = false;
    } else {
      setNameError(null);
    }
    if (!subject.trim()) {
      setSubjectError("Subject is required");
      valid = false;
    } else {
      setSubjectError(null);
    }
    return valid;
  };

  /* Save */
  const handleSave = async () => {
    if (!validate()) return;
    setSaving(true);
    try {
      const payload = {
        name: name.trim(),
        subject: subject.trim(),
        htmlBody,
        segmentId,
      };
      if (isNew) {
        const created = await api.create(payload);
        navigate(`/campaigns/${created.id}`, { replace: true });
      } else {
        await api.update(Number(id), payload);
      }
      navigate("/campaigns");
    } catch (err) {
      alert(err instanceof Error ? err.message : "Failed to save");
    } finally {
      setSaving(false);
    }
  };

  /* Send */
  const handleSend = async () => {
    if (!id || isNew) return;
    if (!window.confirm("Send this campaign now? This action cannot be undone."))
      return;
    setSending(true);
    try {
      await api.send(Number(id));
      navigate("/campaigns");
    } catch (err) {
      alert(err instanceof Error ? err.message : "Failed to send");
    } finally {
      setSending(false);
    }
  };

  /* Insert token into HTML body */
  const insertToken = (token: string) => {
    setHtmlBody((prev) => prev + token);
  };

  if (loadError) return <div className="error-banner">{loadError}</div>;

  return (
    <div>
      <div className="page-header">
        <h1>{isNew ? "Create Campaign" : "Edit Campaign"}</h1>
        <div className="page-header-actions">
          {!isNew && status === "Draft" && (
            <button
              className="btn btn-success"
              disabled={sending}
              onClick={handleSend}
            >
              {sending ? "Sending..." : "Send Campaign"}
            </button>
          )}
        </div>
      </div>

      <div className="tabs">
        <button
          className={`tab${tab === "edit" ? " tab--active" : ""}`}
          onClick={() => setTab("edit")}
        >
          Edit
        </button>
        <button
          className={`tab${tab === "preview" ? " tab--active" : ""}`}
          onClick={() => setTab("preview")}
        >
          Preview
        </button>
      </div>

      {tab === "preview" ? (
        <CampaignPreview htmlBody={htmlBody} />
      ) : (
        <div className="card">
          <div className="card-body">
            <div className="form-group">
              <label>Campaign Name *</label>
              <input
                className={`form-control${nameError ? " form-control--error" : ""}`}
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="e.g., Spring Product Launch"
              />
              {nameError && <div className="form-error">{nameError}</div>}
            </div>

            <div className="form-group" style={{ position: "relative" }}>
              <label>Subject Line *</label>
              <div style={{ display: "flex", gap: 8 }}>
                <input
                  className={`form-control${subjectError ? " form-control--error" : ""}`}
                  value={subject}
                  onChange={(e) => setSubject(e.target.value)}
                  placeholder="Enter email subject..."
                />
                <button
                  className="btn btn-secondary btn-sm"
                  onClick={handleSuggestSubjects}
                  disabled={suggestionsLoading || !subject.trim()}
                  title="Get AI suggestions"
                >
                  {suggestionsLoading ? "..." : "AI Suggest"}
                </button>
              </div>
              {subjectError && <div className="form-error">{subjectError}</div>}

              {showSuggestions && (
                <div className="suggestions-dropdown">
                  {suggestionsLoading ? (
                    <div className="suggestion-item">
                      <span className="suggestion-subject">
                        Generating suggestions...
                      </span>
                    </div>
                  ) : suggestions.length === 0 ? (
                    <div className="suggestion-item">
                      <span className="suggestion-subject">
                        No suggestions available
                      </span>
                    </div>
                  ) : (
                    suggestions.map((s, i) => (
                      <div
                        key={i}
                        className="suggestion-item"
                        onClick={() => pickSuggestion(s)}
                      >
                        <div className="suggestion-subject">{s.subject}</div>
                        <div className="suggestion-reason">{s.reason}</div>
                      </div>
                    ))
                  )}
                  <div
                    className="suggestion-item"
                    style={{ textAlign: "center", fontSize: 12, color: "var(--color-gray-400)" }}
                    onClick={() => setShowSuggestions(false)}
                  >
                    Close
                  </div>
                </div>
              )}
            </div>

            <div className="form-group">
              <label>Target Segment</label>
              <select
                className="form-control"
                value={segmentId ?? ""}
                onChange={(e) =>
                  setSegmentId(e.target.value ? Number(e.target.value) : null)
                }
              >
                <option value="">All contacts</option>
                {segmentList.map((seg) => (
                  <option key={seg.id} value={seg.id}>
                    {seg.name}
                  </option>
                ))}
              </select>
            </div>

            <div className="form-group">
              <label>HTML Body</label>
              <textarea
                className="form-control"
                rows={12}
                value={htmlBody}
                onChange={(e) => setHtmlBody(e.target.value)}
                placeholder="<h1>Hello {{firstName}},</h1><p>Your content here...</p>"
                style={{ fontFamily: "monospace", fontSize: 12 }}
              />
              <div className="token-hints">
                <span style={{ fontSize: 11, color: "var(--color-gray-500)", marginRight: 4 }}>
                  Tokens:
                </span>
                {PERSONALIZATION_TOKENS.map((token) => (
                  <span
                    key={token}
                    className="token-hint"
                    onClick={() => insertToken(token)}
                  >
                    {token}
                  </span>
                ))}
              </div>
            </div>

            <div className="form-actions">
              <button
                className="btn btn-primary"
                disabled={saving}
                onClick={handleSave}
              >
                {saving ? "Saving..." : "Save Draft"}
              </button>
              <button
                className="btn btn-secondary"
                onClick={() => navigate("/campaigns")}
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
