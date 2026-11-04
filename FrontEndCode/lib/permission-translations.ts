const PERMISSION_LABELS: Record<string, string> = {
  "Company.View": "Şirkətə bax",
  "Company.Create": "Şirkət yarat",
  "Company.Update": "Şirkəti redaktə et",
  "Company.Delete": "Şirkəti sil",

  "CompanySettings.View": "Tənzimləmələrə bax",
  "CompanySettings.Update": "Tənzimləmələri dəyiş",

  "User.View": "İstifadəçilərə bax",
  "User.Create": "İstifadəçi yarat",
  "User.Update": "İstifadəçini redaktə et",
  "User.Delete": "İstifadəçini sil",

  "Restaurant.View": "Filiala bax",
  "Restaurant.Create": "Filial yarat",
  "Restaurant.Update": "Filialı redaktə et",
  "Restaurant.Delete": "Filialı sil",

  "StockCategory.View": "Anbar kateqoriyalarına bax",
  "StockCategory.Create": "Anbar kateqoriyası yarat",
  "StockCategory.Update": "Anbar kateqoriyasını redaktə et",
  "StockCategory.Delete": "Anbar kateqoriyasını sil",

  "Warehouse.View": "Anbara bax",
  "Warehouse.Create": "Anbar yarat",
  "Warehouse.Update": "Anbarı redaktə et",
  "Warehouse.Delete": "Anbarı sil",

  "StockItem.View": "Stok məhsullarına bax",
  "StockItem.Create": "Stok məhsulu yarat",
  "StockItem.Update": "Stok məhsulunu redaktə et",
  "StockItem.Delete": "Stok məhsulunu sil",

  "WarehouseStock.View": "Anbar qalığına bax",
  "WarehouseStock.Create": "Anbar sənədi yarat",
  "WarehouseStock.Update": "Anbar sənədini redaktə et",
  "WarehouseStock.Delete": "Anbar sənədini sil",

  "StockRequest.View": "Anbar sorğularına bax",
  "StockRequest.Create": "Anbar sorğusu yarat",
  "StockRequest.Update": "Anbar sorğusunu redaktə et",
  "StockRequest.Delete": "Anbar sorğusunu sil",
  "StockRequest.Approve": "Anbar sorğusunu təsdiqlə",
  "StockRequest.Reject": "Anbar sorğusunu rədd et",
  "StockRequest.Submit": "Anbar sorğusunu göndər",
  "StockRequest.Recall": "Anbar sorğusunu geri çağır",

  "RestaurantTable.View": "Masalara bax",
  "RestaurantTable.Create": "Masa yarat",
  "RestaurantTable.Update": "Masanı redaktə et",
  "RestaurantTable.Delete": "Masanı sil",

  "Department.View": "Şöbələrə bax",
  "Department.Create": "Şöbə yarat",
  "Department.Update": "Şöbəni redaktə et",
  "Department.Delete": "Şöbəni sil",

  "Position.View": "Vəzifələrə bax",
  "Position.Create": "Vəzifə yarat",
  "Position.Update": "Vəzifəni redaktə et",
  "Position.Delete": "Vəzifəni sil",

  "Employee.View": "İşçilərə bax",
  "Employee.Create": "İşçi yarat",
  "Employee.Update": "İşçini redaktə et",
  "Employee.Delete": "İşçini sil",

  "Orders.View": "Sifarişlərə bax",
  "Orders.Create": "Sifariş yarat (masa aç)",
  "Orders.Update": "Sifarişi redaktə et",
  "Orders.Add": "Sifarişə məhsul əlavə et",
  "Orders.Serve": "Sifarişi təqdim et",
  "Orders.Pay": "Ödəniş qəbul et",

  "Payment.Create": "Ödəniş yarat",

  "Role.View": "Rollara bax",
  "Role.Create": "Rol yarat",
  "Role.Update": "Rolu redaktə et",
  "Role.Delete": "Rolu sil",
  "UserRole.Manage": "İstifadəçi rollarını idarə et",

  "Analytics.View": "Analitikaya bax",

  "MenuItem.View": "Menyu məhsullarına bax",
  "MenuItem.Create": "Menyu məhsulu yarat",
  "MenuItem.Update": "Menyu məhsulunu redaktə et",
  "MenuItem.Delete": "Menyu məhsulunu sil",

  "MenuCategory.View": "Menyu kateqoriyalarına bax",
  "MenuCategory.Create": "Menyu kateqoriyası yarat",
  "MenuCategory.Update": "Menyu kateqoriyasını redaktə et",
  "MenuCategory.Delete": "Menyu kateqoriyasını sil",

  "Kitchen.View": "Mətbəx ekranına bax",
  "Kitchen.MarkReady": "Məhsulu hazır kimi qeyd et",
  "Kitchen.StartPreparing": "Hazırlamağa başla",

  "Permissions.AuditLog.View": "Audit qeydlərinə bax",

  "StockPurchase.View": "Alışlara bax",
  "StockPurchase.Create": "Alış yarat",
  "StockPurchase.Update": "Alışı redaktə et",
  "StockPurchase.Delete": "Alışı sil",
  "StockPurchase.Submit": "Alışı göndər",
  "StockPurchase.Approve": "Alışı təsdiqlə",
  "StockPurchase.Reject": "Alışı rədd et",
  "StockPurchase.Cancel": "Təsdiqlənmiş alışı ləğv et",
  "StockPurchase.Pay": "Alış üzrə təchizatçıya ödəniş yaz",
  "StockPurchase.Return": "Alışdan təchizatçıya mal qaytar",

  "StockWriteOff.View": "Stok tənzimləməyə bax",
  "StockWriteOff.Create": "Anbardan mal sil (stok tənzimləmə)",

  "StoreProduct.View": "Mağaza: məhsullara bax",
  "StoreProduct.Manage": "Mağaza: məhsul yarat/dəyiş",
  "StoreStock.View": "Mağaza: stoka bax",
  "StoreStock.Adjust": "Mağaza: stok düzəlişi",
  "StoreSale.View": "Mağaza: satışlara bax",
  "StoreSale.Create": "Mağaza: satış et",
  "StoreSale.Cancel": "Mağaza: satışı ləğv et",
  "StoreSale.Discount": "Mağaza: endirim ver",
  "StoreSale.ChangePrice": "Mağaza: satışda qiymət dəyiş",
  "StoreSale.Return": "Mağaza: geri qaytarma / dəyişdirmə",
  "StoreSale.Approve": "Mağaza: endirim limitini aşmağa və fiskal çeki yenidən göndərməyə icazə",
  "StoreCash.Open": "Mağaza: növbə aç",
  "StoreCash.Close": "Mağaza: növbə bağla (Z)",
  "StoreCash.Reopen": "Mağaza: bağlı növbəni yenidən aç",
  "StoreSale.NoReceipt": "Mağaza: çeksiz satış",
  "StoreSale.RemoveLine": "Mağaza: səbətdən məhsul sil (olmasa menecer təsdiqi lazımdır)",
  "StoreCash.Movement": "Mağaza: kassa mədaxil/məxaric (ikisi)",
  "StoreCash.MovementIn": "Mağaza: kassaya mədaxil (olmasa menecer təsdiqi lazımdır)",
  "StoreCash.MovementOut": "Mağaza: kassadan məxaric (olmasa menecer təsdiqi lazımdır)",
  "StoreRegister.Manage": "Mağaza: kassaları idarə et",
  "StoreReport.View": "Mağaza: növbə və hesabatlara bax",
  "StoreReport.Finance": "Mağaza: maya dəyəri, mənfəət və borc hesabatları",
  "StoreSettings.Manage": "Mağaza: stok qaydalarını dəyiş",
  "StoreSupplier.View": "Mağaza: təchizatçılara bax",
  "StoreSupplier.Manage": "Mağaza: təchizatçı yarat/dəyiş",
  "StoreSupplier.Pay": "Mağaza: təchizatçıya ödəniş yaz",
  "StoreInventory.View": "Mağaza: inventarizasiyaya bax",
  "StoreInventory.Manage": "Mağaza: inventarizasiya aç / say",
  "StoreInventory.Confirm": "Mağaza: inventarizasiyanı təsdiqlə (stok düzəlişi)",
  "StoreTransfer.View": "Mağaza: transferlərə bax",
  "StoreTransfer.Manage": "Mağaza: transfer yarat / dəyiş",
  "StoreTransfer.Ship": "Mağaza: transferi göndər",
  "StoreTransfer.Receive": "Mağaza: transferi qəbul et",
  "StoreCustomer.View": "Mağaza: müştərilərə bax",
  "StoreCustomer.Manage": "Mağaza: müştəri yarat / dəyiş",
  "StoreCustomer.Pay": "Mağaza: müştəri borc ödənişi qəbul et",
  "StoreBonus.Adjust": "Mağaza: bonus düzəlişi",
  "StoreSale.Debt": "Mağaza: borca satış",
  "StorePurchase.View": "Mağaza: alışlara bax",
  "StorePurchase.Manage": "Mağaza: alış sənədi yarat/dəyiş",
  "StorePurchase.Confirm": "Mağaza: alışı təsdiqlə / ləğv et",
  "StorePurchase.Return": "Mağaza: alışı təchizatçıya qaytar",

  "CompanyDeposit.View": "Şirkət depozitinə bax",
  "CompanyDeposit.Create": "Şirkət depoziti yaz",

  "Reservation.View": "Rezervasiyalara bax",
  "Reservation.Create": "Rezervasiya yarat",
  "Reservation.Update": "Rezervasiyanı redaktə et",
  "Reservation.Delete": "Rezervasiyanı sil",
  "Reservation.Confirm": "Rezervasiyanı təsdiqlə",
  "Reservation.Cancel": "Rezervasiyanı ləğv et",

  "Discount.View": "Endirimlərə bax",
  "Discount.Create": "Endirim yarat",
  "Discount.Update": "Endirimi redaktə et",
  "Discount.Delete": "Endirimi sil",
  "Discount.Apply": "Endirimi tətbiq et",

  "Pos.MoveTable": "Masanı köçür",
  "Pos.DeleteOrder": "Sifarişi ləğv et (POS)",
  "Pos.AccessSettings": "POS tənzimləmələrinə giriş",
  "Pos.ChangePrice": "Satış qiymətini dəyiş (yalnız qaldırmaq)",
  "Pos.LowerPrice": "Satış qiymətini aşağı sal",
  "Pos.ZReport": "Z-hesabatı",
  "Pos.PrintReceipt": "Qəbz çap et",
  "Pos.OverridePrice": "Qalıcı qiymətə müdaxilə et",
  "Pos.ChangeDepartment": "Şöbəni dəyiş",
  "Pos.RedirectUser": "Ofisiantı dəyiş / başqasının masasına bax",
  "Pos.ChangeWaiter": "Masalar ekranında \"Ofisiant dəyiş\" düyməsini göstər və istifadə et",
  "Pos.EditProductInSale": "Satışda məhsulu redaktə et",
  "Pos.DeleteProductInSale": "Satışda məhsulu sil",
  "Pos.TableServiceCharge": "Masa üzrə servis haqqı tətbiq et",
  "Pos.DeleteReceipt": "Köhnə qəbzi sil",
  "Pos.WarehouseAmountChange": "Anbar məbləğini dəyiş",
  "Pos.PrintOldReceipt": "Köhnə qəbzi çap et",
  "Pos.ReturnSale": "Satışı geri qaytar (barkodla)",
  "Pos.UnlockBill": "Verilmiş hesabın kilidini aç",

  "CashRegister.View": "Kassaya bax",
  "CashRegister.Manage": "Kassa mədaxil/məxaric et",

  "Finance.View": "Maliyyə hesabatına bax",
  "Finance.Manage": "Gəlir/xərc qeydlərini idarə et",

  "DeliveryIntegration.View": "Çatdırılma inteqrasiyalarına bax",
  "DeliveryIntegration.Create": "Çatdırılma inteqrasiyası yarat",
  "DeliveryIntegration.Update": "Çatdırılma inteqrasiyasını redaktə et",
  "DeliveryIntegration.Delete": "Çatdırılma inteqrasiyasını sil",

  "RestaurantSection.View": "Filial bölmələrinə bax",
  "RestaurantSection.Create": "Filial bölməsi yarat",
  "RestaurantSection.Update": "Filial bölməsini redaktə et",
  "RestaurantSection.Delete": "Filial bölməsini sil",

  "Printer.View": "Printerlərə bax",
  "Printer.Create": "Printer yarat",
  "Printer.Update": "Printeri redaktə et",
  "Printer.Delete": "Printeri sil",
  "Printer.Print": "Çap et",

  "Counterparty.View": "Kontragentlərə bax",
  "Counterparty.Create": "Kontragent yarat",
  "Counterparty.Update": "Kontragenti redaktə et",
  "Counterparty.Delete": "Kontragenti sil",

  "FiscalDevice.View": "Fiskal kassalara bax",
  "FiscalDevice.Create": "Fiskal kassa əlavə et",
  "FiscalDevice.Update": "Fiskal kassanı redaktə et",
  "FiscalDevice.Delete": "Fiskal kassanı sil",

  "ScaleDevice.View": "Tərəzilərə bax",
  "ScaleDevice.Create": "Tərəzi əlavə et",
  "ScaleDevice.Update": "Tərəzini redaktə et",
  "ScaleDevice.Delete": "Tərəzini sil",

  "Workstation.View": "POS terminallarına bax",
  "Workstation.Create": "POS terminalı əlavə et",
  "Workstation.Update": "POS terminalını redaktə et",
  "Workstation.Delete": "POS terminalını sil",
};

