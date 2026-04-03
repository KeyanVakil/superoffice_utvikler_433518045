import { useMemo, useRef, useEffect } from "react";

interface CampaignPreviewProps {
  htmlBody: string;
}

const SAMPLE_DATA: Record<string, string> = {
  "{{firstName}}": "Alex",
  "{{lastName}}": "Johnson",
  "{{email}}": "alex.johnson@example.com",
  "{{company}}": "Acme Corp",
  "{{industry}}": "Technology",
};

function substituteTokens(html: string): string {
  let result = html;
  for (const [token, value] of Object.entries(SAMPLE_DATA)) {
    result = result.replaceAll(token, value);
  }
  return result;
}

export default function CampaignPreview({ htmlBody }: CampaignPreviewProps) {
  const iframeRef = useRef<HTMLIFrameElement>(null);
  const rendered = useMemo(() => substituteTokens(htmlBody), [htmlBody]);

  useEffect(() => {
    const iframe = iframeRef.current;
    if (!iframe) return;
    const doc = iframe.contentDocument;
    if (!doc) return;
    doc.open();
    doc.write(rendered);
    doc.close();

    /* Auto-resize iframe to content height */
    const resize = () => {
      if (doc.body) {
        iframe.style.height = doc.body.scrollHeight + 20 + "px";
      }
    };
    resize();
    const timer = setTimeout(resize, 200);
    return () => clearTimeout(timer);
  }, [rendered]);

  if (!htmlBody.trim()) {
    return (
      <div className="html-preview" style={{ color: "var(--color-gray-400)" }}>
        Enter HTML content in the editor to see a preview here.
      </div>
    );
  }

  return (
    <div className="html-preview">
      <div
        style={{
          marginBottom: 10,
          fontSize: 12,
          color: "var(--color-gray-500)",
        }}
      >
        Preview with sample data: {Object.entries(SAMPLE_DATA).map(([k, v]) => `${k}="${v}"`).join(", ")}
      </div>
      <iframe
        ref={iframeRef}
        title="Campaign Preview"
        sandbox="allow-same-origin"
        style={{ width: "100%", border: "1px solid var(--color-gray-200)", borderRadius: 6, minHeight: 200 }}
      />
    </div>
  );
}
