# ZKTeco SDK Files

**IMPORTANT**: You need to obtain the ZKTeco SDK files and place them in this directory.

## Required Files

1. **zkemkeeper.dll** - Main SDK library (COM DLL)
2. **zkemkeeper.tlb** - Type library (optional)

## Where to Get SDK

### Official Source:
https://www.zkteco.com/en/download_category/standalone-sdk

Download the "**Standalone SDK**" package for .NET development.

## Installation Steps

### Step 1: Download SDK
1. Visit ZKTeco website
2. Download "Standalone SDK"
3. Extract the ZIP file

### Step 2: Copy Files
Copy `zkemkeeper.dll` from the SDK package to this directory:
```
/workspaces/HRAPP/src/HRMS.DeviceSync/SDK/zkemkeeper.dll
```

### Step 3: Register COM DLL (Windows Only)

**Option A: Register Globally** (Requires Admin)
```cmd
cd C:\Path\To\SDK
regsvr32 zkemkeeper.dll
```

**Option B: Use Without Registration** (Recommended)
The middleware will load the DLL from this SDK folder automatically.
No registration needed!

## Verification

To verify the SDK is properly installed, run:

```powershell
# Check if file exists
Test-Path "./zkemkeeper.dll"

# Check if COM registration works (optional)
$type = [Type]::GetTypeFromProgID("zkemkeeper.ZKEM")
if ($type) {
    Write-Host "✅ SDK is registered and ready"
} else {
    Write-Host "ℹ️  SDK will be loaded from file (no registration needed)"
}
```

## SDK Version Information

- **Recommended**: Latest version (2024+)
- **Minimum**: Version from 2018 or later
- **Platform**: Windows x86 or x64
- **Framework**: Compatible with .NET 9.0

## Troubleshooting

### Error: "SDK not found"
- Ensure `zkemkeeper.dll` is in this SDK folder
- Check file permissions (must be readable)
- Try registering with `regsvr32` if using COM

### Error: "Cannot load DLL"
- Make sure you have the correct version (32-bit vs 64-bit)
- Install Visual C++ Redistributable if needed
- Check Windows event log for details

## License

ZKTeco SDK is proprietary software owned by ZKTeco Inc.
Ensure you have proper licensing before use in production.

Contact ZKTeco or your local distributor for licensing information.

## Support

For SDK-related issues:
- ZKTeco Official Support: https://www.zkteco.com/en/support
- SDK Documentation: Included in SDK download package
- Developer Forum: ZKTeco developer community

---

**Note**: The middleware will work without the SDK if you're just testing the code structure,
but you'll need the actual DLL files to connect to real ZKTeco devices.
