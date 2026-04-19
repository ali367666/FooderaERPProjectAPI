export interface Department {
  id: string;
  name: string;
  manager: string;
  description: string;
  employeeCount: number;
  status: "active" | "inactive" | "pending";
}

export interface Position {
  id: string;
  title: string;
  department: string;
  level: "junior" | "mid" | "senior";
  status: "active" | "inactive" | "open";
}

export interface Employee {
  id: string;
  name: string;
  position: string;
  department: string;
  email: string;
  phone: string;
  status: "active" | "inactive" | "on_leave";
  avatar?: string;
}

export const departments: Department[] = [
  {
    id: "1",
    name: "Kitchen Operations",
    manager: "Marco Rossi",
    description: "Manages food preparation and kitchen staff",
    employeeCount: 24,
    status: "active",
  },
  {
    id: "2",
    name: "Front of House",
    manager: "Sarah Chen",
    description: "Customer service and dining area management",
    employeeCount: 18,
    status: "active",
  },
  {
    id: "3",
    name: "Supply Chain",
    manager: "James Wilson",
    description: "Procurement and inventory management",
    employeeCount: 8,
    status: "active",
  },
  {
    id: "4",
    name: "Finance & Admin",
    manager: "Angela Kumar",
    description: "Financial operations and administrative support",
    employeeCount: 6,
    status: "active",
  },
  {
    id: "5",
    name: "Quality Assurance",
    manager: "David Martinez",
    description: "Food quality and safety standards",
    employeeCount: 5,
    status: "active",
  },
];

export const positions: Position[] = [
  {
    id: "1",
    title: "Head Chef",
    department: "Kitchen Operations",
    level: "senior",
    status: "active",
  },
  {
    id: "2",
    title: "Sous Chef",
    department: "Kitchen Operations",
    level: "mid",
    status: "active",
  },
  {
    id: "3",
    title: "Line Cook",
    department: "Kitchen Operations",
    level: "junior",
    status: "active",
  },
  {
    id: "4",
    title: "Restaurant Manager",
    department: "Front of House",
    level: "senior",
    status: "active",
  },
  {
    id: "5",
    title: "Server",
    department: "Front of House",
    level: "junior",
    status: "active",
  },
  {
    id: "6",
    title: "Bartender",
    department: "Front of House",
    level: "mid",
    status: "active",
  },
  {
    id: "7",
    title: "Inventory Manager",
    department: "Supply Chain",
    level: "mid",
    status: "active",
  },
  {
    id: "8",
    title: "Accountant",
    department: "Finance & Admin",
    level: "mid",
    status: "active",
  },
  {
    id: "9",
    title: "Quality Inspector",
    department: "Quality Assurance",
    level: "mid",
    status: "open",
  },
];

export const employees: Employee[] = [
  {
    id: "1",
    name: "Marco Rossi",
    position: "Head Chef",
    department: "Kitchen Operations",
    email: "marco@foodera.com",
    phone: "+1 (555) 123-4567",
    status: "active",
  },
  {
    id: "2",
    name: "Sofia Garcia",
    position: "Sous Chef",
    department: "Kitchen Operations",
    email: "sofia@foodera.com",
    phone: "+1 (555) 234-5678",
    status: "active",
  },
  {
    id: "3",
    name: "James Wilson",
    position: "Line Cook",
    department: "Kitchen Operations",
    email: "james@foodera.com",
    phone: "+1 (555) 345-6789",
    status: "active",
  },
  {
    id: "4",
    name: "Sarah Chen",
    position: "Restaurant Manager",
    department: "Front of House",
    email: "sarah@foodera.com",
    phone: "+1 (555) 456-7890",
    status: "active",
  },
  {
    id: "5",
    name: "Michael Brown",
    position: "Server",
    department: "Front of House",
    email: "michael@foodera.com",
    phone: "+1 (555) 567-8901",
    status: "active",
  },
  {
    id: "6",
    name: "Emily Rodriguez",
    position: "Bartender",
    department: "Front of House",
    email: "emily@foodera.com",
    phone: "+1 (555) 678-9012",
    status: "active",
  },
  {
    id: "7",
    name: "James Wilson",
    position: "Inventory Manager",
    department: "Supply Chain",
    email: "james.w@foodera.com",
    phone: "+1 (555) 789-0123",
    status: "active",
  },
  {
    id: "8",
    name: "Angela Kumar",
    position: "Accountant",
    department: "Finance & Admin",
    email: "angela@foodera.com",
    phone: "+1 (555) 890-1234",
    status: "active",
  },
  {
    id: "9",
    name: "David Martinez",
    position: "Quality Inspector",
    department: "Quality Assurance",
    email: "david@foodera.com",
    phone: "+1 (555) 901-2345",
    status: "active",
  },
  {
    id: "10",
    name: "Lisa Thompson",
    position: "Server",
    department: "Front of House",
    email: "lisa@foodera.com",
    phone: "+1 (555) 012-3456",
    status: "on_leave",
  },
];

