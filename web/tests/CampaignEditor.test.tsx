import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import CampaignEditor from "../src/components/campaigns/CampaignEditor";

/* ── Mocks ─────────────────────────────────────────── */

const mockCreate = vi.fn().mockResolvedValue({ id: 1 });
const mockSuggestSubjects = vi.fn().mockResolvedValue({
  suggestions: [
    { subject: "Boost Your Sales Today", reason: "Action-oriented" },
    { subject: "Exclusive Offer Inside", reason: "Creates urgency" },
    { subject: "Don't Miss Out", reason: "FOMO effect" },
  ],
});

vi.mock("../src/api/client", () => ({
  campaigns: {
    list: vi.fn().mockResolvedValue([]),
    get: vi.fn().mockResolvedValue({
      id: 1,
      name: "Test",
      subject: "Hello",
      htmlBody: "<p>Hi</p>",
      status: "Draft",
      segmentId: null,
      segmentName: null,
      sentAt: null,
      createdAt: "2026-01-01",
      sendCount: 0,
      openRate: 0,
      clickRate: 0,
    }),
    create: (...args: unknown[]) => mockCreate(...args),
    update: vi.fn().mockResolvedValue({ id: 1 }),
    send: vi.fn().mockResolvedValue({ id: 1 }),
    stats: vi.fn().mockResolvedValue({}),
  },
  segments: {
    list: vi.fn().mockResolvedValue([
      { id: 1, name: "VIP", description: null, createdAt: "2026-01-01" },
    ]),
  },
  ai: {
    suggestSubjects: (...args: unknown[]) => mockSuggestSubjects(...args),
    sendTime: vi.fn().mockResolvedValue({}),
  },
}));

const mockNavigate = vi.fn();
vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual("react-router-dom");
  return {
    ...actual,
    useNavigate: () => mockNavigate,
    useParams: () => ({}),
  };
});

function renderEditor() {
  return render(
    <MemoryRouter>
      <CampaignEditor />
    </MemoryRouter>,
  );
}

/* ── Tests ─────────────────────────────────────────── */

describe("CampaignEditor", () => {
  beforeEach(() => {
    mockNavigate.mockClear();
    mockCreate.mockClear();
    mockSuggestSubjects.mockClear();
  });

  it("renders the create campaign form", async () => {
    renderEditor();
    expect(screen.getByText("Create Campaign")).toBeTruthy();
    expect(screen.getByPlaceholderText("e.g., Spring Product Launch")).toBeTruthy();
    expect(screen.getByPlaceholderText("Enter email subject...")).toBeTruthy();
  });

  it("shows personalization token hints", async () => {
    renderEditor();
    await waitFor(() => {
      expect(screen.getByText("{{firstName}}")).toBeTruthy();
      expect(screen.getByText("{{company}}")).toBeTruthy();
      expect(screen.getByText("{{lastName}}")).toBeTruthy();
    });
  });

  it("validates required fields on save", async () => {
    renderEditor();
    const saveBtn = screen.getByText("Save Draft");
    await userEvent.click(saveBtn);

    await waitFor(() => {
      expect(screen.getByText("Campaign name is required")).toBeTruthy();
      expect(screen.getByText("Subject is required")).toBeTruthy();
    });
    expect(mockCreate).not.toHaveBeenCalled();
  });

  it("does not show validation errors when fields are filled", async () => {
    renderEditor();

    await userEvent.type(
      screen.getByPlaceholderText("e.g., Spring Product Launch"),
      "My Campaign",
    );
    await userEvent.type(
      screen.getByPlaceholderText("Enter email subject..."),
      "Welcome!",
    );

    const saveBtn = screen.getByText("Save Draft");
    await userEvent.click(saveBtn);

    await waitFor(() => {
      expect(mockCreate).toHaveBeenCalledWith({
        name: "My Campaign",
        subject: "Welcome!",
        htmlBody: "",
        segmentId: null,
      });
    });
  });

  it("shows AI suggestion dropdown and picks a suggestion", async () => {
    renderEditor();

    // Type a subject first
    await userEvent.type(
      screen.getByPlaceholderText("Enter email subject..."),
      "Check this out",
    );

    // Click AI Suggest button
    const suggestBtn = screen.getByText("AI Suggest");
    await userEvent.click(suggestBtn);

    // Wait for suggestions to load
    await waitFor(() => {
      expect(screen.getByText("Boost Your Sales Today")).toBeTruthy();
      expect(screen.getByText("Action-oriented")).toBeTruthy();
    });

    expect(mockSuggestSubjects).toHaveBeenCalledWith(
      "Check this out",
      undefined,
    );

    // Click a suggestion
    await userEvent.click(screen.getByText("Boost Your Sales Today"));

    // Subject field should update
    const subjectInput = screen.getByPlaceholderText("Enter email subject...") as HTMLInputElement;
    expect(subjectInput.value).toBe("Boost Your Sales Today");
  });

  it("disables AI Suggest when subject is empty", () => {
    renderEditor();
    const suggestBtn = screen.getByText("AI Suggest");
    expect(suggestBtn).toBeDisabled();
  });

  it("inserts a personalization token when clicked", async () => {
    renderEditor();
    const bodyTextarea = screen.getByPlaceholderText(
      "<h1>Hello {{firstName}},</h1><p>Your content here...</p>",
    ) as HTMLTextAreaElement;

    // Click a token hint
    await userEvent.click(screen.getByText("{{firstName}}"));

    expect(bodyTextarea.value).toBe("{{firstName}}");
  });

  it("switches between Edit and Preview tabs", async () => {
    renderEditor();

    // Start on Edit tab
    expect(screen.getByPlaceholderText("e.g., Spring Product Launch")).toBeTruthy();

    // Switch to Preview
    await userEvent.click(screen.getByText("Preview"));
    expect(
      screen.getByText("Enter HTML content in the editor to see a preview here."),
    ).toBeTruthy();

    // Switch back to Edit
    await userEvent.click(screen.getByText("Edit"));
    expect(screen.getByPlaceholderText("e.g., Spring Product Launch")).toBeTruthy();
  });

  it("loads segment options into the dropdown", async () => {
    renderEditor();
    await waitFor(() => {
      expect(screen.getByText("VIP")).toBeTruthy();
    });
  });
});
