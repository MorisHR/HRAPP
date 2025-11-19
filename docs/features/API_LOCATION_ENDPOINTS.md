# Location Management API - Complete Documentation

## Overview

Production-ready REST API endpoints for managing physical work locations in the HRMS system, with specialized support for **Mauritius geography** (9 districts, cities, towns, villages).

**Base URL**: `https://api.your-domain.com/api/location`

**Controller**: `/workspaces/HRAPP/src/HRMS.API/Controllers/LocationController.cs`

---

## Features

- **CRUD Operations**: Complete create, read, update, delete functionality
- **Advanced Filtering**: Filter by district, type, region, postal code, search terms
- **Pagination**: Server-side pagination with customizable page size
- **Sorting**: Multi-field sorting (name, code, district, city, date)
- **Mauritius Districts**: Support for all 9 Mauritius districts
- **Geographic Data**: Latitude/longitude coordinates for mapping
- **Role-Based Authorization**: Admin, HR, and Manager roles
- **Audit Logging**: Comprehensive audit trail for all modifications
- **Input Validation**: DataAnnotations and FluentValidation
- **Error Handling**: Standardized error responses
- **Data Seeding**: Pre-populate with 30 Mauritius locations

---

## Authentication

All endpoints require JWT Bearer token authentication.

```bash
Authorization: Bearer <your-jwt-token>
```

### Role Requirements

| Endpoint | Admin | HR | Manager |
|----------|-------|----|---------|
| GET (all endpoints) | ✓ | ✓ | ✓ |
| POST (create) | ✓ | ✓ | ✗ |
| PUT (update) | ✓ | ✓ | ✗ |
| DELETE | ✓ | ✗ | ✗ |
| POST /seed | ✓ | ✗ | ✗ |

---

## Endpoints

### 1. Get Locations (Filtered & Paginated)

Get all locations with advanced filtering, sorting, and pagination.

**Request**:
```
GET /api/location?District=Port%20Louis&Type=City&Page=1&PageSize=20&SortBy=LocationName&SortDirection=asc
```

**Query Parameters**:

| Parameter | Type | Required | Description | Example |
|-----------|------|----------|-------------|---------|
| `District` | string | No | Filter by Mauritius district | `Port Louis` |
| `Type` | string | No | Filter by location type | `City`, `Town`, `Village` |
| `Region` | string | No | Filter by region | `North`, `South`, `East`, `West`, `Central` |
| `PostalCode` | string | No | Filter by postal code | `11101` |
| `IsActive` | boolean | No | Filter by active status | `true`, `false` |
| `SearchTerm` | string | No | Search in name/code/city/district | `Port` |
| `LocationManagerId` | GUID | No | Filter by location manager | `guid` |
| `Page` | integer | No | Page number (1-based) | `1` (default) |
| `PageSize` | integer | No | Items per page (1-100) | `20` (default) |
| `SortBy` | string | No | Sort field | `LocationName`, `LocationCode`, `District`, `City`, `CreatedAt` |
| `SortDirection` | string | No | Sort direction | `asc` (default), `desc` |

**Response (200 OK)**:
```json
{
  "success": true,
  "data": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "locationCode": "PL-001",
      "locationName": "Port Louis",
      "locationType": "City",
      "city": "Port Louis",
      "district": "Port Louis",
      "region": "North",
      "country": "Mauritius",
      "phone": null,
      "email": null,
      "locationManagerId": null,
      "locationManagerName": null,
      "capacityHeadcount": null,
      "deviceCount": 5,
      "employeeCount": 120,
      "isActive": true,
      "createdAt": "2025-01-15T10:00:00Z",
      "updatedAt": null
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 30,
    "totalPages": 2
  }
}
```

**cURL Example**:
```bash
curl -X GET "https://api.your-domain.com/api/location?District=Port%20Louis&Page=1&PageSize=10" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json"
```

---

### 2. Get Location by ID

Retrieve detailed information for a specific location.

