import { NavLink } from "react-router-dom";
import type { ReactNode } from "react";

interface LayoutProps {
  children: ReactNode;
}

const NAV_ITEMS = [
  { to: "/analytics", label: "Analytics", icon: "\u2261" },
  { to: "/contacts", label: "Contacts", icon: "\u263A" },
  { to: "/segments", label: "Segments", icon: "\u25CB" },
  { to: "/campaigns", label: "Campaigns", icon: "\u2709" },
  { to: "/journeys", label: "Journeys", icon: "\u21A0" },
] as const;

export default function Layout({ children }: LayoutProps) {
  return (
    <div className="app-layout">
      <aside className="sidebar">
        <div className="sidebar-brand">
          <span className="brand-icon">M</span>
          <span className="brand-text">MarketFlow</span>
        </div>
        <nav className="sidebar-nav">
          {NAV_ITEMS.map(({ to, label, icon }) => (
            <NavLink
              key={to}
              to={to}
              className={({ isActive }) =>
                `nav-item${isActive ? " nav-item--active" : ""}`
              }
            >
              <span className="nav-icon">{icon}</span>
              <span className="nav-label">{label}</span>
            </NavLink>
          ))}
        </nav>
      </aside>
      <main className="main-content">{children}</main>
    </div>
  );
}
