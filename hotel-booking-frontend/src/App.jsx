import { Routes, Route } from "react-router-dom";
import SearchPage from "./pages/SearchPage";
import AdminPage from "./pages/AdminPage";
import HotelDetailPage from "./pages/HotelDetailPage";
import ProfilePage from "./pages/ProfilePage";

function App() {
  return (
    <Routes>
      <Route path="/" element={<SearchPage />} />
      <Route path="/admin" element={<AdminPage />} />
      <Route path="/hotel/:id" element={<HotelDetailPage />} />
      <Route path="/profile" element={<ProfilePage />} />
    </Routes>
  );
}
export default App;