export interface PendingAction {
  id: string;
  title: string;
  type: "approval" | "request" | "review" | "urgent";
  description: string;
  dueDate: Date;
  assignee: string;
}

export interface RecentActivity {
  id: string;
  action: string;
  description: string;
  user: string;
  timestamp: Date;
  type: "add" | "update" | "delete" | "status_change";
}

export interface Notification {
  id: string;
  title: string;
  message: string;
  type: "info" | "success" | "warning" | "error";
  timestamp: Date;
  read: boolean;
}

export const pendingActions: PendingAction[] = [
  {
    id: "1",
    title: "Approve Leave Request",
    type: "approval",
    description: "Lisa Thompson - 5 days vacation",
    dueDate: new Date(Date.now() + 2 * 24 * 60 * 60 * 1000),
    assignee: "Sarah Chen",
  },
  {
    id: "2",
    title: "Review Salary Adjustment",
    type: "review",
    description: "Marco Rossi - Performance bonus",
    dueDate: new Date(Date.now() + 1 * 24 * 60 * 60 * 1000),
    assignee: "Angela Kumar",
  },
  {
    id: "3",
    title: "Urgent: Equipment Maintenance",
    type: "urgent",
    description: "Kitchen equipment needs inspection",
    dueDate: new Date(),
    assignee: "Marco Rossi",
  },
  {
    id: "4",
    title: "Complete Onboarding",
    type: "request",
    description: "New hire training documents",
    dueDate: new Date(Date.now() + 3 * 24 * 60 * 60 * 1000),
    assignee: "HR Team",
  },
];

export const recentActivities: RecentActivity[] = [
  {
    id: "1",
    action: "Employee Added",
    description: "New Line Cook hired in Kitchen Operations",
    user: "Angela Kumar",
    timestamp: new Date(Date.now() - 2 * 60 * 60 * 1000),
    type: "add",
  },
  {
    id: "2",
    action: "Position Opened",
    description: "Quality Inspector position marked as open",
    user: "Sarah Chen",
    timestamp: new Date(Date.now() - 4 * 60 * 60 * 1000),
    type: "update",
  },
  {
    id: "3",
    action: "Status Changed",
    description: "Lisa Thompson marked as on leave",
    user: "Sarah Chen",
    timestamp: new Date(Date.now() - 6 * 60 * 60 * 1000),
    type: "status_change",
  },
  {
    id: "4",
    action: "Department Updated",
    description: "Kitchen Operations department info updated",
    user: "Marco Rossi",
    timestamp: new Date(Date.now() - 8 * 60 * 60 * 1000),
    type: "update",
  },
  {
    id: "5",
    action: "Approval Granted",
    description: "Overtime request approved for Sofia Garcia",
    user: "Marco Rossi",
    timestamp: new Date(Date.now() - 12 * 60 * 60 * 1000),
    type: "update",
  },
];

export const notifications: Notification[] = [
  {
    id: "1",
    title: "Action Required",
    message: "You have a pending leave approval request",
    type: "warning",
    timestamp: new Date(Date.now() - 30 * 60 * 1000),
    read: false,
  },
  {
    id: "2",
    title: "System Update",
    message: "ERP system maintenance scheduled for tonight",
    type: "info",
    timestamp: new Date(Date.now() - 2 * 60 * 60 * 1000),
    read: false,
  },
  {
    id: "3",
    title: "Employee Milestone",
    message: "Marco Rossi completed 5 years with Foodera",
    type: "success",
    timestamp: new Date(Date.now() - 24 * 60 * 60 * 1000),
    read: true,
  },
  {
    id: "4",
    title: "Inventory Alert",
    message: "Low stock alert for premium ingredients",
    type: "warning",
    timestamp: new Date(Date.now() - 48 * 60 * 60 * 1000),
    read: true,
  },
];

