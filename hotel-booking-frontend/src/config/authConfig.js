//export const msalConfig = {
//    auth: {
//        clientId: "9b8ffaf0-8710-4543-aae7-1d232fbf1370", // Copy from Azure App Registration
//        authority: "https://hotelsystemselcuk.ciamlogin.com/", // Copy from your "Instance" URL
//        knownAuthorities: ["hotelsystemselcuk.ciamlogin.com"],
//        redirectUri: "http://localhost:5173", // Vite usually runs on 5173
//    },
//    cache: {
//        cacheLocation: "sessionStorage",
//        storeAuthStateInCookie: false,
//    }
//};

//// Scopes you need to call your API
//export const loginRequest = {
//    scopes: ["api://9b8ffaf0-8710-4543-aae7-1d232fbf1370/access_as_user"] // Or just ["access_as_user"] depending on your setup
//};export const msalConfig = {
//    auth: {
//        clientId: "9b8ffaf0-8710-4543-aae7-1d232fbf1370", // Copy from Azure App Registration
//        authority: "https://hotelsystemselcuk.ciamlogin.com/", // Copy from your "Instance" URL
//        knownAuthorities: ["hotelsystemselcuk.ciamlogin.com"],
//        redirectUri: "http://localhost:5173", // Vite usually runs on 5173
//    },
//    cache: {
//        cacheLocation: "sessionStorage",
//        storeAuthStateInCookie: false,
//    }
//};

//// Scopes you need to call your API
//export const loginRequest = {
//    scopes: ["api://9b8ffaf0-8710-4543-aae7-1d232fbf1370/access_as_user"] // Or just ["access_as_user"] depending on your setup
//};


/*
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License.
 */

import { LogLevel } from "@azure/msal-browser";

// --- MEVCUT YAPIN (Dokunmuyoruz) ---
export const msalConfig = {
    auth: {
        // Loglardan aldığım senin Client ID'n
        clientId: "9b8ffaf0-8710-4543-aae7-1d232fbf1370", 
        
        // Loglardan aldığım senin Authority URL'in
        authority: "https://hotelsystemselcuk.ciamlogin.com/8821ad87-ef1b-4cd6-8b32-2fde18cc880f", 
        
        redirectUri: window.location.origin, // "http://localhost:5173" yerine dinamik olsun
        postLogoutRedirectUri: window.location.origin,
        navigateToLoginRequestUrl: true,
    },
    cache: {
        cacheLocation: "sessionStorage", // Veya "localStorage" senin tercihine göre
        storeAuthStateInCookie: false,
    },
    system: {
        loggerOptions: {
            loggerCallback: (level, message, containsPii) => {
                if (containsPii) {
                    return;
                }
                switch (level) {
                    case LogLevel.Error:
                        console.error(message);
                        return;
                    case LogLevel.Info:
                        console.info(message);
                        return;
                    case LogLevel.Verbose:
                        console.debug(message);
                        return;
                    case LogLevel.Warning:
                        console.warn(message);
                        return;
                    default:
                        return;
                }
            },
        },
    },
};

// --- EKLENMESİ GEREKEN KISIM (SORUNU ÇÖZEN YER) ---

// 1. Kullanıcı giriş yaparken istenecek genel izinler
export const loginRequest = {
    scopes: ["User.Read", "openid", "profile", "offline_access"]
};

// 2. Backend API isteği için gerekli izinler (Token Request)
// Hata mesajındaki "90009" kodunu düzelten kısım burası.
// Azure External ID'de backend ve frontend aynı App Registration ise
// scope olarak "<Client-ID>/.default" kullanılır.
export const tokenRequest = {
    scopes: ["9b8ffaf0-8710-4543-aae7-1d232fbf1370/.default"]
};