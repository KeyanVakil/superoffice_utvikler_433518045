import { useState, useCallback } from "react";
import { useApi } from "../../hooks/useApi";
import { contacts as api } from "../../api/client";
import ContactForm from "./ContactForm";
import type { Contact, ContactDetail, PagedResult } from "../../types";

const INDUSTRIES = [
  "",
  "Technology",
  "Finance",
  "Healthcare",
  "Retail",
  "Manufacturing",
  "Education",
  "Media",
  "Other",
];

export default function ContactList() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [searchQuery, setSearchQuery] = useState("");
  const [industry, setIndustry] = useState("");
  const [showForm, setShowForm] = useState(false);
  const [editingContact, setEditingContact] = useState<ContactDetail | null>(
    null,
  );
  const [expandedId, setExpandedId] = useState<number | null>(null);
  const [expandedDetail, setExpandedDetail] = useState<ContactDetail | null>(
    null,
  );

  const {
    data,
    loading,
    error,
    refetch,
  } = useApi<PagedResult<Contact>>(
    () => api.list(page, 20, searchQuery || undefined, industry || undefined),
    [page, searchQuery, industry],
  );

  const handleSearch = useCallback(() => {
    setSearchQuery(search);
    setPage(1);
  }, [search]);

  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent) => {
      if (e.key === "Enter") handleSearch();
    },
    [handleSearch],
  );

  const handleExpand = useCallback(
    async (id: number) => {
      if (expandedId === id) {
        setExpandedId(null);
        setExpandedDetail(null);
        return;
      }
      setExpandedId(id);
      try {
        const detail = await api.get(id);
        setExpandedDetail(detail);
      } catch {
        setExpandedDetail(null);
      }
    },
    [expandedId],
  );

  const handleDelete = useCallback(
    async (id: number) => {
      if (!window.confirm("Delete this contact?")) return;
      try {
        await api.delete(id);
        refetch();
      } catch {
        /* error handled by refetch */
      }
    },
    [refetch],
  );

  const handleImportCsv = useCallback(async () => {
    const input = document.createElement("input");
    input.type = "file";
    input.accept = ".csv";
    input.onchange = async () => {
      const file = input.files?.[0];
      if (!file) return;
      try {
        const result = await api.importCsv(file);
        alert(
          `Imported: ${result.imported}, Skipped: ${result.skipped}` +
            (result.errors.length
              ? `\nErrors:\n${result.errors.join("\n")}`
              : ""),
        );
        refetch();
      } catch (err) {
        alert(`Import failed: ${err instanceof Error ? err.message : err}`);
      }
    };
    input.click();
  }, [refetch]);

  const handleFormClose = useCallback(() => {
    setShowForm(false);
    setEditingContact(null);
  }, []);

  const handleFormSaved = useCallback(() => {
    handleFormClose();
    refetch();
  }, [handleFormClose, refetch]);

  const handleEdit = useCallback(async (id: number) => {
    try {
      const detail = await api.get(id);
      setEditingContact(detail);
      setShowForm(true);
    } catch {
      /* ignore */
    }
  }, []);

  const totalPages = data ? Math.ceil(data.totalCount / data.pageSize) : 0;

  return (
    <div>
      <div className="page-header">
        <h1>Contacts</h1>
        <div className="page-header-actions">
          <button className="btn btn-secondary" onClick={handleImportCsv}>
            Import CSV
          </button>
          <button className="btn btn-primary" onClick={() => setShowForm(true)}>
            + Add Contact
          </button>
        </div>
      </div>

      <div className="toolbar">
        <input
          className="form-control search-input"
          placeholder="Search by name, email, or company..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          onKeyDown={handleKeyDown}
        />
        <button className="btn btn-secondary" onClick={handleSearch}>
          Search
        </button>
        <select
          className="form-control"
          style={{ width: "auto" }}
          value={industry}
          onChange={(e) => {
            setIndustry(e.target.value);
            setPage(1);
          }}
        >
          <option value="">All Industries</option>
          {INDUSTRIES.filter(Boolean).map((ind) => (
            <option key={ind} value={ind}>
              {ind}
            </option>
          ))}
        </select>
      </div>

      {error && <div className="error-banner">{error}</div>}

      {loading ? (
        <div className="loading">
          <span className="spinner" /> Loading contacts...
        </div>
      ) : !data || data.items.length === 0 ? (
        <div className="empty-state">
          <p>No contacts found.</p>
          <button
            className="btn btn-primary"
            onClick={() => setShowForm(true)}
          >
            Add your first contact
          </button>
        </div>
      ) : (
        <>
          <div className="card">
            <div className="table-wrapper">
              <table>
                <thead>
                  <tr>
                    <th>Name</th>
                    <th>Email</th>
                    <th>Company</th>
                    <th>Industry</th>
                    <th>Tags</th>
                    <th>Engagement</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((contact) => (
                    <>
                      <tr
                        key={contact.id}
                        style={{ cursor: "pointer" }}
                        onClick={() => handleExpand(contact.id)}
                      >
                        <td style={{ fontWeight: 500 }}>
                          {contact.firstName} {contact.lastName}
                        </td>
                        <td>{contact.email}</td>
                        <td>{contact.company || "-"}</td>
                        <td>{contact.industry || "-"}</td>
                        <td>
                          {contact.tags.map((tag) => (
                            <span key={tag} className="tag">
                              {tag}
                            </span>
                          ))}
                        </td>
                        <td>
                          <EngagementIndicator
                            score={contact.engagementScore}
                          />
                        </td>
                        <td>
                          <button
                            className="btn btn-ghost btn-sm"
                            onClick={(e) => {
                              e.stopPropagation();
                              handleEdit(contact.id);
                            }}
                          >
                            Edit
                          </button>
                          <button
                            className="btn btn-ghost btn-sm"
                            style={{ color: "var(--color-danger)" }}
                            onClick={(e) => {
                              e.stopPropagation();
                              handleDelete(contact.id);
                            }}
                          >
                            Delete
                          </button>
                        </td>
                      </tr>
                      {expandedId === contact.id && expandedDetail && (
                        <tr
                          key={`detail-${contact.id}`}
                          className="contact-detail-row"
                        >
                          <td colSpan={7}>
                            <ContactDetailPanel detail={expandedDetail} />
                          </td>
                        </tr>
                      )}
                    </>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          <div className="pagination">
            <span className="pagination-info">
              {data.totalCount} contact{data.totalCount !== 1 ? "s" : ""}
            </span>
            <button
              className="btn btn-secondary btn-sm"
              disabled={page <= 1}
              onClick={() => setPage(page - 1)}
            >
              Previous
            </button>
            <span style={{ fontSize: 13, color: "var(--color-gray-600)" }}>
              Page {page} of {totalPages}
            </span>
            <button
              className="btn btn-secondary btn-sm"
              disabled={page >= totalPages}
              onClick={() => setPage(page + 1)}
            >
              Next
            </button>
          </div>
        </>
      )}

      {showForm && (
        <div className="modal-backdrop" onClick={handleFormClose}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2>{editingContact ? "Edit Contact" : "Add Contact"}</h2>
              <button className="btn btn-ghost" onClick={handleFormClose}>
                x
              </button>
            </div>
            <ContactForm
              contact={editingContact}
              onSaved={handleFormSaved}
              onCancel={handleFormClose}
            />
          </div>
        </div>
      )}
    </div>
  );
}

function EngagementIndicator({ score }: { score: number }) {
  const pct = Math.round(score * 100);
  const level = score >= 0.6 ? "high" : score >= 0.3 ? "mid" : "low";
  return (
    <div className="engagement-bar">
      <div className="engagement-track">
        <div
          className={`engagement-fill engagement-fill--${level}`}
          style={{ width: `${pct}%` }}
        />
      </div>
      <span style={{ fontSize: 12, color: "var(--color-gray-500)" }}>
        {pct}%
      </span>
    </div>
  );
}

function ContactDetailPanel({ detail }: { detail: ContactDetail }) {
  return (
    <div className="contact-detail-inner">
      <div style={{ display: "flex", gap: 40 }}>
        <div>
          <strong>Email:</strong> {detail.email}
        </div>
        <div>
          <strong>Company:</strong> {detail.company || "-"}
        </div>
        <div>
          <strong>Industry:</strong> {detail.industry || "-"}
        </div>
        <div>
          <strong>Created:</strong>{" "}
          {new Date(detail.createdAt).toLocaleDateString()}
        </div>
      </div>
      {detail.activityTimeline.length > 0 && (
        <div className="contact-timeline">
          <strong style={{ fontSize: 13 }}>Recent Activity</strong>
          {detail.activityTimeline.slice(0, 10).map((evt) => (
            <div key={evt.id} className="timeline-item">
              <span className="timeline-dot" />
              <span className="timeline-time">
                {new Date(evt.occurredAt).toLocaleString()}
              </span>
              <span>
                {evt.eventType}
                {evt.campaignName ? ` - ${evt.campaignName}` : ""}
              </span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
