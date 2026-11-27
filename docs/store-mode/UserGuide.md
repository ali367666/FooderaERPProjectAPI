# Mağaza rejimi — qısa təlimat

## İlk quraşdırma (admin)
1. Şirkətdə mağaza rejimini aktiv edin (`moduleDataSecimi`).
2. Rollara `Store*` icazələrini verin (məs. Kassir: `StoreSale.Create`, `StoreCash.Open`, `StoreCash.Close`; Menecer: əlavə `StoreSale.Approve`, `StoreSale.Discount`, `StoreSale.ChangePrice`, `StoreSale.Cancel`, `StoreSale.Return`).
3. **Kassalar** → kassa yaradın (filial + anbar seçilir). Qaydaları yazın: endirim limiti, bonus faizləri və müddəti, mənfi stok, fiskal cihaz.
4. **Məhsullar** → kateqoriya və məhsul əlavə edin, ya da CSV idxal edin (ixracdakı sütunlarla; `;` ayırıcı).
5. **Alışlar** → təchizatçıdan mal qəbulu (təsdiqlənəndə stok artır, borc yaranır).

## Kassir
- **Növbəni aç** → ilkin nağd daxil edin. Barkodu skan edin (təkrar skan miqdarı artırır) və ya ada görə axtarın; 50 sürətli düymə kassa üzrə təyin olunur.
- Çəkili mal: tərəzi etiketi (13 rəqəm, `2` ilə başlayır) skan olunanda çəki avtomatik yazılır; ya da miqdarı əl ilə daxil edin.
- IMEI/serial izlənən məhsulda hər vahid üçün nömrə seçilir.
- Ödəniş: nağd/kart/bonus/borc qarışıq ola bilər; qaytarılacaq pul avtomatik hesablanır.
- Satışı saxlamaq (hold) və sonra açmaq mümkündür.
- Limitdən çox endirim və ya qiymət dəyişikliyi icazəniz yoxdursa menecerin istifadəçi adı/parolu istənir.
- **X-hesabat** növbəni bağlamır; **Z-hesabat** (növbəni bağla) faktiki nağdı sayıb növbəni bağlayır.
- Bağlı növbəni yalnız `StoreCash.Reopen` icazəsi olan istifadəçi səbəblə yenidən aça bilər.

## Qaytarma / dəyişdirmə
**Qaytarma** → çek nömrəsi ilə satış tapılır; qaytarılan miqdar əvvəlki qaytarmalar çıxılandan çox ola bilməz. Məhsulun vəziyyəti seçilir (satışa yararlı / istifadəyə yararsız / yoxlanılmalı). Bonus və borc ilkin satışa uyğun düzəldilir.

## İnternet kəsiləndə
- Başlıqdakı şəbəkə işarəsi vəziyyəti göstərir. Oflayn satışlar bu cihazda saxlanır və bağlantı qayıdanda avtomatik göndərilir (eyni satış iki dəfə yazılmır).
- Məhsul və qiymətlər cihazdakı surətdən oxunur; anbar qalığı son yenilənmə anına qədər dəqiqdir.
- Server satışı qəbul etməsə (məs. qalıq çatmır) satış "Konflikt/Xəta" kimi **silinmədən** qalır; işarənin pəncərəsində səbəbi görünür, yenidən cəhd və ya silmək olar.
- Oflayn satışda menecer təsdiqi mümkün deyil.

## Hesabatlar
Satış və mənfəət, stok, kassa, müştəri borcları (yaşlandırma), bonus; tarix, anbar, kateqoriya filtri. Maliyyə hesabatları `StoreReport.Finance` icazəsi tələb edir.

## Əlavə imkanlar
- **Çek:** Kassalar səhifəsində çek eni (58/80 mm), başlıq və son sətir. "Satışlar" səhifəsindən çap olunan çek "DUBLİKAT" işarəsi daşıyır.
- **Çeksiz satış:** şirkət qaydasında açılır və kassirdə "Çeksiz satış" icazəsi olmalıdır; satış yenə də qeyd edilir (fiskal tələb varsa fiskal göndərilir).
- **Ödənişin ləğvi:** müştərinin borc ödənişi (növbə açıq olarkən) və təchizatçıya ödəniş səbəblə ləğv edilir; ödəniş silinmir, əks yazılışla ləğv olunur.
- **Rol şablonları:** Kassalar səhifəsində rola Kassir/Filial meneceri/Anbardar/Mühasib/Auditor icazə dəstini əlavə edin.
- **Hesabatlar:** hər tabda "Excel/CSV ixrac"; "Çeklər, ləğvlər, qaytarmalar" tabı.