**Request**:
```
GET /api/location/{id}
```

**Parameters**:
- `id` (path, GUID): Location ID

**Response (200 OK)**:
```json
{
  "success": true,
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "locationCode": "PL-001",
    "locationName": "Port Louis",
    "locationType": "City",
    "addressLine1": "123 Main Street",
    "addressLine2": "Suite 100",
    "city": "Port Louis",
    "district": "Port Louis",
    "region": "North",
    "postalCode": "11101",
    "country": "Mauritius",
    "phone": "+230 123 4567",
    "email": "portlouis@company.com",
    "workingHoursJson": "{\"monday\":{\"start\":\"08:00\",\"end\":\"17:00\"}}",
    "timezone": "Indian/Mauritius",
    "locationManagerId": "660e8400-e29b-41d4-a716-446655440000",
    "locationManagerName": "John Doe",
    "capacityHeadcount": 200,
    "latitude": -20.1609,
    "longitude": 57.5012,
    "isActive": true,
    "deviceCount": 5,
    "employeeCount": 120,
    "createdAt": "2025-01-15T10:00:00Z",
    "updatedAt": "2025-01-20T14:30:00Z",
    "createdBy": "admin@company.com",
    "updatedBy": "hr@company.com"
  }
}
```

**Response (404 Not Found)**:
```json
{
  "success": false,
  "error": "Location not found"
}
```

**cURL Example**:
```bash
curl -X GET "https://api.your-domain.com/api/location/550e8400-e29b-41d4-a716-446655440000" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

### 3. Get Districts

Retrieve list of all unique districts from existing locations.

**Request**:
```
GET /api/location/districts
```

**Response (200 OK)**:
```json
{
  "success": true,
  "data": [
    "Black River",
    "Flacq",
    "Grand Port",
    "Moka",
    "Pamplemousses",
    "Plaines Wilhems",
    "Port Louis",
    "Rivière du Rempart",
    "Savanne"
  ],
  "count": 9
}
```

**cURL Example**:
```bash
curl -X GET "https://api.your-domain.com/api/location/districts" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

### 4. Get Locations by District

Get all locations in a specific Mauritius district.

**Request**:
```
GET /api/location/district/{district}?activeOnly=true
```

**Parameters**:
- `district` (path, string): District name (URL encoded)
- `activeOnly` (query, boolean): Filter for active locations only (default: `true`)

**Response (200 OK)**:
```json
{
  "success": true,
  "data": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "locationCode": "PL-001",
      "locationName": "Port Louis",
      "locationType": "City",
      "city": "Port Louis",
      "district": "Port Louis",
      "region": "North",
      "country": "Mauritius",
      "deviceCount": 5,
      "employeeCount": 120,
      "isActive": true
    },
    {
      "id": "660e8400-e29b-41d4-a716-446655440000",
      "locationCode": "PL-002",
      "locationName": "Baie du Tombeau",
      "locationType": "Village",
      "city": "Baie du Tombeau",
      "district": "Port Louis",
      "region": "North",
      "country": "Mauritius",
      "deviceCount": 2,
      "employeeCount": 45,
      "isActive": true
    }
  ],
  "count": 2,
  "district": "Port Louis"
}
```

**cURL Example**:
```bash
curl -X GET "https://api.your-domain.com/api/location/district/Port%20Louis?activeOnly=true" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

### 5. Search Locations

Full-text search across location name, code, city, district, and region.

**Request**:
```
GET /api/location/search?q=Port&activeOnly=true
```

**Parameters**:
- `q` (query, string, required): Search term (minimum 2 characters)
- `activeOnly` (query, boolean): Filter for active locations only (default: `true`)

**Response (200 OK)**:
```json
{
  "success": true,
  "data": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "locationCode": "PL-001",
      "locationName": "Port Louis",
      "locationType": "City",
      "city": "Port Louis",
      "district": "Port Louis",
      "region": "North",
      "country": "Mauritius",
      "deviceCount": 5,
      "employeeCount": 120,
      "isActive": true
    }
  ],
  "count": 1,
  "query": "Port"
}
```

**Response (400 Bad Request)** - Search term too short:
```json
{
  "success": false,
  "error": "Search query must be at least 2 characters"
}
```

**cURL Example**:
```bash
curl -X GET "https://api.your-domain.com/api/location/search?q=Port&activeOnly=true" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

