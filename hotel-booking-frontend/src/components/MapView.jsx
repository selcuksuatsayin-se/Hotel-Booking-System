import { MapContainer, TileLayer, Marker, Popup } from 'react-leaflet';
import 'leaflet/dist/leaflet.css';

export default function MapView({ lat, lng, hotelName }) {
    return (
        <div className="h-64 w-full rounded-lg overflow-hidden border mt-4">
            <MapContainer center={[lat, lng]} zoom={13} scrollWheelZoom={false}>
                <TileLayer url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" />
                <Marker position={[lat, lng]}>
                    <Popup>{hotelName}</Popup>
                </Marker>
            </MapContainer>
        </div>
    );
}