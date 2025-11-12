# Location API - Quick Reference Card

## Base URL
```
https://api.your-domain.com/api/location
```

## Authentication
All endpoints require JWT Bearer token:
```
Authorization: Bearer <your-jwt-token>
```

---

## Endpoints Summary

| Method | Endpoint | Auth Role | Description |
|--------|----------|-----------|-------------|
| GET | `/api/location` | Admin/HR/Manager | Get filtered locations |
| GET | `/api/location/{id}` | Admin/HR/Manager | Get by ID |
| GET | `/api/location/districts` | Admin/HR/Manager | Get districts |
| GET | `/api/location/district/{district}` | Admin/HR/Manager | Get by district |
| GET | `/api/location/search?q={query}` | Admin/HR/Manager | Search |
| POST | `/api/location` | Admin/HR | Create |
| PUT | `/api/location/{id}` | Admin/HR | Update |
| DELETE | `/api/location/{id}` | Admin | Delete |
| POST | `/api/location/seed` | Admin | Seed data |

---

## Common Queries

### Get all locations
```bash
GET /api/location
```

### Filter by district
```bash
GET /api/location?District=Port%20Louis
```

### Filter by type
```bash
GET /api/location?Type=City
```

### Search
```bash
GET /api/location/search?q=Port
```

### Paginate
```bash
GET /api/location?Page=2&PageSize=20
```

### Sort
```bash
GET /api/location?SortBy=LocationName&SortDirection=desc
```

### Combined filters
```bash
GET /api/location?District=Port%20Louis&Type=City&Page=1&PageSize=10&SortBy=LocationName
```

---

## Request Body (Create/Update)

```json
{
  "locationCode": "PL-010",
  "locationName": "Office Name",
  "locationType": "Office",
  "addressLine1": "123 Street",
  "addressLine2": "Suite 100",
  "city": "Port Louis",
  "district": "Port Louis",
  "region": "North",
  "postalCode": "11101",
  "country": "Mauritius",
  "phone": "+230 123 4567",
  "email": "office@company.com",
  "workingHoursJson": "{...}",
  "timezone": "Indian/Mauritius",
  "locationManagerId": "guid",
  "capacityHeadcount": 150,
  "latitude": -20.1609,
  "longitude": 57.5012,
  "isActive": true
}
```

### Required Fields
- `locationCode` (unique)
- `locationName`
- `country` (default: "Mauritius")
- `timezone` (default: "Indian/Mauritius")

---

## Mauritius Districts (9)

1. Black River
2. Flacq
3. Grand Port
4. Moka
5. Pamplemousses
6. Plaines Wilhems
7. Port Louis
8. Rivière du Rempart
9. Savanne

---

## Location Types

- City
- Town
- Village
- Office
- Factory
- Warehouse
- Branch

---

## Response Format

### Success (200/201)
```json
{
  "success": true,
  "data": { ... },
  "pagination": { ... }  // if paginated
}
```

### Error (400/404/500)
```json
{
  "success": false,
  "error": "Error message",
  "errors": { ... }  // validation errors
}
```

---

## Quick cURL Examples

### Get districts
```bash
curl -X GET "https://api.your-domain.com/api/location/districts" \
  -H "Authorization: Bearer TOKEN"
```

### Search
```bash
curl -X GET "https://api.your-domain.com/api/location/search?q=Port" \
  -H "Authorization: Bearer TOKEN"
```

### Create
```bash
curl -X POST "https://api.your-domain.com/api/location" \
  -H "Authorization: Bearer TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "locationCode": "PL-010",
    "locationName": "New Office",
    "locationType": "Office",
    "city": "Port Louis",
    "district": "Port Louis",
    "region": "North",
    "country": "Mauritius",
    "isActive": true
  }'
```

### Seed data (Admin only, run once)
```bash
curl -X POST "https://api.your-domain.com/api/location/seed" \
  -H "Authorization: Bearer TOKEN"
```

---

## Filter Parameters

| Parameter | Type | Example |
|-----------|------|---------|
| District | string | `Port Louis` |
| Type | string | `City`, `Town`, `Village` |
| Region | string | `North`, `South`, `East`, `West`, `Central` |
| PostalCode | string | `11101` |
| IsActive | boolean | `true`, `false` |
| SearchTerm | string | `Port` |
| LocationManagerId | GUID | `550e8400-...` |
| Page | integer | `1` (default) |
| PageSize | integer | `20` (default, max 100) |
| SortBy | string | `LocationName`, `LocationCode`, `District`, `City`, `CreatedAt` |
| SortDirection | string | `asc` (default), `desc` |

---

## Status Codes

| Code | Meaning |
|------|---------|
| 200 | Success |
| 201 | Created |
| 400 | Bad Request (validation error) |
| 401 | Unauthorized (missing/invalid token) |
| 403 | Forbidden (insufficient permissions) |
| 404 | Not Found |
| 500 | Internal Server Error |

---

## Common Errors

### Duplicate Code
```json
{
  "success": false,
  "error": "Location with code 'PL-001' already exists"
}
```

### Cannot Delete (Dependencies)
```json
{
  "success": false,
  "error": "Cannot delete location that has biometric devices assigned"
}
```

### Validation Error
```json
{
  "success": false,
  "error": "Invalid input data",
  "errors": {
    "locationCode": ["Required"],
    "email": ["Invalid email"]
  }
}
```

---

## Files Reference

- **Controller**: `/workspaces/HRAPP/src/HRMS.API/Controllers/LocationController.cs`
- **Service**: `/workspaces/HRAPP/src/HRMS.Infrastructure/Services/LocationService.cs`
- **DTOs**: `/workspaces/HRAPP/src/HRMS.Application/DTOs/LocationDtos/`
- **Documentation**: `/workspaces/HRAPP/docs/API_LOCATION_ENDPOINTS.md`

---

**Last Updated**: 2025-11-12
