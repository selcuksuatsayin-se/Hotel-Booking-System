import { Link } from "react-router-dom";
import { useMsal, useIsAuthenticated } from "@azure/msal-react";
import { loginRequest } from "../config/authConfig";

export default function Navbar() {
    const { instance, accounts } = useMsal();
    const isAuthenticated = useIsAuthenticated();

    const handleLogin = () => instance.loginPopup(loginRequest).catch(e => console.error(e));
    const handleLogout = () => instance.logoutPopup().catch(e => console.error(e));

    // --- ADMIN KONTROLÜ ---
    const ADMIN_EMAILS = ["selcuk@ornek.com", "admin@hotelsystem.com", "senin_azure_mailin@outlook.com"]; // Burayı kendi mailinle güncelle
    
    let isAdmin = false;
    if (isAuthenticated && accounts[0]) {
        const userEmail = accounts[0].username.toLowerCase();
        const userRoles = accounts[0].idTokenClaims?.roles || [];
        if (userRoles.includes("Admin") || ADMIN_EMAILS.includes(userEmail)) {
            isAdmin = true;
        }
    }
    // ----------------------

    return (
        <nav className="bg-blue-600 p-4 shadow-md text-white sticky top-0 z-50">
            <div className="container mx-auto flex justify-between items-center">
                <Link to="/" className="text-2xl font-bold flex items-center">
                    🏨 HotelSystem
                </Link>
                
                <div className="space-x-6 flex items-center font-medium">
                    <Link to="/" className="hover:text-blue-200 transition">Search</Link>
                    
                    {/* SADECE ADMIN İSE GÖSTER */}
                    {isAdmin && (
                        <Link to="/admin" className="text-yellow-300 hover:text-yellow-100 font-bold border border-yellow-300 px-2 py-1 rounded transition">
                            Admin Panel
                        </Link>
                    )}
                    
                    {isAuthenticated ? (
                        <>
                            <Link to="/profile" className="hover:text-blue-200 transition flex items-center gap-1 bg-blue-700 px-3 py-1 rounded">
                                📅 My Bookings
                            </Link>
                            
                            <div className="flex items-center gap-4 border-l border-blue-500 pl-4 ml-2">
                                <span className="text-sm text-blue-100 hidden md:block">
                                    {accounts[0]?.name}
                                </span>
                                <button onClick={handleLogout} className="bg-red-500 px-4 py-2 rounded hover:bg-red-600 text-sm transition shadow">
                                    Logout
                                </button>
                            </div>
                        </>
                    ) : (
                        <button onClick={handleLogin} className="bg-green-500 px-4 py-2 rounded hover:bg-green-600 font-bold shadow transition flex flex-col items-center leading-tight">
                            <span>Login</span>
                            <span className="text-[10px] font-normal">%10 Discount</span>
                        </button>
                    )}
                </div>
            </div>
        </nav>
    );
}