// ERP Inventory Management
export interface InventoryItem {
  id: string;
  name: string;
  sku: string;
  category: string;
  currentStock: number;
  minimumStock: number;
  unit: string;
  status: "in_stock" | "low_stock" | "critical" | "out_of_stock";
}

export interface PendingApproval {
  id: string;
  type: "stock_request" | "warehouse_transfer" | "order";
  title: string;
  createdBy: string;
  status: "pending" | "approved" | "rejected";
  createdAt: Date;
  items?: string;
  quantity?: number;
}

export interface SystemActivity {
  id: string;
  type: "stock_movement" | "transfer" | "order_update" | "approval";
  description: string;
  details: string;
  user: string;
  timestamp: Date;
  status: "completed" | "in_progress" | "pending";
}

export interface InventoryAlert {
  id: string;
  itemName: string;
  sku: string;
  currentStock: number;
  minimumStock: number;
  alertType: "low_stock" | "critical";
  unit: string;
}

export const inventoryItems: InventoryItem[] = [
  {
    id: "1",
    name: "Premium Olive Oil",
    sku: "OIL-001",
    category: "Oils & Condiments",
    currentStock: 45,
    minimumStock: 50,
    unit: "L",
    status: "low_stock",
  },
  {
    id: "2",
    name: "Fresh Basil",
    sku: "HRB-002",
    category: "Herbs & Spices",
    currentStock: 12,
    minimumStock: 30,
    unit: "kg",
    status: "critical",
  },
  {
    id: "3",
    name: "San Marzano Tomatoes",
    sku: "TOM-003",
    category: "Canned Goods",
    currentStock: 120,
    minimumStock: 80,
    unit: "cans",
    status: "in_stock",
  },
  {
    id: "4",
    name: "Fresh Mozzarella",
    sku: "CHE-004",
    category: "Dairy",
    currentStock: 8,
    minimumStock: 20,
    unit: "kg",
    status: "critical",
  },
  {
    id: "5",
    name: "Aged Balsamic Vinegar",
    sku: "VIN-005",
    category: "Oils & Condiments",
    currentStock: 30,
    minimumStock: 25,
    unit: "L",
    status: "in_stock",
  },
  {
    id: "6",
    name: "Sea Salt",
    sku: "SAL-006",
    category: "Spices",
    currentStock: 5,
    minimumStock: 15,
    unit: "kg",
    status: "critical",
  },
];

export const pendingApprovals: PendingApproval[] = [
  {
    id: "1",
    type: "stock_request",
    title: "Stock Request - Fresh Herbs",
    createdBy: "Marco Rossi",
    status: "pending",
    createdAt: new Date(Date.now() - 4 * 60 * 60 * 1000),
    items: "Basil, Parsley, Oregano",
    quantity: 50,
  },
  {
    id: "2",
    type: "warehouse_transfer",
    title: "Transfer from Warehouse A to B",
    createdBy: "James Wilson",
    status: "pending",
    createdAt: new Date(Date.now() - 2 * 60 * 60 * 1000),
    items: "Canned Tomatoes, Olive Oil",
    quantity: 200,
  },
  {
    id: "3",
    type: "order",
    title: "Purchase Order - Premium Ingredients",
    createdBy: "Angela Kumar",
    status: "pending",
    createdAt: new Date(Date.now() - 1 * 60 * 60 * 1000),
    items: "Mozzarella, Balsamic Vinegar",
    quantity: 100,
  },
];

