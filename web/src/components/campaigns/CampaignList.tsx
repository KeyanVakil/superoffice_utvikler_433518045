import { useNavigate } from "react-router-dom";
import { useApi } from "../../hooks/useApi";
import { campaigns as api } from "../../api/client";
import type { Campaign } from "../../types";

function statusBadge(status: string) {
  const cls = status === "Sent" ? "badge-success" : "badge-gray";
  return <span className={`badge ${cls}`}>{status}</span>;
}

function pct(value: number): string {
  return `${(value * 100).toFixed(1)}%`;
}

export default function CampaignList() {
  const navigate = useNavigate();
  const { data, loading, error } = useApi<Campaign[]>(
    () => api.list(),
    [],
  );

  return (
    <div>
      <div className="page-header">
        <h1>Campaigns</h1>
        <button
          className="btn btn-primary"
          onClick={() => navigate("/campaigns/new")}
        >
          + Create Campaign
        </button>
      </div>

      {error && <div className="error-banner">{error}</div>}

      {loading ? (
        <div className="loading">
          <span className="spinner" /> Loading campaigns...
        </div>
      ) : !data || data.length === 0 ? (
        <div className="empty-state">
          <p>No campaigns yet.</p>
          <button
            className="btn btn-primary"
            onClick={() => navigate("/campaigns/new")}
          >
            Create your first campaign
          </button>
        </div>
      ) : (
        <div className="card">
          <div className="table-wrapper">
            <table>
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Subject</th>
                  <th>Status</th>
                  <th>Segment</th>
                  <th>Sent</th>
                  <th>Open Rate</th>
                  <th>Click Rate</th>
                </tr>
              </thead>
              <tbody>
                {data.map((c) => (
                  <tr
                    key={c.id}
                    style={{ cursor: "pointer" }}
                    onClick={() => navigate(`/campaigns/${c.id}`)}
                  >
                    <td style={{ fontWeight: 500 }}>{c.name}</td>
                    <td style={{ color: "var(--color-gray-600)" }}>
                      {c.subject}
                    </td>
                    <td>{statusBadge(c.status)}</td>
                    <td>{c.segmentName || "All contacts"}</td>
                    <td>{c.sendCount}</td>
                    <td>{pct(c.openRate)}</td>
                    <td>{pct(c.clickRate)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
