# Hafif bir Python sürümü
FROM python:3.11-slim

# Çalışma klasörü
WORKDIR /app

# Önce gereksinim dosyasını kopyalayıp yüklüyoruz
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Kalan tüm Python kodlarını (model dosyaları dahil) kopyalıyoruz
COPY . .

# Flask portu
EXPOSE 5000

# Uygulamayı başlat
CMD ["python", "app.py"]