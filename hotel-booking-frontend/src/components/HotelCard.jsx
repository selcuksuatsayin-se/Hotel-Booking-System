import { useState } from "react";
import { Link } from "react-router-dom";
import MapView from "./MapView";

export default function HotelCard({ hotel, searchParams }) {
    const [showMap, setShowMap] = useState(false);
    
    // --- ID DÜZELTMESİ ---
    // Search API 'hotelId', List API 'id' döndürüyor.
    // İkisini de kontrol edip geçerli olanı alıyoruz.
    const realId = hotel.id || hotel.hotelId; 
    // ---------------------

    // Koordinat kontrolü (Veri yoksa varsayılan koordinat)
    const lat = hotel.latitude && hotel.latitude !== 0 ? hotel.latitude : 39.9334;
    const lng = hotel.longitude && hotel.longitude !== 0 ? hotel.longitude : 32.8597;

    // SearchPage'den gelen tarihleri alıyoruz, yoksa boş varsayıyoruz
    const params = searchParams || { checkIn: "", checkOut: "" };

    return (
        <div className="bg-white rounded-lg shadow-md p-6 mb-4 flex flex-col md:flex-row border border-gray-100">
            {/* Sol Taraf: Otel Bilgileri */}
            <div className="flex-1">
                <div className="flex justify-between items-start">
                     <h3 className="text-xl font-bold text-gray-800">{hotel.name || hotel.hotelName}</h3>
                </div>
               
                {/* Lokasyon Gösterimi: District, City, Country */}
                <p className="text-gray-600 mt-1 flex items-center">
                    <span className="mr-1">📍</span> 
                    {hotel.district ? `${hotel.district}, ` : ''}{hotel.city}{hotel.country ? `, ${hotel.country}` : ''}
                </p>

                <p className="text-gray-500 mt-2 text-sm line-clamp-2">{hotel.description}</p>
                
                {/* Rating Badge */}
                <div className="mt-3 flex items-center">
                    <span className="bg-blue-600 text-white text-xs font-bold px-2 py-1 rounded">
                        {hotel.rating || 9.0}
                    </span>
                    <span className="ml-2 text-sm font-semibold text-gray-600">Excellent</span>
                </div>

                {/* Harita Butonu */}
                <button 
                    onClick={() => setShowMap(!showMap)}
                    className="text-blue-600 hover:text-blue-800 hover:underline mt-4 text-sm flex items-center font-medium transition-colors"
                >
                    {showMap ? "Hide Map" : "Show on Map"}
                </button>

                {/* Harita Bileşeni */}
                {showMap && (
                    <div className="mt-3 rounded-lg overflow-hidden border border-gray-200 shadow-inner">
                        <MapView lat={lat} lng={lng} hotelName={hotel.name} />
                    </div>
                )}
            </div>

            {/* Sağ Taraf: Detay Butonu */}
            <div className="md:ml-6 mt-4 md:mt-0 flex flex-col justify-center items-end min-w-[150px]">
                <Link 
                    to={`/hotel/${realId}`} 
                    state={{ 
                        // Tarih bilgisini detay sayfasına taşıyoruz
                        checkIn: params.checkIn, 
                        checkOut: params.checkOut 
                    }} 
                    className="bg-blue-600 text-white px-6 py-3 rounded-lg hover:bg-blue-700 w-full text-center font-bold shadow-sm transition-colors"
                >
                    View Details
                </Link>
            </div>
        </div>
    );
}