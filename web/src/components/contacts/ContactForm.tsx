import { useState, useCallback } from "react";
import { contacts as api } from "../../api/client";
import type { ContactDetail, CreateContact } from "../../types";

interface ContactFormProps {
  contact?: ContactDetail | null;
  onSaved: () => void;
  onCancel: () => void;
}

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

interface FormErrors {
  firstName?: string;
  lastName?: string;
  email?: string;
}

function validateEmail(email: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}

export default function ContactForm({
  contact,
  onSaved,
  onCancel,
}: ContactFormProps) {
  const [firstName, setFirstName] = useState(contact?.firstName ?? "");
  const [lastName, setLastName] = useState(contact?.lastName ?? "");
  const [email, setEmail] = useState(contact?.email ?? "");
  const [company, setCompany] = useState(contact?.company ?? "");
  const [industry, setIndustry] = useState(contact?.industry ?? "");
  const [tagInput, setTagInput] = useState("");
  const [tags, setTags] = useState<string[]>(contact?.tags ?? []);
  const [errors, setErrors] = useState<FormErrors>({});
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const validate = useCallback((): boolean => {
    const errs: FormErrors = {};
    if (!firstName.trim()) errs.firstName = "First name is required";
    if (!lastName.trim()) errs.lastName = "Last name is required";
    if (!email.trim()) errs.email = "Email is required";
    else if (!validateEmail(email)) errs.email = "Invalid email format";
    setErrors(errs);
    return Object.keys(errs).length === 0;
  }, [firstName, lastName, email]);

  const handleAddTag = useCallback(
    (e: React.KeyboardEvent) => {
      if (e.key === "Enter" || e.key === ",") {
        e.preventDefault();
        const tag = tagInput.trim().replace(/,/g, "");
        if (tag) {
          setTags((prev) => (prev.includes(tag) ? prev : [...prev, tag]));
        }
        setTagInput("");
      }
    },
    [tagInput],
  );

  const handleRemoveTag = useCallback((tag: string) => {
    setTags((prev) => prev.filter((t) => t !== tag));
  }, []);

  const handleSubmit = useCallback(
    async (e: React.FormEvent) => {
      e.preventDefault();
      if (!validate()) return;

      setSubmitting(true);
      setSubmitError(null);

      const payload: CreateContact = {
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        email: email.trim(),
        company: company.trim() || null,
        industry: industry || null,
        tags: tags.length > 0 ? tags : null,
      };

      try {
        if (contact) {
          await api.update(contact.id, payload);
        } else {
          await api.create(payload);
        }
        onSaved();
      } catch (err) {
        setSubmitError(err instanceof Error ? err.message : String(err));
      } finally {
        setSubmitting(false);
      }
    },
    [
      validate,
      firstName,
      lastName,
      email,
      company,
      industry,
      tags,
      contact,
      onSaved,
    ],
  );

  return (
    <form onSubmit={handleSubmit}>
      {submitError && <div className="error-banner">{submitError}</div>}

      <div className="form-row">
        <div className="form-group">
          <label>First Name *</label>
          <input
            className={`form-control${errors.firstName ? " form-control--error" : ""}`}
            value={firstName}
            onChange={(e) => setFirstName(e.target.value)}
          />
          {errors.firstName && (
            <div className="form-error">{errors.firstName}</div>
          )}
        </div>
        <div className="form-group">
          <label>Last Name *</label>
          <input
            className={`form-control${errors.lastName ? " form-control--error" : ""}`}
            value={lastName}
            onChange={(e) => setLastName(e.target.value)}
          />
          {errors.lastName && (
            <div className="form-error">{errors.lastName}</div>
          )}
        </div>
      </div>

      <div className="form-group">
        <label>Email *</label>
        <input
          className={`form-control${errors.email ? " form-control--error" : ""}`}
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
        />
        {errors.email && <div className="form-error">{errors.email}</div>}
      </div>

      <div className="form-row">
        <div className="form-group">
          <label>Company</label>
          <input
            className="form-control"
            value={company}
            onChange={(e) => setCompany(e.target.value)}
          />
        </div>
        <div className="form-group">
          <label>Industry</label>
          <select
            className="form-control"
            value={industry}
            onChange={(e) => setIndustry(e.target.value)}
          >
            {INDUSTRIES.map((ind) => (
              <option key={ind} value={ind}>
                {ind || "-- Select --"}
              </option>
            ))}
          </select>
        </div>
      </div>

      <div className="form-group">
        <label>Tags</label>
        <div>
          {tags.map((tag) => (
            <span key={tag} className="tag">
              {tag}
              <span
                className="tag-remove"
                onClick={() => handleRemoveTag(tag)}
              >
                x
              </span>
            </span>
          ))}
        </div>
        <input
          className="form-control"
          placeholder="Type a tag and press Enter"
          value={tagInput}
          onChange={(e) => setTagInput(e.target.value)}
          onKeyDown={handleAddTag}
          style={{ marginTop: 6 }}
        />
      </div>

      <div className="form-actions">
        <button
          type="submit"
          className="btn btn-primary"
          disabled={submitting}
        >
          {submitting ? "Saving..." : contact ? "Update Contact" : "Create Contact"}
        </button>
        <button
          type="button"
          className="btn btn-secondary"
          onClick={onCancel}
        >
          Cancel
        </button>
      </div>
    </form>
  );
}
