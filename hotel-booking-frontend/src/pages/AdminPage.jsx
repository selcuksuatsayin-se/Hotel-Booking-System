import { useState, useEffect } from "react";
import { useIsAuthenticated, useMsal } from "@azure/msal-react";
import Navbar from "../components/Navbar"; 
import api from "../services/api";

export default function AdminPage() {
    const { accounts } = useMsal();
    const isAuthenticated = useIsAuthenticated();

    // --- GÜVENLİK STATE'LERİ ---
    const [isAdmin, setIsAdmin] = useState(false);
    const [isChecking, setIsChecking] = useState(true);

    // --- MEVCUT FORM STATE'LERİ ---
    const [loading, setLoading] = useState(false);
    const [hotels, setHotels] = useState([]);
    
    const [selectedHotelId, setSelectedHotelId] = useState("");
    const [startDate, setStartDate] = useState("");
    const [endDate, setEndDate] = useState("");
    const [roomType, setRoomType] = useState("Standard Room");
    const [roomCount, setRoomCount] = useState(5);
    const [price, setPrice] = useState(""); 
    const [status, setStatus] = useState("vacant"); 

    // *** ADMIN KONTROLÜ (UPDATE BURADA) ***
    // Buraya kendi Azure giriş mailini yazmalısın.
    const ADMIN_EMAILS = [
        "selcuk@example.com", 
        "admin@hotelsystem.com", 
        "senin_mail_adresin@outlook.com" // <-- KENDİ MAİLİNİ BURAYA EKLE
    ]; 

    useEffect(() => {
        const checkRole = () => {
            if (isAuthenticated && accounts[0]) {
                const userEmail = accounts[0].username.toLowerCase();
                const userRoles = accounts[0].idTokenClaims?.roles || [];

                // Kontrol: Ya Azure Rolü "Admin" olacak YA DA mail adresi listede olacak
                if (userRoles.includes("Admin") || ADMIN_EMAILS.includes(userEmail)) {
                    setIsAdmin(true);
                } else {
                    setIsAdmin(false);
                }
            }
            setIsChecking(false);
        };

        checkRole();
    }, [isAuthenticated, accounts]);

    // Otelleri Listeleme
    useEffect(() => {
        const fetchHotels = async () => {
            if (isAdmin) { // Sadece admin ise çek
                try {
                    const res = await api.get("/hotel-service/api/v1/Hotels");
                    setHotels(res.data);
                    if (res.data.length > 0) setSelectedHotelId(res.data[0].id);
                } catch (err) {
                    console.error("Hotels could not be loaded.", err);
                }
            }
        };
        if (!isChecking && isAdmin) fetchHotels();
    }, [isAdmin, isChecking]);

    // 1. ADIM: AI Tahmini Al
    const handlePredict = async () => {
        if (!startDate) { alert("Please choose a date first.."); return; }
        
        setLoading(true);
        try {
            const dateObj = new Date(startDate);
            const monthNames = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];
            const monthName = monthNames[dateObj.getMonth()];

            const res = await api.post("/hotel-service/api/v1/Prediction", {
                month: monthName,
                roomType: roomType.includes("Standard") ? "A" : "D",
                leadTime: 30,
                guests: 2
            });

            setPrice(res.data.suggested_price);
            
        } catch (err) {
            alert("Prediction service could not be reached. (Is ML Service running?)");
        } finally {
            setLoading(false);
        }
    };

    // 2. ADIM: Veritabanına Kaydet (Bulk Update)
    const handleSave = async () => {
        if (!selectedHotelId || !startDate || !endDate || !price) {
            alert("Please fill in all fields.");
            return;
        }

        setLoading(true);
        try {
            const payload = {
                hotelId: parseInt(selectedHotelId),
                roomTitle: roomType,
                startDate: startDate,
                endDate: endDate,
                stock: parseInt(roomCount),
                price: parseFloat(price),
                isVacant: status === "vacant"
            };

            await api.post("/hotel-service/api/v1/Availability/BulkUpdate", payload);
            alert("Update Successful! Date range processed.");
        } catch (err) {
            console.error(err);
            alert("Update failed.");
        } finally {
            setLoading(false);
        }
    };

    // --- RENDER AŞAMASI ---

    // 1. Kontrol ediliyor
    if (isChecking) return <div className="p-10 text-center font-bold text-gray-500">Yetkiler kontrol ediliyor...</div>;

    // 2. Giriş Yapılmamış
    if (!isAuthenticated) return (
        <div className="min-h-screen bg-gray-100 flex flex-col items-center justify-center">
            <Navbar />
            <div className="p-10 text-red-500 font-bold text-xl mt-10">Lütfen Admin Girişi Yapınız</div>
        </div>
    );

    // 3. Giriş Yapılmış ama ADMIN DEĞİL (Erişim Reddedildi)
    if (!isAdmin) return (
        <div className="min-h-screen bg-gray-100">
            <Navbar />
            <div className="flex flex-col items-center justify-center mt-20 p-6">
                <div className="bg-red-50 border-l-4 border-red-500 text-red-700 p-6 w-full max-w-2xl shadow-md rounded-r" role="alert">
                    <p className="font-bold text-xl mb-2">🚫 Erişim Reddedildi (Access Denied)</p>
                    <p>To view this page, you must have <strong>Admin</strong> privileges.</p>
                </div>
                <div className="mt-6 text-gray-500 text-sm text-center">
                    <p>Login account: <span className="font-bold text-gray-700">{accounts[0]?.username}</span></p>
                    <button 
                        onClick={() => window.location.href = "/"}
                        className="mt-4 bg-gray-800 text-white px-6 py-2 rounded hover:bg-gray-900 transition"
                    >
                        Ana Sayfaya Dön
                    </button>
                </div>
            </div>
        </div>
    );

    // 4. ADMIN (Sayfanın Orijinal Hali)
    return (
        <div className="min-h-screen bg-gray-50">
            {/* Admin Header */}
            <div className="bg-slate-800 text-white p-4 shadow-md flex justify-between items-center">
                <h1 className="text-xl font-bold tracking-wider">HOTEL ADMIN SERVICE</h1>
                <div className="flex items-center gap-4">
                     <span className="text-sm text-green-400 font-bold">Admin Access Granted</span>
                     <div className="text-sm text-gray-400">admin.hotels.com (Simulated)</div>
                </div>
            </div>

            <div className="container mx-auto p-6 max-w-4xl">
                
                {/* 1. Otel Seçimi */}
                <div className="bg-white p-6 rounded-lg shadow-sm border border-gray-200 mb-6">
                    <label className="block text-gray-700 font-bold mb-2">🏨 Select the hotel to be managed</label>
                    <select 
                        className="w-full border p-3 rounded bg-gray-50 text-lg"
                        value={selectedHotelId}
                        onChange={(e) => setSelectedHotelId(e.target.value)}
                    >
                        {hotels.map(h => (
                            <option key={h.id} value={h.id}>{h.name} ({h.city})</option>
                        ))}
                    </select>
                </div>

                {/* 2. Yönetim Formu */}
                <div className="bg-white p-8 rounded-lg shadow-md border border-gray-200">
                    <h2 className="text-2xl font-bold mb-6 text-gray-800 border-b pb-2">Availability and Pricing Management</h2>

                    {/* Tarihler */}
                    <div className="grid grid-cols-2 gap-6 mb-6">
                        <div>
                            <label className="block text-sm font-bold text-gray-600 mb-1">Start Date</label>
                            <input type="date" className="w-full border p-2 rounded" value={startDate} onChange={e => setStartDate(e.target.value)} />
                        </div>
                        <div>
                            <label className="block text-sm font-bold text-gray-600 mb-1">End Date</label>
                            <input type="date" className="w-full border p-2 rounded" value={endDate} onChange={e => setEndDate(e.target.value)} />
                        </div>
                    </div>

                    {/* Oda Bilgileri */}
                    <div className="grid grid-cols-2 gap-6 mb-6">
                        <div>
                            <label className="block text-sm font-bold text-gray-600 mb-1">Room Type</label>
                            <select className="w-full border p-2 rounded" value={roomType} onChange={e => setRoomType(e.target.value)}>
                                <option value="Standard Room">Standard Room</option>
                                <option value="Deluxe Room">Deluxe Room</option>
                            </select>
                        </div>
                        <div>
                            <label className="block text-sm font-bold text-gray-600 mb-1">Room Count (Stock)</label>
                            <input type="number" className="w-full border p-2 rounded" value={roomCount} onChange={e => setRoomCount(e.target.value)} />
                        </div>
                    </div>

                    {/* Fiyat ve AI Tahmini */}
                    <div className="mb-6">
                        <label className="block text-sm font-bold text-gray-600 mb-1">Nightly Price (€)</label>
                        <div className="flex gap-2">
                            <input 
                                type="number" 
                                className="flex-1 border p-2 rounded text-lg font-bold text-green-700" 
                                placeholder="Example: 150" 
                                value={price} 
                                onChange={e => setPrice(e.target.value)} 
                            />
                            <button 
                                onClick={handlePredict} 
                                disabled={loading}
                                className="bg-indigo-600 text-white px-4 py-2 rounded hover:bg-indigo-700 flex items-center shadow"
                            >
                                {loading ? "..." : "🤖 AI Predict"}
                            </button>
                        </div>
                        <p className="text-xs text-gray-400 mt-1">*AI button will write the recommended price to the input field, you can change it.</p>
                    </div>

                    {/* Durum (Status) */}
                    <div className="mb-8">
                        <label className="block text-sm font-bold text-gray-600 mb-2">Status</label>
                        <div className="flex gap-6">
                            <label className="flex items-center cursor-pointer">
                                <input 
                                    type="radio" 
                                    name="status" 
                                    checked={status === "vacant"} 
                                    onChange={() => setStatus("vacant")}
                                    className="w-5 h-5 text-green-600" 
                                />
                                <span className="ml-2 font-medium">Vacant - Available for Sale</span>
                            </label>
                            <label className="flex items-center cursor-pointer">
                                <input 
                                    type="radio" 
                                    name="status" 
                                    checked={status === "occupied"} 
                                    onChange={() => setStatus("occupied")}
                                    className="w-5 h-5 text-red-600" 
                                />
                                <span className="ml-2 font-medium">Occupied - Closed</span>
                            </label>
                        </div>
                    </div>

                    {/* Kaydet Butonu */}
                    <button 
                        onClick={handleSave} 
                        disabled={loading}
                        className="w-full bg-slate-900 text-white p-4 rounded-lg font-bold text-lg hover:bg-slate-800 transition shadow-lg"
                    >
                        {loading ? "Processing..." : "💾 Save Changes"}
                    </button>
                </div>
            </div>
        </div>
    );
}