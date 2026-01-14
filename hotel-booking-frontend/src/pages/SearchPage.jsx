import { useEffect, useState, useMemo } from "react";
import api from "../services/api";
import Navbar from "../components/Navbar";
import SearchForm from "../components/SearchForm";
import HotelCard from "../components/HotelCard";

export default function SearchPage() {
    const [hotels, setHotels] = useState([]); // API'den gelen ham veri
    const [loading, setLoading] = useState(true);

    // --- FİLTRE & SIRALAMA STATE'LERİ ---
    const [sortOption, setSortOption] = useState("recommended"); // Varsayılan
    const [minRating, setMinRating] = useState(0); // 0 = Hepsi
    
    // Tarih parametreleri (HotelCard'a göndermek için)
    const [searchParams, setSearchParams] = useState({
        checkIn: "",
        checkOut: "",
        city: "",
        guests: 2
    });

    // 1. Tüm otelleri çek
    const fetchAllHotels = async () => {
        setLoading(true);
        try {
            const res = await api.get("/hotel-service/api/v1/Hotels");
            setHotels(res.data);
        } catch (err) {
            console.error("Failed to fetch hotels", err);
        } finally {
            setLoading(false);
        }
    };

    // 2. Arama yap
    const handleSearch = async (params) => {
        setSearchParams(params);
        setLoading(true);
        try {
            const res = await api.get("/hotel-service/api/v1/Search", {
                params: {
                    city: params.city,
                    startDate: params.checkIn, 
                    endDate: params.checkOut, 
                    guests: params.guests
                }
            });
            setHotels(res.data);
        } catch (err) {
            console.error("Search failed", err);
            alert("Arama sırasında hata oluştu.");
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => { 
        fetchAllHotels(); 
    }, []);

    // --- MAGIC HAPPENS HERE (Filtreleme ve Sıralama Mantığı) ---
    const filteredAndSortedHotels = useMemo(() => {
        let result = [...hotels];

        // A. FİLTRELEME (Rating)
        if (minRating > 0) {
            result = result.filter(h => (h.rating || 0) >= minRating);
        }

        // B. SIRALAMA
        switch (sortOption) {
            case "priceAsc":
                // Otelin en ucuz odasını baz alarak sırala
                result.sort((a, b) => {
                    const priceA = a.rooms?.length ? Math.min(...a.rooms.map(r => r.basePrice)) : 99999;
                    const priceB = b.rooms?.length ? Math.min(...b.rooms.map(r => r.basePrice)) : 99999;
                    return priceA - priceB;
                });
                break;
            
            case "priceDesc":
                result.sort((a, b) => {
                    const priceA = a.rooms?.length ? Math.min(...a.rooms.map(r => r.basePrice)) : 0;
                    const priceB = b.rooms?.length ? Math.min(...b.rooms.map(r => r.basePrice)) : 0;
                    return priceB - priceA;
                });
                break;

            case "ratingDesc":
                result.sort((a, b) => (b.rating || 0) - (a.rating || 0));
                break;

            case "nameAsc":
                result.sort((a, b) => a.name.localeCompare(b.name));
                break;

            case "nameDesc":
                result.sort((a, b) => b.name.localeCompare(a.name));
                break;

            default: // "recommended"
                break;
        }

        return result;
    }, [hotels, sortOption, minRating]);


    return (
        <div className="min-h-screen bg-gray-100">
            <Navbar />
            <div className="container mx-auto p-4 max-w-5xl">
                
                {/* Arama Formu */}
                <SearchForm onSearch={(params) => handleSearch(params)} />
                
                {/* --- FİLTRELEME & SIRALAMA TOOLBAR --- */}
                <div className="bg-white p-4 rounded-lg shadow-sm mt-4 flex flex-col md:flex-row justify-between items-center border border-gray-200">
                    
                    {/* Sol: Sonuç Sayısı */}
                    <div className="text-gray-600 font-medium mb-2 md:mb-0">
                        Found <span className="text-blue-600 font-bold">{filteredAndSortedHotels.length}</span> hotels
                    </div>

                    {/* Sağ: Kontroller */}
                    <div className="flex gap-4">
                        {/* Rating Filtresi */}
                        <select 
                            className="border p-2 rounded text-sm text-gray-700 focus:outline-blue-500"
                            value={minRating}
                            onChange={(e) => setMinRating(Number(e.target.value))}
                        >
                            <option value="0">All Ratings</option>
                            <option value="7">7+ (Good)</option>
                            <option value="8">8+ (Very Good)</option>
                            <option value="9">9+ (Excellent)</option>
                        </select>

                        {/* Sıralama */}
                        <select 
                            className="border p-2 rounded text-sm text-gray-700 focus:outline-blue-500"
                            value={sortOption}
                            onChange={(e) => setSortOption(e.target.value)}
                        >
                            <option value="recommended">Sort by: Recommended</option>
                            <option value="priceAsc">Price: Low to High</option>
                            <option value="priceDesc">Price: High to Low</option>
                            <option value="ratingDesc">Rating: High to Low</option>
                            <option value="nameAsc">Name: A to Z</option>
                            <option value="nameDesc">Name: Z to A</option>
                        </select>
                    </div>
                </div>

                {/* --- LİSTELEME --- */}
                {loading ? (
                    <div className="flex justify-center mt-10">
                        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
                    </div>
                ) : (
                    <div className="mt-6">
                        {filteredAndSortedHotels.map(hotel => (
                            <HotelCard 
                                key={hotel.id || hotel.hotelId} 
                                hotel={hotel} 
                                searchParams={searchParams} 
                            />
                        ))}
                        
                        {filteredAndSortedHotels.length === 0 && (
                            <div className="text-center mt-10 bg-white p-8 rounded-lg shadow">
                                <h3 className="text-xl text-gray-600">Hotel not found.</h3>
                                <p className="text-gray-400 mt-2">Try changing the filters or viewing all hotels.</p>
                                <button 
                                    onClick={() => { setMinRating(0); setSortOption("recommended"); fetchAllHotels(); }}
                                    className="mt-4 text-blue-500 underline hover:text-blue-700"
                                >
                                    Reset All Hotels and Filters
                                </button>
                            </div>
                        )}
                    </div>
                )}
            </div>
        </div>
    );
}