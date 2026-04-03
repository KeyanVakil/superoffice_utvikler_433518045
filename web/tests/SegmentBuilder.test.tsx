import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import SegmentBuilder, {
  toApiRules,
  fromApiRules,
  makeDefaultRule,
  FIELD_DEFS,
  type RuleGroup,
} from "../src/components/segments/SegmentBuilder";

/* ── Mock API ──────────────────────────────────────── */

vi.mock("../src/api/client", () => ({
  segments: {
    list: vi.fn().mockResolvedValue([]),
    get: vi.fn().mockResolvedValue({
      id: 1,
      name: "Test Segment",
      description: "desc",
      createdAt: "2026-01-01",
      rules: [],
    }),
    create: vi.fn().mockResolvedValue({ id: 1 }),
    update: vi.fn().mockResolvedValue({ id: 1 }),
    delete: vi.fn().mockResolvedValue(undefined),
    preview: vi.fn().mockResolvedValue({ matchingCount: 5, sampleContacts: [] }),
  },
}));

/* Mock useNavigate */
const mockNavigate = vi.fn();
vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual("react-router-dom");
  return {
    ...actual,
    useNavigate: () => mockNavigate,
    useParams: () => ({}),
  };
});

function renderBuilder() {
  return render(
    <MemoryRouter>
      <SegmentBuilder />
    </MemoryRouter>,
  );
}

/* ── Unit tests for rule conversion ────────────────── */

describe("toApiRules", () => {
  it("converts local rule groups to flat API rules with correct groupIndex", () => {
    const groups: RuleGroup[] = [
      { rules: [{ field: "industry", operator: "equals", value: "Tech" }] },
      {
        rules: [
          { field: "company", operator: "contains", value: "Acme" },
          { field: "tag", operator: "has", value: "vip" },
        ],
      },
    ];

    const result = toApiRules(groups);

    expect(result).toHaveLength(3);
    expect(result[0]).toEqual({
      groupIndex: 0,
      field: "industry",
      operator: "equals",
      value: "Tech",
    });
    expect(result[1].groupIndex).toBe(1);
    expect(result[2].groupIndex).toBe(1);
    expect(result[2].field).toBe("tag");
  });
});

describe("fromApiRules", () => {
  it("groups API rules by groupIndex into RuleGroups", () => {
    const apiRules = [
      { groupIndex: 0, field: "industry", operator: "equals", value: "Finance" },
      { groupIndex: 1, field: "tag", operator: "has", value: "lead" },
      { groupIndex: 1, field: "engagementScore", operator: "greater_than", value: "0.5" },
    ];

    const result = fromApiRules(apiRules);

    expect(result).toHaveLength(2);
    expect(result[0].rules).toHaveLength(1);
    expect(result[0].rules[0].field).toBe("industry");
    expect(result[1].rules).toHaveLength(2);
  });

  it("returns a default group if rules are empty", () => {
    const result = fromApiRules([]);
    expect(result).toHaveLength(1);
    expect(result[0].rules).toHaveLength(1);
    expect(result[0].rules[0].field).toBe("industry");
  });
});

describe("makeDefaultRule", () => {
  it("creates a rule with industry field and empty value", () => {
    const rule = makeDefaultRule();
    expect(rule.field).toBe("industry");
    expect(rule.operator).toBe("equals");
    expect(rule.value).toBe("");
  });
});

describe("FIELD_DEFS operator availability", () => {
  it("industry field has equals and not_equals operators", () => {
    const ops = FIELD_DEFS.industry.operators.map((o) => o.value);
    expect(ops).toContain("equals");
    expect(ops).toContain("not_equals");
  });

  it("engagementScore has numeric operators", () => {
    const ops = FIELD_DEFS.engagementScore.operators.map((o) => o.value);
    expect(ops).toContain("greater_than");
    expect(ops).toContain("less_than");
  });

  it("tag field has has and not_has operators", () => {
    const ops = FIELD_DEFS.tag.operators.map((o) => o.value);
    expect(ops).toContain("has");
    expect(ops).toContain("not_has");
  });

  it("lastActivity has date-oriented operators", () => {
    const ops = FIELD_DEFS.lastActivity.operators.map((o) => o.value);
    expect(ops).toContain("before");
    expect(ops).toContain("after");
    expect(ops).toContain("within_days");
  });
});

/* ── Component integration tests ───────────────────── */

describe("SegmentBuilder component", () => {
  beforeEach(() => {
    mockNavigate.mockClear();
  });

  it("renders the create segment form", () => {
    renderBuilder();
    expect(screen.getByRole("heading", { name: "Create Segment" })).toBeTruthy();
    expect(screen.getByPlaceholderText("e.g., High-value Tech Contacts")).toBeTruthy();
  });

  it("has an Add Group (OR) button", () => {
    renderBuilder();
    expect(screen.getByText("+ Add Group (OR)")).toBeTruthy();
  });

  it("has an Add Condition button within the first group", () => {
    renderBuilder();
    expect(screen.getByText("+ Add Condition")).toBeTruthy();
  });

  it("adds a new rule group when clicking Add Group", async () => {
    renderBuilder();
    const addGroupBtn = screen.getByText("+ Add Group (OR)");
    await userEvent.click(addGroupBtn);
    // Should now show OR divider and Group 2
    expect(screen.getByText("OR")).toBeTruthy();
    expect(screen.getByText("Group 2 (AND)")).toBeTruthy();
  });

  it("adds a new rule within a group when clicking Add Condition", async () => {
    renderBuilder();
    const addBtn = screen.getByText("+ Add Condition");
    await userEvent.click(addBtn);
    // Should now have 2 field dropdowns in group 1
    const fieldSelects = screen.getAllByDisplayValue("Industry");
    expect(fieldSelects.length).toBeGreaterThanOrEqual(2);
  });

  it("removes a rule when clicking x", async () => {
    renderBuilder();
    // Add a second rule first
    await userEvent.click(screen.getByText("+ Add Condition"));
    const removeButtons = screen.getAllByText("x");
    // Click the first x button to remove
    await userEvent.click(removeButtons[0]);
    // Should be back to 1 rule
    const fieldSelects = screen.getAllByDisplayValue("Industry");
    expect(fieldSelects).toHaveLength(1);
  });

  it("changes available operators when field changes", async () => {
    renderBuilder();
    // Default field is "industry" with "equals" operator
    const fieldSelect = screen.getByDisplayValue("Industry");
    await userEvent.selectOptions(fieldSelect, "engagementScore");

    // Now operator dropdown should have "greater than"
    await waitFor(() => {
      expect(screen.getByDisplayValue("greater than")).toBeTruthy();
    });
  });

  it("disables Create Segment button when name is empty", () => {
    renderBuilder();
    const createBtn = screen.getByRole("button", { name: /Create Segment/i });
    expect(createBtn).toBeDisabled();
  });

  it("enables Create Segment button when name is provided", async () => {
    renderBuilder();
    const nameInput = screen.getByPlaceholderText("e.g., High-value Tech Contacts");
    await userEvent.type(nameInput, "My Segment");
    const createBtn = screen.getByRole("button", { name: /Create Segment/i });
    expect(createBtn).not.toBeDisabled();
  });
});
