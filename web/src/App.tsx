import { Routes, Route, Navigate } from "react-router-dom";
import Layout from "./components/Layout";
import Dashboard from "./components/analytics/Dashboard";
import ContactList from "./components/contacts/ContactList";
import SegmentList from "./components/segments/SegmentList";
import SegmentBuilder from "./components/segments/SegmentBuilder";
import CampaignList from "./components/campaigns/CampaignList";
import CampaignEditor from "./components/campaigns/CampaignEditor";
import JourneyList from "./components/journeys/JourneyList";
import JourneyDesigner from "./components/journeys/JourneyDesigner";

export default function App() {
  return (
    <Layout>
      <Routes>
        <Route path="/" element={<Navigate to="/analytics" replace />} />
        <Route path="/analytics" element={<Dashboard />} />
        <Route path="/contacts" element={<ContactList />} />
        <Route path="/segments" element={<SegmentList />} />
        <Route path="/segments/new" element={<SegmentBuilder />} />
        <Route path="/segments/:id" element={<SegmentBuilder />} />
        <Route path="/campaigns" element={<CampaignList />} />
        <Route path="/campaigns/new" element={<CampaignEditor />} />
        <Route path="/campaigns/:id" element={<CampaignEditor />} />
        <Route path="/journeys" element={<JourneyList />} />
        <Route path="/journeys/new" element={<JourneyDesigner />} />
        <Route path="/journeys/:id" element={<JourneyDesigner />} />
      </Routes>
    </Layout>
  );
}
