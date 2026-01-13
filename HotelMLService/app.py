from flask import Flask, request, jsonify
import pickle
import pandas as pd
import numpy as np
import logging

app = Flask(__name__)

# Setup logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Load the trained model and encoders ONCE when the app starts
try:
    logger.info("Loading ML model and encoders...")
    with open('price_model.pkl', 'rb') as f:
        model = pickle.load(f)

    with open('encoders.pkl', 'rb') as f:
        encoders = pickle.load(f)

    le_month = encoders['month']
    le_room = encoders['room']
    
    # Get available months and room types for validation
    available_months = list(le_month.classes_)
    available_rooms = list(le_room.classes_)
    
    logger.info(f"Model loaded successfully!")
    logger.info(f"Available months: {available_months}")
    logger.info(f"Available room types: {available_rooms}")
    
except Exception as e:
    logger.error(f"Error loading model: {e}")
    model = None
    encoders = None

# Room type mapping for frontend compatibility
ROOM_TYPE_MAPPING = {
    "Standard": "A",
    "Deluxe": "D", 
    "Luxury": "L",
    "Suite": "S",
    "Economy": "E",
    "Premium": "P",
    "A": "A",
    "B": "B", 
    "C": "C",
    "D": "D",
    "E": "E",
    "F": "F",
    "G": "G",
    "H": "H",
    "I": "I",
    "J": "J",
    "K": "K",
    "L": "L",
    "M": "M",
    "N": "N",
    "O": "O",
    "P": "P"
}

# Month mapping for case-insensitive matching
MONTH_MAPPING = {
    "january": "January",
    "february": "February", 
    "march": "March",
    "april": "April",
    "may": "May",
    "june": "June",
    "july": "July",
    "august": "August",
    "september": "September",
    "october": "October",
    "november": "November",
    "december": "December"
}

def validate_and_preprocess_input(month_str, room_str, lead_time, guests):
    """
    Validate and preprocess input parameters for the model
    Model expects: ['arrival_date_month', 'reserved_room_type', 'lead_time', 'adults', 'children', 'babies']
    """
    # 1. Validate and normalize month
    month_str = month_str.strip().title()
    if month_str.lower() in MONTH_MAPPING:
        month_str = MONTH_MAPPING[month_str.lower()]
    
    # Check if month is valid
    try:
        month_val = le_month.transform([month_str])[0]
    except ValueError:
        logger.warning(f"Invalid month '{month_str}'. Defaulting to 'July'")
        # Default to July (peak season)
        month_val = le_month.transform(['July'])[0]
        month_str = 'July'
    
    # 2. Validate and normalize room type
    room_str = str(room_str).strip()
    # Check if it's in our mapping
    if room_str in ROOM_TYPE_MAPPING:
        room_str = ROOM_TYPE_MAPPING[room_str]
    else:
        # If not in mapping, try to use as-is (might be 'A', 'B', etc.)
        room_str = room_str.upper()[:1]  # Take only first character
    
    # Check if room type is valid
    try:
        room_val = le_room.transform([room_str])[0]
    except ValueError:
        logger.warning(f"Invalid room type '{room_str}'. Defaulting to 'A'")
        # Default to Standard room
        room_val = le_room.transform(['A'])[0]
        room_str = 'A'
    
    # 3. Validate lead time (should be positive integer)
    try:
        lead_time = int(lead_time)
        if lead_time < 0:
            lead_time = 0
        if lead_time > 365:  # Cap at 1 year
            lead_time = 365
    except (ValueError, TypeError):
        logger.warning(f"Invalid lead time '{lead_time}'. Defaulting to 14")
        lead_time = 14
    
    # 4. Validate guests and split into adults/children/babies
    # For simplicity: all guests are adults, no children/babies
    # You can modify this logic based on your frontend input
    try:
        guests = int(guests)
        if guests < 1:
            guests = 1
        if guests > 10:  # Cap at reasonable number
            guests = 10
        
        adults = guests
        children = 0
        babies = 0
        
    except (ValueError, TypeError):
        logger.warning(f"Invalid guests '{guests}'. Defaulting to 2 adults")
        adults = 2
        children = 0
        babies = 0
    
    return month_str, month_val, room_str, room_val, lead_time, adults, children, babies