### 6. Create Location

Create a new work location.

**Request**:
```
POST /api/location
```

**Request Body**:
```json
{
  "locationCode": "PL-004",
  "locationName": "New Office - Port Louis",
  "locationType": "Office",
  "addressLine1": "456 Business Avenue",
  "addressLine2": "Floor 5",
  "city": "Port Louis",
  "district": "Port Louis",
  "region": "North",
  "postalCode": "11102",
  "country": "Mauritius",
  "phone": "+230 987 6543",
  "email": "newoffice@company.com",
  "workingHoursJson": "{\"monday\":{\"start\":\"08:00\",\"end\":\"17:00\"},\"tuesday\":{\"start\":\"08:00\",\"end\":\"17:00\"}}",
  "timezone": "Indian/Mauritius",
  "locationManagerId": "770e8400-e29b-41d4-a716-446655440000",
  "capacityHeadcount": 150,
  "latitude": -20.1609,
  "longitude": 57.5012,
  "isActive": true
}
```

**Validation Rules**:

| Field | Required | Max Length | Notes |
|-------|----------|------------|-------|
| `locationCode` | Yes | 50 | Unique identifier |
| `locationName` | Yes | 200 | Display name |
| `locationType` | No | 100 | City, Town, Village, Office, Factory, etc. |
| `addressLine1` | No | 500 | |
| `addressLine2` | No | 500 | |
| `city` | No | 100 | |
| `district` | No | 100 | One of 9 Mauritius districts |
| `region` | No | 100 | North, South, East, West, Central |
| `postalCode` | No | 20 | |
| `country` | Yes | 100 | Default: "Mauritius" |
| `phone` | No | 20 | |
| `email` | No | 100 | Valid email format |
| `timezone` | Yes | 100 | Default: "Indian/Mauritius" |
| `capacityHeadcount` | No | 1-10000 | |
| `latitude` | No | -90 to 90 | Decimal degrees |
| `longitude` | No | -180 to 180 | Decimal degrees |

**Response (201 Created)**:
```json
{
  "success": true,
  "message": "Location created successfully",
  "id": "880e8400-e29b-41d4-a716-446655440000"
}
```

**Response (400 Bad Request)** - Duplicate location code:
```json
{
  "success": false,
  "error": "Location with code 'PL-004' already exists"
}
```

**Response (400 Bad Request)** - Validation error:
```json
{
  "success": false,
  "error": "Invalid input data",
  "errors": {
    "locationCode": ["The locationCode field is required."],
    "email": ["The Email field is not a valid e-mail address."]
  }
}
```

**cURL Example**:
```bash
curl -X POST "https://api.your-domain.com/api/location" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "locationCode": "PL-004",
    "locationName": "New Office - Port Louis",
    "locationType": "Office",
    "city": "Port Louis",
    "district": "Port Louis",
    "region": "North",
    "postalCode": "11102",
    "country": "Mauritius",
    "latitude": -20.1609,
    "longitude": 57.5012,
    "isActive": true
  }'
```

---

### 7. Update Location

Update an existing location.

**Request**:
```
PUT /api/location/{id}
```

**Parameters**:
- `id` (path, GUID): Location ID

**Request Body**: Same structure as Create Location

**Response (200 OK)**:
```json
{
  "success": true,
  "message": "Location updated successfully"
}
```

**Response (404 Not Found)**:
```json
{
  "success": false,
  "error": "Location with ID {id} not found"
}
```