export const systemActivities: SystemActivity[] = [
  {
    id: "1",
    type: "stock_movement",
    description: "Stock received from supplier",
    details: "100 units of Fresh Basil received",
    user: "James Wilson",
    timestamp: new Date(Date.now() - 30 * 60 * 1000),
    status: "completed",
  },
  {
    id: "2",
    type: "transfer",
    description: "Warehouse transfer completed",
    details: "Moved 50 units of Olive Oil from A to B",
    user: "Angela Kumar",
    timestamp: new Date(Date.now() - 2 * 60 * 60 * 1000),
    status: "completed",
  },
  {
    id: "3",
    type: "order_update",
    description: "Order status updated",
    details: "Order #2024-001 marked as shipped",
    user: "Sarah Chen",
    timestamp: new Date(Date.now() - 4 * 60 * 60 * 1000),
    status: "completed",
  },
  {
    id: "4",
    type: "approval",
    description: "Stock request approved",
    details: "Fresh herbs stock request approved by Marco Rossi",
    user: "Marco Rossi",
    timestamp: new Date(Date.now() - 6 * 60 * 60 * 1000),
    status: "completed",
  },
  {
    id: "5",
    type: "stock_movement",
    description: "Stock adjustment",
    details: "Inventory count adjustment - Mozzarella",
    user: "David Martinez",
    timestamp: new Date(Date.now() - 8 * 60 * 60 * 1000),
    status: "completed",
  },
];

export const inventoryAlerts: InventoryAlert[] = [
  {
    id: "1",
    itemName: "Fresh Basil",
    sku: "HRB-002",
    currentStock: 12,
    minimumStock: 30,
    alertType: "critical",
    unit: "kg",
  },
  {
    id: "2",
    itemName: "Fresh Mozzarella",
    sku: "CHE-004",
    currentStock: 8,
    minimumStock: 20,
    alertType: "critical",
    unit: "kg",
  },
  {
    id: "3",
    itemName: "Sea Salt",
    sku: "SAL-006",
    currentStock: 5,
    minimumStock: 15,
    alertType: "critical",
    unit: "kg",
  },
  {
    id: "4",
    itemName: "Premium Olive Oil",
    sku: "OIL-001",
    currentStock: 45,
    minimumStock: 50,
    alertType: "low_stock",
    unit: "L",
  },
];

// Stock Requests
export interface StockRequestItem {
  id: string;
  stockItemId: string;
  stockItemName: string;
  quantity: number;
  unit: string;
  note?: string;
}

export interface StockRequest {
  id: string;
  requestNumber: string;
  from: string;
  requestedBy: string;
  date: Date;
  status: "draft" | "submitted" | "approved" | "rejected" | "cancelled";
  items: StockRequestItem[];
}

export const stockRequests: StockRequest[] = [
  {
    id: "sr1",
    requestNumber: "SR-2024-001",
    from: "Kitchen Operations",
    requestedBy: "Marco Rossi",
    date: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000),
    status: "approved",
    items: [
      {
        id: "item1",
        stockItemId: "2",
        stockItemName: "Fresh Basil",
        quantity: 50,
        unit: "kg",
        note: "For daily menu prep",
      },
      {
        id: "item2",
        stockItemId: "3",
        stockItemName: "San Marzano Tomatoes",
        quantity: 100,
        unit: "cans",
        note: "Emergency stock",
      },
    ],
  },
];

// Restaurants
export interface Restaurant {
  id: string;
  name: string;
  location: string;
  contact: string;
  status: "active" | "inactive" | "maintenance";
  tables: number;
}

export const restaurants: Restaurant[] = [
  {
    id: "r1",
    name: "Main Dining",
    location: "Ground Floor, Central Building",
    contact: "+1 (555) 123-4567",
    status: "active",
    tables: 24,
  },
  {
    id: "r2",
    name: "Patio",
    location: "Outdoor Area, West Wing",
    contact: "+1 (555) 234-5678",
    status: "active",
    tables: 12,
  },
  {
    id: "r3",
    name: "Bar Counter",
    location: "Ground Floor, Central Bar",
    contact: "+1 (555) 345-6789",
    status: "active",
    tables: 8,
  },
  {
    id: "r4",
    name: "Private Dining",
    location: "First Floor, East Wing",
    contact: "+1 (555) 456-7890",
    status: "maintenance",
    tables: 6,
  },
];

// Restaurant Tables
export interface RestaurantTable {
  id: string;
  tableNumber: string;
  restaurantId: string;
  restaurantName: string;
  capacity: number;
  status: "available" | "occupied" | "reserved" | "maintenance";
}

