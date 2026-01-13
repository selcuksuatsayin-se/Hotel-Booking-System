# Senin kullandığın Node sürümü (v22)
FROM node:22-alpine

WORKDIR /app

# Paketleri yükle
COPY package*.json ./
RUN npm install

# Kodları kopyala
COPY . .

# Vite portu
EXPOSE 5173

# Uygulamayı başlat (--host parametresi dışarıdan erişim için şart)
CMD ["npm", "run", "dev", "--", "--host"]