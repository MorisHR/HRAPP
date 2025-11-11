# Subscription Dashboard Testing Checklist

## Pre-Testing Setup
- [ ] Backend API is running
- [ ] SuperAdmin user exists in database
- [ ] Sample subscription payment data exists
- [ ] Frontend dev server is running (`npm start`)

## Authentication Tests
- [ ] Login as SuperAdmin successfully
- [ ] Access denied for non-SuperAdmin users
- [ ] Subscription menu item visible in sidebar
- [ ] Navigation to `/admin/subscriptions` works

## Dashboard Loading Tests
- [ ] Dashboard loads without errors
- [ ] Loading spinner displays during data fetch
- [ ] Error message displays if API fails
- [ ] Analytics cards populate with data
- [ ] Charts render correctly

## Analytics Cards Tests
- [ ] ARR (Annual Recurring Revenue) displays
- [ ] MRR (Monthly Recurring Revenue) displays
- [ ] Churn Rate displays with %
- [ ] LTV (Lifetime Value) displays
- [ ] Active Subscriptions count displays
- [ ] Total Revenue displays

## Chart Tests
### Revenue Trend Chart
- [ ] Line chart renders
- [ ] Monthly labels display correctly
- [ ] Revenue data line shows
- [ ] Subscription count line shows
- [ ] Dual Y-axis works
- [ ] Legend displays

### Tier Distribution Chart
- [ ] Bar chart renders
- [ ] All tiers display (Free, Basic, Pro, Enterprise)
- [ ] Count bars show
- [ ] Revenue bars show
- [ ] Colors differentiate bars
- [ ] Legend displays

## Tab Navigation Tests
- [ ] Overdue Payments tab opens
- [ ] Pending Payments tab opens
- [ ] Upcoming Renewals tab opens
- [ ] Recently Suspended tab opens
- [ ] Tab badges show correct counts
- [ ] Tab icons display

## Overdue Payments Tab
- [ ] Table displays overdue payments
- [ ] Tenant name and subdomain show
- [ ] Amount formatted as currency
- [ ] Due date displays
- [ ] Days overdue calculated correctly
- [ ] Status chip shows "Overdue"
- [ ] Tier chip displays with color
- [ ] Action buttons present

## Pending Payments Tab
- [ ] Table displays pending payments
- [ ] Relative dates show ("in 5 days")
- [ ] Status chip shows "Pending"
- [ ] All action buttons present

## Upcoming Renewals Tab
- [ ] Table displays next 30 days renewals
- [ ] Days until renewal badge shows
- [ ] Sorted by date (nearest first)

## Recently Suspended Tab
- [ ] Suspended tenants list displays
- [ ] Days suspended calculated
- [ ] Overdue amount shows
- [ ] Suspended date displays

## Action Button Tests
### Record Payment Button
- [ ] Button visible on overdue/pending rows
- [ ] Click triggers function
- [ ] (Future: Opens dialog)

### Send Reminder Button
- [ ] Button visible on overdue/pending rows
- [ ] Click sends API request
- [ ] Success alert displays
- [ ] Reminder date updates

### View Details Button
- [ ] Button visible on all payment rows
- [ ] Click opens payment detail dialog
- [ ] Dialog displays correctly

## Payment Detail Dialog Tests
- [ ] Dialog opens on "View Details"
- [ ] Close button works
- [ ] Payment info section displays
  - [ ] Payment ID
  - [ ] Tenant name and subdomain
  - [ ] Amount
  - [ ] Due date
  - [ ] Status chip
  - [ ] Tier chip
  - [ ] Payment date (if paid)
  - [ ] Reminder date (if sent)
- [ ] Subscription summary displays
  - [ ] Current tier
  - [ ] Start date
  - [ ] Total payments count
  - [ ] Total paid (green)
  - [ ] Total overdue (red)
- [ ] Payment history displays
  - [ ] All historical payments listed
  - [ ] Status shown for each
  - [ ] Current payment highlighted
  - [ ] Dates formatted correctly
- [ ] Action buttons in dialog work
  - [ ] Record Payment button
  - [ ] Send Reminder button

## Responsive Design Tests
### Desktop (>768px)
- [ ] Multi-column grid layout
- [ ] Analytics cards in 3 columns
- [ ] Charts side-by-side
- [ ] Tables full width

### Tablet (768px)
- [ ] 2-column grid
- [ ] Analytics cards in 2 columns
- [ ] Charts stacked
- [ ] Horizontal scroll on tables

### Mobile (<768px)
- [ ] Single column stack
- [ ] Analytics cards full width
- [ ] Charts full width
- [ ] Tables scroll horizontally
- [ ] Dialog width 90vw

## Dark Mode Tests
- [ ] Toggle dark mode
- [ ] Dashboard adapts to dark theme
- [ ] Charts update colors
- [ ] Tables readable
- [ ] Dialog dark themed
- [ ] All text readable

## Currency & Date Formatting Tests
- [ ] Amounts show $ symbol
- [ ] Amounts show 2 decimals
- [ ] Thousands separator (commas)
- [ ] Dates show "Month DD, YYYY"
- [ ] Relative dates work ("2 days ago", "in 5 days")

## Error Handling Tests
- [ ] Network error shows error message
- [ ] Retry button works
- [ ] Empty states display (no data)
- [ ] Loading states work
- [ ] API errors handled gracefully

## Performance Tests
- [ ] Dashboard loads < 3 seconds
- [ ] Charts render smoothly
- [ ] No console errors
- [ ] No memory leaks
- [ ] Lazy loading works

## Browser Compatibility
- [ ] Chrome/Edge (latest)
- [ ] Firefox (latest)
- [ ] Safari (latest)
- [ ] Mobile browsers

## Refresh Tests
- [ ] Manual refresh button works
- [ ] Data reloads correctly
- [ ] Loading state shows
- [ ] Charts update

## Navigation Tests
- [ ] Back button works
- [ ] Sidebar navigation works
- [ ] Page title updates
- [ ] Breadcrumbs work (if implemented)

---

## Test Results Template

**Tested By:** _______________
**Date:** _______________
**Environment:** Production / Staging / Local
**Browser:** _______________
**Result:** PASS / FAIL

**Issues Found:**
1. 
2. 
3. 

**Notes:**


---

## Quick Test Commands

```bash
# Start backend
cd /workspaces/HRAPP/src/HRMS.API
dotnet run

# Start frontend
cd /workspaces/HRAPP/hrms-frontend
npm start

# Build frontend
npm run build

# Run tests
npm test
```

## Test Data Setup (SQL)

```sql
-- Create test SuperAdmin
INSERT INTO Users (Email, PasswordHash, Role, FirstName, LastName)
VALUES ('superadmin@hrms.com', 'hash', 'SuperAdmin', 'Super', 'Admin');

-- Create test subscription payment
INSERT INTO SubscriptionPayments (TenantId, Amount, DueDate, Status, SubscriptionTier)
VALUES (1, 199.99, '2025-11-15', 'Pending', 'Professional');
```