export const restaurantTables: RestaurantTable[] = [
  {
    id: "t1",
    tableNumber: "1",
    restaurantId: "r1",
    restaurantName: "Main Dining",
    capacity: 4,
    status: "available",
  },
  {
    id: "t2",
    tableNumber: "2",
    restaurantId: "r1",
    restaurantName: "Main Dining",
    capacity: 4,
    status: "occupied",
  },
  {
    id: "t3",
    tableNumber: "3",
    restaurantId: "r1",
    restaurantName: "Main Dining",
    capacity: 6,
    status: "reserved",
  },
  {
    id: "t4",
    tableNumber: "4",
    restaurantId: "r1",
    restaurantName: "Main Dining",
    capacity: 4,
    status: "available",
  },
  {
    id: "t5",
    tableNumber: "5",
    restaurantId: "r1",
    restaurantName: "Main Dining",
    capacity: 8,
    status: "occupied",
  },
  {
    id: "t6",
    tableNumber: "1",
    restaurantId: "r2",
    restaurantName: "Patio",
    capacity: 2,
    status: "available",
  },
  {
    id: "t7",
    tableNumber: "2",
    restaurantId: "r2",
    restaurantName: "Patio",
    capacity: 4,
    status: "occupied",
  },
  {
    id: "t8",
    tableNumber: "3",
    restaurantId: "r2",
    restaurantName: "Patio",
    capacity: 6,
    status: "available",
  },
  {
    id: "t9",
    tableNumber: "1",
    restaurantId: "r3",
    restaurantName: "Bar Counter",
    capacity: 1,
    status: "occupied",
  },
  {
    id: "t10",
    tableNumber: "2",
    restaurantId: "r3",
    restaurantName: "Bar Counter",
    capacity: 2,
    status: "available",
  },
];

// Menu Categories
export interface MenuCategory {
  id: string;
  name: string;
  description: string;
  status: "active" | "inactive";
}

export const menuCategories: MenuCategory[] = [
  {
    id: "mc1",
    name: "Pizza",
    description: "Traditional Italian pizzas with fresh ingredients",
    status: "active",
  },
  {
    id: "mc2",
    name: "Pasta",
    description: "Handmade pasta dishes with authentic sauces",
    status: "active",
  },
  {
    id: "mc3",
    name: "Main Course",
    description: "Premium main courses and specialty dishes",
    status: "active",
  },
  {
    id: "mc4",
    name: "Seafood",
    description: "Fresh seafood prepared daily",
    status: "active",
  },
  {
    id: "mc5",
    name: "Salad",
    description: "Fresh salads with seasonal ingredients",
    status: "active",
  },
  {
    id: "mc6",
    name: "Appetizer",
    description: "Small plates and starters",
    status: "active",
  },
  {
    id: "mc7",
    name: "Dessert",
    description: "Sweet treats and classic desserts",
    status: "active",
  },
  {
    id: "mc8",
    name: "Beverage",
    description: "Coffee, tea, and other drinks",
    status: "inactive",
  },
];

// Warehouse Transfers
export interface WarehouseTransferItem {
  id: string;
  stockItemId: string;
  stockItemName: string;
  quantity: number;
  unit: string;
  note?: string;
}

export interface WarehouseTransfer {
  id: string;
  transferNumber: string;
  fromWarehouse: string;
  toWarehouse: string;
  date: Date;
  status:
    | "draft"
    | "submitted"
    | "approved"
    | "rejected"
    | "dispatched"
    | "received"
    | "cancelled";
  items: WarehouseTransferItem[];
}

export const warehouseTransfers: WarehouseTransfer[] = [
  {
    id: "wt1",
    transferNumber: "WT-2024-001",
    fromWarehouse: "Warehouse A",
    toWarehouse: "Warehouse B",
    date: new Date(Date.now() - 3 * 24 * 60 * 60 * 1000),
    status: "received",
    items: [
      {
        id: "wtitem1",
        stockItemId: "3",
        stockItemName: "San Marzano Tomatoes",
        quantity: 200,
        unit: "cans",
        note: "Regular stock replenishment",
      },
      {
        id: "wtitem2",
        stockItemId: "1",
        stockItemName: "Premium Olive Oil",
        quantity: 50,
        unit: "L",
      },
    ],
  },
  {
    id: "wt2",
    transferNumber: "WT-2024-002",
    fromWarehouse: "Warehouse B",
    toWarehouse: "Warehouse A",
    date: new Date(Date.now() - 1 * 24 * 60 * 60 * 1000),
    status: "dispatched",
    items: [
      {
        id: "wtitem3",
        stockItemId: "5",
        stockItemName: "Aged Balsamic Vinegar",
        quantity: 30,
        unit: "L",
        note: "For premium products",
      },
    ],
  },
  {
    id: "wt3",
    transferNumber: "WT-2024-003",
    fromWarehouse: "Warehouse A",
    toWarehouse: "Warehouse C",
    date: new Date(),
    status: "approved",
    items: [
      {
        id: "wtitem4",
        stockItemId: "2",
        stockItemName: "Fresh Basil",
        quantity: 60,
        unit: "kg",
      },
    ],
  },
  {
    id: "wt4",
    transferNumber: "WT-2024-004",
    fromWarehouse: "Warehouse C",
    toWarehouse: "Warehouse B",
    date: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000),
    status: "submitted",
    items: [
      {
        id: "wtitem5",
        stockItemId: "6",
        stockItemName: "Sea Salt",
        quantity: 50,
        unit: "kg",
      },
      {
        id: "wtitem6",
        stockItemId: "4",
        stockItemName: "Fresh Mozzarella",
        quantity: 15,
        unit: "kg",
      },
    ],
  },
];

