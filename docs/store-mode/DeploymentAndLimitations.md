# Mağaza rejimi — quraşdırma, ehtiyat nüsxə, məlum məhdudiyyətlər

## Quraşdırma
Mağaza rejimi eyni backend və eyni Next tətbiqinin içindədir (`/store`); ayrıca deploy lazım deyil.

1. Miqrasiyaları tətbiq edin (API-ni dayandırın):
   ```
   cd src
   dotnet ef database update --project Infrastructure --startup-project API --configuration Release
   ```
   Mağaza miqrasiyaları: `Add_StoreMode_Foundation`, `_Phase2` … `_Phase6`, `_Phase7_Campaign`, `_Phase8_Access_Pricing`, `_Phase9_Receipt_Reversal` (hamısı birlikdə tətbiq olunmalıdır).
2. Konfiqurasiya mühit dəyişənləri ilə verilir (bağlantı sətri, JWT açarı); gizli açarlar repozitoriyaya yazılmır.
3. Rollara `Store*` icazələrini verin (icazələr `AppPermissions` sabitlərindən avtomatik tapılır).

## Ehtiyat nüsxə və bərpa (SQL Server)
```
BACKUP DATABASE [<db>] TO DISK = N'/var/opt/mssql/backup/<db>.bak' WITH COMPRESSION, CHECKSUM;
RESTORE VERIFYONLY FROM DISK = N'/var/opt/mssql/backup/<db>.bak';
-- bərpa:
RESTORE DATABASE [<db>] FROM DISK = N'/var/opt/mssql/backup/<db>.bak' WITH REPLACE;
```
Gündəlik tam, saatlıq log nüsxəsi tövsiyə olunur. Bərpadan sonra stok/borc uzlaşdırması üçün: balans cədvəllərini hərəkət/ledger cədvəllərindən yenidən hesablayıb müqayisə edin.

## Məlum məhdudiyyətlər (təhvil siyahısı)
- **Testlər:** `src/Tests` (xUnit), 35 test: unit (bonus müddəti, audit, CSV, qiymət, filial girişi) və integration (real handler + EF InMemory: satış/ləğv/qaytarma, alış, transfer, sinxron idempotentliyi və konflikt). InMemory SQL Server-in eyni anda iki satış davranışını yoxlamır; UI testləri yoxdur (`cd src/Tests && dotnet test`).
- **Oflayn:** yalnız satış növbəyə düşür (qaytarma, ödəniş və s. yox); növbə brauzer yaddaşındadır, ayrıca SQLite agent yoxdur.
- **Fiskal cihaz, bank terminalı, tərəzi (protokol), printer modeli:** adapter interfeysi hazırdır, konkret cihaz inteqrasiyası yoxdur (bax Decision Log #9, #10).
- **Filial məhdudiyyəti** var (Kassalar → "İstifadəçinin filial girişi"): kassa, növbə, satış, stok və hesabatlara tətbiq olunur. Alış, transfer, inventarizasiya və qaytarmaya da tətbiq olunur (anbar/filial üzrə). Müştəri və təchizatçılar şirkət səviyyəsindədir (filiala bağlı deyil). Anbar üzrə ayrıca məhdudiyyət yoxdur (filialın anbarları).
- **Qiymət:** topdan və tarix aralıqlı kampaniya qiyməti var (kassa kampaniya qiymətini avtomatik tətbiq edir); filial üzrə qiymət siyahısı var (məhsul → "Filial qiymətləri"); üstünlük: kampaniya > filial qiyməti > normal qiymət.
- **Audit:** əmr səviyyəsində (kim/nə/nə vaxt/sorğu); əvvəlki dəyər yalnız məhsul dəyişikliyi və qaydalar üçün saxlanır.
- **Bonus müddəti** hər 6 saatdan bir fon işi ilə və balansa baxanda/istifadədə yazılır.
- **Pagination/sorting:** məhsul, satış siyahılarında var; bəzi digər siyahılarda yoxdur.
- **API versiyası:** mağaza API-si həm `/api/store`, həm `/api/v1/store` altında işləyir (digər modullar versiyasızdır). **SignalR:** mağaza bildirişləri `/hubs/store` ilə canlı gəlir (qoşulma alınmasa 2 dəqiqəlik sorğu qalır). **İngilis dili:** başlıqdakı AZ/EN düyməsi ilə; ekran mətnləri `lib/store/i18n-en.ts` lüğətindən (təxminən 600 mətn) yerində dəyişdirilir. Lüğətdə olmayan mətnlər (server xətaları, dinamik cümlələr, çap olunan çek) Azərbaycan dilində qalır; yeni mətn əlavə edəndə lüğətə də əlavə edilməlidir.

## Hələ edilməyənlər (qəsdən)
- **Brend və vergi qrupu** ayrıca cədvəl deyil, məhsulda mətn/faiz sahəsidir; **məhsul atributları** yoxdur.
- **"Digər" qarşı tərəf növü** yoxdur (yalnız təchizatçı və müştəri).
- **Oflayn qaytarma/borc ödənişi:** SRS §14.1 yalnız satış, ödəniş və çek nömrəsi növbəsini tələb edir; qaytarma üçün əvvəlki satışı serverdən tapmaq lazım olduğundan oflayn dəstək yoxdur.
- **Məhsul hərəkəti hesabatında təchizatçı/lot** sütunları yoxdur (partiya uçotu yoxdur).
- **Kassa mədaxil/məxaric** əməliyyatlarının ayrıca sənəd nömrəsi yoxdur (növbə və səbəblə izlənir).
- **Deploy:** istehsal quraşdırması, backup skripti və monitorinq qurulmayıb (təlimat yalnız yazılıb).
- **ERD:** `docs/store-mode/ERD.md` EF modelindən testlərlə avtomatik yenilənir.
