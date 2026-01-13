import axios from 'axios';
import { PublicClientApplication } from "@azure/msal-browser";
import { msalConfig, tokenRequest } from "../config/authConfig"; // <--- 1. tokenRequest IMPORT EDİLDİ

const msalInstance = new PublicClientApplication(msalConfig);

// MSAL v3 başlatma (Initialize)
let msalInitPromise = null;
const initializeMsal = () => {
    if (!msalInitPromise) {
        msalInitPromise = msalInstance.initialize();
    }
    return msalInitPromise;
};

// Gateway URL
const API_GATEWAY_URL = 'http://localhost:5292'; 

const api = axios.create({
  baseURL: API_GATEWAY_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Interceptor
api.interceptors.request.use(async (config) => {
    await initializeMsal();

    // Aktif hesap kontrolü
    if (!msalInstance.getActiveAccount()) {
         const accounts = msalInstance.getAllAccounts();
         if (accounts.length > 0) {
             msalInstance.setActiveAccount(accounts[0]);
         }
    }

    const account = msalInstance.getActiveAccount();
    if (account) {
        try {
            // --- 2. DÜZELTME BURADA ---
            // Eskiden "...msalConfig" yazıyordu, bu yanlıştı.
            // Şimdi "scopes" parametresini doğru gönderiyoruz.
            const response = await msalInstance.acquireTokenSilent({
                scopes: tokenRequest.scopes, // <--- DOĞRU PARAMETRE
                account: account
            });
            
            config.headers.Authorization = `Bearer ${response.accessToken}`;
            
            // Debug için token'ı konsola basabilirsin (İsteğe bağlı)
            // console.log("Access Token:", response.accessToken); 

        } catch (error) {
            console.error("Token acquisition failed", error);
            // Eğer "interaction_required" hatası alırsan kullanıcıyı login'e zorlamak gerekebilir
            // ama şimdilik sadece loglayalım.
        }
    }
    return config;
}, (error) => {
    return Promise.reject(error);
});

export default api;