**Response (400 Bad Request)** - Duplicate code:
```json
{
  "success": false,
  "error": "Location with code 'PL-001' already exists"
}
```

**cURL Example**:
```bash
curl -X PUT "https://api.your-domain.com/api/location/880e8400-e29b-41d4-a716-446655440000" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "locationCode": "PL-004",
    "locationName": "Updated Office Name",
    "locationType": "Office",
    "city": "Port Louis",
    "district": "Port Louis",
    "region": "North",
    "postalCode": "11102",
    "country": "Mauritius",
    "isActive": true
  }'
```

---

### 8. Delete Location

Soft delete a location (Admin only).

**Request**:
```
DELETE /api/location/{id}
```

**Parameters**:
- `id` (path, GUID): Location ID

**Response (200 OK)**:
```json
{
  "success": true,
  "message": "Location deleted successfully"
}
```

**Response (404 Not Found)**:
```json
{
  "success": false,
  "error": "Location with ID {id} not found"
}
```

**Response (400 Bad Request)** - Has assigned devices:
```json
{
  "success": false,
  "error": "Cannot delete location that has biometric devices assigned. Please reassign or delete devices first."
}
```

**Response (400 Bad Request)** - Has assigned employees:
```json
{
  "success": false,
  "error": "Cannot delete location that has employees assigned. Please reassign employees first."
}
```

**cURL Example**:
```bash
curl -X DELETE "https://api.your-domain.com/api/location/880e8400-e29b-41d4-a716-446655440000" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

### 9. Seed Mauritius Locations

Populate database with 30 pre-configured Mauritius locations (Admin only).

**Request**:
```
POST /api/location/seed
```

**Response (200 OK)**:
```json
{
  "success": true,
  "message": "Mauritius locations seeded successfully",
  "count": 30,
  "districts": [
    "Port Louis",
    "Pamplemousses",
    "Rivière du Rempart",
    "Flacq",
    "Grand Port",
    "Savanne",
    "Plaines Wilhems",
    "Moka",
    "Black River"
  ]
}
```

**Response (400 Bad Request)** - Already seeded:
```json
{
  "success": false,
  "error": "Locations already exist. Cannot seed data."
}
```

**Seeded Locations** (30 total):

| District | Locations | Types |
|----------|-----------|-------|
| Port Louis | 3 | City (Port Louis), Villages (Baie du Tombeau, Roche Bois) |
| Pamplemousses | 3 | Village (Pamplemousses, Triolet), Town (Grand Baie) |
| Rivière du Rempart | 3 | Town (Goodlands), Villages (Cap Malheureux, Mapou) |
| Flacq | 3 | Town (Centre de Flacq), Villages (Bon Accueil, Quatre Cocos) |
| Grand Port | 3 | Towns (Mahébourg, Rose Belle), Village (Plaine Magnien) |
| Savanne | 3 | Town (Souillac), Villages (Rivière des Anguilles, Surinam) |
| Plaines Wilhems | 5 | Towns (Curepipe, Quatre Bornes, Vacoas-Phoenix, Rose Hill, Beau Bassin) |
| Moka | 3 | Villages (Moka, Quartier Militaire, Saint Pierre) |
| Black River | 3 | Villages (Tamarin, Flic en Flac, Grande Rivière Noire) |

Each location includes:
- Location code (district prefix + number)
- Type (City/Town/Village)
- District, Region, Postal Code
- Geographic coordinates (latitude/longitude)

**cURL Example**:
```bash
curl -X POST "https://api.your-domain.com/api/location/seed" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

## Error Responses

All endpoints return standardized error responses:

### 401 Unauthorized
```json
{
  "success": false,
  "error": "Unauthorized"
}
```

### 403 Forbidden
```json
{
  "success": false,
  "error": "Forbidden - Insufficient permissions"
}
```

### 500 Internal Server Error
```json
{
  "success": false,
  "error": "An error occurred while processing your request"
}
```

---

## Mauritius Districts Reference

The 9 official districts of Mauritius:

