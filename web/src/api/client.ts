import type {
  Contact,
  ContactDetail,
  CreateContact,
  UpdateContact,
  ContactImportResult,
  PagedResult,
  Segment,
  SegmentDetail,
  CreateSegment,
  SegmentPreview,
  SegmentRule,
  Campaign,
  CampaignDetail,
  CreateCampaign,
  CampaignStats,
  Journey,
  JourneyDetail,
  CreateJourney,
  JourneyStats,
  Overview,
  EngagementTrend,
  SubjectSuggestionResponse,
  SendTimeRecommendation,
} from "../types";

class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

async function request<T>(
  url: string,
  options?: RequestInit,
): Promise<T> {
  const res = await fetch(url, {
    headers: { "Content-Type": "application/json", ...options?.headers },
    ...options,
  });
  if (!res.ok) {
    const body = await res.text().catch(() => "");
    throw new ApiError(res.status, body || `Request failed: ${res.status}`);
  }
  if (res.status === 204) return undefined as T;
  return res.json();
}

/* ── Contacts ──────────────────────────────────────── */

export const contacts = {
  list(
    page = 1,
    pageSize = 20,
    search?: string,
    industry?: string,
  ): Promise<PagedResult<Contact>> {
    const params = new URLSearchParams({
      page: String(page),
      pageSize: String(pageSize),
    });
    if (search) params.set("search", search);
    if (industry) params.set("industry", industry);
    return request(`/api/contacts?${params}`);
  },

  get(id: number): Promise<ContactDetail> {
    return request(`/api/contacts/${id}`);
  },

  create(data: CreateContact): Promise<ContactDetail> {
    return request("/api/contacts", {
      method: "POST",
      body: JSON.stringify(data),
    });
  },

  update(id: number, data: UpdateContact): Promise<ContactDetail> {
    return request(`/api/contacts/${id}`, {
      method: "PUT",
      body: JSON.stringify(data),
    });
  },

  delete(id: number): Promise<void> {
    return request(`/api/contacts/${id}`, { method: "DELETE" });
  },

  importCsv(file: File): Promise<ContactImportResult> {
    const formData = new FormData();
    formData.append("file", file);
    return request("/api/contacts/import", {
      method: "POST",
      headers: {},
      body: formData,
    });
  },
};

/* ── Segments ──────────────────────────────────────── */

export const segments = {
  list(): Promise<Segment[]> {
    return request("/api/segments");
  },

  get(id: number): Promise<SegmentDetail> {
    return request(`/api/segments/${id}`);
  },

  create(data: CreateSegment): Promise<SegmentDetail> {
    return request("/api/segments", {
      method: "POST",
      body: JSON.stringify(data),
    });
  },

  update(id: number, data: CreateSegment): Promise<SegmentDetail> {
    return request(`/api/segments/${id}`, {
      method: "PUT",
      body: JSON.stringify(data),
    });
  },

  delete(id: number): Promise<void> {
    return request(`/api/segments/${id}`, { method: "DELETE" });
  },

  preview(rules: SegmentRule[]): Promise<SegmentPreview> {
    return request("/api/segments/preview", {
      method: "POST",
      body: JSON.stringify({ name: "_preview", rules }),
    });
  },
};

/* ── Campaigns ─────────────────────────────────────── */

export const campaigns = {
  list(): Promise<Campaign[]> {
    return request("/api/campaigns");
  },

  get(id: number): Promise<CampaignDetail> {
    return request(`/api/campaigns/${id}`);
  },

  create(data: CreateCampaign): Promise<CampaignDetail> {
    return request("/api/campaigns", {
      method: "POST",
      body: JSON.stringify(data),
    });
  },

  update(id: number, data: CreateCampaign): Promise<CampaignDetail> {
    return request(`/api/campaigns/${id}`, {
      method: "PUT",
      body: JSON.stringify(data),
    });
  },

  send(id: number): Promise<CampaignDetail> {
    return request(`/api/campaigns/${id}/send`, { method: "POST" });
  },

  stats(id: number): Promise<CampaignStats> {
    return request(`/api/campaigns/${id}/stats`);
  },
};

/* ── Journeys ──────────────────────────────────────── */

export const journeys = {
  list(): Promise<Journey[]> {
    return request("/api/journeys");
  },

  get(id: number): Promise<JourneyDetail> {
    return request(`/api/journeys/${id}`);
  },

  create(data: CreateJourney): Promise<JourneyDetail> {
    return request("/api/journeys", {
      method: "POST",
      body: JSON.stringify(data),
    });
  },

  update(id: number, data: CreateJourney): Promise<JourneyDetail> {
    return request(`/api/journeys/${id}`, {
      method: "PUT",
      body: JSON.stringify(data),
    });
  },

  activate(id: number): Promise<JourneyDetail> {
    return request(`/api/journeys/${id}/activate`, { method: "POST" });
  },

  deactivate(id: number): Promise<JourneyDetail> {
    return request(`/api/journeys/${id}/deactivate`, { method: "POST" });
  },

  stats(id: number): Promise<JourneyStats> {
    return request(`/api/journeys/${id}/stats`);
  },
};

/* ── Analytics ─────────────────────────────────────── */

export const analytics = {
  overview(): Promise<Overview> {
    return request("/api/analytics/overview");
  },

  engagement(days = 30): Promise<EngagementTrend[]> {
    return request(`/api/analytics/engagement?days=${days}`);
  },
};

/* ── AI ────────────────────────────────────────────── */

export const ai = {
  suggestSubjects(
    draftSubject: string,
    campaignContext?: string,
  ): Promise<SubjectSuggestionResponse> {
    return request("/api/ai/subject-suggestions", {
      method: "POST",
      body: JSON.stringify({ draftSubject, campaignContext }),
    });
  },

  sendTime(segmentId?: number): Promise<SendTimeRecommendation> {
    const path = segmentId ? `/api/ai/send-time/${segmentId}` : "/api/ai/send-time";
    return request(path);
  },
};