// Menu Items
export interface MenuItem {
  id: string;
  name: string;
  category: string;
  price: number;
  description?: string;
}

export const menuItems: MenuItem[] = [
  {
    id: "m1",
    name: "Margherita Pizza",
    category: "Pizza",
    price: 14.99,
    description: "Classic tomato, mozzarella, basil",
  },
  {
    id: "m2",
    name: "Spaghetti Carbonara",
    category: "Pasta",
    price: 12.99,
    description: "Pasta with eggs, cheese, and pancetta",
  },
  {
    id: "m3",
    name: "Risotto ai Funghi",
    category: "Main Course",
    price: 16.99,
    description: "Creamy mushroom risotto",
  },
  {
    id: "m4",
    name: "Grilled Salmon",
    category: "Seafood",
    price: 22.99,
    description: "Fresh salmon with lemon butter sauce",
  },
  {
    id: "m5",
    name: "Tiramisu",
    category: "Dessert",
    price: 8.99,
    description: "Classic Italian dessert",
  },
  {
    id: "m6",
    name: "Caesar Salad",
    category: "Salad",
    price: 9.99,
    description: "Romaine lettuce with caesar dressing",
  },
  {
    id: "m7",
    name: "Bruschetta",
    category: "Appetizer",
    price: 6.99,
    description: "Toasted bread with tomato and basil",
  },
  {
    id: "m8",
    name: "Espresso",
    category: "Beverage",
    price: 3.50,
    description: "Strong Italian coffee",
  },
];

// Order Items
export interface OrderLine {
  id: string;
  menuItemId: string;
  menuItemName: string;
  quantity: number;
  price: number;
  note?: string;
  total: number;
}

// Orders
export interface Order {
  id: string;
  orderNumber: string;
  restaurant: string;
  tableNumber: string;
  totalAmount: number;
  status: "draft" | "submitted" | "in_progress" | "completed" | "cancelled";
  date: Date;
  lines: OrderLine[];
}

