import { useNavigate } from "react-router-dom";
import { useApi } from "../../hooks/useApi";
import { segments as api } from "../../api/client";
import type { Segment } from "../../types";

export default function SegmentList() {
  const navigate = useNavigate();
  const { data, loading, error, refetch } = useApi<Segment[]>(
    () => api.list(),
    [],
  );

  const handleDelete = async (id: number) => {
    if (!window.confirm("Delete this segment?")) return;
    try {
      await api.delete(id);
      refetch();
    } catch {
      /* ignore */
    }
  };

  return (
    <div>
      <div className="page-header">
        <h1>Segments</h1>
        <button
          className="btn btn-primary"
          onClick={() => navigate("/segments/new")}
        >
          + Create Segment
        </button>
      </div>

      {error && <div className="error-banner">{error}</div>}

      {loading ? (
        <div className="loading">
          <span className="spinner" /> Loading segments...
        </div>
      ) : !data || data.length === 0 ? (
        <div className="empty-state">
          <p>No segments yet.</p>
          <button
            className="btn btn-primary"
            onClick={() => navigate("/segments/new")}
          >
            Create your first segment
          </button>
        </div>
      ) : (
        <div className="card">
          <div className="table-wrapper">
            <table>
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Description</th>
                  <th>Created</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {data.map((seg) => (
                  <tr
                    key={seg.id}
                    style={{ cursor: "pointer" }}
                    onClick={() => navigate(`/segments/${seg.id}`)}
                  >
                    <td style={{ fontWeight: 500 }}>{seg.name}</td>
                    <td style={{ color: "var(--color-gray-500)" }}>
                      {seg.description || "-"}
                    </td>
                    <td>{new Date(seg.createdAt).toLocaleDateString()}</td>
                    <td>
                      <button
                        className="btn btn-ghost btn-sm"
                        onClick={(e) => {
                          e.stopPropagation();
                          navigate(`/segments/${seg.id}`);
                        }}
                      >
                        Edit
                      </button>
                      <button
                        className="btn btn-ghost btn-sm"
                        style={{ color: "var(--color-danger)" }}
                        onClick={(e) => {
                          e.stopPropagation();
                          handleDelete(seg.id);
                        }}
                      >
                        Delete
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
