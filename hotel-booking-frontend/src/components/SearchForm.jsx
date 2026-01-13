import { useState } from "react";

export default function SearchForm({ onSearch }) {
    // Tarihleri boş string ("") olarak başlatıyoruz
    const [city, setCity] = useState("");
    const [checkIn, setCheckIn] = useState(""); 
    const [checkOut, setCheckOut] = useState("");
    const [guests, setGuests] = useState(2);

    const handleSubmit = () => {
        // Basit validasyon: Tarih seçilmemişse uyarı verebiliriz
        if (!city && !checkIn && !checkOut) {
            alert("Lütfen arama kriteri giriniz.");
            return;
        }
        
        onSearch({
            city,
            checkIn,
            checkOut,
            guests
        });
    };

    return (
        <div className="bg-white p-6 rounded-lg shadow-md mb-8">
            <h2 className="text-xl font-bold mb-4 text-gray-700">Find your next stay</h2>
            <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
                <input 
                    type="text" 
                    placeholder="Destination (Bodrum, Muğla, Turkey...)" 
                    className="border p-2 rounded" 
                    value={city}
                    onChange={(e) => setCity(e.target.value)}
                />
                <input 
                    type="date" 
                    className="border p-2 rounded" 
                    value={checkIn}
                    onChange={(e) => setCheckIn(e.target.value)}
                />
                <input 
                    type="date" 
                    className="border p-2 rounded" 
                    value={checkOut}
                    onChange={(e) => setCheckOut(e.target.value)}
                />
                <input 
                    type="number" 
                    placeholder="Guests" 
                    value={guests} 
                    className="border p-2 rounded" 
                    onChange={(e) => setGuests(e.target.value)}
                />
                <button 
                    onClick={handleSubmit} 
                    className="bg-blue-600 text-white p-2 rounded hover:bg-blue-700 font-bold">
                    Search
                </button>
            </div>
        </div>
    );
}