1. **Port Louis** - Capital city, urban center
2. **Pamplemousses** - North region, tourist areas
3. **Rivière du Rempart** - Northeast, coastal areas
4. **Flacq** - East coast, rural areas
5. **Grand Port** - Southeast, industrial zones
6. **Savanne** - South region, rural areas
7. **Plaines Wilhems** - Central plateau, major cities
8. **Moka** - Central, residential/university areas
9. **Black River** - West coast, tourist resorts

---

## Location Types

Common location types in the system:

- **City**: Major urban centers (Port Louis, etc.)
- **Town**: Medium-sized urban areas (Curepipe, Quatre Bornes, etc.)
- **Village**: Small settlements
- **Office**: Corporate offices
- **Factory**: Manufacturing facilities
- **Warehouse**: Storage facilities
- **Branch**: Company branches

---

## Integration Examples

### TypeScript/Angular Example

```typescript
import { HttpClient } from '@angular/common/http';

interface LocationFilter {
  district?: string;
  type?: string;
  searchTerm?: string;
  page?: number;
  pageSize?: number;
}

@Injectable()
export class LocationService {
  constructor(private http: HttpClient) {}

  getLocations(filter: LocationFilter) {
    return this.http.get('/api/location', { params: filter as any });
  }

  searchLocations(query: string) {
    return this.http.get(`/api/location/search`, {
      params: { q: query }
    });
  }

  createLocation(data: any) {
    return this.http.post('/api/location', data);
  }
}
```

### Python Example

```python
import requests

class LocationAPI:
    def __init__(self, base_url, token):
        self.base_url = base_url
        self.headers = {
            'Authorization': f'Bearer {token}',
            'Content-Type': 'application/json'
        }

    def get_locations(self, district=None, page=1, page_size=20):
        params = {'Page': page, 'PageSize': page_size}
        if district:
            params['District'] = district

        response = requests.get(
            f'{self.base_url}/api/location',
            headers=self.headers,
            params=params
        )
        return response.json()

    def search_locations(self, query):
        response = requests.get(
            f'{self.base_url}/api/location/search',
            headers=self.headers,
            params={'q': query}
        )
        return response.json()
```

---

## Testing Checklist

- [ ] Authentication required for all endpoints
- [ ] Role-based authorization enforced
- [ ] Pagination works correctly
- [ ] Filtering by district, type, region
- [ ] Search functionality (minimum 2 characters)
- [ ] Validation errors returned for invalid input
- [ ] Duplicate location codes prevented
- [ ] Soft delete prevents deletion with dependencies
- [ ] Audit logs created for all modifications
- [ ] Seeding works once only
- [ ] Geographic coordinates within valid ranges
- [ ] All 9 Mauritius districts supported

---

## Performance Considerations

- **Pagination**: Default page size is 20, maximum is 100
- **Search**: Limited to 50 results
- **Caching**: Consider caching districts list (rarely changes)
- **Indexes**: Ensure database indexes on `District`, `LocationCode`, `LocationName`

---

## Security Features

1. **JWT Authentication**: All endpoints require valid JWT token
2. **Role-Based Authorization**: Admin, HR, Manager roles enforced
3. **Audit Logging**: All create/update/delete operations logged
4. **Input Validation**: DataAnnotations + FluentValidation
5. **SQL Injection Protection**: Entity Framework parameterized queries
6. **XSS Prevention**: JSON serialization escapes special characters

---

## Support

For issues or questions:
- **Documentation**: This file
- **Source Code**: `/workspaces/HRAPP/src/HRMS.API/Controllers/LocationController.cs`
- **Service Layer**: `/workspaces/HRAPP/src/HRMS.Infrastructure/Services/LocationService.cs`
- **DTOs**: `/workspaces/HRAPP/src/HRMS.Application/DTOs/LocationDtos/`

---

**Last Updated**: 2025-11-12
**API Version**: v1.0
**Status**: Production Ready
