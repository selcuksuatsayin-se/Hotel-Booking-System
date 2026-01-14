import { useEffect, useState } from "react";
import { useParams, useLocation } from "react-router-dom";
import { useIsAuthenticated, useMsal } from "@azure/msal-react";
import Navbar from "../components/Navbar";
import api from "../services/api";

export default function HotelDetailPage() {
    const { id } = useParams();
    const location = useLocation();
    const { accounts } = useMsal();
    const isAuthenticated = useIsAuthenticated();
    
    const [hotel, setHotel] = useState(null);
    const [loading, setLoading] = useState(true);
    
    // Guest Email State (Giriş yapmamışlar için)
    const [guestEmail, setGuestEmail] = useState("");

    const initialCheckIn = location.state?.checkIn || new Date().toISOString().split('T')[0];
    const initialCheckOut = location.state?.checkOut || new Date(new Date().setDate(new Date().getDate() + 1)).toISOString().split('T')[0];

    const [checkIn] = useState(initialCheckIn);
    const [checkOut] = useState(initialCheckOut);
    const [bookingStatus, setBookingStatus] = useState(null);

    useEffect(() => {
        const fetchHotel = async () => {
            try {
                const res = await api.get(`/hotel-service/api/v1/Hotels/${id}`, {
                    params: { checkIn, checkOut }
                });
                setHotel(res.data);
            } catch (err) { console.error(err); } 
            finally { setLoading(false); }
        };
        if (id) fetchHotel();
    }, [id, checkIn, checkOut]);

    const handleBook = async (roomId) => {
        // VALIDASYON: Giriş yapmadıysa Email zorunlu
        if (!isAuthenticated && !guestEmail) {
            alert("Please enter your email address for reservation confirmation.");
            return;
        }

        setBookingStatus("loading");

        const payload = {
            roomId: roomId,
            // Eğer giriş yaptıysa token'dan alacak (backend handle ediyor), yapmadıysa buradan gönderiyoruz
            guestEmail: isAuthenticated ? accounts[0].username : guestEmail, 
            checkInDate: checkIn,
            checkOutDate: checkOut
        };

        try {
            await api.post("/hotel-service/api/v1/Reservations", payload);
            setBookingStatus("success");
            alert(`Booking Successful!\n\n${isAuthenticated ? "You can check your reservations in the My Reservations page." : "A confirmation email has been sent to your address."}`);
        } catch (err) {
            console.error(err);
            setBookingStatus("error");
            alert("Booking Failed.");
        }
    };

    if (loading) return <div>Loading...</div>;
    if (!hotel) return <div>Not Found</div>;

    return (
        <div className="min-h-screen bg-gray-100">
            <Navbar />
            <div className="container mx-auto p-6 max-w-4xl">
                
                {/* Otel Header (Aynı kalabilir) */}
                <div className="bg-white rounded-lg shadow-md p-6 mb-6">
                    <h1 className="text-3xl font-bold text-gray-800">{hotel.name}</h1>
                    <p className="text-gray-600">📍 {hotel.city}, {hotel.country}</p>
                </div>

                {/* --- MİSAFİR İÇİN EMAİL ALANI --- */}
                {!isAuthenticated && (
                    <div className="bg-orange-50 border border-orange-200 rounded-lg p-6 mb-6">
                        <h3 className="text-lg font-bold text-orange-800 mb-2">Guest Booking</h3>
                        <p className="text-sm text-gray-600 mb-4">
                            You are not logged in. Please enter your email address for reservation details.
                            <span className="font-bold"> (Log in to get 10% discount)</span>
                        </p>
                        <input 
                            type="email" 
                            placeholder="name@example.com" 
                            className="w-full border p-3 rounded"
                            value={guestEmail}
                            onChange={(e) => setGuestEmail(e.target.value)}
                        />
                    </div>
                )}


                {/* --- YENİ TARİH GÖSTERİMİ (Select Box Yerine) --- */}
                <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-6 flex justify-between items-center">
                    <div>
                        <span className="text-blue-800 font-bold text-lg">📅 Reservation Dates:</span>
                        <span className="ml-3 text-gray-700 font-medium text-lg">
                            {checkIn.split('-').reverse().join('.')} — {checkOut.split('-').reverse().join('.')}
                        </span>
                    </div>
                </div>

                {/* Oda Listesi */}
                <div className="space-y-4">
                    {hotel.rooms?.map(room => {
                        // Frontend'de görsel olarak indirim hesabı (Sadece bilgi amaçlı)
                        const finalPrice = isAuthenticated ? (room.basePrice * 0.9).toFixed(2) : room.basePrice;

                        return (
                            <div key={room.id} className="bg-white p-6 rounded-lg shadow-md flex justify-between items-center border-l-4 border-blue-500">
                                <div>
                                    <h4 className="text-xl font-bold">{room.title}</h4>
                                    <p className="text-gray-500">Capacity: {room.capacity}</p>
                                    
                                    <div className="mt-2">
                                        {isAuthenticated ? (
                                            <>
                                                <span className="text-gray-400 line-through text-sm mr-2">€{room.basePrice}</span>
                                                <span className="text-green-600 font-bold text-2xl">€{finalPrice}</span>
                                                <span className="text-xs bg-green-100 text-green-800 px-2 py-1 rounded ml-2">Member Price</span>
                                            </>
                                        ) : (
                                            <span className="text-gray-800 font-bold text-2xl">€{room.basePrice}</span>
                                        )}
                                        <span className="text-sm text-gray-400"> / night</span>
                                    </div>
                                </div>
                                <button 
                                    onClick={() => handleBook(room.id)}
                                    className="bg-blue-600 text-white px-8 py-3 rounded-lg font-bold hover:bg-blue-700"
                                >
                                    Book Now
                                </button>
                            </div>
                        );
                    })}
                </div>
            </div>
        </div>
    );
}