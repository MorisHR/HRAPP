# Mauritius Districts and Major Locations - Quick Reference

## 9 Districts of Mauritius

### 1. Port Louis (PL) - Capital District
- **Region**: West
- **Area**: 42 km²
- **Population**: ~150,000
- **Major Locations**: Port Louis (City, 11302)
- **Description**: Capital city, business center, port

### 2. Plaines Wilhems (PW) - Most Populous District
- **Region**: Central
- **Area**: 203 km²
- **Population**: ~380,000
- **Major Locations**:
  - Curepipe (City, 74504)
  - Quatre Bornes (Town, 72426)
  - Vacoas (Town, 73403)
  - Rose Hill (Town, 71259)
  - Beau Bassin (Town, 71504)
  - Phoenix (Town, 73504)
  - Floreal (Town, 74207)
  - Ebene (City, 72201)
- **Description**: Most urbanized, tech hub (Ebene Cybercity)

### 3. Pamplemousses (PA) - North Tourist Region
- **Region**: North
- **Area**: 179 km²
- **Population**: ~140,000
- **Major Locations**:
  - Grand Baie (Town, 30515) - Beach resort
  - Triolet (Village, 21618)
  - Goodlands (Village, 51502)
- **Description**: Tourist hotspot, beaches, botanical gardens

### 4. Rivière du Rempart (RR) - North Coast
- **Region**: North
- **Area**: 147 km²
- **Population**: ~109,000
- **Major Locations**:
  - Cap Malheureux (Village, 31708)
  - Pereybere (Village, 30546)
  - Poudre d'Or (Village, 31707)
- **Description**: Northern beaches, fishing villages

### 5. Flacq (FL) - East Coast
- **Region**: East
- **Area**: 298 km² (largest by area)
- **Population**: ~138,000
- **Major Locations**:
  - Centre de Flacq (Town, 40901)
  - Belle Mare (Village, 41601) - Beach resort
  - Quatre Cocos (Village, 41520)
  - Bras d'Eau (Village, 41605)
- **Description**: East coast beaches, sugar cane

### 6. Grand Port (GP) - Historic Southeast
- **Region**: South
- **Area**: 260 km²
- **Population**: ~113,000
- **Major Locations**:
  - Mahebourg (Town, 50802) - Historic port town
  - Blue Bay (Village, 50803) - Marine park
- **Description**: Historic landing site, Blue Bay Marine Park

### 7. Savanne (SA) - South Coast
- **Region**: South
- **Area**: 245 km²
- **Population**: ~69,000
- **Major Locations**:
  - Souillac (Village, 60805)
  - Chamarel (Village, 91001) - Seven-colored earth
- **Description**: Rugged south coast, waterfalls, nature

### 8. Black River (BL) - West Coast
- **Region**: West
- **Area**: 259 km²
- **Population**: ~80,000
- **Major Locations**:
  - Flic en Flac (Village, 90501) - Beach resort
  - Tamarin (Village, 90601) - Surfing spot
  - Albion (Village, 90306)
- **Description**: West coast beaches, Black River Gorges National Park

### 9. Moka (MO) - Central Highlands
- **Region**: Central
- **Area**: 231 km²
- **Population**: ~83,000
- **Major Locations**:
  - Moka (Town, 80402)
  - Reduit (Village, 80835) - University of Mauritius
  - Dagotière (Village, 80520)
- **Description**: Central plateau, university, mountains

---

## Locality Types

### City (4 locations)
Large urban centers with extensive infrastructure
- **Port Louis** (Capital)
- **Curepipe** (Central plateau city)
- **Ebene** (Cybercity, tech hub)
- **Beau Bassin-Rose Hill** (Combined urban area)

### Town (12 locations)
Medium-sized urban areas
- **Quatre Bornes**, Vacoas, Rose Hill, Beau Bassin
- **Phoenix**, Floreal (Plaines Wilhems)
- **Grand Baie** (North tourist hub)
- **Mahebourg** (Historic port)
- **Centre de Flacq** (East commercial center)
- **Moka** (Central highlands)