const MODULE_LABELS: Record<string, string> = {
  Company: "Şirkət",
  CompanySettings: "Tənzimləmələr",
  User: "İstifadəçilər",
  Restaurant: "Filial",
  StockCategory: "Anbar kateqoriyaları",
  Warehouse: "Anbarlar",
  StockItem: "Stok məhsulları",
  WarehouseStock: "Anbar qalığı",
  StockRequest: "Anbar sorğuları",
  RestaurantTable: "Masalar",
  Department: "Şöbələr",
  Position: "Vəzifələr",
  Employee: "İşçilər",
  Orders: "Sifarişlər",
  Payment: "Ödənişlər",
  Role: "Rollar",
  UserRole: "İstifadəçi rolları",
  Analytics: "Analitika",
  MenuItem: "Menyu məhsulları",
  MenuCategory: "Menyu kateqoriyaları",
  Kitchen: "Mətbəx",
  AuditLog: "Audit qeydləri",
  StockPurchase: "Alışlar",
  StockWriteOff: "Stok tənzimləmə",
  CompanyDeposit: "Şirkət depoziti",
  StoreProduct: "Mağaza: məhsullar",
  StoreStock: "Mağaza: stok",
  StoreSale: "Mağaza: satış",
  StoreCash: "Mağaza: kassa",
  StoreRegister: "Mağaza: kassalar",
  StoreReport: "Mağaza: hesabatlar",
  StoreSettings: "Mağaza: parametrlər",
  StoreSupplier: "Mağaza: təchizatçılar",
  StorePurchase: "Mağaza: alışlar",
  StoreInventory: "Mağaza: inventarizasiya",
  StoreTransfer: "Mağaza: transfer",
  StoreCustomer: "Mağaza: müştərilər",
  StoreBonus: "Mağaza: bonus",
  Reservation: "Rezervasiyalar",
  Discount: "Endirimlər",
  Pos: "POS",
  RestaurantSection: "Filial bölmələri",
  Printer: "Printerlər",
  Counterparty: "Kontragentlər",
  FiscalDevice: "Fiskal kassalar",
  ScaleDevice: "Tərəzilər",
  Workstation: "POS terminalları",
  CashRegister: "Kassa",
  Finance: "Maliyyə",
  DeliveryIntegration: "Çatdırılma inteqrasiyaları",
};