@app.route('/predict', methods=['POST'])
def predict():
    """
    Predict hotel room price based on input parameters
    Expected JSON format (from your frontend):
    {
        "month": "August",
        "roomType": "Standard",  # or "Deluxe" or "A" or "D"
        "leadTime": 30,
        "guests": 2
    }
    
    Model expects features in this order:
    1. arrival_date_month (encoded)
    2. reserved_room_type (encoded)  
    3. lead_time
    4. adults
    5. children
    6. babies
    """
    if model is None or encoders is None:
        return jsonify({
            'error': 'ML model not loaded. Please check server logs.',
            'suggested_price': 100.0,
            'currency': 'EUR',
            'details': 'Using fallback price due to model loading error'
        }), 503
    
    try:
        req = request.json
        logger.info(f"Received prediction request: {req}")
        
        # 1. Extract Inputs with defaults
        month_str = req.get('month', 'August')
        room_str = req.get('roomType', 'A')
        lead_time = req.get('leadTime', 14)
        guests = req.get('guests', 2)
        
        # 2. Validate and preprocess
        month_str, month_val, room_str, room_val, lead_time, adults, children, babies = validate_and_preprocess_input(
            month_str, room_str, lead_time, guests
        )
        
        # 3. Prepare features for prediction
        # Feature order MUST match training: 
        # ['arrival_date_month', 'reserved_room_type', 'lead_time', 'adults', 'children', 'babies']
        
        features = np.array([[month_val, room_val, lead_time, adults, children, babies]])
        
        # 4. Make prediction
        predicted_price = float(model.predict(features)[0])
        
        # 5. Apply business logic constraints
        # Ensure price is reasonable (based on your dataset)
        if predicted_price < 20:
            predicted_price = 50.0  # Minimum price
        elif predicted_price > 1000:
            predicted_price = 500.0  # Cap very high predictions
        
        # Round to 2 decimal places
        predicted_price = round(predicted_price, 2)
        
        # 6. Prepare user-friendly room name for response
        room_display_name = room_str
        if room_str == "A":
            room_display_name = "Standard"
        elif room_str == "D":
            room_display_name = "Deluxe"
        elif room_str == "L":
            room_display_name = "Luxury"
        elif room_str == "S":
            room_display_name = "Suite"
        
        # 7. Prepare response
        response = {
            'suggested_price': predicted_price,
            'currency': 'EUR',
            'details': f"Prediction for {month_str}, {room_display_name} Room, {lead_time} days in advance, {guests} guests",
            'input_processed': {
                'month': month_str,
                'room_type': room_str,
                'room_display': room_display_name,
                'lead_time': lead_time,
                'adults': adults,
                'children': children,
                'babies': babies,
                'total_guests': adults + children + babies
            },
            'model_info': {
                'feature_count': 6,
                'model_type': 'RandomForestRegressor'
            }
        }
        
        logger.info(f"Prediction result: €{predicted_price}")
        return jsonify(response)
        
    except Exception as e:
        logger.error(f"Prediction error: {str(e)}", exc_info=True)
        return jsonify({
            'error': f'Prediction failed: {str(e)}',
            'suggested_price': 120.0,
            'currency': 'EUR',
            'details': 'Using fallback price due to prediction error'
        }), 500

@app.route('/health', methods=['GET'])
def health_check():
    """Health check endpoint for monitoring"""
    status = {
        'status': 'healthy' if model is not None else 'unhealthy',
        'model_loaded': model is not None,
        'encoders_loaded': encoders is not None,
        'service': 'hotel-price-prediction',
        'version': '1.0.0',
        'available_months': list(le_month.classes_) if encoders else [],
        'available_rooms': list(le_room.classes_) if encoders else []
    }
    return jsonify(status)

@app.route('/info', methods=['GET'])
def model_info():
    """Get information about the model and available parameters"""
    if model is None or encoders is None:
        return jsonify({'error': 'Model not loaded'}), 503
    
    info = {
        'available_months': list(le_month.classes_),
        'available_room_types': list(le_room.classes_),
        'room_type_mapping': ROOM_TYPE_MAPPING,
        'feature_names': ['arrival_date_month', 'reserved_room_type', 'lead_time', 'adults', 'children', 'babies'],
        'model_type': 'RandomForestRegressor',
        'model_params': {
            'n_estimators': 100,
            'max_depth': 20,
            'min_samples_split': 10,
            'min_samples_leaf': 5
        },
        'max_guests_recommended': 10,
        'max_lead_time_days': 365
    }
    return jsonify(info)

@app.route('/test', methods=['GET'])
def test_prediction():
    """Test endpoint to verify model is working"""
    if model is None:
        return jsonify({'error': 'Model not loaded'}), 503
    
    # Test with sample data
    test_cases = [
        {'month': 'August', 'roomType': 'A', 'leadTime': 30, 'guests': 2},
        {'month': 'July', 'roomType': 'D', 'leadTime': 14, 'guests': 4},
        {'month': 'December', 'roomType': 'A', 'leadTime': 90, 'guests': 2},
        {'month': 'January', 'roomType': 'D', 'leadTime': 7, 'guests': 1}
    ]
    
    results = []
    for test in test_cases:
        try:
            # Simulate prediction
            month_val = le_month.transform([test['month']])[0]
            room_val = le_room.transform([test['roomType']])[0]
            features = np.array([[month_val, room_val, test['leadTime'], test['guests'], 0, 0]])
            predicted_price = float(model.predict(features)[0])
            
            results.append({
                'input': test,
                'predicted_price': round(predicted_price, 2),
                'status': 'success'
            })
        except Exception as e:
            results.append({
                'input': test,
                'error': str(e),
                'status': 'error'
            })
    
    return jsonify({
        'service': 'hotel-price-prediction',
        'test_results': results,
        'status': 'operational'
    })

if __name__ == '__main__':
    logger.info("=" * 60)
    logger.info("Starting Hotel Price Prediction Service")
    logger.info("=" * 60)
    logger.info("Endpoints:")
    logger.info("  POST /predict     - Get price prediction")
    logger.info("  GET  /health      - Health check")
    logger.info("  GET  /info        - Model information") 
    logger.info("  GET  /test        - Test predictions")
    logger.info("=" * 60)
    
    app.run(host='0.0.0.0', port=5000, debug=False)