# Foodera — Yerli quraşdırma (internetsiz restoranlar üçün)

Sistem restoranın öz kompüterində işləyir, terminallar (kassa, ofisiant planşetləri) ona restoranın
Wi-Fi / kabel şəbəkəsi ilə qoşulur. İnternet lazım deyil. Lisenziya imzalı açarla, internetsiz yoxlanılır.

## Bir dəfəlik: platforma açarları (yalnız sizin mərkəzi serverdə)

Lisenziya açarlarını imzalamaq üçün bir cüt açar yaradın:

```powershell
dotnet run --project src/API -- --generate-license-keypair
```

- `Licensing:PrivateKeyPem` — **yalnız mərkəzi serverə** (gizli saxlayın, heç vaxt restorana verməyin).
- `Licensing:PublicKeyPem` — mərkəzi serverə **və** hər yerli quraşdırmaya (`.env` → `LICENSE_PUBLIC_KEY`).

Açarları itirməyin: yeni cüt yaratsanız, köhnə açarla imzalanmış lisenziyalar işləməyəcək.

## Restoranda quraşdırma

Tələblər: Windows 10/11 kompüter (min. 8 GB RAM), Docker Desktop, sabit lokal IP.

1. Bu qovluğu (`deploy/local`) və layihəni kompüterə köçürün.
2. PowerShell-də: `.\install.ps1` — ilk dəfə `.env` faylı açılacaq, doldurun:
   - `SERVER_IP` — bu kompüterin lokal IP-si (routerdə sabitləyin),
   - şifrələr, `JWT_SECRET`, `LICENSE_PUBLIC_KEY`, SuperAdmin hesabı.
3. Yenidən `.\install.ps1` — sistem qurulur və kompüter yenidən başlayanda avtomatik açılır.
4. `http://localhost:3000` → SuperAdmin ilə daxil olun → **Şirkətlər** → restoranın şirkətini yaradın.
   **Company Code mərkəzi serverdəki ilə eyni olmalıdır** — lisenziya açarı şirkəti bu kodla tanıyır.
5. Mərkəzi serverdə: Şirkətlər → Lisenziya → Quraşdırma növü **Yerli** → ödənişi qeyd edin
   (**Ödənildi, uzat**) → **Açar yarat** → açarı restorana göndərin.
6. Restoranda: `http://localhost:3000/license` → açarı yapışdırın → **Aktivləşdir**.
7. Terminallarda brauzerdə: `http://SERVER_IP:3000`.

## Hər ay

Ödəniş alanda mərkəzi serverdə **Ödənildi, uzat** → **Açar yarat** → yeni açarı restorana göndərin
(WhatsApp / SMS). Restoran onu `/license` səhifəsində daxil edir. Bitməyə 3 gün qalmış həm restoranın
ekranında, həm sizin paneldə xatırlatma çıxır. Açarın vaxtı bitəndə sistem yeni açar istəyir.

## Ehtiyat nüsxə

- İndi: `.\backup.ps1` → `backups\` qovluğunda (son 14 nüsxə saxlanılır).
- Gündəlik avtomatik (saat 03:00): `.\backup.ps1 -Schedule`
- `backups\` qovluğunu vaxtaşırı flash-karta və ya başqa diskə köçürün.

## Yeniləmə

Layihənin yeni versiyasını köçürüb `.\install.ps1` işə salın — baza avtomatik yenilənir, data qalır.
Yeniləmədən əvvəl `.\backup.ps1` edin.
