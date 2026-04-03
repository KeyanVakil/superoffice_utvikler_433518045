import { useState } from "react";
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Legend,
} from "recharts";
import { useApi } from "../../hooks/useApi";
import { analytics as api } from "../../api/client";
import type { Overview, EngagementTrend, Campaign } from "../../types";

const DATE_RANGES = [
  { label: "7d", days: 7 },
  { label: "30d", days: 30 },
  { label: "90d", days: 90 },
];

function pct(value: number): string {
  return `${(value * 100).toFixed(1)}%`;
}

function formatDate(dateStr: string): string {
  const d = new Date(dateStr);
  return `${d.getMonth() + 1}/${d.getDate()}`;
}

export default function Dashboard() {
  const [days, setDays] = useState(30);

  const {
    data: overview,
    loading: overviewLoading,
    error: overviewError,
  } = useApi<Overview>(() => api.overview(), []);

  const {
    data: trends,
    loading: trendsLoading,
    error: trendsError,
  } = useApi<EngagementTrend[]>(() => api.engagement(days), [days]);

  const error = overviewError || trendsError;

  return (
    <div>
      <div className="page-header">
        <h1>Analytics</h1>
      </div>

      {error && <div className="error-banner">{error}</div>}

      {/* Stat cards */}
      {overviewLoading ? (
        <div className="loading">
          <span className="spinner" /> Loading overview...
        </div>
      ) : overview ? (
        <div className="stats-grid">
          <div className="stat-card">
            <div className="stat-label">Total Contacts</div>
            <div className="stat-value">
              {overview.totalContacts.toLocaleString()}
            </div>
          </div>
          <div className="stat-card">
            <div className="stat-label">Active Campaigns</div>
            <div className="stat-value">{overview.activeCampaigns}</div>
          </div>
          <div className="stat-card">
            <div className="stat-label">Active Journeys</div>
            <div className="stat-value">{overview.activeJourneys}</div>
          </div>
          <div className="stat-card">
            <div className="stat-label">Engagement Rate</div>
            <div className="stat-value">
              {pct(overview.overallEngagementRate)}
            </div>
          </div>
        </div>
      ) : null}

      {/* Engagement Trend Chart */}
      <div className="card" style={{ marginBottom: 24 }}>
        <div
          className="card-header"
          style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}
        >
          <span>Engagement Trends</span>
          <div className="date-range-selector">
            {DATE_RANGES.map((range) => (
              <button
                key={range.days}
                className={`date-range-btn${days === range.days ? " date-range-btn--active" : ""}`}
                onClick={() => setDays(range.days)}
              >
                {range.label}
              </button>
            ))}
          </div>
        </div>
        <div className="card-body">
          {trendsLoading ? (
            <div className="loading">
              <span className="spinner" /> Loading chart data...
            </div>
          ) : !trends || trends.length === 0 ? (
            <div className="empty-state">
              <p>No engagement data yet. Send your first campaign to see trends.</p>
            </div>
          ) : (
            <ResponsiveContainer width="100%" height={320}>
              <LineChart data={trends}>
                <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
                <XAxis
                  dataKey="date"
                  tickFormatter={formatDate}
                  tick={{ fontSize: 12 }}
                  stroke="#9ca3af"
                />
                <YAxis tick={{ fontSize: 12 }} stroke="#9ca3af" />
                <Tooltip
                  labelFormatter={(label) => `Date: ${label}`}
                  contentStyle={{
                    fontSize: 13,
                    borderRadius: 6,
                    border: "1px solid #e5e7eb",
                  }}
                />
                <Legend />
                <Line
                  type="monotone"
                  dataKey="sends"
                  stroke="#6b7280"
                  strokeWidth={2}
                  dot={false}
                  name="Sends"
                />
                <Line
                  type="monotone"
                  dataKey="opens"
                  stroke="#2563eb"
                  strokeWidth={2}
                  dot={false}
                  name="Opens"
                />
                <Line
                  type="monotone"
                  dataKey="clicks"
                  stroke="#16a34a"
                  strokeWidth={2}
                  dot={false}
                  name="Clicks"
                />
              </LineChart>
            </ResponsiveContainer>
          )}
        </div>
      </div>

      {/* Recent Campaigns */}
      {overview && overview.recentCampaigns.length > 0 && (
        <div className="card">
          <div className="card-header">Recent Campaigns</div>
          <div className="table-wrapper">
            <table>
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Status</th>
                  <th>Sent</th>
                  <th>Open Rate</th>
                  <th>Click Rate</th>
                </tr>
              </thead>
              <tbody>
                {overview.recentCampaigns.map((c: Campaign) => (
                  <tr key={c.id}>
                    <td style={{ fontWeight: 500 }}>{c.name}</td>
                    <td>
                      <span
                        className={`badge ${c.status === "Sent" ? "badge-success" : "badge-gray"}`}
                      >
                        {c.status}
                      </span>
                    </td>
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
