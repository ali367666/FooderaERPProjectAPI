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

  "Restaurant.View": "Restorana bax",
  "Restaurant.Create": "Restoran yarat",
  "Restaurant.Update": "Restoranı redaktə et",
  "Restaurant.Delete": "Restoranı sil",

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

  "BscInvoice.View": "BSC hesab-fakturalarına bax",
  "BscInvoice.Sync": "BSC hesab-fakturalarını sinxronlaşdır",

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
  "Pos.ChangePrice": "Satış qiymətini dəyiş",
  "Pos.ZReport": "Z-hesabatı",
  "Pos.PrintReceipt": "Qəbz çap et",
  "Pos.OverridePrice": "Qalıcı qiymətə müdaxilə et",
  "Pos.ChangeDepartment": "Şöbəni dəyiş",
  "Pos.RedirectUser": "Ofisiantı dəyiş / başqasının masasına bax",
  "Pos.EditProductInSale": "Satışda məhsulu redaktə et",
  "Pos.DeleteProductInSale": "Satışda məhsulu sil",
  "Pos.TableServiceCharge": "Masa üzrə servis haqqı tətbiq et",
  "Pos.DeleteReceipt": "Köhnə qəbzi sil",
  "Pos.WarehouseAmountChange": "Anbar məbləğini dəyiş",
  "Pos.PrintOldReceipt": "Köhnə qəbzi çap et",

  "RestaurantSection.View": "Bölmələrə bax",
  "RestaurantSection.Create": "Bölmə yarat",
  "RestaurantSection.Update": "Bölməni redaktə et",
  "RestaurantSection.Delete": "Bölməni sil",

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
};

const MODULE_LABELS: Record<string, string> = {
  Company: "Şirkət",
  CompanySettings: "Tənzimləmələr",
  User: "İstifadəçilər",
  Restaurant: "Restoran",
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
  BscInvoice: "BSC hesab-fakturaları",
  MenuItem: "Menyu məhsulları",
  MenuCategory: "Menyu kateqoriyaları",
  Kitchen: "Mətbəx",
  AuditLog: "Audit qeydləri",
  StockPurchase: "Alışlar",
  Reservation: "Rezervasiyalar",
  Discount: "Endirimlər",
  Pos: "POS",
  RestaurantSection: "Restoran bölmələri",
  Printer: "Printerlər",
  Counterparty: "Kontragentlər",
  FiscalDevice: "Fiskal kassalar",
  ScaleDevice: "Tərəzilər",
};

export function translatePermissionLabel(name: string, fallback: string): string {
  return PERMISSION_LABELS[name] ?? fallback;
}

export function translateModuleLabel(module: string): string {
  return MODULE_LABELS[module] ?? module;
}
