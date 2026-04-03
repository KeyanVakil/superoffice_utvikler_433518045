import { useNavigate } from "react-router-dom";
import { useApi } from "../../hooks/useApi";
import { journeys as api } from "../../api/client";
import type { Journey } from "../../types";

export default function JourneyList() {
  const navigate = useNavigate();
  const { data, loading, error, refetch } = useApi<Journey[]>(
    () => api.list(),
    [],
  );

  const handleToggleActive = async (journey: Journey) => {
    try {
      if (journey.isActive) {
        await api.deactivate(journey.id);
      } else {
        await api.activate(journey.id);
      }
      refetch();
    } catch (err) {
      alert(err instanceof Error ? err.message : "Failed to update");
    }
  };

  return (
    <div>
      <div className="page-header">
        <h1>Journeys</h1>
        <button
          className="btn btn-primary"
          onClick={() => navigate("/journeys/new")}
        >
          + Create Journey
        </button>
      </div>

      {error && <div className="error-banner">{error}</div>}

      {loading ? (
        <div className="loading">
          <span className="spinner" /> Loading journeys...
        </div>
      ) : !data || data.length === 0 ? (
        <div className="empty-state">
          <p>No journeys yet.</p>
          <button
            className="btn btn-primary"
            onClick={() => navigate("/journeys/new")}
          >
            Create your first journey
          </button>
        </div>
      ) : (
        <div className="card">
          <div className="table-wrapper">
            <table>
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Trigger</th>
                  <th>Status</th>
                  <th>Enrolled</th>
                  <th>Completed</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {data.map((j) => (
                  <tr
                    key={j.id}
                    style={{ cursor: "pointer" }}
                    onClick={() => navigate(`/journeys/${j.id}`)}
                  >
                    <td style={{ fontWeight: 500 }}>{j.name}</td>
                    <td>
                      <span className="badge badge-gray">
                        {j.triggerType}
                      </span>
                    </td>
                    <td>
                      <span
                        className={`badge ${j.isActive ? "badge-success" : "badge-gray"}`}
                      >
                        {j.isActive ? "Active" : "Inactive"}
                      </span>
                    </td>
                    <td>{j.enrolledCount}</td>
                    <td>{j.completedCount}</td>
                    <td>
                      <button
                        className={`btn btn-sm ${j.isActive ? "btn-secondary" : "btn-success"}`}
                        onClick={(e) => {
                          e.stopPropagation();
                          handleToggleActive(j);
                        }}
                      >
                        {j.isActive ? "Deactivate" : "Activate"}
                      </button>
                    </td>
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