### Village (13+ locations)
Smaller populated areas
- **Beach Villages**: Flic en Flac, Tamarin, Belle Mare, Blue Bay, Pereybere
- **Rural Villages**: Triolet, Goodlands, Chamarel, Souillac, etc.

---

## Regional Breakdown

### North (2 districts)
- **Pamplemousses** (PA) - 5 locations
- **Rivière du Rempart** (RR) - 3 locations
- **Characteristics**: Tourism, beaches, resorts

### Central (2 districts)
- **Plaines Wilhems** (PW) - 8 locations (most populous)
- **Moka** (MO) - 3 locations
- **Characteristics**: Urban, residential, tech, education

### East (1 district)
- **Flacq** (FL) - 4 locations
- **Characteristics**: Agriculture, east coast resorts

### South (2 districts)
- **Grand Port** (GP) - 2 locations
- **Savanne** (SA) - 2 locations
- **Characteristics**: Nature, historic sites, rugged coast

### West (2 districts)
- **Port Louis** (PL) - 1 location (capital)
- **Black River** (BL) - 3 locations
- **Characteristics**: Capital, port, west coast beaches, national park

---

## Postal Code Format

Mauritius uses **5-digit postal codes**:
- **Format**: XXXXX (e.g., 11302, 74504)
- **Range**: 11302 (Port Louis) to 91001 (Chamarel)
- **Coverage**: All major cities, towns, and villages

---

## API Quick Reference

### Get All Districts
```bash
curl http://localhost:5000/api/GeographicLocations/districts
```

### Get District by Code
```bash
curl http://localhost:5000/api/GeographicLocations/districts/by-code/PW
```

### Get Villages in District
```bash
curl http://localhost:5000/api/GeographicLocations/villages/by-district-code/PW
```

### Search Locations
```bash
curl "http://localhost:5000/api/GeographicLocations/villages/search?searchTerm=cure"
```

### Validate Address
```bash
curl "http://localhost:5000/api/GeographicLocations/validate-address?districtCode=PL&villageCode=PLOU&postalCode=11302"
```

### Get Statistics
```bash
curl http://localhost:5000/api/GeographicLocations/statistics
```

---

## Common Use Cases

### Employee Registration Form
1. Load districts: `GET /api/GeographicLocations/districts`
2. User selects district → Load villages: `GET /api/GeographicLocations/villages/by-district/{districtId}`
3. User selects village → Auto-populate postal code

### Address Autocomplete
1. User types search term → `GET /api/GeographicLocations/address-suggestions?searchTerm=...`
2. Display suggestions with village, district, postal code
3. User selects → Auto-populate all address fields

### Address Validation
1. Before saving employee record → `GET /api/GeographicLocations/validate-address?...`
2. Check `isValid` flag
3. Display errors/suggestions if invalid

### Reports by Region
1. Get statistics: `GET /api/GeographicLocations/statistics`
2. Get villages by region: `GET /api/GeographicLocations/districts/by-region/North`
3. Filter employees by district/village

---

## Data Completeness

### Current Coverage (29 locations)
✅ All 9 districts
✅ Capital city (Port Louis)
✅ All major cities (Curepipe, Ebene, etc.)
✅ All major towns (Quatre Bornes, Grand Baie, Mahebourg, etc.)
✅ Tourist hotspots (Grand Baie, Flic en Flac, Belle Mare, etc.)

### Future Expansion (~300-400 locations)
- All villages across Mauritius
- Rural areas and hamlets
- Industrial zones
- Can be imported from Mauritius government open data

---

## Notes

- **Master Database Schema**: Geographic data stored in `master` schema (shared across all tenants)
- **Reference Data**: Read-only for most operations (public access)
- **Caching**: Can be cached for 24+ hours (rarely changes)
- **Performance**: Optimized with indexes for sub-100ms response times
- **Denormalization**: Postal codes include village/district names for fast lookups
