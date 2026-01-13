import { useEffect, useState } from "react";
import { useMsal, useIsAuthenticated } from "@azure/msal-react";
import Navbar from "../components/Navbar";
import api from "../services/api";

export default function ProfilePage() {
    const { accounts } = useMsal();
    const isAuthenticated = useIsAuthenticated();
    const [reservations, setReservations] = useState([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const fetchReservations = async () => {
            if (isAuthenticated) {
                try {
                    // Backend artık emaili token'dan alıyor, parametre göndermiyoruz
                    const res = await api.get("/hotel-service/api/v1/Reservations/MyReservations");
                    setReservations(res.data);
                } catch (err) {
                    console.error("Failed to fetch reservations", err);
                } finally {
                    setLoading(false);
                }
            } else {
                setLoading(false);
            }
        };

        fetchReservations();
    }, [isAuthenticated]);

    if (!isAuthenticated) return (
        <div className="min-h-screen bg-gray-100">
            <Navbar />
            <div className="flex flex-col items-center justify-center mt-20">
                <h2 className="text-2xl font-bold text-gray-700">Guest Access Restricted</h2>
                <p className="text-gray-500 mt-2">Please login to view your reservation history.</p>
            </div>
        </div>
    );

    return (
        <div className="min-h-screen bg-gray-50">
            <Navbar />
            <div className="container mx-auto p-6 max-w-4xl">
                <div className="bg-white p-6 rounded-lg shadow-sm border border-gray-200 mb-6 flex items-center justify-between">
                    <div>
                        <h1 className="text-2xl font-bold text-gray-800">My Reservations</h1>
                        <p className="text-gray-500">Welcome back, {accounts[0]?.name}</p>
                    </div>
                </div>

                {loading ? <p>Loading...</p> : reservations.length > 0 ? (
                    <div className="space-y-4">
                        {reservations.map((res) => (
                            <div key={res.id} className="bg-white p-6 rounded-lg shadow-md border-l-4 border-blue-500 flex justify-between items-center">
                                <div>
                                    <h3 className="text-lg font-bold">{res.hotelName}</h3>
                                    <p className="text-gray-500 text-sm">{res.city} • {res.roomTitle}</p>
                                    <p className="text-sm mt-1">📅 {new Date(res.checkIn).toLocaleDateString()} - {new Date(res.checkOut).toLocaleDateString()}</p>
                                </div>
                                <div className="text-right">
                                    <p className="text-xl font-bold text-green-600">€{res.totalPrice}</p>
                                    <span className="bg-green-100 text-green-800 text-xs px-2 py-1 rounded">{res.status}</span>
                                </div>
                            </div>
                        ))}
                    </div>
                ) : (
                    <div className="text-center p-10 bg-white rounded shadow-sm">
                        <p className="text-gray-500">You have no active reservations.</p>
                    </div>
                )}
            </div>
        </div>
    );
}