import pandas as pd
import pickle
from sklearn.model_selection import train_test_split
from sklearn.ensemble import RandomForestRegressor
from sklearn.preprocessing import LabelEncoder

print("Loading dataset...")
# 1. Load Data
df = pd.read_csv('hotel_bookings.csv')

# 2. Select Useful Features for Price Prediction
# We want to predict 'adr' (Average Daily Rate)
features = ['arrival_date_month', 'reserved_room_type', 'lead_time', 'adults', 'children', 'babies']
target = 'adr'

df = df[features + [target]]

# 3. Clean Data
df.fillna(0, inplace=True) # Fill missing values (e.g. 0 children)
df = df[df['adr'] < 5000] # Remove outliers (some errors have price 54000)

# 4. Convert Text to Numbers (Preprocessing)
le_month = LabelEncoder()
df['arrival_date_month'] = le_month.fit_transform(df['arrival_date_month'])

le_room = LabelEncoder()
df['reserved_room_type'] = le_room.fit_transform(df['reserved_room_type'])

# Save the encoders
with open('encoders.pkl', 'wb') as f:
    pickle.dump({'month': le_month, 'room': le_room}, f)

# 5. Train Model
print("Training Random Forest Model...")
X = df.drop(target, axis=1)
y = df[target]

X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.2, random_state=42)

model = RandomForestRegressor(
    n_estimators=100,        # İyileştirildi
    max_depth=20,            # İyileştirildi
    min_samples_split=10,    # İyileştirildi
    min_samples_leaf=5,      # İyileştirildi
    random_state=42,
    n_jobs=-1
)
model.fit(X_train, y_train)

print(f"Model Trained! Test Score (R2): {model.score(X_test, y_test):.2f}")

# 6. Save the Model
with open('price_model.pkl', 'wb') as f:
    pickle.dump(model, f)

print("Files saved: price_model.pkl and encoders.pkl")