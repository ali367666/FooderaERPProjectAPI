import type { OrderWorkflowStatus } from "@/lib/services/order-service";

export function isActiveOrderStatus(status: OrderWorkflowStatus): boolean {
  return status !== "paid" && status !== "cancelled";
}

export function orderStatusLabel(status: OrderWorkflowStatus): string {
  switch (status) {
    case "open":
      return "Açıq";
    case "in_preparation":
      return "Hazırlanır";
    case "ready":
      return "Hazır";
    case "served":
      return "Verildi";
    default:
      return status;
  }
}

/** Fill colour of a busy tile — the same as an occupied table on the tables screen. */
export function orderStatusBg(status: OrderWorkflowStatus): string {
  switch (status) {
    case "in_preparation":
      return "bg-[#fb923c]";
    case "ready":
      return "bg-[#34d399]";
    case "served":
      return "bg-[#f472b6]";
    default:
      return "bg-[#6366f1]";
  }
}

export function formatElapsed(openedAt: string, now: number): string {
  const opened = new Date(openedAt).getTime();
  if (!Number.isFinite(opened)) return "";
  const minutes = Math.max(0, Math.floor((now - opened) / 60000));
  if (minutes < 60) return `${minutes} dəq`;
  const hours = Math.floor(minutes / 60);
  const remMinutes = minutes % 60;
  return `${hours} saat ${remMinutes} dəq`;
}