export const orders: Order[] = [
  {
    id: "o1",
    orderNumber: "ORD-2024-001",
    restaurant: "Main Dining",
    tableNumber: "5",
    totalAmount: 45.97,
    status: "completed",
    date: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000),
    lines: [
      {
        id: "ol1",
        menuItemId: "m1",
        menuItemName: "Margherita Pizza",
        quantity: 2,
        price: 14.99,
        total: 29.98,
      },
      {
        id: "ol2",
        menuItemId: "m5",
        menuItemName: "Tiramisu",
        quantity: 1,
        price: 8.99,
        total: 8.99,
      },
    ],
  },
  {
    id: "o2",
    orderNumber: "ORD-2024-002",
    restaurant: "Main Dining",
    tableNumber: "12",
    totalAmount: 64.96,
    status: "in_progress",
    date: new Date(Date.now() - 1 * 24 * 60 * 60 * 1000),
    lines: [
      {
        id: "ol3",
        menuItemId: "m3",
        menuItemName: "Risotto ai Funghi",
        quantity: 1,
        price: 16.99,
        total: 16.99,
      },
      {
        id: "ol4",
        menuItemId: "m4",
        menuItemName: "Grilled Salmon",
        quantity: 2,
        price: 22.99,
        note: "No lemon for one",
        total: 45.98,
      },
    ],
  },
  {
    id: "o3",
    orderNumber: "ORD-2024-003",
    restaurant: "Patio",
    tableNumber: "8",
    totalAmount: 37.96,
    status: "submitted",
    date: new Date(),
    lines: [
      {
        id: "ol5",
        menuItemId: "m2",
        menuItemName: "Spaghetti Carbonara",
        quantity: 2,
        price: 12.99,
        total: 25.98,
      },
      {
        id: "ol6",
        menuItemId: "m6",
        menuItemName: "Caesar Salad",
        quantity: 1,
        price: 9.99,
        total: 9.99,
      },
    ],
  },
  {
    id: "o4",
    orderNumber: "ORD-2024-004",
    restaurant: "Main Dining",
    tableNumber: "15",
    totalAmount: 0,
    status: "draft",
    date: new Date(),
    lines: [],
  },
  {
    id: "o5",
    orderNumber: "ORD-2024-005",
    restaurant: "Bar Counter",
    tableNumber: "1",
    totalAmount: 32.46,
    status: "cancelled",
    date: new Date(Date.now() - 3 * 24 * 60 * 60 * 1000),
    lines: [
      {
        id: "ol7",
        menuItemId: "m7",
        menuItemName: "Bruschetta",
        quantity: 3,
        price: 6.99,
        total: 20.97,
      },
      {
        id: "ol8",
        menuItemId: "m8",
        menuItemName: "Espresso",
        quantity: 2,
        price: 3.50,
        total: 7.00,
      },
    ],
  },
];

// Audit Logs
export interface AuditLog {
  id: string;
  user: string;
  action: string;
  module: string;
  date: Date;
  details: string;
}

export const auditLogs: AuditLog[] = [
  {
    id: "al1",
    user: "Marco Rossi",
    action: "Created",
    module: "Stock Request",
    date: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000),
    details: "Created new stock request SR-2024-001 for Fresh Basil and Parsley",
  },
  {
    id: "al2",
    user: "Angela Kumar",
    action: "Approved",
    module: "Stock Request",
    date: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000),
    details: "Approved stock request with 50kg fresh herbs",
  },
  {
    id: "al3",
    user: "James Wilson",
    action: "Updated",
    module: "Warehouse Transfer",
    date: new Date(Date.now() - 3 * 24 * 60 * 60 * 1000),
    details: "Updated warehouse transfer WT-2024-001 status to received",
  },
  {
    id: "al4",
    user: "Sarah Chen",
    action: "Created",
    module: "Order",
    date: new Date(),
    details: "Created new order ORD-2024-003 for Patio table 8, 2 Carbonara + 1 Salad",
  },
  {
    id: "al5",
    user: "David Martinez",
    action: "Updated",
    module: "Menu Item",
    date: new Date(Date.now() - 1 * 60 * 60 * 1000),
    details: "Updated Margherita Pizza price from $14.99 to $15.99",
  },
  {
    id: "al6",
    user: "Sofia Garcia",
    action: "Created",
    module: "Restaurant Table",
    date: new Date(Date.now() - 4 * 60 * 60 * 1000),
    details: "Created new restaurant table for Main Dining area, capacity 4",
  },
  {
    id: "al7",
    user: "Angela Kumar",
    action: "Rejected",
    module: "Stock Request",
    date: new Date(Date.now() - 3 * 24 * 60 * 60 * 1000),
    details: "Rejected mozzarella request SR-2024-004 - insufficient budget",
  },
  {
    id: "al8",
    user: "Marco Rossi",
    action: "Updated",
    module: "Restaurant",
    date: new Date(Date.now() - 5 * 24 * 60 * 60 * 1000),
    details: "Updated Main Dining restaurant contact information",
  },
  {
    id: "al9",
    user: "James Wilson",
    action: "Created",
    module: "Warehouse Transfer",
    date: new Date(Date.now() - 1 * 24 * 60 * 60 * 1000),
    details: "Created warehouse transfer WT-2024-002 for Balsamic Vinegar",
  },
  {
    id: "al10",
    user: "Sarah Chen",
    action: "Updated",
    module: "Order",
    date: new Date(Date.now() - 1 * 24 * 60 * 60 * 1000),
    details: "Updated order ORD-2024-002 status from submitted to in_progress",
  },
];
