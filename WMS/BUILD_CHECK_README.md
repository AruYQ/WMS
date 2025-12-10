# Build Check System untuk WMS Project

## Overview
Sistem ini membantu memastikan tidak ada compilation error setelah melakukan perubahan pada kode.

## Scripts yang Tersedia

### 1. build-check.ps1
Script untuk melakukan build check sekali jalan.

**Cara Penggunaan:**
```powershell
.\build-check.ps1
```

**Output:**
- ✅ BUILD SUCCESS! - Jika tidak ada error
- ❌ BUILD FAILED! - Jika ada compilation error
- ⚠️ Warning count - Menampilkan jumlah warning (non-critical)

### 2. auto-build-check.ps1
Script untuk monitoring file changes dan otomatis menjalankan build check.

**Cara Penggunaan:**
```powershell
.\auto-build-check.ps1
```

**Fitur:**
- Monitor perubahan file dengan ekstensi: .cs, .cshtml, .js, .css
- Otomatis jalankan build check saat ada perubahan
- Real-time monitoring
- Press Ctrl+C untuk stop

## Workflow yang Direkomendasikan

### Setiap Kali Ada Perubahan:
1. **Manual Check:**
   ```powershell
   .\build-check.ps1
   ```

2. **Auto Monitoring (untuk development aktif):**
   ```powershell
   .\auto-build-check.ps1
   ```

### Sebelum Commit:
```powershell
.\build-check.ps1
```
Pastikan status: ✅ BUILD SUCCESS!

## Status Build

| Status | Arti | Action |
|--------|------|--------|
| ✅ BUILD SUCCESS! | Tidak ada error | Lanjutkan development |
| ❌ BUILD FAILED! | Ada compilation error | Fix error dulu |
| ⚠️ Warning | Ada warning (non-critical) | Optional fix |

## Tips

1. **Jalankan build check** setiap kali selesai mengubah file .cs
2. **Gunakan auto monitoring** saat development aktif
3. **Fix error** sebelum melanjutkan development
4. **Warning** tidak perlu di-fix kecuali critical

## Troubleshooting

### Jika Script Tidak Bisa Dijalankan:
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Jika Build Failed:
1. Check error message
2. Fix compilation error
3. Run build check lagi
4. Ulangi sampai success

## Integration dengan IDE

### Visual Studio:
- Build check otomatis saat build
- Error akan muncul di Error List

### VS Code:
- Install C# extension
- Error akan muncul di Problems panel

## Best Practices

1. **Always run build check** setelah perubahan besar
2. **Fix error immediately** - jangan ditunda
3. **Use auto monitoring** untuk development session panjang
4. **Check before commit** - pastikan clean build
