/* ── Contact ────────────────────────────────────────── */

export interface Contact {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  company: string | null;
  industry: string | null;
  tags: string[];
  engagementScore: number;
  lastActivityAt: string | null;
}

export interface ContactDetail extends Contact {
  createdAt: string;
  updatedAt: string;
  activityTimeline: ActivityEvent[];
}

export interface ActivityEvent {
  id: number;
  eventType: string;
  occurredAt: string;
  campaignId: number | null;
  campaignName: string | null;
  metadata: string | null;
}

export interface CreateContact {
  firstName: string;
  lastName: string;
  email: string;
  company?: string | null;
  industry?: string | null;
  tags?: string[] | null;
}

export interface UpdateContact extends CreateContact {}

export interface ContactImportResult {
  imported: number;
  skipped: number;
  errors: string[];
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

/* ── Segment ───────────────────────────────────────── */

export interface Segment {
  id: number;
  name: string;
  description: string | null;
  createdAt: string;
}

export interface SegmentDetail extends Segment {
  rules: SegmentRule[];
}

export interface CreateSegment {
  name: string;
  description?: string | null;
  rules?: SegmentRule[] | null;
}

export interface SegmentRule {
  groupIndex: number;
  field: string;
  operator: string;
  value: string;
}

export interface SegmentPreview {
  segmentId: number;
  matchingCount: number;
  sampleContacts: Contact[];
}

/* ── Campaign ──────────────────────────────────────── */

export interface Campaign {
  id: number;
  name: string;
  subject: string;
  status: string;
  segmentName: string | null;
  sentAt: string | null;
  sendCount: number;
  openRate: number;
  clickRate: number;
}

export interface CampaignDetail {
  id: number;
  name: string;
  subject: string;
  htmlBody: string;
  status: string;
  segmentId: number | null;
  segmentName: string | null;
  sentAt: string | null;
  createdAt: string;
  sendCount: number;
  openRate: number;
  clickRate: number;
}

export interface CreateCampaign {
  name: string;
  subject: string;
  htmlBody: string;
  segmentId?: number | null;
}

export interface CampaignStats {
  campaignId: number;
  totalSent: number;
  totalOpens: number;
  totalClicks: number;
  openRate: number;
  clickThroughRate: number;
  timeline: DayStats[];
}

export interface DayStats {
  date: string;
  sends: number;
  opens: number;
  clicks: number;
}

/* ── Journey ───────────────────────────────────────── */

export interface Journey {
  id: number;
  name: string;
  triggerType: string;
  isActive: boolean;
  createdAt: string;
  enrolledCount: number;
  completedCount: number;
}

export interface JourneyDetail {
  id: number;
  name: string;
  triggerType: string;
  triggerConfig: Record<string, unknown> | null;
  isActive: boolean;
  createdAt: string;
  steps: JourneyStep[];
  enrolledCount: number;
  completedCount: number;
}

export interface CreateJourney {
  name: string;
  triggerType: string;
  triggerConfig?: Record<string, unknown> | null;
  steps?: CreateJourneyStep[] | null;
}

export interface JourneyStep {
  id: number;
  stepOrder: number;
  stepType: string;
  config: Record<string, unknown> | null;
  trueNextStepId: number | null;
  falseNextStepId: number | null;
}

export interface CreateJourneyStep {
  stepOrder: number;
  stepType: string;
  config?: Record<string, unknown> | null;
}

export interface JourneyStats {
  journeyId: number;
  totalEnrolled: number;
  active: number;
  completed: number;
  exited: number;
  stepStats: StepStats[];
}

export interface StepStats {
  stepId: number;
  stepOrder: number;
  stepType: string;
  reached: number;
  completed: number;
}

/* ── Analytics ─────────────────────────────────────── */

export interface Overview {
  totalContacts: number;
  activeCampaigns: number;
  activeJourneys: number;
  overallEngagementRate: number;
  recentCampaigns: Campaign[];
}

export interface EngagementTrend {
  date: string;
  sends: number;
  opens: number;
  clicks: number;
}

/* ── AI ────────────────────────────────────────────── */

export interface SubjectSuggestion {
  subject: string;
  reason: string;
}

export interface SubjectSuggestionResponse {
  suggestions: SubjectSuggestion[];
}

export interface SendTimeRecommendation {
  recommendedHour: number;
  recommendedDay: string;
  reason: string;
}