/** Mağaza (store) wording — a store sells products, not a menu (same data, same sidebar naming). */
const STORE_PERMISSION_LABELS: Record<string, string> = {
  "MenuItem.View": "Məhsullara bax",
  "MenuItem.Create": "Məhsul yarat",
  "MenuItem.Update": "Məhsulu redaktə et",
  "MenuItem.Delete": "Məhsulu sil",
  "MenuCategory.View": "Məhsul kateqoriyalarına bax",
  "MenuCategory.Create": "Məhsul kateqoriyası yarat",
  "MenuCategory.Update": "Məhsul kateqoriyasını redaktə et",
  "MenuCategory.Delete": "Məhsul kateqoriyasını sil",
};

const STORE_MODULE_LABELS: Record<string, string> = {
  MenuItem: "Məhsullar",
  MenuCategory: "Məhsul kateqoriyaları",
};

export function translatePermissionLabel(name: string, fallback: string, storeMode = false): string {
  return (storeMode ? STORE_PERMISSION_LABELS[name] : undefined) ?? PERMISSION_LABELS[name] ?? fallback;
}

/** Permission modules that only make sense for a restaurant (tables, kitchen...); hidden when the company works as a store. */
const RESTAURANT_ONLY_MODULES = new Set([
  "RestaurantTable",
  "Reservation",
  "Kitchen",
  "RestaurantSection",
  "DeliveryIntegration",
  // the restaurant's orders, menu and stock: a store works with its own Store* sales, products and stock
  "Orders",
  "Payment",
  "MenuItem",
  "MenuCategory",
  "Discount",
  "StockCategory",
  "StockItem",
  "WarehouseStock",
  "StockRequest",
  "StockPurchase",
  "StockWriteOff",
  // the restaurant's till; a store has its own StoreCash* permissions
  "CashRegister",
]);

/**
 * Whether a permission is offered for a company's mode: a restaurant never sees the Store* permissions, a store never sees the
 * restaurant-only ones. (Roles keep what they already have; this only decides what the screen lists.)
 */
export function isPermissionForMode(module: string, storeMode: boolean, name = ""): boolean {
  if (module.startsWith("Store")) return storeMode;
  if (storeMode && RESTAURANT_ONLY_MODULES.has(module)) return false;
  // The restaurant POS (tables, waiters, orders) has no place in a store, which sells through its own Store* permissions;
  // only the Z-report stays because the dashboard reports page uses it.
  if (storeMode && module === "Pos" && name !== "Pos.ZReport") return false;
  return true;
}

export function translateModuleLabel(module: string, storeMode = false): string {
  return (storeMode ? STORE_MODULE_LABELS[module] : undefined) ?? MODULE_LABELS[module] ?? module;
}
